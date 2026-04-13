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

        // Unified Audio States
        private static AudioSession _autoSession = new() { IsManual = false };
        private static AudioSession _manualSession = new() { IsManual = true };
        private static AudioSession? _activeSession = null;

        private static bool _isCleaningUp = false; 
        private static bool _isActuallySpeaking = false; 
        private static TaskCompletionSource<bool>? _pauseTcs;
        private static Queue<AudioSession> _audioQueue = new();

        public class AudioSession
        {
            public string PoiId { get; set; } = "";
            public string Name { get; set; } = "Quán ăn";
            public string[] Sentences { get; set; } = Array.Empty<string>();
            public int SentenceIndex { get; set; } = 0;
            public string Language { get; set; } = "vi-VN";
            public bool IsPaused { get; set; } = true;
            public bool IsManual { get; set; } = false;
            public bool IsActive { get; set; } = false;

            public void Reset()
            {
                PoiId = ""; Sentences = Array.Empty<string>(); SentenceIndex = 0;
                IsPaused = true; IsActive = false;
            }
        }

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
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        string normalizedText = NormalizeText(text);
                        var sentences = normalizedText.Split(new[] { '.', '!', '?', ';', ',', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                                      .Select(s => s.Trim())
                                                      .Where(s => s.Length > 0)
                                                      .ToArray();

                        if (isManual)
                        {
                            StopSpeech(fullReset: false, clearQueue: false); // Stop current (auto or previous manual)
                            
                            _manualSession.PoiId = id;
                            _manualSession.Language = lang;
                            _manualSession.Sentences = sentences;
                            _manualSession.SentenceIndex = 0;
                            _manualSession.IsPaused = false;
                            _manualSession.IsActive = true;
                            _activeSession = _manualSession;

                            _isCleaningUp = true;
                            await MainThread.InvokeOnMainThreadAsync(() => miniPlayer.IsVisible = false);
                            await SyncPlayerUI(_manualSession);
                            await Task.Delay(100);
                            _isCleaningUp = false;

                            _ = SpeakWithChunksAsync(_manualSession);
                        }
                        else
                        {
                            // Automatic GPS entry
                            if (_manualSession.IsActive)
                            {
                                // If manual is playing, queue or hold the auto hit
                                if (string.IsNullOrEmpty(_autoSession.PoiId))
                                {
                                    _autoSession.PoiId = id;
                                    _autoSession.Language = lang;
                                    _autoSession.Sentences = sentences;
                                    _autoSession.SentenceIndex = 0;
                                    _autoSession.IsPaused = true;
                                    _autoSession.IsActive = true;
                                }
                                else if (_autoSession.PoiId != id)
                                {
                                    _audioQueue.Enqueue(new AudioSession { PoiId = id, Language = lang, Sentences = sentences, IsManual = false });
                                }
                                return;
                            }

                            if (string.IsNullOrEmpty(_autoSession.PoiId))
                            {
                                _autoSession.PoiId = id;
                                _autoSession.Language = lang;
                                _autoSession.Sentences = sentences;
                                _autoSession.SentenceIndex = 0;
                                _autoSession.IsPaused = false;
                                _autoSession.IsActive = true;
                                _activeSession = _autoSession;

                                await SyncPlayerUI(_autoSession);
                                _ = SpeakWithChunksAsync(_autoSession);
                            }
                            else if (_autoSession.PoiId != id)
                            {
                                // Priority interruption: pause current auto, queue it, start new one
                                _autoSession.IsPaused = true;
                                _audioQueue.Enqueue(new AudioSession 
                                { 
                                    PoiId = _autoSession.PoiId, Language = _autoSession.Language, 
                                    Sentences = _autoSession.Sentences, SentenceIndex = _autoSession.SentenceIndex, 
                                    IsManual = false 
                                });

                                StopSpeech(fullReset: false, clearQueue: false);

                                _autoSession.PoiId = id;
                                _autoSession.Language = lang;
                                _autoSession.Sentences = sentences;
                                _autoSession.SentenceIndex = 0;
                                _autoSession.IsPaused = false;
                                _activeSession = _autoSession;

                                await SyncPlayerUI(_autoSession);
                                _ = SpeakWithChunksAsync(_autoSession);
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
                        StopGlobalAudio();
                    }
                    else if (_activeSession != null)
                    {
                        _activeSession.IsPaused = true;
                        _currentSentenceCts?.Cancel();
                        await SyncPlayerUI(_activeSession);
                    }
                }
                 else if (e.Url.StartsWith("app-request-reload://markers?"))
                {
                    e.Cancel = true;
                    var uri = new Uri(e.Url);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    string lang = query["lang"] ?? "vi";
                    
                    StopGlobalAudio();
                    
                    _autoSession.Language = lang;
                    _manualSession.Language = lang;

                    await Task.Delay(200);
                    if (mapView != null) await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
                    LoadFoods(lang);
                }
            };

            LoadFoods();
            StartLocationTracking();
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
                    var payload = new { user_id = userId, latitude = location.Latitude, longitude = location.Longitude };
                    using HttpClient client = new HttpClient();
                    var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                    await client.PostAsync($"{AppConfig.AuthApiUrl}/update-location", content);
                }
            }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: Error reporting location: {ex.Message}"); }
        }

        private async Task SyncPlayerUI(AudioSession? session)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() => {
                    if (session == null || !session.IsActive)
                    {
                        miniPlayer.IsVisible = false;
                        manualMiniPlayer.IsVisible = false;
                        return;
                    }

                    string name = "Quán ăn";
                    if (_foodsJson != null) {
                        try {
                            var foods = JsonSerializer.Deserialize<List<JsonElement>>(_foodsJson);
                            var food = foods?.FirstOrDefault(f => f.GetProperty("id").GetInt32().ToString() == session.PoiId);
                            if (food.HasValue && food.Value.ValueKind != JsonValueKind.Undefined)
                                name = food.Value.GetProperty("name").GetString() ?? "Quán ăn";
                        } catch { }
                    }

                    if (session.IsManual) {
                        manualMiniPlayer.IsVisible = true;
                        mPlayIcon.IsVisible = session.IsPaused;
                        mPauseIcon.IsVisible = !session.IsPaused;
                        manualShopLabel.Text = name;
                    } else {
                        miniPlayer.IsVisible = true;
                        playIcon.IsVisible = session.IsPaused;
                        pauseIcon.IsVisible = !session.IsPaused;
                        detectedShopLabel.Text = name;
                    }
                });
            }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SyncUI error: {ex.Message}"); }
        }

        private async Task AnimateVisualizer(AudioSession session)
        {
            Random rnd = new Random();
            while (_isActuallySpeaking && _activeSession == session && !session.IsPaused)
            {
                await MainThread.InvokeOnMainThreadAsync(() => {
                    if (session.IsManual) {
                        mWave1.HeightRequest = rnd.Next(8, 20); mWave2.HeightRequest = rnd.Next(10, 25);
                        mWave3.HeightRequest = rnd.Next(5, 15); mWave4.HeightRequest = rnd.Next(12, 28);
                        mWave5.HeightRequest = rnd.Next(8, 22); mWave6.HeightRequest = rnd.Next(10, 24);
                    } else {
                        wave1.HeightRequest = rnd.Next(8, 20); wave2.HeightRequest = rnd.Next(10, 25);
                        wave3.HeightRequest = rnd.Next(5, 15); wave4.HeightRequest = rnd.Next(12, 28);
                        wave5.HeightRequest = rnd.Next(8, 22); wave6.HeightRequest = rnd.Next(10, 24);
                    }
                });
                await Task.Delay(130);
            }
            await MainThread.InvokeOnMainThreadAsync(() => {
                if (session.IsManual) {
                    mWave1.HeightRequest = 12; mWave2.HeightRequest = 18; mWave3.HeightRequest = 10;
                    mWave4.HeightRequest = 22; mWave5.HeightRequest = 14; mWave6.HeightRequest = 19;
                } else {
                    wave1.HeightRequest = 12; wave2.HeightRequest = 18; wave3.HeightRequest = 10;
                    wave4.HeightRequest = 22; wave5.HeightRequest = 14; wave6.HeightRequest = 19;
                }
            });
        }

        private async void OnClosePlayerClicked(object sender, EventArgs e)
        {
            StopSpeech(fullReset: false, clearQueue: false);
            if (mapView != null) await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");

            if (_audioQueue.Count > 0)
            {
                bool shouldContinue = await DisplayAlert("Thông báo", "Bạn muốn nghe quán tiếp theo hay hủy bỏ?", "Nghe", "Hủy bỏ");
                if (shouldContinue && _audioQueue.TryDequeue(out var next))
                {
                    await Task.Delay(200);
                    _autoSession.PoiId = next.PoiId;
                    _autoSession.Sentences = next.Sentences;
                    _autoSession.SentenceIndex = 0;
                    _autoSession.Language = next.Language;
                    _autoSession.IsPaused = false;
                    _autoSession.IsActive = true;
                    _activeSession = _autoSession;
                    
                    await SyncPlayerUI(_autoSession);
                    _ = SpeakWithChunksAsync(_autoSession);
                    return;
                }
            }

            _autoSession.Reset();
            _audioQueue.Clear();
            if (_activeSession == _autoSession) StopSpeech(true);
            await SyncPlayerUI(null);
        }

        private async void OnPlayAudioClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_autoSession.PoiId)) return;
            _autoSession.IsPaused = !_autoSession.IsPaused;
            await SyncPlayerUI(_autoSession);

            if (!_autoSession.IsPaused)
            {
                if (_manualSession.IsActive) { _manualSession.IsActive = false; StopSpeech(false, false); manualMiniPlayer.IsVisible = false; }
                _activeSession = _autoSession;
                if (!_isActuallySpeaking) _ = SpeakWithChunksAsync(_autoSession);
                else _pauseTcs?.TrySetResult(true);
            }
            else { _pauseTcs?.TrySetResult(false); }
        }

        private async void OnReplayAudioClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_autoSession.PoiId)) return;
            _autoSession.SentenceIndex = 0; _autoSession.IsPaused = false;
            await SyncPlayerUI(_autoSession);

            if (_manualSession.IsActive) { _manualSession.IsActive = false; StopSpeech(false, false); manualMiniPlayer.IsVisible = false; }
            _activeSession = _autoSession;
            if (!_isActuallySpeaking) _ = SpeakWithChunksAsync(_autoSession);
            else _pauseTcs?.TrySetResult(true);
        }

        private async void OnManualPlayClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_manualSession.PoiId)) return;
            _manualSession.IsPaused = !_manualSession.IsPaused;
            await SyncPlayerUI(_manualSession);

            if (!_manualSession.IsPaused)
            {
                if (!_manualSession.IsActive && _isActuallySpeaking) StopSpeech(false, false);
                _manualSession.IsActive = true;
                _activeSession = _manualSession;
                if (!_isActuallySpeaking) _ = SpeakWithChunksAsync(_manualSession);
                else _pauseTcs?.TrySetResult(true);
            }
            else { _pauseTcs?.TrySetResult(false); }
        }

        private async void OnManualReplayClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_manualSession.PoiId)) return;
            _manualSession.SentenceIndex = 0; _manualSession.IsPaused = false;
            await SyncPlayerUI(_manualSession);

            if (!_manualSession.IsActive && _isActuallySpeaking) StopSpeech(false, false);
            _manualSession.IsActive = true;
            _activeSession = _manualSession;
            if (!_isActuallySpeaking) _ = SpeakWithChunksAsync(_manualSession);
            else _pauseTcs?.TrySetResult(true);
        }

        private async void OnManualCloseClicked(object sender, EventArgs e)
        {
            _manualSession.IsActive = false;
            StopSpeech(fullReset: false, clearQueue: false);
            await SyncPlayerUI(null);
            if (mapView != null) await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
            
            bool isInterrupted = !string.IsNullOrEmpty(_autoSession.PoiId) && (_autoSession.Sentences != null && _autoSession.SentenceIndex < _autoSession.Sentences.Length);
            
            if (isInterrupted || _audioQueue.Count > 0)
            {
                if (await DisplayAlert("Thông báo", "Bạn có muốn nghe tiếp audio ở gần không?", "Tiếp tục", "Bỏ qua"))
                {
                    if (string.IsNullOrEmpty(_autoSession.PoiId) && _audioQueue.Count > 0 && _audioQueue.TryDequeue(out var next))
                    {
                        _autoSession.PoiId = next.PoiId;
                        _autoSession.Language = next.Language;
                        _autoSession.Sentences = next.Sentences;
                        _autoSession.SentenceIndex = 0;
                    }

                    if (!string.IsNullOrEmpty(_autoSession.PoiId))
                    {
                        _autoSession.IsPaused = true;
                        _autoSession.IsActive = true;
                        await SyncPlayerUI(_autoSession);
                    }
                }
                else
                {
                    _autoSession.Reset();
                    _audioQueue.Clear();
                    await SyncPlayerUI(null);
                }
            }
        }

        private async void OnDetailClicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_autoSession.PoiId))
                await mapView.EvaluateJavaScriptAsync($"openDetails({_autoSession.PoiId})");
        }

        public async void StopGlobalAudio()
        {
            StopSpeech(true);
            miniPlayer.IsVisible = false;
            manualMiniPlayer.IsVisible = false;
            // Dừng mọi animation và logic phía JS
            if (mapView != null) {
                await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
                await mapView.EvaluateJavaScriptAsync("if(window.stopAudioProfessional) window.stopAudioProfessional();");
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
                await client.PostAsync($"{AppConfig.FoodApiUrl}/{poiId}/audio-log", content);
            }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: Log error: {ex.Message}"); }
        }

        private void StopSpeech(bool fullReset = false, bool clearQueue = true)
        {
            _isActuallySpeaking = false;
            _currentSentenceCts?.Cancel();
            _ttsCts?.Cancel(); 
            _pauseTcs?.TrySetResult(false);
            _audioStopwatch.Stop();
            
            if (fullReset)
            {
                if (clearQueue) _audioQueue.Clear(); 
                _autoSession.Reset();
                _manualSession.Reset();
                _activeSession = null;
            }
        }

        private async Task SpeakWithChunksAsync(AudioSession session)
        {
            if (!await _ttsSemaphore.WaitAsync(500)) return;
            _isActuallySpeaking = true;
            _ttsCts = new CancellationTokenSource();
            _audioStopwatch.Restart();

            try
            {
                var sentences = session.Sentences;
                if (sentences == null || sentences.Length == 0) return;

                if (_cachedLocales == null) _cachedLocales = await TextToSpeech.Default.GetLocalesAsync();
                var locale = _cachedLocales?.FirstOrDefault(l => l.Language.Equals(session.Language, StringComparison.OrdinalIgnoreCase)) ??
                             _cachedLocales?.FirstOrDefault(l => l.Language.ToLower().StartsWith(session.Language.Split('-')[0].ToLower()));
                var options = new SpeechOptions { Locale = locale };

                _ = AnimateVisualizer(session);

                while (session.SentenceIndex < sentences.Length)
                {
                    if (_ttsCts.Token.IsCancellationRequested || !_isActuallySpeaking || _activeSession != session) break;

                    int index = session.SentenceIndex;
                    if (session.IsManual) await mapView.EvaluateJavaScriptAsync($"if(window.onTtsProgress) window.onTtsProgress({index}, {sentences.Length});");

                    if (_isActuallySpeaking && session.IsPaused)
                    {
                        _pauseTcs = new TaskCompletionSource<bool>();
                        bool resume = await _pauseTcs.Task;
                        if (!resume || _ttsCts.Token.IsCancellationRequested || !_isActuallySpeaking || _activeSession != session) break;
                    }

                    if (_ttsCts.Token.IsCancellationRequested || !_isActuallySpeaking || _activeSession != session) break;
                    
                    _currentSentenceCts = CancellationTokenSource.CreateLinkedTokenSource(_ttsCts.Token);
                    string sentence = sentences[index];
                    if (!string.IsNullOrWhiteSpace(sentence)) await TextToSpeech.Default.SpeakAsync(sentence, options, _currentSentenceCts.Token);
                    
                    if (_ttsCts.Token.IsCancellationRequested || !_isActuallySpeaking || _activeSession != session) break;
                    session.SentenceIndex++;
                }

                if (!session.IsManual && session.SentenceIndex >= sentences.Length) _ = SendAudioLogAsync(session.PoiId, (int)_audioStopwatch.Elapsed.TotalSeconds);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Debug.WriteLine($"TTS Error: {ex.Message}"); }
            finally 
            { 
                _isActuallySpeaking = false; 
                _ttsSemaphore.Release(); 
                MainThread.BeginInvokeOnMainThread(async () => {
                    if (mapView != null) await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
                });
            }

            if (_isCleaningUp) return;

            if (session.SentenceIndex >= session.Sentences.Length)
            {
                if (session.IsManual)
                {
                    session.IsActive = false;
                    await MainThread.InvokeOnMainThreadAsync(async () => {
                        await SyncPlayerUI(null);
                        if (!string.IsNullOrEmpty(_autoSession.PoiId)) {
                            _autoSession.IsPaused = true;
                            _autoSession.IsActive = true;
                            _activeSession = _autoSession;
                            await SyncPlayerUI(_autoSession);
                        }
                    });
                }
                else
                {
                    if (_audioQueue.Count > 0 && _audioQueue.TryDequeue(out var next))
                    {
                        _autoSession.PoiId = next.PoiId;
                        _autoSession.Sentences = next.Sentences;
                        _autoSession.SentenceIndex = 0;
                        _autoSession.Language = next.Language;
                        _autoSession.IsPaused = false;
                        _activeSession = _autoSession;
                        await SyncPlayerUI(_autoSession);
                        _ = SpeakWithChunksAsync(_autoSession);
                    }
                    else
                    {
                        session.PoiId = ""; 
                        await SyncPlayerUI(session);
                    }
                }
            }
        }
        }
        public void HandleSystemInterruption()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_isActuallySpeaking && _activeSession != null)
                {
                    _activeSession.IsPaused = true;
                    StopSpeech(fullReset: false, clearQueue: false);
                    _ = SyncPlayerUI(_activeSession);
                }
            });
        }


        private static IEnumerable<Locale>? _cachedLocales = null;
        private static CancellationTokenSource? _ttsCts;
        private static CancellationTokenSource? _currentSentenceCts;
        private readonly SemaphoreSlim _ttsSemaphore = new SemaphoreSlim(1, 1);
        private bool _isMapLoaded = false;
        private static string? _foodsJson = null;
        private static Stopwatch _audioStopwatch = new Stopwatch();
        private IDispatcherTimer? _locationTimer;

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_manualSession.IsActive)
            {
                _manualSession.IsPaused = true;
                _activeSession = _manualSession;
                _ = SyncPlayerUI(_manualSession);
            }
            else if (_autoSession.IsActive)
            {
                _autoSession.IsPaused = true;
                _activeSession = _autoSession;
                _ = SyncPlayerUI(_autoSession);
            }
            else if (_audioQueue.Count > 0)
            {
                if (_audioQueue.TryPeek(out var next))
                {
                    _autoSession.PoiId = next.PoiId;
                    _autoSession.IsActive = true;
                    _autoSession.IsPaused = true;
                    _activeSession = _autoSession;
                    _ = SyncPlayerUI(_autoSession);
                }
            }

            // 2. CÁC TÁC VỤ KHÁC (CÓ THỂ GÂY TRỄ)
            _ = Permissions.RequestAsync<Permissions.LocationWhenInUse>(); // Không await để không chặn UI
            
            if (_isMapLoaded) 
            { 
                string currentLang = _autoLang?.Split('-')[0] ?? "vi";
                _ = LoadFoods(currentLang); 
                await TryOpenPendingDetail(); 
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            if (_isActuallySpeaking && _activeSession != null)
            {
                _activeSession.IsPaused = true;
                StopSpeech(fullReset: false, clearQueue: false);
            }

            MainThread.BeginInvokeOnMainThread(async () => {
                await SyncPlayerUI(_activeSession);
            });
        }

        public async Task TryOpenPendingDetail()
        {
            if (PendingOpenFoodId.HasValue && _isMapLoaded) {
                int id = PendingOpenFoodId.Value; PendingOpenFoodId = null;
                for (int i = 0; i < 20; i++) {
                    await Task.Delay(200);
                    try {
                        string typeofRes = await mapView.EvaluateJavaScriptAsync("typeof openDetails");
                        if (typeofRes != null && typeofRes.Contains("function")) {
                            await mapView.EvaluateJavaScriptAsync($"openDetails({id})");
                            return;
                        }
                    } catch { }
                }
            }
        }

        public async Task TryStartPendingRoute()
        {
            if (PendingRouteFoodId.HasValue && _isMapLoaded) {
                int id = PendingRouteFoodId.Value; PendingRouteFoodId = null;
                await Task.Delay(300); await mapView.EvaluateJavaScriptAsync($"window.routeToPoi({id})");
            }
        }

        async Task LoadFoods(string lang = "vi")
        {
            HttpClient client = new HttpClient();
            try {
                _foodsJson = await client.GetStringAsync($"{AppConfig.FoodApiUrl}?lang={lang}");
                if (_isMapLoaded) {
                    int userId = Preferences.Default.Get("user_id", 0);
                    await mapView.EvaluateJavaScriptAsync($"loadFoods({_foodsJson}, {userId});");
                    await TryOpenPendingDetail();
                }
            } catch (Exception ex) { Console.WriteLine(ex); }
        }

    }

    public class FoodItem
    {
        public int id { get; set; }
        public string? name { get; set; }
    }
}
