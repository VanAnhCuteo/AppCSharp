using System.Collections.Generic;
using FoodMapApp.Services;
using System.Net.Http.Json;
using FoodMapApp.Models;
using System.Web;
using System.Text.Json;
using System.Diagnostics;
using System.Threading;

namespace FoodMapApp.Views
{
    public partial class TourMapPage : ContentPage
    {
        private int _tourId;
        private TourModel? _tour;
        private int _currentStopIndex = 0;
        private Location? _userLocation;
        private bool _isJourneyStarted = false;
        private int _visitedCount = 0;

        // Audio Session for Tour
        private AudioSession _tourSession = new() { IsManual = true };
        private bool _isActuallySpeaking = false;
        private bool _isCleaningUp = false;
        private CancellationTokenSource? _ttsCts;
        private CancellationTokenSource? _currentSentenceCts;
        private TaskCompletionSource<bool>? _pauseTcs;
        private SemaphoreSlim _ttsSemaphore = new(1, 1);
        private Stopwatch _audioStopwatch = new();
        private IEnumerable<Locale>? _cachedLocales;

        public class AudioSession
        {
            public string PoiId { get; set; } = "";
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

        public TourMapPage(int tourId)
        {
            InitializeComponent();
            _tourId = tourId;

            tourMapView.Navigated += async (s, e) =>
            {
                if (e.Url.Contains("tour_map.html"))
                {
                    await tourMapView.EvaluateJavaScriptAsync($"platformApiBase = '{AppConfig.FoodApiUrl}';");
                    if (_tour != null)
                    {
                        await SendTourToMap();
                    }
                }
            };

            tourMapView.Navigating += async (s, e) =>
            {
                if (e.Url.StartsWith("app-tts://speak?"))
                {
                    e.Cancel = true;
                    var uri = new Uri(e.Url);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    string text = query["text"] ?? "";
                    string lang = query["lang"] ?? "vi-VN";
                    string id = query["id"] ?? "";

                    if (string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(id) && _tour?.pois != null)
                    {
                        var poi = _tour.pois.FirstOrDefault(p => p.poi_id.ToString() == id || p.id.ToString() == id);
                        if (poi != null && !string.IsNullOrEmpty(poi.description))
                        {
                            text = $"{poi.name}. {poi.description}";
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        // Stop any previous speech
                        StopSpeech(fullReset: false);
                        
                        _tourSession.PoiId = id;
                        _tourSession.Language = lang;
                        
                        // Normalize and split text into sentences like MainPage
                        string normalizedText = System.Text.RegularExpressions.Regex.Replace(text.Trim().ToLower(), @"\s+", " ");
                        _tourSession.Sentences = normalizedText.Split(new[] { '.', '!', '?', ';', ',', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(s => s.Trim())
                                                    .Where(s => s.Length > 0)
                                                    .ToArray();
                                                    
                        _tourSession.SentenceIndex = 0;
                        _tourSession.IsPaused = false;
                        _tourSession.IsActive = true;

                        _isCleaningUp = true;
                        await MainThread.InvokeOnMainThreadAsync(() => tourMiniPlayer.IsVisible = false);
                        await Task.Delay(100);
                        _isCleaningUp = false;
                        await SyncPlayerUI();

                        _ = SpeakWithChunksAsync();
                    }
                }
                else if (e.Url.StartsWith("app-tts://stop"))
                {
                    e.Cancel = true;
                    var uri = new Uri(e.Url);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    bool fullReset = (query["reset"] == "true");
                    
                    if (fullReset) {
                        StopSpeech(true);
                        await SyncPlayerUI();
                    } else {
                        _tourSession.IsPaused = true;
                        StopSpeech(false);
                        await SyncPlayerUI();
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
                        StopSpeech(true);
                        _tourSession.Language = lang;
                        await SyncPlayerUI();
                        await Task.Delay(200);
                        if (tourMapView != null) await tourMapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
                        await LoadTourDetails(lang);
                    }
                }
                else if (e.Url.StartsWith("app-request-reload://markers?"))
                {
                    e.Cancel = true;
                    var uri = new Uri(e.Url);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    string lang = query["lang"] ?? "vi";
                    
                    StopSpeech(true);
                    _tourSession.Language = lang;
                    await SyncPlayerUI();

                    await Task.Delay(200);
                    if (tourMapView != null) await tourMapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
                    
                    await LoadTourDetails(lang);
                }
            };

            tourMapView.Source = "tour_map.html";
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadTourDetails();
        }

        private string GetTtsLanguageCode(string lang)
        {
            if (string.IsNullOrEmpty(lang)) return "vi-VN";
            if (lang.ToLower() == "vi") return "vi-VN";
            if (lang.ToLower() == "en") return "en-US";
            if (lang.Contains("-")) return lang;
            return lang;
        }

        private async Task LoadTourDetails(string lang = "vi")
        {
            try
            {
                // Sync session language
                _tourSession.Language = GetTtsLanguageCode(lang);

                // Try to get current location
                try
                {
                    var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                        status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                    if (status == PermissionStatus.Granted)
                    {
                        var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                        _userLocation = await Geolocation.Default.GetLocationAsync(request);
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Location Error: {ex.Message}"); }

                // Replacement for: using HttpClient client = new HttpClient();
                // _tour = await client.GetFromJsonAsync<TourModel>($"{AppConfig.TourApiUrl}/{_tourId}?lang={lang}");
                
                _tour = await HttpService.GetWithCacheAsync<TourModel>($"{AppConfig.TourApiUrl}/{_tourId}?lang={lang}", $"tour_details_{_tourId}_{lang}");
                
                if (_tour != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() => {
                        currentStopNameLabel.Text = _tour.name;
                        stopCountLabel.Text = $"{_tour.pois?.Count ?? 0} quán";
                        UpdateStopInfo();
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", "Không thể tải chi tiết tour. " + ex.Message, "OK");
            }
        }

        private async void UpdateStopInfo()
        {
            if (_tour?.pois == null || _tour.pois.Count == 0) return;

            if (!_isJourneyStarted)
            {
                statusTitleLabel.Text = "BẢN ĐỒ TOUR";
                currentStopNameLabel.Text = _tour.name;
                actionButton.Text = "Bắt đầu hành trình";
            }
            else
            {
                var currentPoi = _tour.pois[_currentStopIndex];
                statusTitleLabel.Text = $"ĐỊA ĐIỂM {_visitedCount}";
                currentStopNameLabel.Text = currentPoi.name;
                actionButton.Text = _visitedCount < _tour.pois.Count ? "Đến địa điểm tiếp theo" : "Hoàn thành hành trình";
                
                await tourMapView.EvaluateJavaScriptAsync($"focusStop({_currentStopIndex})");
            }

            await SendTourToMap();
        }

        private async Task SendTourToMap()
        {
            if (_tour?.pois == null) return;
            var json = System.Text.Json.JsonSerializer.Serialize(_tour.pois);
            string userLocParams = _userLocation != null 
                ? $"{_userLocation.Latitude}, {_userLocation.Longitude}" 
                : "null, null";
            
            // Pass the state: isJourneyStarted (0/1)
            int journeyState = _isJourneyStarted ? 1 : 0;
            await tourMapView.EvaluateJavaScriptAsync($"platformApiBase = '{AppConfig.FoodApiUrl}';");
            await tourMapView.EvaluateJavaScriptAsync($"loadTourRoute({json}, {userLocParams}, {_currentStopIndex}, {journeyState})");
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void OnNextStopClicked(object sender, EventArgs e)
        {
            if (_tour?.pois == null || _tour.pois.Count == 0) return;

            if (!_isJourneyStarted)
            {
                _currentStopIndex = FindNearestPoiIndex();
                _isJourneyStarted = true;
                _visitedCount = 1;
                UpdateStopInfo();
            }
            else
            {
                if (_visitedCount >= _tour.pois.Count)
                {
                    FinishTour();
                    return;
                }

                // GIẢ LẬP: Coi như người dùng đã đến quán hiện tại.
                // Khi bấm "Tiếp theo", vị trí xuất phát cho chặng kế tiếp sẽ là quán vừa ở.
                var justVisitedPoi = _tour.pois[_currentStopIndex];
                if (justVisitedPoi != null)
                {
                    _userLocation = new Location(justVisitedPoi.latitude, justVisitedPoi.longitude);
                }

                _currentStopIndex = (_currentStopIndex + 1) % _tour.pois.Count;
                _visitedCount++;
                UpdateStopInfo();
            }
        }

        private int FindNearestPoiIndex()
        {
            if (_tour?.pois == null || _userLocation == null) return 0;
            
            double minDistance = double.MaxValue;
            int nearestIndex = 0;

            for (int i = 0; i < _tour.pois.Count; i++)
            {
                var poi = _tour.pois[i];
                double dist = Location.CalculateDistance(_userLocation.Latitude, _userLocation.Longitude, poi.latitude, poi.longitude, DistanceUnits.Kilometers);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestIndex = i;
                }
            }
            return nearestIndex;
        }

        private async void FinishTour()
        {
            await DisplayAlert("Chúc mừng!", "Bạn đã hoàn thành toàn bộ hành trình tour.", "Tuyệt vời");
            await Navigation.PopAsync();
        }
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            StopSpeech(true);
        }

        private async Task SyncPlayerUI()
        {
            try {
                await MainThread.InvokeOnMainThreadAsync(() => {
                    if (_isCleaningUp) return;
                    
                    tourMiniPlayer.IsVisible = _tourSession.IsActive;
                    if (_tourSession.IsActive) {
                        playIcon.IsVisible = _tourSession.IsPaused;
                        pauseIcon.IsVisible = !_tourSession.IsPaused;
                        
                        // Set shop name
                        var poi = _tour?.pois?.FirstOrDefault(p => p.id.ToString() == _tourSession.PoiId || p.poi_id.ToString() == _tourSession.PoiId);
                        tourShopLabel.Text = poi?.name ?? "...";
                    }
                });
            } catch (Exception ex) { Debug.WriteLine($"SyncUI error: {ex.Message}"); }
        }

        private void StopSpeech(bool fullReset = false)
        {
            try {
                _isActuallySpeaking = false;
                _currentSentenceCts?.Cancel();
                _ttsCts?.Cancel(); 
                _pauseTcs?.TrySetResult(false);
                _audioStopwatch.Stop();
                
                // Hard reset the OS TTS engine to clear buffers
                _ = TextToSpeech.Default.SpeakAsync("", new SpeechOptions { Volume = 0 });

                if (fullReset) _tourSession.Reset();
            }
            catch (Exception ex) { Debug.WriteLine($"Tour StopSpeech error: {ex.Message}"); }
        }

        private async Task SpeakWithChunksAsync()
        {
            if (!await _ttsSemaphore.WaitAsync(500)) return;
            _isActuallySpeaking = true;
            _ttsCts = new CancellationTokenSource();
            _audioStopwatch.Restart();

            try {
                var sentences = _tourSession.Sentences;
                if (sentences == null || sentences.Length == 0) return;

                if (_cachedLocales == null) _cachedLocales = await TextToSpeech.Default.GetLocalesAsync();
                var locale = _cachedLocales?.FirstOrDefault(l => l.Language.Equals(_tourSession.Language, StringComparison.OrdinalIgnoreCase)) ??
                             _cachedLocales?.FirstOrDefault(l => l.Language.ToLower().StartsWith(_tourSession.Language.Split('-')[0].ToLower()));
                var options = new SpeechOptions { Locale = locale };

                _ = AnimateVisualizer();

                while (_tourSession.SentenceIndex < sentences.Length) {
                    if (_ttsCts.Token.IsCancellationRequested || !_isActuallySpeaking) break;

                    int index = _tourSession.SentenceIndex;
                    await tourMapView.EvaluateJavaScriptAsync($"if(window.onTtsProgress) window.onTtsProgress({index}, {sentences.Length});");

                    if (_isActuallySpeaking && _tourSession.IsPaused) {
                        _pauseTcs = new TaskCompletionSource<bool>();
                        bool resume = await _pauseTcs.Task;
                        if (!resume || _ttsCts.Token.IsCancellationRequested || !_isActuallySpeaking) break;
                    }

                    if (_ttsCts.Token.IsCancellationRequested || !_isActuallySpeaking) break;
                    
                    _currentSentenceCts = CancellationTokenSource.CreateLinkedTokenSource(_ttsCts.Token);
                    string sentence = sentences[index];
                    if (!string.IsNullOrWhiteSpace(sentence)) await TextToSpeech.Default.SpeakAsync(sentence, options, _currentSentenceCts.Token);
                    
                    if (_ttsCts.Token.IsCancellationRequested || !_isActuallySpeaking) break;
                    _tourSession.SentenceIndex++;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Debug.WriteLine($"Tour TTS Error: {ex.Message}"); }
            finally { 
                _isActuallySpeaking = false; 
                _ttsSemaphore.Release(); 
                MainThread.BeginInvokeOnMainThread(async () => {
                    if (tourMapView != null) await tourMapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
                });
            }

            if (_isCleaningUp) return;

            if (_tourSession.SentenceIndex >= _tourSession.Sentences.Length) {
                _tourSession.IsActive = false;
                await SyncPlayerUI();
                await MainThread.InvokeOnMainThreadAsync(async () => {
                   if (tourMapView != null) await tourMapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
                });
            }
        }

        private async Task AnimateVisualizer()
        {
            Random rnd = new Random();
            while (_isActuallySpeaking && !_tourSession.IsPaused && _tourSession.IsActive) {
                await MainThread.InvokeOnMainThreadAsync(() => {
                    wave1.HeightRequest = rnd.Next(8, 20); wave2.HeightRequest = rnd.Next(10, 25);
                    wave3.HeightRequest = rnd.Next(5, 15); wave4.HeightRequest = rnd.Next(12, 28);
                    wave5.HeightRequest = rnd.Next(8, 22); wave6.HeightRequest = rnd.Next(10, 24);
                });
                await Task.Delay(130);
            }
            await MainThread.InvokeOnMainThreadAsync(() => {
                wave1.HeightRequest = 12; wave2.HeightRequest = 18; wave3.HeightRequest = 10;
                wave4.HeightRequest = 22; wave5.HeightRequest = 14; wave6.HeightRequest = 19;
            });
        }

        private async void OnPlayAudioClicked(object sender, EventArgs e)
        {
            _tourSession.IsPaused = !_tourSession.IsPaused;
            await SyncPlayerUI();
            if (!_tourSession.IsPaused) {
                if (!_isActuallySpeaking) _ = SpeakWithChunksAsync();
                else _pauseTcs?.TrySetResult(true);
            } else _pauseTcs?.TrySetResult(false);
        }

        private async void OnReplayAudioClicked(object sender, EventArgs e)
        {
            _tourSession.SentenceIndex = 0; _tourSession.IsPaused = false;
            await SyncPlayerUI();
            if (!_isActuallySpeaking) _ = SpeakWithChunksAsync();
            else _pauseTcs?.TrySetResult(true);
        }

        private async void OnClosePlayerClicked(object sender, EventArgs e)
        {
            StopSpeech(true);
            await SyncPlayerUI();
            if (tourMapView != null) await tourMapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
        }
    }
}
