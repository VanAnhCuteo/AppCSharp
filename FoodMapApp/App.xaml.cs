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
            return base.CreateWindow(activationState);
        }

        protected override void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Đợi một chút để Shell ổn định khi khởi động nguội (Cold Start)
                await Task.Delay(500);

                // Expecting: foodmap://poi/{id} or foodmap://guest
                if (uri.Scheme == "foodmap")
                {
                    var authService = new FoodMapApp.Services.AuthService();
                    
                    if (uri.Host == "poi")
                    {
                        var idStr = uri.Segments.LastOrDefault();
                        if (int.TryParse(idStr, out int id))
                        {
                            // Tự động đăng nhập khách nếu chưa có tài khoản
                            if (!authService.IsLoggedIn)
                            {
                                int guestId = new Random().Next(100000, 999999);
                                authService.LoginAsGuest(guestId);
                            }

                            // Set static pending ID
                            FoodMapApp.MainPage.PendingOpenFoodId = id;

                            // Điều hướng đến Bản đồ
                            if (Shell.Current != null)
                            {
                                await Shell.Current.GoToAsync("//MainPage");
                            }

                            // Kích hoạt mở chi tiết quán
                            if (FoodMapApp.MainPage.Instance != null)
                            {
                                await FoodMapApp.MainPage.Instance.TryOpenPendingDetail();
                            }
                        }
                    }
                    else if (uri.Host == "guest")
                    {
                        if (!authService.IsLoggedIn)
                        {
                            int guestId = new Random().Next(100000, 999999);
                            authService.LoginAsGuest(guestId);
                        }

                        if (Shell.Current != null)
                        {
                            await Shell.Current.GoToAsync("//HomePage");
                        }
                    }
                }
            });
        }
    }
}
