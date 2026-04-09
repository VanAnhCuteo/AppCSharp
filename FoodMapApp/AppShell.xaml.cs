namespace FoodMapApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("RegisterPage", typeof(Views.RegisterPage));
            Routing.RegisterRoute("ShopQRListPage", typeof(Views.ShopQRListPage));
            Routing.RegisterRoute("QRViewerPage", typeof(Views.QRViewerPage));
        }

        protected override void OnNavigating(ShellNavigatingEventArgs args)
        {
            base.OnNavigating(args);

            // Handle Map Page Audio cleanup
            if (args.Current?.Location.OriginalString.Contains("MainPage") == true)
            {
                MainPage.Instance?.StopGlobalAudio();
            }

            // Handle QR Page Audio - We'll let the QR page handle itself in OnDisappearing 
            // but we can ensure a clean break here if needed.
        }
    }
}
