namespace FoodMapApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }

        public static bool PendingGuestLogin { get; set; } = false;

        protected override async void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);
            System.Diagnostics.Debug.WriteLine($"App Link Received: {uri}");

            var authService = new FoodMapApp.Services.AuthService();
            bool notLoggedIn = !authService.IsLoggedIn;

            // 1. Handle POI Deep Link (foodmap://poi/{id})
            if (uri.Scheme == "foodmap" && uri.Host == "poi")
            {
                var idStr = uri.Segments.LastOrDefault();
                if (int.TryParse(idStr, out int id))
                {
                    FoodMapApp.MainPage.PendingOpenFoodId = id;
                    
                    if (notLoggedIn)
                    {
                        PendingGuestLogin = true;
                    }
                    else
                    {
                        if (Shell.Current != null) await Shell.Current.GoToAsync("//MainPage");
                        if (FoodMapApp.MainPage.Instance != null) _ = FoodMapApp.MainPage.Instance.TryOpenPendingDetail();
                    }
                }
            }
            // 2. Handle Guest Login Deep Link (foodmap://guest or https://foodmap.app/guest)
            else if (uri.OriginalString.ToLower().Contains("guest"))
            {
                int guestId = new Random().Next(100000, 999999);
                authService.LoginAsGuest(guestId);
                if (Shell.Current != null) await Shell.Current.GoToAsync("//MainTabs");
                return;
            }


        }
    }
}
