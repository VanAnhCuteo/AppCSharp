using System.Net.Http;
using System.Text.Json;

namespace FoodMapApp;

public partial class MainPage : ContentPage
{
    private bool _isMapLoaded = false;
    private string _foodsJson = null;

    public MainPage()
    {
        InitializeComponent();

        mapView.Navigated += (s, e) =>
        {
            _isMapLoaded = true;
            if (_foodsJson != null)
                mapView.EvaluateJavaScriptAsync($"loadFoods({_foodsJson});");
        };

        mapView.Source = "map.html";

        LoadFoods();
    }

    async void LoadFoods()
    {
        HttpClient client = new HttpClient();

        try
        {
            _foodsJson = await client.GetStringAsync("http://10.0.2.2:5000/api/food");

            if (_isMapLoaded)
                await mapView.EvaluateJavaScriptAsync($"loadFoods({_foodsJson});");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}