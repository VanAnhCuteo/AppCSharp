using System.Net.Http;
using FoodMapApp.Services;
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
        private static AudioSession _manualSession = new() { IsManual = true };
        private static AudioSession? _activeSession = null;

        private static bool _isCleaningUp = false; 
        private static bool _isActuallySpeaking = false; 
        private static TaskCompletionSource<bool>? _pauseTcs;


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
                    string text = query["text"] ?? "";
                    string id = query["id"] ?? "";
                    string lang = query["lang"] ?? "vi-VN";
                    bool isManual = (query["manual"] == "true" || query["isManual"] == "true");

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
                            await MainThread.InvokeOnMainThreadAsync(() => manualMiniPlayer.IsVisible = false);
                            await SyncPlayerUI(_manualSession);
                            await Task.Delay(100);
                            _isCleaningUp = false;

                            _ = SpeakWithChunksAsync(_manualSession);
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
                        StopSpeech(fullReset: false, clearQueue: false);
                        await SyncPlayerUI(_activeSession);
                    }
                }
                else if (e.Url.StartsWith("app-ui://alert?"))
                {
                    e.Cancel = true;
                    var uri = new Uri(e.Url);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    string message = query["message"] ?? "";
                    await DisplayAlert("Thông báo", message, "Đóng");
                }
                else if (e.Url.StartsWith("app-request-confirm://lang-switch?"))
                {
                    e.Cancel = true;
                    var uri = new Uri(e.Url);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    string lang = query["lang"] ?? "vi";
                    string name = query["name"] ?? "";
                    
                    bool confirmed = await DisplayAlert("Thông báo", $"Bạn muốn chuyển sang {name}?", "Đồng ý", "Hủy");
                    if (confirmed)
                    {
                        StopSpeech(fullReset: false, clearQueue: false);
                        _manualSession.Language = lang;
                        await Task.Delay(200);
                        if (mapView != null) await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
                        LoadFoods(lang);
                    }
                }
                else if (e.Url.StartsWith("app-request-reload://markers?"))
                {
                    e.Cancel = true;
                    var uri = new Uri(e.Url);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    string lang = query["lang"] ?? "vi";
                    
                    // Proceed with reload
                    StopSpeech(fullReset: false, clearQueue: false); 
                    _manualSession.Language = lang;

                    await Task.Delay(200);
                    if (mapView != null) await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
                    LoadFoods(lang);
                }
                else if (e.Url.StartsWith("app-tour://update?"))
                {
                    e.Cancel = true;
                    var uri = new Uri(e.Url);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    UpdateTourProgressUI(query["duration"] ?? "", query["price"] ?? "", query["progress"] ?? "");
                }
                else if (e.Url.StartsWith("app-tour://save?"))
                {
                    e.Cancel = true;
                    var uri = new Uri(e.Url);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    int tid = int.Parse(query["id"] ?? "0");
                    decimal pct = decimal.Parse(query["pct"] ?? "0");
                    string st = query["status"] ?? "";
                    if (tid > 0) _ = SaveTourHistoryLocallyAsync(tid, pct, st);
                }
            };

            LoadFoods();
            StartLocationTracking();
        }

        private async Task SaveTourHistoryLocallyAsync(int tourId, decimal progressPercentage, string status)
        {
            try
            {
                int userId = Preferences.Default.Get("user_id", 0);
                if (userId == 0) return;

                var payload = new { UserId = userId, TourId = tourId, ProgressPercentage = progressPercentage, Status = status };
                using HttpClient client = new HttpClient();
                var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                await client.PostAsync($"{AppConfig.BaseUrl}/Tours/history", content);
            }
            catch (Exception ex) { Debug.WriteLine($"Tour History Save Error: {ex.Message}"); }
        }

        private void StartLocationTracking()
        {
            _locationTimer = Dispatcher.CreateTimer();
            _locationTimer.Interval = TimeSpan.FromSeconds(10);
            _locationTimer.Tick += async (s, e) => await ReportCurrentLocationAsync();
            _locationTimer.Start();
            
            // Immediate report on start
            _ = ReportCurrentLocationAsync();
        }

        private async Task ReportCurrentLocationAsync()
        {
            try
            {
                int userId = Preferences.Default.Get("user_id", 0);
                if (userId == 0) return;

                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                var location = await Geolocation.Default.GetLocationAsync(request);

                if (location != null)
                {
                    bool isListening = _isActuallySpeaking && _activeSession != null && !_activeSession.IsPaused;
                    int? currentPoiId = null;
                    if (isListening && !string.IsNullOrEmpty(_activeSession?.PoiId))
                    {
                        if (int.TryParse(_activeSession.PoiId, out int pid))
                        {
                            currentPoiId = pid;
                        }
                    }

                    var payload = new { 
                        user_id = userId, 
                        latitude = location.Latitude, 
                        longitude = location.Longitude,
                        is_listening = isListening,
                        poi_id = currentPoiId
                    };

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
                    }
                });
                await Task.Delay(130);
            }
            await MainThread.InvokeOnMainThreadAsync(() => {
                if (session.IsManual) {
                    mWave1.HeightRequest = 12; mWave2.HeightRequest = 18; mWave3.HeightRequest = 10;
                    mWave4.HeightRequest = 22; mWave5.HeightRequest = 14; mWave6.HeightRequest = 19;
                }
            });
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
            _ = ReportCurrentLocationAsync();
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
            _ = ReportCurrentLocationAsync();
        }

        private async void OnManualCloseClicked(object sender, EventArgs e)
        {
            _manualSession.IsActive = false;
            StopSpeech(fullReset: false, clearQueue: false);
            await SyncPlayerUI(null);
            if (mapView != null) await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
            _ = ReportCurrentLocationAsync();
        }



        public async void StopGlobalAudio()
        {
            StopSpeech(true);
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
            try
            {
                _isActuallySpeaking = false;
                _currentSentenceCts?.Cancel();
                _ttsCts?.Cancel(); 
                _pauseTcs?.TrySetResult(false);
                _audioStopwatch.Stop();
                
                // Hard reset the OS TTS engine to clear buffers (especially for non-default voices)
                _ = TextToSpeech.Default.SpeakAsync("", new SpeechOptions { Volume = 0 });

                if (fullReset)
                {
                    _manualSession.Reset();
                    _activeSession = null;
                }
            }
            catch (Exception ex) { Debug.WriteLine($"StopSpeech error: {ex.Message}"); }
            _ = ReportCurrentLocationAsync();
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
                var options = new SpeechOptions 
                { 
                    Locale = locale,
                    Pitch = AppConfig.AudioPitch
                };

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
                    if (!string.IsNullOrWhiteSpace(sentence)) 
                    {
                        // Some TTS engines don't handle cancellation perfectly, so we use the linked token
                        await TextToSpeech.Default.SpeakAsync(sentence, options, _currentSentenceCts.Token);
                    }
                    
                    if (_ttsCts.Token.IsCancellationRequested || !_isActuallySpeaking || _activeSession != session) break;
                    session.SentenceIndex++;
                }


            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Debug.WriteLine($"TTS Error: {ex.Message}"); }
            finally 
            { 
                _isActuallySpeaking = false; 
                _ttsSemaphore.Release(); 
                MainThread.BeginInvokeOnMainThread(async () => {
                    if (mapView != null) await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
                    _ = ReportCurrentLocationAsync();
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
                    });
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

            // 2. CÁC TÁC VỤ KHÁC (CÓ THỂ GÂY TRỄ)
            _ = Permissions.RequestAsync<Permissions.LocationWhenInUse>(); // Không await để không chặn UI
            
            if (_isMapLoaded) 
            { 
                string currentLang = _manualSession.Language?.Split('-')[0] ?? "vi";
                _ = LoadFoods(currentLang); 
                await TryOpenPendingDetail(); 
            }

            // Immediately report location when appearing
            _ = ReportCurrentLocationAsync();
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
            try {
                // Use HttpService with caching support
                _foodsJson = await HttpService.GetStringWithCacheAsync($"{AppConfig.FoodApiUrl}?lang={lang}", $"map_foods_{lang}");
                
                if (_foodsJson != null && _isMapLoaded) {
                    int userId = Preferences.Default.Get("user_id", 0);
                    await mapView.EvaluateJavaScriptAsync($"loadFoods({_foodsJson}, {userId});");
                    await TryOpenPendingDetail();
                }
            } catch (Exception ex) { Console.WriteLine($"LoadFoods Map error: {ex.Message}"); }
        }

        // --- TOUR UI & LOGIC ---

        private List<TourModel>? _tours = null;
        private TourDetailModel? _currentTour = null;

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            tourDrawerOverlay.IsVisible = true;
            await tourDrawer.TranslateTo(0, 0, 250, Easing.CubicOut);
            
            if (_tours == null)
            {
                await LoadToursAsync();
            }
        }

        private async void OnCloseDrawerClicked(object sender, EventArgs e)
        {
            await tourDrawer.TranslateTo(300, 0, 250, Easing.CubicIn);
            tourDrawerOverlay.IsVisible = false;
        }

        private async Task LoadToursAsync()
        {
            try
            {
                tourListContainer.Children.Clear();
                var loadingLabel = new Label { 
                    Text = "Đang tải danh sách Tour...", 
                    TextColor = Colors.Gray, 
                    FontAttributes = FontAttributes.Italic,
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 20)
                };
                tourListContainer.Children.Add(loadingLabel);

                var response = await HttpService.Client.GetAsync($"{AppConfig.BaseUrl}/Tours");
                
                // Clear again before results
                tourListContainer.Children.Clear();

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _tours = JsonSerializer.Deserialize<List<TourModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (_tours == null || !_tours.Any())
                    {
                        tourListContainer.Children.Add(new Label { Text = "Không có Tour nào", TextColor = Colors.Gray, HorizontalOptions = LayoutOptions.Center, Margin = 20 });
                        return;
                    }

                    foreach (var tour in _tours.OrderByDescending(t => t.Id))
                    {
                        var frame = new Border
                        {
                            StrokeThickness = 1,
                            Stroke = Color.FromArgb("#E0E0E0"),
                            BackgroundColor = Colors.White,
                            Padding = new Thickness(15),
                            Margin = new Thickness(0,0,0,10),
                            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(10) }
                        };
                        
                        var layout = new VerticalStackLayout { Spacing = 5 };
                        layout.Children.Add(new Label { Text = tour.Name, FontAttributes = FontAttributes.Bold, FontSize = 16, TextColor = Color.FromArgb("#333") });
                        layout.Children.Add(new Label { Text = (tour.Description?.Length > 60 ? tour.Description.Substring(0, 60) + "..." : tour.Description), FontSize = 13, TextColor = Colors.Gray });
                        layout.Children.Add(new Button 
                        { 
                            Text = "Xem Tour này", 
                            BackgroundColor = Color.FromArgb("#FF6B81"), 
                            TextColor = Colors.White, 
                            HeightRequest = 35, 
                            Margin = new Thickness(0,10,0,0),
                            CornerRadius = 8,
                            Command = new Command(async () => await StartTourModeAsync(tour.Id))
                        });

                        frame.Content = layout;
                        tourListContainer.Children.Add(frame);
                    }
                }
                else
                {
                    tourListContainer.Children.Add(new Label { Text = $"Lỗi kết nối: {(int)response.StatusCode}", TextColor = Colors.Red, HorizontalOptions = LayoutOptions.Center, Margin = 20 });
                }
            }
            catch (Exception ex)
            {
                tourListContainer.Children.Clear();
                tourListContainer.Children.Add(new Label { Text = "Lỗi đường truyền thiết bị", TextColor = Colors.Red, HorizontalOptions = LayoutOptions.Center, Margin = 20 });
                Debug.WriteLine($"LoadTours error: {ex.Message}");
            }
        }

        private async Task StartTourModeAsync(int tourId)
        {
            try
            {
                var json = await HttpService.GetStringAsync($"{AppConfig.BaseUrl}/Tours/{tourId}");
                if (json != null)
                {
                    _currentTour = JsonSerializer.Deserialize<TourDetailModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (_currentTour != null)
                    {
                        // Close drawer
                        await tourDrawer.TranslateTo(300, 0, 250, Easing.CubicIn);
                        tourDrawerOverlay.IsVisible = false;

                        // Setup UI
                        simTourName.Text = _currentTour.Name;
                        simTotalTime.Text = "Đang tính toán...";
                        
                        // Calculate total price string manually or via JS
                        simTotalPrice.Text = "Đang tính toán...";
                        simProgress.Text = "Chưa di chuyển";

                        tourSimulationPanel.IsVisible = true;
                        btnSimulateStart.IsVisible = true;
                        btnSimulateNext.IsVisible = false;

                        // Pass to JS to draw route and get total time
                        var jsArray = JsonSerializer.Serialize(_currentTour.TourPois);
                        await mapView.EvaluateJavaScriptAsync($"window.startTourRoute({jsArray})");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StartTourMode error: {ex.Message}");
            }
        }

        private async void OnSimulateStartClicked(object sender, EventArgs e)
        {
            btnSimulateStart.IsVisible = false;
            btnSimulateNext.IsVisible = true;
            if (mapView != null)
            {
                await mapView.EvaluateJavaScriptAsync("window.simulateTourNextStop()");
            }
        }

        private async void OnSimulateNextClicked(object sender, EventArgs e)
        {
            if (mapView != null)
            {
                await mapView.EvaluateJavaScriptAsync("window.simulateTourNextStop()");
            }
        }

        private async void OnEndTourClicked(object sender, EventArgs e)
        {
            _currentTour = null;
            tourSimulationPanel.IsVisible = false;

            if (mapView != null)
            {
                await mapView.EvaluateJavaScriptAsync("window.endTour()");
            }
        }

        // JS CALLABLE: update UI from JS
        public void UpdateTourProgressUI(string durationStr, string priceStr, string progressStr)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!string.IsNullOrEmpty(durationStr)) simTotalTime.Text = durationStr;
                if (!string.IsNullOrEmpty(priceStr)) simTotalPrice.Text = priceStr;
                if (!string.IsNullOrEmpty(progressStr)) simProgress.Text = progressStr;
            });
        }


    }

    public class FoodItem
    {
        public int id { get; set; }
        public string? name { get; set; }
    }

    public class TourModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class TourDetailModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<TourPoiModel> TourPois { get; set; } = new();
    }

    public class TourPoiModel
    {
        public int Id { get; set; }
        public int PoiId { get; set; }
        public int StayDurationMinutes { get; set; }
        public string ApproximatePrice { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
    }
}


