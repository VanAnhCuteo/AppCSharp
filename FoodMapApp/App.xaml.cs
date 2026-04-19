using FoodMapApp.Services;

namespace FoodMapApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(MainPage ?? new AppShell());
        }

        public static bool PendingGuestLogin { get; set; } = false;

        protected override async void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);
            System.Diagnostics.Debug.WriteLine($"App Link Received: {uri}");

            var authService = new FoodMapApp.Services.AuthService();

            // 1. Handle POI Deep Link (foodmap://poi/{id})
            if (uri.Scheme == "foodmap" && uri.Host == "poi")
            {
                var idStr = uri.Segments.LastOrDefault()?.Trim('/');
                if (int.TryParse(idStr, out int id))
                {
                    FoodMapApp.MainPage.PendingOpenFoodId = id;

                    // Chỉ login guest nếu chưa đăng nhập
                    if (!authService.IsLoggedIn)
                    {
                        authService.LoginAsGuest(new Random().Next(100000, 999999));
                    }

                    _ = NavigateDirectlyAsync("//MainTabs/MainPage");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid POI ID: {idStr}");
                    _ = NavigateDirectlyAsync("//MainTabs/MainPage");
                }
            }
            // 2. Handle Audio QR/Deep Link (foodmap://audio/{id})
            else if (uri.Scheme == "foodmap" && uri.Host == "audio")
            {
                var idStr = uri.Segments.LastOrDefault()?.Trim('/');
                if (int.TryParse(idStr, out int id))
                {
                    if (!authService.IsLoggedIn)
                    {
                        authService.LoginAsGuest(new Random().Next(100000, 999999));
                    }
                    _ = NavigateDirectlyAsync($"QRViewerPage?id={id}&auto=true");
                }
            }
            // 3. Handle Guest Login Deep Link (foodmap://guest)
            else if (uri.Scheme == "foodmap" && uri.Host == "guest")
            {
                if (!authService.IsLoggedIn)
                {
                    authService.LoginAsGuest(new Random().Next(100000, 999999));
                }

                _ = NavigateDirectlyAsync("//MainTabs/HomePage");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Unhandled deep link: {uri}");
            }
        }

        private async Task NavigateDirectlyAsync(string route)
        {
            // Chờ Shell sẵn sàng (đặc biệt khi Cold Start)
            for (int i = 0; i < 40; i++)
            {
                if (Shell.Current != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        try
                        {
                            await Shell.Current.GoToAsync(route);

                            // Nếu là trang Map, mở detail nếu có pending
                            if (route.Contains("MainPage") && FoodMapApp.MainPage.Instance != null)
                            {
                                await FoodMapApp.MainPage.Instance.TryOpenPendingDetail();
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Navigation Error: {ex.Message}");
                        }
                    });
                    return;
                }
                await Task.Delay(200);
            }

            System.Diagnostics.Debug.WriteLine("Shell not available after timeout.");
        }
    }
}