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

        // Automatic Geofencing Session
        private static string _autoPoiId = "";
        private static string _autoSpokenText = "";
        private static string[] _autoSentences = Array.Empty<string>();
        private static int _autoSentenceIndex = 0;
        private static bool _isAutoPaused = true;

        // Manual Selection Session
        private static string _manualPoiId = "";
        private static string _manualSpokenText = "";
        private static string[] _manualSentences = Array.Empty<string>();
        private static int _manualSentenceIndex = 0;
        private static bool _isManualActive = false;
        private static bool _isManualPaused = false;

        private static string _autoLang = "vi-VN";
        private static string _manualLang = "vi-VN";
        private static bool _isActuallySpeaking = false; 
        private static bool _isCleaningUp = false; 
        private static TaskCompletionSource<bool>? _pauseTcs;
        private static Queue<(string Text, string Id, string Lang, int StartIndex)> _audioQueue = new();

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

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        string normalizedText = NormalizeText(text);
                        
                        if (isManual)
                        {
                            _manualLang = lang; // Store manual language
                            // 1. NGƯỢC LẠI: Mở audio thủ công thì "Tạm ẩn" audio tự động
                            if (_isActuallySpeaking && !_isManualActive)
                            {
                                StopSpeech(fullReset: false, clearQueue: false); 
                            }

                            _isManualActive = true;
                            _isManualPaused = false;
                            
                            _isCleaningUp = true;
                            StopSpeech(fullReset: false, clearQueue: false); 
                            await MainThread.InvokeOnMainThreadAsync(() => {
                                miniPlayer.IsVisible = false; // Ẩn thanh Auto
                            });

                            await ShowMiniPlayer(id, true); // HIỆN THANH THỦ CÔNG - QUAN TRỌNG
                            
                            await Task.Delay(100);
                            _isCleaningUp = false;

                            StartNewManualSpeech(text, id, normalizedText, lang);
                        }
                        else
                        {
                            // 2. NGƯỢC LẠI: Mở audio tự động (GPS)
                            if (_isManualActive)
                            {
                                // Nếu ID tự động đang trống, hãy nạp nó vào làm quán đang chờ sẵn
                                if (string.IsNullOrEmpty(_autoPoiId))
                                {
                                    _autoPoiId = id;
                                    _autoSentences = normalizedText.Split(new[] { '.', '!', '?', ';', ',', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                                            .Select(s => s.Trim())
                                                            .Where(s => s.Length > 0)
                                                            .ToArray();
                                    _autoSentenceIndex = 0;
                                    _isAutoPaused = true;
                                }
                                else if (_autoPoiId != id)
                                {
                                    // Đưa vào hàng chờ bổ sung
                                    _audioQueue.Enqueue((normalizedText, id, lang, 0));
                                }
                                return;
                            }

                            if (string.IsNullOrEmpty(_autoPoiId))
                            {
                                _autoPoiId = id;
                                _autoLang = lang; // Store auto language
                                _autoSentences = normalizedText.Split(new[] { '.', '!', '?', ';', ',', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                                        .Select(s => s.Trim())
                                                        .Where(s => s.Length > 0)
                                                        .ToArray();
                                _autoSentenceIndex = 0;
                                _isAutoPaused = false;
                                await ShowMiniPlayer(id, false); 
                                _ = SpeakWithChunksAsync(lang, isManual: false);
                            }
                            else
                            {
                                _audioQueue.Enqueue((normalizedText, id, lang, 0));
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
                        manualMiniPlayer.IsVisible = false;
                    }
                    else
                    {
                        if (_isManualActive)
                        {
                            _isManualPaused = true;
                            mPlayIcon.IsVisible = true;
                            mPauseIcon.IsVisible = false;
                        }
                        else
                        {
                            _isAutoPaused = true;
                            playIcon.IsVisible = true;
                            pauseIcon.IsVisible = false;
                        }
                        _currentSentenceCts?.Cancel();
                    }
                }
                else if (e.Url.StartsWith("app-request-reload://markers?"))
                {
                    e.Cancel = true;
                    var uri = new Uri(e.Url);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    string lang = query["lang"] ?? "vi";
                    
                    // NGỪNG NGAY LẬP TỨC AUDIO CŨ KHI CHUYỂN NGÔN NGỮ
                    // Nếu đang nghe thủ công, ta chỉ "dừng phát" chứ không xoá hàng đợi tự động
                    StopSpeech(fullReset: !_isManualActive, clearQueue: !_isManualActive);
                    
                    _autoLang = lang;
                    _manualLang = lang;

                    miniPlayer.IsVisible = false;
                    manualMiniPlayer.IsVisible = false;
                    
                    // Cập nhật lại tên biến loa bên JS
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

        private async Task ShowMiniPlayer(string poiId, bool isManual, bool startPaused = false)
        {
            try
            {
                string name = "Quán ăn";
                if (!string.IsNullOrEmpty(_foodsJson))
                {
                    var foods = JsonSerializer.Deserialize<List<JsonElement>>(_foodsJson);
                    var food = foods.FirstOrDefault(f => f.GetProperty("id").GetInt32().ToString() == poiId);
                    if (food.ValueKind != JsonValueKind.Undefined)
                    {
                        name = food.GetProperty("name").GetString() ?? "Quán ăn";
                    }
                }

                await MainThread.InvokeOnMainThreadAsync(() => {
                    if (isManual)
                    {
                        manualMiniPlayer.IsVisible = true;
                        mPlayIcon.IsVisible = startPaused;
                        mPauseIcon.IsVisible = !startPaused;
                        manualShopLabel.Text = name;
                    }
                    else
                    {
                        miniPlayer.IsVisible = true; 
                        playIcon.IsVisible = startPaused;
                        pauseIcon.IsVisible = !startPaused;
                        detectedShopLabel.Text = name;
                    }
                });
            }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: Error showing miniplayer: {ex.Message}"); }
        }

        private async Task AnimateVisualizer(bool isManual)
        {
            Random rnd = new Random();
            bool isAnimating = true;
            while (isAnimating)
            {
                if (isManual)
                    isAnimating = _isActuallySpeaking && _isManualActive && !mPlayIcon.IsVisible && manualMiniPlayer.IsVisible;
                else
                    isAnimating = _isActuallySpeaking && !_isManualActive && !playIcon.IsVisible && miniPlayer.IsVisible;

                if (!isAnimating) break;

                await MainThread.InvokeOnMainThreadAsync(() => {
                    if (isManual) {
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
                if (isManual) {
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
            // 1. Dừng ngay lập tức âm thanh hiện tại khi bấm X
            StopSpeech(fullReset: false, clearQueue: false);
            
            // Cập nhật giao diện loa trong webview ngay
            if (mapView != null) await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");

            if (_audioQueue.Count > 0)
            {
                bool shouldContinue = await DisplayAlert("Thông báo", "Bạn muốn nghe quán tiếp theo hay hủy bỏ?", "Nghe", "Hủy bỏ");
                if (shouldContinue)
                {
                    if (_audioQueue.TryDequeue(out var next))
                    {
                        // 2. Chờ một chút để luồng cũ giải phóng Semaphore hoàn toàn
                        await Task.Delay(200);

                        _autoPoiId = next.Id;
                        _autoSentences = next.Text.Split(new[] { '.', '!', '?', ';', ',', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
                        _autoSentenceIndex = 0;
                        _isAutoPaused = false;
                        
                        await ShowMiniPlayer(next.Id, false);
                        if (!_isManualActive) _ = SpeakWithChunksAsync(next.Lang, isManual: false);
                        return;
                    }
                }
            }

            _autoPoiId = "";
            _audioQueue.Clear();
            if (!_isManualActive) StopSpeech(true);
            miniPlayer.IsVisible = false;
        }

        private void OnPlayAudioClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_autoPoiId)) return;
            _isAutoPaused = !_isAutoPaused;
            if (!_isAutoPaused)
            {
                playIcon.IsVisible = false;
                pauseIcon.IsVisible = true;
                if (_isManualActive) { _isManualActive = false; StopSpeech(false, false); manualMiniPlayer.IsVisible = false; }
                if (!_isActuallySpeaking) _ = SpeakWithChunksAsync(_autoLang, isManual: false);
                else _pauseTcs?.TrySetResult(true);
            }
            else { playIcon.IsVisible = true; pauseIcon.IsVisible = false; _pauseTcs?.TrySetResult(false); }
        }

        private void OnReplayAudioClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_autoPoiId)) return;
            _autoSentenceIndex = 0; _isAutoPaused = false;
            playIcon.IsVisible = false; pauseIcon.IsVisible = true;
            if (_isManualActive) { _isManualActive = false; StopSpeech(false, false); manualMiniPlayer.IsVisible = false; }
            if (!_isActuallySpeaking) _ = SpeakWithChunksAsync(_autoLang, isManual: false);
            else _pauseTcs?.TrySetResult(true);
        }

        private void OnManualPlayClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_manualPoiId)) return;
            _isManualPaused = !_isManualPaused;
            if (!_isManualPaused)
            {
                mPlayIcon.IsVisible = false; mPauseIcon.IsVisible = true;
                // Dừng auto nếu đang phát
                if (!_isManualActive && _isActuallySpeaking) StopSpeech(false, false);
                _isManualActive = true;
                if (!_isActuallySpeaking) _ = SpeakWithChunksAsync(_manualLang, isManual: true);
                else _pauseTcs?.TrySetResult(true);
            }
            else { mPlayIcon.IsVisible = true; mPauseIcon.IsVisible = false; _pauseTcs?.TrySetResult(false); }
        }

        private void OnManualReplayClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_manualPoiId)) return;
            _manualSentenceIndex = 0; _isManualPaused = false;
            mPlayIcon.IsVisible = false; mPauseIcon.IsVisible = true;
            if (!_isManualActive && _isActuallySpeaking) StopSpeech(false, false);
            _isManualActive = true;
            if (!_isActuallySpeaking) _ = SpeakWithChunksAsync(_manualLang, isManual: true);
            else _pauseTcs?.TrySetResult(true);
        }

        private async void OnManualCloseClicked(object sender, EventArgs e)
        {
            _isManualActive = false;
            _isManualPaused = false;
            StopSpeech(fullReset: false, clearQueue: false); // Chỉ dừng speech thủ công
            
            // Ẩn thanh thủ công ngay lập tức
            manualMiniPlayer.IsVisible = false;
            
            // Dừng hình loa đập trong trang chi tiết
            if (mapView != null) await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
            
            // KIỂM TRA HÀNG ĐỢI HOẶC AUDIO TỰ ĐỘNG BỊ TẠM DỪNG
            if (!string.IsNullOrEmpty(_autoPoiId) || _audioQueue.Count > 0)
            {
                bool shouldResume = await DisplayAlert("Thông báo", "Bạn có muốn nghe audio ở gần không?", "OK", "Hủy");
                
                if (shouldResume)
                {
                    // Nếu _autoPoiId đang trống nhưng hàng đợi có, lấy quán tiếp theo
                    if (string.IsNullOrEmpty(_autoPoiId) && _audioQueue.Count > 0)
                    {
                        if (_audioQueue.TryDequeue(out var next))
                        {
                            _autoPoiId = next.Id;
                            _autoLang = next.Lang;
                            _autoSentences = next.Text.Split(new[] { '.', '!', '?', ';', ',', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
                            _autoSentenceIndex = 0;
                        }
                    }

                    if (!string.IsNullOrEmpty(_autoPoiId))
                    {
                        _isAutoPaused = true;
                        miniPlayer.IsVisible = true;
                        playIcon.IsVisible = true;
                        pauseIcon.IsVisible = false;
                        
                        // Đảm bảo tên quán tự động vẫn đúng
                        if (_foodsJson != null) {
                            try {
                                var foods = JsonSerializer.Deserialize<List<FoodItem>>(_foodsJson);
                                var currentFood = foods?.FirstOrDefault(f => f.id.ToString() == _autoPoiId);
                                if (currentFood != null) detectedShopLabel.Text = currentFood.name;
                            } catch { }
                        }
                    }
                }
                else
                {
                    // Nếu hủy, xóa audio tự động hiện tại và hàng đợi để không làm phiền
                    _autoPoiId = "";
                    _audioQueue.Clear();
                    miniPlayer.IsVisible = false;
                }
            }
        }

        private async void OnDetailClicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_autoPoiId))
                await mapView.EvaluateJavaScriptAsync($"openDetails({_autoPoiId})");
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
            _currentSentenceCts?.Cancel();
            _ttsCts?.Cancel(); // Hủy toàn bộ chuỗi speech đang chạy
            _pauseTcs?.TrySetResult(false);
            _audioStopwatch.Stop();
            
            if (fullReset)
            {
                if (clearQueue) _audioQueue.Clear(); 
                _autoPoiId = ""; _autoSentenceIndex = 0; _isAutoPaused = true;
                _manualPoiId = ""; _manualSentenceIndex = 0; _isManualActive = false; _isManualPaused = false;
            }
        }

        private void StartNewManualSpeech(string text, string id, string normalizedText, string lang)
        {
            _manualPoiId = id;
            _manualSentences = normalizedText.Split(new[] { '.', '!', '?', ';', ',', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
            _manualSentenceIndex = 0;
            _ = SpeakWithChunksAsync(lang, isManual: true);
        }

        private async Task SpeakWithChunksAsync(string lang, bool isManual)
        {
            if (!await _ttsSemaphore.WaitAsync(0)) return;
            _isActuallySpeaking = true;
            _ttsCts = new CancellationTokenSource();
            _audioStopwatch.Restart();

            try
            {
                // Capture local state to prevent "zombie tasks" from using new ID/sentences
                var currentId = isManual ? _manualPoiId : _autoPoiId;
                var sentences = isManual ? _manualSentences : _autoSentences;
                
                if (sentences == null || sentences.Length == 0) return;

                if (_cachedLocales == null) _cachedLocales = await TextToSpeech.Default.GetLocalesAsync();
                var locale = _cachedLocales?.FirstOrDefault(l => l.Language.Equals(lang, StringComparison.OrdinalIgnoreCase)) ??
                             _cachedLocales?.FirstOrDefault(l => l.Language.ToLower().StartsWith(lang.Split('-')[0].ToLower()));
                var options = new SpeechOptions { Locale = locale };

                _ = AnimateVisualizer(isManual);

                while ((isManual ? _manualSentenceIndex : _autoSentenceIndex) < sentences.Length)
                {
                    int index = isManual ? _manualSentenceIndex : _autoSentenceIndex;
                    
                    // Đảm bảo vẫn đang trong cùng 1 phiên (ID không đổi)
                    var activeId = isManual ? _manualPoiId : _autoPoiId;
                    if (activeId != currentId || _ttsCts.Token.IsCancellationRequested) break;

                    if (isManual) await mapView.EvaluateJavaScriptAsync($"if(window.onTtsProgress) window.onTtsProgress({index}, {sentences.Length});");

                    if (_isActuallySpeaking && ((isManual && _isManualPaused) || (!isManual && _isAutoPaused)))
                    {
                        _pauseTcs = new TaskCompletionSource<bool>();
                        bool resume = await _pauseTcs.Task;
                        if (!resume || _ttsCts.Token.IsCancellationRequested) break;
                    }

                    if (_ttsCts.Token.IsCancellationRequested) break;
                    
                    _currentSentenceCts = CancellationTokenSource.CreateLinkedTokenSource(_ttsCts.Token);
                    string sentence = sentences[index];
                    if (!string.IsNullOrWhiteSpace(sentence)) await TextToSpeech.Default.SpeakAsync(sentence, options, _currentSentenceCts.Token);
                    
                    // Kiểm tra lại sau khi nói xong 1 câu
                    if (_ttsCts.Token.IsCancellationRequested) break;

                    if (isManual) _manualSentenceIndex++; else _autoSentenceIndex++;
                }
                if (!isManual && _autoSentenceIndex >= sentences.Length) _ = SendAudioLogAsync(_autoPoiId, (int)_audioStopwatch.Elapsed.TotalSeconds);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Debug.WriteLine($"TTS Error: {ex.Message}"); }
            finally 
            { 
                _isActuallySpeaking = false; 
                _ttsSemaphore.Release(); 
                // LUÔN LUÔN thông báo cho JS khi kết thúc (dù là xong hay là bị ngắt quãng)
                MainThread.BeginInvokeOnMainThread(async () => {
                    if (mapView != null) await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
                });
            }

            if (_isCleaningUp) return;
            if (isManual) { 
                if (_manualSentenceIndex >= _manualSentences.Length) { 
                    _isManualActive = false; 
                    await MainThread.InvokeOnMainThreadAsync(() => {
                        manualMiniPlayer.IsVisible = false;
                        // Hồi phục thanh tự động
                        if (!string.IsNullOrEmpty(_autoPoiId)) {
                            _isAutoPaused = true;
                            miniPlayer.IsVisible = true;
                            playIcon.IsVisible = true;
                            pauseIcon.IsVisible = false;
                            
                            // Đảm bảo tên quán tự động vẫn đúng khi hiện lại
                            if (_foodsJson != null) {
                                try {
                                    var foods = JsonSerializer.Deserialize<List<FoodItem>>(_foodsJson);
                                    var currentFood = foods?.FirstOrDefault(f => f.id.ToString() == _autoPoiId);
                                    if (currentFood != null) detectedShopLabel.Text = currentFood.name;
                                } catch { }
                            }
                        }
                    });
                    await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();"); 
                } 
            }
            else {
                if (_autoSentenceIndex >= _autoSentences.Length) {
                    if (_audioQueue.Count > 0) {
                        if (_audioQueue.TryDequeue(out var next)) {
                            _autoPoiId = next.Id;
                            _autoSentences = next.Text.Split(new[] { '.', '!', '?', ';', ',', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
                            _autoSentenceIndex = 0; _isAutoPaused = false; await ShowMiniPlayer(next.Id, false);
                            _ = SpeakWithChunksAsync(next.Lang, isManual: false);
                        }
                    }
                    else { playIcon.IsVisible = true; pauseIcon.IsVisible = false; }
                }
            }
        }
        public void HandleSystemInterruption()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_isActuallySpeaking)
                {
                    StopSpeech(fullReset: false, clearQueue: false);
                    if (_isManualActive)
                    {
                        _isManualPaused = true;
                        mPlayIcon.IsVisible = true;
                        mPauseIcon.IsVisible = false;
                    }
                    else
                    {
                        _isAutoPaused = true;
                        playIcon.IsVisible = true;
                        pauseIcon.IsVisible = false;
                    }
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

            // 1. KHÔI PHỤC GIAO DIỆN AUDIO NGAY LẬP TỨC (TRƯỚC KHI LÀM VIỆC KHÁC)
            // Ưu tiên session Thủ công nếu đang active
            if (_isManualActive && !string.IsNullOrEmpty(_manualPoiId))
            {
                _isManualPaused = true;
                _ = ShowMiniPlayer(_manualPoiId, true, startPaused: true);
            }
            // Nếu không thì khôi phục session Tự động (GPS)
            else if (!string.IsNullOrEmpty(_autoPoiId))
            {
                _isAutoPaused = true;
                _ = ShowMiniPlayer(_autoPoiId, false, startPaused: true);
            }
            // Trường hợp hàng đợi có quán tiếp theo
            else if (_audioQueue.Count > 0)
            {
                if (_audioQueue.TryPeek(out var next))
                {
                    _isAutoPaused = true;
                    _ = ShowMiniPlayer(next.Id, false, startPaused: true);
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
            
            // Khi rời trang bản đồ, tạm dừng đọc để tiết kiệm pin/tài nguyên
            // nhưng TUYỆT ĐỐI KHÔNG reset session (_autoPoiId, _manualPoiId, _audioQueue)
            if (_isActuallySpeaking)
            {
                StopSpeech(fullReset: false, clearQueue: false);
                
                if (_isManualActive) _isManualPaused = true;
                else _isAutoPaused = true;
            }

            // Đảm bảo icon hiển thị đúng trạng thái "Pause" (Sẵn sàng Play) kể cả khi vừa tắt/đang đợi
            MainThread.BeginInvokeOnMainThread(() => {
                mPlayIcon.IsVisible = true;
                mPauseIcon.IsVisible = false;
                playIcon.IsVisible = true;
                pauseIcon.IsVisible = false;
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
