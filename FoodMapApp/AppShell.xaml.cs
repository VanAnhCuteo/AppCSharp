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
            Routing.RegisterRoute("QrScannerPage", typeof(Views.QrScannerPage));
        }

        protected override void OnNavigating(ShellNavigatingEventArgs args)
        {
            base.OnNavigating(args);
            // Audio cleanup is handled by MainPage.OnDisappearing()
            // Do NOT call StopGlobalAudio here as it destroys sessions
            // that OnDisappearing is trying to preserve for tab resume.
        }
    }
}
