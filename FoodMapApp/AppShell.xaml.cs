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
    }
}
