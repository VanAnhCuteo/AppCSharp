namespace FoodMapApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }

        protected override void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);

            // Expecting: foodmap://poi/{id}
            if (uri.Scheme == "foodmap" && uri.Host == "poi")
            {
                var idStr = uri.Segments.LastOrDefault();
                if (int.TryParse(idStr, out int id))
                {
                    // Set static pending ID
                    FoodMapApp.MainPage.PendingOpenFoodId = id;

                    // Navigate to MainPage (Map) to show the detail
                    if (Shell.Current != null)
                    {
                        _ = Shell.Current.GoToAsync("//MainPage");
                    }

                    // If we're already on MainPage instance, trigger it immediately
                    if (FoodMapApp.MainPage.Instance != null)
                    {
                        _ = FoodMapApp.MainPage.Instance.TryOpenPendingDetail();
                    }
                }
            }
        }
    }
}
