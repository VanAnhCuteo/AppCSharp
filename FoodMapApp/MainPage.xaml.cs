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

        private CancellationTokenSource _ttsCts;
        private CancellationTokenSource _currentSentenceCts;
        private readonly SemaphoreSlim _ttsSemaphore = new SemaphoreSlim(1, 1);
        private bool _isPaused = false;
        private TaskCompletionSource<bool> _pauseTcs;

        private string[] _currentSentences = Array.Empty<string>();
        private int _currentSentenceIndex = 0;
        private string _lastSpokenText = "";
        private string _lastPoiId = "";
        private bool _isMapLoaded = false;
        private string _foodsJson = null;

        private string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            return System.Text.RegularExpressions.Regex.Replace(text.Trim().ToLower(), @"\s+", " ");
        }

        public MainPage()
        {
            InitializeComponent();
            Instance = this;

            mapView.Navigated += async (s, e) =>
            {
                // Only initialize if we're on the map page, not for cancelled custom protocols 
                // that some platforms might still trigger a 'Navigated' event for.
                if (!e.Url.Contains("map.html")) return;

                _isMapLoaded = true;
                Console.WriteLine($"DEBUG: WebView Navigated to {e.Url}. Result: {e.Result}");
                
                // Inject dynamic API base URL from C# config to JS
                await mapView.EvaluateJavaScriptAsync($"platformApiBase = '{AppConfig.FoodApiUrl}';");

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
                    string id = query["id"] ?? "";

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        string normalizedText = NormalizeText(text);
                        bool textChanged = normalizedText != _lastSpokenText;
                        bool poiChanged = id != _lastPoiId;
                        
                        Console.WriteLine($"DEBUG: TTS Request. ID: {id}. POI Changed: {poiChanged}. Text Changed: {textChanged}. Current Index: {_currentSentenceIndex}. IsPaused: {_isPaused}");

                        if (poiChanged || (textChanged && _currentSentenceIndex == 0))
                        {
                            StopSpeech(true); // Full reset for new content or POI
                            _lastPoiId = id;
                            _lastSpokenText = normalizedText;
                            _currentSentences = text.Split(new[] { '.', '!', '?', ';', ':', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(s => s.Trim())
                                                    .Where(s => s.Length > 0)
                                                    .ToArray();
                            _currentSentenceIndex = 0;
                            Console.WriteLine($"DEBUG: New content loaded. Sentence count: {_currentSentences.Length}");
                            _ = SpeakWithChunksAsync(lang);
                        }
                        else if (_isPaused)
                        {
                            Console.WriteLine("DEBUG: Resuming from pause.");
                            _isPaused = false;
                            _pauseTcs?.TrySetResult(true);
                        }
                        else 
                        {
                            // If not paused and not changed, maybe it's already playing or we just want to ensure it's playing
                            _ = SpeakWithChunksAsync(lang);
                        }
                    }
                }
                else if (e.Url.StartsWith("app-tts://stop"))
                {
                    e.Cancel = true;
                    var uri = new Uri(e.Url);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    bool fullReset = (query["reset"] == "true");
                    
                    Console.WriteLine($"DEBUG: TTS STOP Request. Reset: {fullReset}. Current Index: {_currentSentenceIndex}");
                    
                    if (fullReset)
                    {
                        StopSpeech(true);
                        _currentSentenceIndex = 0;
                        _lastSpokenText = "";
                        _lastPoiId = "";
                    }
                    else
                    {
                        // Pause logic
                        _isPaused = true;
                        _currentSentenceCts?.Cancel();
                        Console.WriteLine($"DEBUG: TTS Paused at index {_currentSentenceIndex}");
                    }
                }
                else if (e.Url.StartsWith("app-request-reload://markers?"))
                {
                    e.Cancel = true;
                    var uri = new Uri(e.Url);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    string lang = query["lang"] ?? "vi";
                    
                    // Reset audio on language change
                    StopSpeech();
                    _currentSentenceIndex = 0;
                    _lastSpokenText = "";
                    _lastPoiId = "";

                    LoadFoods(lang);
                }
            };

            LoadFoods();
        }

        private IEnumerable<Locale> _cachedLocales = null;

        private void StopSpeech(bool fullReset = false)
        {
            _currentSentenceCts?.Cancel();
            if (fullReset)
            {
                _ttsCts?.Cancel();
                _isPaused = false;
                _pauseTcs?.TrySetResult(false);
            }
        }

        private async Task SpeakWithChunksAsync(string lang)
        {
            // Ensure only one TTS loop is active
            await _ttsSemaphore.WaitAsync();

            try
            {
                _ttsCts = new CancellationTokenSource();
                var mainToken = _ttsCts.Token;
                _isPaused = false;

                SpeechOptions options = new SpeechOptions();
                
                // Cache locales to avoid repeated slow calls
                if (_cachedLocales == null)
                {
                    _cachedLocales = await TextToSpeech.Default.GetLocalesAsync();
                    Console.WriteLine($"DEBUG: Cached {_cachedLocales.Count()} locales.");
                }

                options.Locale = _cachedLocales.FirstOrDefault(l => l.Language.Equals(lang, StringComparison.OrdinalIgnoreCase)) ??
                                 _cachedLocales.FirstOrDefault(l => l.Language.StartsWith(lang.Split('-')[0], StringComparison.OrdinalIgnoreCase));

                Console.WriteLine($"DEBUG: SpeakWithChunks loop started. From index {_currentSentenceIndex}/{_currentSentences.Length}");

                while (_currentSentenceIndex < _currentSentences.Length && !mainToken.IsCancellationRequested)
                {
                    if (_isPaused)
                    {
                        Console.WriteLine($"DEBUG: Loop waiting for resume at index {_currentSentenceIndex}");
                        _pauseTcs = new TaskCompletionSource<bool>();
                        bool resume = await _pauseTcs.Task;
                        if (!resume || mainToken.IsCancellationRequested) break;
                        Console.WriteLine($"DEBUG: Loop resuming at index {_currentSentenceIndex}");
                    }

                    _currentSentenceCts = CancellationTokenSource.CreateLinkedTokenSource(mainToken);
                    string sentence = _currentSentences[_currentSentenceIndex];
                    
                    try
                    {
                        Console.WriteLine($"DEBUG: Speaking index {_currentSentenceIndex}: {(sentence.Length > 20 ? sentence.Substring(0, 20) : sentence)}...");
                        
                        await TextToSpeech.Default.SpeakAsync(sentence, options, _currentSentenceCts.Token);
                        
                        _currentSentenceIndex++;
                        
                        // Update JS with progress AFTER completion
                        await mapView.EvaluateJavaScriptAsync($"if(window.onTtsProgress) window.onTtsProgress({_currentSentenceIndex}, {_currentSentences.Length});");
                    }
                    catch (OperationCanceledException)
                    {
                        if (!_isPaused) break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"DEBUG: Error at index {_currentSentenceIndex}: {ex.Message}");
                        _currentSentenceIndex++;
                    }
                    finally
                    {
                        _currentSentenceCts?.Dispose();
                        _currentSentenceCts = null;
                    }
                }

                if (_currentSentenceIndex >= _currentSentences.Length && !mainToken.IsCancellationRequested)
                {
                    _currentSentenceIndex = 0;
                    _lastSpokenText = "";
                    _lastPoiId = "";
                    await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
                }
            }
            finally
            {
                _ttsSemaphore.Release();
            }
        }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        
        if (_isMapLoaded)
        {
            LoadFoods(); // Refresh markers and user ID
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
            Console.WriteLine($"DEBUG: Attempting to open pending food detail for ID: {id}");
            for (int i = 0; i < 20; i++)
            {
                await Task.Delay(200);
                try
                {
                    string typeofRes = await mapView.EvaluateJavaScriptAsync("typeof openDetails");
                    Console.WriteLine($"DEBUG: WebView openDetails type check (Attempt {i+1}): {typeofRes}");
                    
                    if (typeofRes != null && typeofRes.Contains("function"))
                    {
                        Console.WriteLine($"DEBUG: Calling JS openDetails({id}) now.");
                        await mapView.EvaluateJavaScriptAsync($"openDetails({id})");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: EvaluateJS Error (Attempt {i+1}): {ex.Message}");
                }
            }
            Console.WriteLine("DEBUG: Failed to open pending detail after all retries.");
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
                
                // Ensure pending actions are consumed even if LoadFoods finished after Navigated
                await TryOpenPendingDetail();
                await TryStartPendingRoute();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}