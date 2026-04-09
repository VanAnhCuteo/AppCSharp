using System.Net.Http;
using System.Text.Json;
using System.Web;
using System.Diagnostics;

namespace FoodMapApp
{
    public partial class MainPage : ContentPage
    {
        public static MainPage? Instance { get; private set; }
        public static int? PendingOpenFoodId { get; set; } = null;
        public static int? PendingRouteFoodId { get; set; } = null;

        private static string BackendUrl => AppConfig.FoodApiUrl;

        private CancellationTokenSource? _ttsCts;
        private CancellationTokenSource? _currentSentenceCts;
        private readonly SemaphoreSlim _ttsSemaphore = new SemaphoreSlim(1, 1);
        private bool _isPaused = false;
        private TaskCompletionSource<bool>? _pauseTcs;
        private bool _isActuallySpeaking = false; 

        private string[] _currentSentences = Array.Empty<string>();
        private int _currentSentenceIndex = 0;
        private string _lastSpokenText = "";
        private string _lastPoiId = "";
        private bool _isMapLoaded = false;
        private string? _foodsJson = null;
        private Stopwatch _audioStopwatch = new Stopwatch();
        private string _reportingPoiId = "";
        private IDispatcherTimer? _locationTimer;
        private Queue<(string Text, string Id, string Lang)> _audioQueue = new();

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
                if (!e.Url.Contains("map.html")) return;

                _isMapLoaded = true;
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
                    bool isManual = query["manual"] == "true";

                    Debug.WriteLine($"DEBUG: Received speak request. ID: {id}, Manual: {isManual}");

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        string normalizedText = NormalizeText(text);
                        bool poiChanged = id != _lastPoiId;

                        // CHỈ cập nhật UI ngay lập tức nếu là bấm tay hoặc hệ thống đang rảnh
                        bool isSystemIdle = !_isActuallySpeaking && _ttsSemaphore.CurrentCount > 0;
                        if (isManual || isSystemIdle)
                        {
                            _ = ShowMiniPlayer(id);
                        }

                        if (isManual)
                        {
                            Debug.WriteLine($"DEBUG: MANUAL click for {id}. Interrupting.");
                            StopSpeech(true);
                            await Task.Delay(100);
                            StartNewSpeech(text, id, normalizedText, lang);
                        }
                        else 
                        {
                            if (_isActuallySpeaking || _ttsSemaphore.CurrentCount == 0)
                            {
                                Debug.WriteLine($"DEBUG: AUTO request for {id}. System BUSY. Enqueuing.");
                                _audioQueue.Enqueue((text, id, lang));
                            }
                            else
                            {
                                Debug.WriteLine($"DEBUG: AUTO request for {id}. System IDLE. Starting.");
                                StartNewSpeech(text, id, normalizedText, lang);
                            }
                        }
                    }
                }
                else if (e.Url.StartsWith("app-tts://stop"))
                {
                    e.Cancel = true;
                    var uri = new Uri(e.Url);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    bool fullReset = (query["reset"] == "true");
                    
                    if (fullReset)
                    {
                        StopSpeech(true);
                        miniPlayer.IsVisible = false;
                    }
                    else
                    {
                        _isPaused = true;
                        playIcon.IsVisible = true;
                        pauseIcon.IsVisible = false;
                        _currentSentenceCts?.Cancel();
                    }
                }
                else if (e.Url.StartsWith("app-request-reload://markers?"))
                {
                    e.Cancel = true;
                    var uri = new Uri(e.Url);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    string lang = query["lang"] ?? "vi";
                    
                    StopSpeech(true);
                    miniPlayer.IsVisible = false;
                    LoadFoods(lang);
                }
            };

            LoadFoods();
            StartLocationTracking();
        }

        private void StartNewSpeech(string text, string id, string normalizedText, string lang)
        {
            _lastPoiId = id;
            _lastSpokenText = normalizedText;
            _currentSentences = text.Split(new[] { '.', '!', '?', ';', ',', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => s.Trim())
                                    .Where(s => s.Length > 0)
                                    .ToArray();
            _currentSentenceIndex = 0;
            _ = SpeakWithChunksAsync(lang);
        }

        private void StartLocationTracking()
        {
            _locationTimer = Dispatcher.CreateTimer();
            _locationTimer.Interval = TimeSpan.FromSeconds(60);
            _locationTimer.Tick += async (s, e) => await ReportCurrentLocationAsync();
            _locationTimer.Start();
        }

        private async Task ReportCurrentLocationAsync()
        {
            try
            {
                int userId = Preferences.Default.Get("user_id", 0);
                if (userId <= 0) return;

                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                var location = await Geolocation.Default.GetLocationAsync(request);

                if (location != null)
                {
                    var payload = new 
                    { 
                        user_id = userId, 
                        latitude = location.Latitude, 
                        longitude = location.Longitude 
                    };

                    using HttpClient client = new HttpClient();
                    var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                    await client.PostAsync($"{AppConfig.AuthApiUrl}/update-location", content);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DEBUG: Error reporting location: {ex.Message}");
            }
        }

        private async Task ShowMiniPlayer(string poiId)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() => {
                    miniPlayer.IsVisible = true; 
                    playIcon.IsVisible = false;
                    pauseIcon.IsVisible = true;
                });

                if (!string.IsNullOrEmpty(_foodsJson))
                {
                    var foods = JsonSerializer.Deserialize<List<JsonElement>>(_foodsJson);
                    var food = foods.FirstOrDefault(f => f.GetProperty("id").GetInt32().ToString() == poiId);
                    
                    if (food.ValueKind != JsonValueKind.Undefined)
                    {
                        string name = food.GetProperty("name").GetString() ?? "Quán ăn";
                        await MainThread.InvokeOnMainThreadAsync(() => {
                            detectedShopLabel.Text = name;
                        });
                        Debug.WriteLine($"DEBUG: UI Updated to Shop: {name}");
                        return;
                    }
                }
                
                await MainThread.InvokeOnMainThreadAsync(() => {
                    detectedShopLabel.Text = "Quán ăn gần bạn";
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DEBUG: Error showing miniplayer: {ex.Message}");
            }
        }

        private async Task AnimateVisualizer()
        {
            Random rnd = new Random();
            while (!_isPaused && pauseIcon.IsVisible && miniPlayer.IsVisible)
            {
                wave1.HeightRequest = rnd.Next(8, 20);
                wave2.HeightRequest = rnd.Next(10, 25);
                wave3.HeightRequest = rnd.Next(5, 15);
                wave4.HeightRequest = rnd.Next(12, 28);
                wave5.HeightRequest = rnd.Next(8, 22);
                wave6.HeightRequest = rnd.Next(10, 24);
                await Task.Delay(130);
            }
            
            wave1.HeightRequest = 12; wave2.HeightRequest = 18; wave3.HeightRequest = 10;
            wave4.HeightRequest = 22; wave5.HeightRequest = 14; wave6.HeightRequest = 19;
        }

        private void OnPlayAudioClicked(object sender, EventArgs e)
        {
            if (_isPaused)
            {
                _isPaused = false;
                playIcon.IsVisible = false;
                pauseIcon.IsVisible = true;
                _pauseTcs?.TrySetResult(true);
                _ = AnimateVisualizer();
            }
            else if (pauseIcon.IsVisible)
            {
                _isPaused = true;
                playIcon.IsVisible = true;
                pauseIcon.IsVisible = false;
                _currentSentenceCts?.Cancel();
            }
        }

        private void OnReplayAudioClicked(object sender, EventArgs e)
        {
            StopSpeech(true);
            _currentSentenceIndex = 0;
            _ = SpeakWithChunksAsync("vi-VN"); 
        }

        private async void OnDetailClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_lastPoiId)) return;
            await mapView.EvaluateJavaScriptAsync($"openDetails({_lastPoiId})");
        }

        private async void OnClosePlayerClicked(object sender, EventArgs e)
        {
            StopSpeech(true);
            miniPlayer.IsVisible = false;
            await mapView.EvaluateJavaScriptAsync("if(window.stopAudioProfessional) window.stopAudioProfessional();");
        }

        public async void StopGlobalAudio()
        {
            StopSpeech(true);
            miniPlayer.IsVisible = false;
            if (mapView != null)
            {
                await mapView.EvaluateJavaScriptAsync("if(window.stopAudioProfessional) window.stopAudioProfessional();");
            }
        }

        private IEnumerable<Locale>? _cachedLocales = null;

        private void StopSpeech(bool fullReset = false)
        {
            _currentSentenceCts?.Cancel();
            _audioStopwatch.Stop();

            if (fullReset)
            {
                _audioQueue.Clear(); 
                _ttsCts?.Cancel();
                _isPaused = false;
                _pauseTcs?.TrySetResult(false);
                _currentSentenceIndex = 0;
                _lastPoiId = "";
                _lastSpokenText = "";

                if (_audioStopwatch.Elapsed.TotalSeconds >= 1 && !string.IsNullOrEmpty(_reportingPoiId))
                {
                    _ = SendAudioLogAsync(_reportingPoiId, (int)_audioStopwatch.Elapsed.TotalSeconds);
                }
                _audioStopwatch.Reset();
                _reportingPoiId = "";
            }
        }

        private async Task SpeakWithChunksAsync(string lang)
        {
            Debug.WriteLine($"DEBUG: SpeakWithChunksAsync waiting for semaphore for {lang}");
            if (!await _ttsSemaphore.WaitAsync(5000)) {
                return;
            }

            _isActuallySpeaking = true;
            try
            {
                _ttsCts = new CancellationTokenSource();
                var mainToken = _ttsCts.Token;
                _isPaused = false;

                if (_cachedLocales == null)
                {
                    _cachedLocales = await TextToSpeech.Default.GetLocalesAsync();
                }

                string prefix = lang.Split('-')[0].ToLower();
                var locale = _cachedLocales?.FirstOrDefault(l => l.Language.Equals(lang, StringComparison.OrdinalIgnoreCase)) ??
                             _cachedLocales?.FirstOrDefault(l => l.Language.ToLower().StartsWith(prefix));

                SpeechOptions options = new SpeechOptions { Locale = locale };
                
                _reportingPoiId = _lastPoiId;
                _ = AnimateVisualizer();

                while (_currentSentenceIndex < _currentSentences.Length && !mainToken.IsCancellationRequested)
                {
                    if (_isPaused)
                    {
                        _pauseTcs = new TaskCompletionSource<bool>();
                        bool resume = await _pauseTcs.Task;
                        if (!resume || mainToken.IsCancellationRequested) break;
                    }

                    _currentSentenceCts = CancellationTokenSource.CreateLinkedTokenSource(mainToken);
                    string sentence = _currentSentences[_currentSentenceIndex];
                    
                    if (string.IsNullOrWhiteSpace(sentence)) {
                        _currentSentenceIndex++;
                        continue;
                    }

                    try
                    {
                        Debug.WriteLine($"DEBUG: Speaking chunk {_currentSentenceIndex + 1}/{_currentSentences.Length}");
                        _audioStopwatch.Start();
                        await TextToSpeech.Default.SpeakAsync(sentence, options, _currentSentenceCts.Token);
                        _audioStopwatch.Stop();
                        
                        _currentSentenceIndex++;
                        await mapView.EvaluateJavaScriptAsync($"if(window.onTtsProgress) window.onTtsProgress({_currentSentenceIndex}, {_currentSentences.Length});");
                    }
                    catch (OperationCanceledException)
                    {
                        if (!_isPaused) break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"DEBUG: Error speaking chunk: {ex.Message}");
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
                    if (_audioStopwatch.Elapsed.TotalSeconds >= 1 && !string.IsNullOrEmpty(_reportingPoiId))
                    {
                        _ = SendAudioLogAsync(_reportingPoiId, (int)_audioStopwatch.Elapsed.TotalSeconds);
                    }
                    _audioStopwatch.Reset();

                    _currentSentenceIndex = 0;
                    _lastPoiId = "";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DEBUG: Global Speak ERROR: {ex.Message}");
            }
            finally
            {
                _isActuallySpeaking = false;
                _ttsSemaphore.Release();
                Debug.WriteLine("DEBUG: Semaphore released.");
            }

            if (_audioQueue.Count > 0 && !(_ttsCts?.IsCancellationRequested ?? false))
            {
                Debug.WriteLine($"DEBUG: Queue processing. Next in line...");
                await Task.Delay(2000);
                if (_audioQueue.TryDequeue(out var next))
                {
                    // QUAN TRỌNG: Chỉ cập nhật tên quán ngay trước khi quán đó bắt đầu nói
                    await ShowMiniPlayer(next.Id);
                    StartNewSpeech(next.Text, next.Id, NormalizeText(next.Text), next.Lang);
                }
            }
            else if (_currentSentenceIndex == 0) 
            {
                playIcon.IsVisible = true;
                pauseIcon.IsVisible = false;
                await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
            }
        }

        private async Task SendAudioLogAsync(string poiId, int duration)
        {
            if (string.IsNullOrEmpty(poiId)) return;
            try
            {
                int userId = Preferences.Default.Get("user_id", 1);
                var payload = new { user_id = userId, duration_seconds = duration };
                using HttpClient client = new HttpClient();
                var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                await client.PostAsync($"{BackendUrl}/{poiId}/audio-log", content);
            }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: Log error: {ex.Message}"); }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (_isMapLoaded)
            {
                LoadFoods(); 
                await TryOpenPendingDetail();
            }
        }

        public async Task TryOpenPendingDetail()
        {
            if (PendingOpenFoodId.HasValue && _isMapLoaded)
            {
                int id = PendingOpenFoodId.Value;
                PendingOpenFoodId = null;
                for (int i = 0; i < 20; i++)
                {
                    await Task.Delay(200);
                    try
                    {
                        string typeofRes = await mapView.EvaluateJavaScriptAsync("typeof openDetails");
                        if (typeofRes != null && typeofRes.Contains("function"))
                        {
                            await mapView.EvaluateJavaScriptAsync($"openDetails({id})");
                            return;
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
                PendingRouteFoodId = null;
                await Task.Delay(300);
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
                    await TryOpenPendingDetail();
                }
            }
            catch (Exception ex) { Console.WriteLine(ex); }
        }
    }
}
