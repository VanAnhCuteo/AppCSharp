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
            var window = new Window(MainPage ?? new AppShell());

            // Process any pending deep link after window is created
            window.Resumed += (s, e) => ProcessPendingDeepLink();
            window.Created += (s, e) =>
            {
                // Delay slightly to let Shell initialize
                _ = Task.Delay(500).ContinueWith(_ =>
                    MainThread.BeginInvokeOnMainThread(() => ProcessPendingDeepLink()));
            };

            return window;
        }

        public static bool PendingGuestLogin { get; set; } = false;
        public static string? PendingDeepLinkUri { get; set; } = null;

        private void ProcessPendingDeepLink()
        {
            if (string.IsNullOrEmpty(PendingDeepLinkUri)) return;

            string uriStr = PendingDeepLinkUri;
            PendingDeepLinkUri = null; // Clear immediately to avoid re-processing

            try
            {
                var uri = new Uri(uriStr);
                System.Diagnostics.Debug.WriteLine($"[DeepLink] Processing pending URI: {uri}");
                OnAppLinkRequestReceived(uri);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeepLink] Error parsing URI: {ex.Message}");
            }
        }

        protected override async void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);
            System.Diagnostics.Debug.WriteLine($"App Link Received: {uri}");

            var authService = new FoodMapApp.Services.AuthService();
            string path = uri.PathAndQuery;
            string host = uri.Host.ToLower();
            string scheme = uri.Scheme.ToLower();

            // 1. Handle POI Deep Link (foodmap://poi/{id} hoặc https://foodmap.app/poi/{id})
            if ((scheme == "foodmap" && host == "poi") || (scheme == "https" && host == "foodmap.app" && path.Contains("/poi/")))
            {
                var idStr = uri.Segments.LastOrDefault()?.Trim('/');
                if (int.TryParse(idStr, out int id))
                {
                    FoodMapApp.MainPage.PendingOpenFoodId = id;

                    if (!authService.IsLoggedIn)
                    {
                        authService.LoginAsGuest(new Random().Next(100000, 999999));
                    }

                    _ = NavigateToMapAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid POI ID: {idStr}");
                    _ = NavigateToMapAsync();
                }
            }
            // 2. Handle Audio QR/Deep Link (foodmap://audio/{id} hoặc https://foodmap.app/audio/{id})
            else if ((scheme == "foodmap" && host == "audio") || (scheme == "https" && host == "foodmap.app" && path.Contains("/audio/")))
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
            // 3. Handle Guest Login Deep Link (foodmap://guest hoặc https://foodmap.app/guest)
            else if ((scheme == "foodmap" && host == "guest") || (scheme == "https" && host == "foodmap.app" && path.Contains("/guest")))
            {
                if (!authService.IsLoggedIn)
                {
                    authService.LoginAsGuest(new Random().Next(100000, 999999));
                }

                _ = NavigateDirectlyAsync("//MainTabs");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Unhandled deep link: {uri}");
            }
        }

        private async Task NavigateToMapAsync()
        {
            // Navigate to Map tab and open POI detail
            for (int i = 0; i < 40; i++)
            {
                if (Shell.Current != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        try
                        {
                            await Shell.Current.GoToAsync("//MainTabs/MainPage");

                            // Wait for MainPage instance to be ready
                            for (int j = 0; j < 20; j++)
                            {
                                if (FoodMapApp.MainPage.Instance != null)
                                {
                                    await FoodMapApp.MainPage.Instance.TryOpenPendingDetail();
                                    break;
                                }
                                await Task.Delay(200);
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
