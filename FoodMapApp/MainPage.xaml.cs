namespace FoodMapApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        mapView.Source = "map.html";
    }
}