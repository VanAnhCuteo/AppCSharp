using System.Net.Http;
using System.Text.Json;
using System.Web;

namespace FoodMapApp;

    public partial class MainPage : ContentPage
    {
        public static MainPage Instance { get; private set; }
        public static int? PendingOpenFoodId { get; set; } = null;
        public static int? PendingRouteFoodId { get; set; } = null;

        // Change this to your host machine's IP if using a physical device (e.g., 192.168.1.x)
        private static string BackendUrl => AppConfig.FoodApiUrl;

        private bool _isMapLoaded = false;
        private string _foodsJson = null;

        public MainPage()
        {
            InitializeComponent();
            Instance = this;

        mapView.Navigated += async (s, e) =>
        {
            _isMapLoaded = true;
            if (_foodsJson != null)
            {
                int userId = Preferences.Default.Get("user_id", 0);
                await mapView.EvaluateJavaScriptAsync($"loadFoods({_foodsJson}, {userId});");
                await TryOpenPendingDetail();
                await TryStartPendingRoute();
            }
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
        
        if (_isMapLoaded)
        {
            await TryOpenPendingDetail();
            await TryStartPendingRoute();
        }
    }

    public async Task TryOpenPendingDetail()
    {
        if (PendingOpenFoodId.HasValue && _isMapLoaded)
        {
            int id = PendingOpenFoodId.Value;
            PendingOpenFoodId = null; // Clear it so it only opens once
            
            // Aggressive retry loop to wait for Android WebView to strictly thaw and expose features.js
            for (int i = 0; i < 20; i++)
            {
                await Task.Delay(150);
                try
                {
                    string typeofRes = await mapView.EvaluateJavaScriptAsync("typeof openDetails");
                    if (typeofRes != null && typeofRes.Contains("function"))
                    {
                        await mapView.EvaluateJavaScriptAsync($"openDetails({id})");
                        break;
                    }
                }
                catch { }
            }
        }
    }

    public async Task TryStartPendingRoute()
    {
        if (PendingRouteFoodId.HasValue && _isMapLoaded)
        {
            int id = PendingRouteFoodId.Value;
            PendingRouteFoodId = null; // Clear it so it only opens once
            await Task.Delay(300); // Wait for webview to layout fully on screen
            await mapView.EvaluateJavaScriptAsync($"window.routeToPoi({id})");
        }
    }

    async void LoadFoods(string lang = "vi")
    {
        HttpClient client = new HttpClient();

        try
        {
            _foodsJson = await client.GetStringAsync($"{BackendUrl}?lang={lang}");

            if (_isMapLoaded)
            {
                int userId = Preferences.Default.Get("user_id", 0);
                await mapView.EvaluateJavaScriptAsync($"loadFoods({_foodsJson}, {userId});");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}