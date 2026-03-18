using System.Net.Http;
using System.Text.Json;
using System.Web;

namespace FoodMapApp;

public partial class MainPage : ContentPage
{
    // Change this to your host machine's IP if using a physical device (e.g., 192.168.1.x)
    private const string BackendIp = "10.0.2.2";
    private const string BackendUrl = $"http://{BackendIp}:5000/api/food";

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

        mapView.Navigating += async (s, e) =>
        {
            if (e.Url.StartsWith("app-tts://speak?"))
            {
                e.Cancel = true;

                var uri = new Uri(e.Url);
                var query = HttpUtility.ParseQueryString(uri.Query);
                string text = query["text"] ?? "";
                string lang = query["lang"] ?? "vi-VN";

                if (!string.IsNullOrWhiteSpace(text))
                {
                    SpeechOptions options = new SpeechOptions();

                    try
                    {
                        var locales = await TextToSpeech.Default.GetLocalesAsync();
                        var locale = locales.FirstOrDefault(l => l.Language.Equals(lang, StringComparison.OrdinalIgnoreCase)) ??
                                     locales.FirstOrDefault(l => l.Language.StartsWith(lang.Split('-')[0], StringComparison.OrdinalIgnoreCase));

                        if (locale != null)
                        {
                            options.Locale = locale;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting locales: {ex.Message}");
                    }

                    await TextToSpeech.Default.SpeakAsync(text, options);
                }
            }
            else if (e.Url.StartsWith("app-request-reload://markers?"))
            {
                e.Cancel = true;
                var uri = new Uri(e.Url);
                var query = HttpUtility.ParseQueryString(uri.Query);
                string lang = query["lang"] ?? "vi";
                LoadFoods(lang);
            }
        };

        LoadFoods();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
    }

    async void LoadFoods(string lang = "vi")
    {
        HttpClient client = new HttpClient();

        try
        {
            _foodsJson = await client.GetStringAsync($"{BackendUrl}?lang={lang}");

            if (_isMapLoaded)
                await mapView.EvaluateJavaScriptAsync($"loadFoods({_foodsJson});");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void OnExitClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}