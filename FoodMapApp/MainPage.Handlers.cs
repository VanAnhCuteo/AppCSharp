using System.Text.Json;
using System.Web;
using FoodMapApp.Services;
using FoodMapApp.Models;

namespace FoodMapApp
{
    public partial class MainPage
    {
        private async void SetupWebViewHandlers()
        {
            mapView.Navigated += OnWebViewNavigated;
            mapView.Navigating += OnWebViewNavigating;

            // Initial Sync with AutoAudioService
            AutoAudioService.Instance.OnStateChanged += (current, queue) => {
                MainThread.BeginInvokeOnMainThread(async () => {
                    if (mapView != null) {
                        string queueIdsJson = JsonSerializer.Serialize(queue.Select(q => q.Poi.id).ToList());
                        int currentId = current?.Poi.id ?? 0;
                        await mapView.EvaluateJavaScriptAsync($"if(window.syncAudioQueue) window.syncAudioQueue({queueIdsJson}, {currentId});");
                    }
                    // Only clear _activeSession if it's truly not active anymore
                    // Don't clear a paused session that user may want to resume
                    if (current == null && !_manualSession.IsActive && (_activeSession == null || !_activeSession.IsActive)) {
                        _activeSession = null;
                        await SyncPlayerUI(null);
                    }
                    else if (_activeSession != null || _manualSession.IsActive) {
                        await SyncPlayerUI(_activeSession ?? _manualSession);
                    }
                });
            };
        }

        private async void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
        {
            if (!e.Url.Contains("map.html")) return;
            _isMapLoaded = true;
            await mapView.EvaluateJavaScriptAsync($"platformApiBase = '{AppConfig.FoodApiUrl}';");
            if (_foodsJson != null)
            {
                int userId = Preferences.Default.Get("user_id", 0);
                var uiSet = await GetUISetAsync(LocalizationService.Instance.CurrentLanguage);
                string uiJson = JsonSerializer.Serialize(uiSet);
                await mapView.EvaluateJavaScriptAsync($"if(window.setUiTranslations) window.setUiTranslations({uiJson});");
                string currentCode = LocalizationService.Instance.CurrentLanguage;
                string currentName = LocalizationService.Instance.GetLanguageName(currentCode);
                await mapView.EvaluateJavaScriptAsync($"if(window.updateAppLanguage) window.updateAppLanguage('{currentCode}', '{currentName?.Replace("'", "\\'")}');");
                await mapView.EvaluateJavaScriptAsync($"loadFoods({_foodsJson}, {userId});");
                await TryOpenPendingDetail();
                await TryStartPendingRoute();
            }
        }

        private async void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
        {
            if (e.Url.StartsWith("app-tts://speak?")) {
                e.Cancel = true;
                var query = HttpUtility.ParseQueryString(new Uri(e.Url).Query);
                string text = query["text"] ?? "", id = query["id"] ?? "", lang = query["lang"] ?? "vi-VN";
                bool isManual = (query["manual"] == "true" || query["isManual"] == "true");
                if (!string.IsNullOrWhiteSpace(text)) {
                    var sentences = NormalizeText(text).Split(new[] { '.', '!', '?', ';', ',', '。', '！', '？' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
                    if (isManual) {
                        // Case 1: Nhớ trạng thái auto trước khi chuyển sang thủ công
                        _hadAutoBeforeManual = AutoAudioService.Instance.CurrentItem != null
                                            || AutoAudioService.Instance.Queue.Count > 0;

                        // Tạm dừng auto audio service
                        AutoAudioService.Instance.SetPaused(true);

                        StopSpeech(false, false);
                        _manualSession.PoiId = id; _manualSession.Language = lang; _manualSession.Sentences = sentences;
                        _manualSession.SentenceIndex = 0; _manualSession.TotalSentences = sentences.Length;
                        _manualSession.IsPaused = false; _manualSession.IsActive = true; _activeSession = _manualSession;
                        _isCleaningUp = true;
                        await MainThread.InvokeOnMainThreadAsync(() => manualMiniPlayer.IsVisible = false);
                        await SyncPlayerUI(_manualSession);
                        await Task.Delay(100);
                        _isCleaningUp = false;
                        _ = SpeakWithChunksAsync(_manualSession);
                    }
                }
            }
            else if (e.Url.StartsWith("app-tts://stop")) {
                e.Cancel = true;
                var query = HttpUtility.ParseQueryString(new Uri(e.Url).Query);
                if (query["reset"] == "true") StopGlobalAudio();
                else if (_activeSession != null) {
                    _activeSession.IsPaused = true;
                    StopSpeech(false, false);
                    await SyncPlayerUI(_activeSession);
                }
            }
            else if (e.Url.StartsWith("app-ui://alert?")) {
                e.Cancel = true;
                await DisplayAlert(LocalizationService.Instance.Get("title"), HttpUtility.ParseQueryString(new Uri(e.Url).Query)["message"] ?? "", "OK");
            }
            else if (e.Url.StartsWith("app-request-confirm://lang-switch?")) {
                e.Cancel = true;
                var query = HttpUtility.ParseQueryString(new Uri(e.Url).Query);
                string lang = query["lang"] ?? "vi", name = query["name"] ?? "";

                // Case 2+3: Xác định trạng thái audio hiện tại
                bool isManualActive = _manualSession.IsActive;
                bool isAutoActive = _activeSession != null && !_activeSession.IsManual && _activeSession.IsActive;

                var uiSet = await GetUISetAsync(_manualSession.Language ?? "vi");

                if (await DisplayAlert(uiSet["title"], uiSet["msg_audio"], uiSet["ok"], uiSet["cancel"]))
                {
                    // Dừng audio hiện tại
                    StopSpeech(false, false);

                    // Cập nhật ngôn ngữ
                    _manualSession.Language = lang;
                    LocalizationService.Instance.CurrentLanguage = lang;
                    await LocalizeUI();

                    // Cập nhật ngôn ngữ cho auto queue item
                    if (AutoAudioService.Instance.CurrentItem != null) {
                        AutoAudioService.Instance.CurrentItem.Language = lang;
                        AutoAudioService.Instance.CurrentItem.CurrentSentenceIndex = 0;
                    }

                    // Cập nhật UI bản đồ
                    if (mapView != null) {
                        var newUiSet = await GetUISetAsync(lang);
                        await mapView.EvaluateJavaScriptAsync($"if(window.setUiTranslations) window.setUiTranslations({JsonSerializer.Serialize(newUiSet)});");
                        await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
                        await mapView.EvaluateJavaScriptAsync($"if(window.updateAppLanguage) window.updateAppLanguage('{lang}', '{name?.Replace("'", "\\'")}');");
                    }
                    LoadFoods("vi");

                    if (!AutoAudioService.Instance.IsCallActive)
                    {
                        if (isManualActive)
                        {
                            // Case 3: Đang nghe thủ công → phát lại thủ công bằng ngôn ngữ mới
                            // (Khi tắt thủ công sẽ kích hoạt Case 1 hỏi tiếp tục auto)
                            _manualSession.SentenceIndex = 0;
                            _manualSession.IsPaused = false;
                            _manualSession.IsActive = true;
                            _activeSession = _manualSession;
                            _ = SpeakWithChunksAsync(_manualSession);
                        }
                        else if (isAutoActive)
                        {
                            // Case 2: Đang nghe tự động → phát lại tự động bằng ngôn ngữ mới
                            if (AutoAudioService.Instance.CurrentItem != null)
                                _ = TriggerAutoAudioAsync(AutoAudioService.Instance.CurrentItem);
                            else
                                _ = AutoAudioService.Instance.PlayNextAsync();
                        }
                    }
                }
            }
            else if (e.Url.StartsWith("app-request-reload://markers?")) {
                e.Cancel = true;
                var query = HttpUtility.ParseQueryString(new Uri(e.Url).Query);
                string lang = query["lang"] ?? "vi";
                StopSpeech(true); _manualSession.Language = lang;
                if (mapView != null) {
                    await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
                    await mapView.EvaluateJavaScriptAsync($"if(window.updateAppLanguage) window.updateAppLanguage('{lang}', '');");
                }
                LoadFoods("vi");
            }
            else if (e.Url.StartsWith("app-tour://update?")) {
                e.Cancel = true;
                var query = HttpUtility.ParseQueryString(new Uri(e.Url).Query);
                UpdateTourProgressUI(query["duration"] ?? "", query["price"] ?? "", query["progress"] ?? "", query["durPrefix"] ?? "", query["pricePrefix"] ?? "");
            }
            else if (e.Url.StartsWith("app-tour://state?")) {
                e.Cancel = true;
                var query = HttpUtility.ParseQueryString(new Uri(e.Url).Query);
                string btn = query["btn"] ?? "";
                MainThread.BeginInvokeOnMainThread(() => {
                    btnSimulateArrive.IsVisible = (btn == "arrive");
                    btnSimulateNext.IsVisible = (btn == "next");
                });
            }
            else if (e.Url.StartsWith("app-tour://open-drawer")) {
                e.Cancel = true;
                OnMenuClicked(this, EventArgs.Empty);
            }
        }

        private async Task OpenShopDetailAsync(string poiId) {
            if (int.TryParse(poiId, out int id)) {
                PendingOpenFoodId = id;
                await TryOpenPendingDetail();
            }
        }

        public async Task TryOpenPendingDetail() {
            if (PendingOpenFoodId.HasValue && _isMapLoaded) {
                int id = PendingOpenFoodId.Value; PendingOpenFoodId = null;
                for (int i = 0; i < 20; i++) {
                    await Task.Delay(200);
                    try {
                        if ((await mapView.EvaluateJavaScriptAsync("typeof openDetails")).Contains("function")) {
                            await mapView.EvaluateJavaScriptAsync($"openDetails({id})");
                            return;
                        }
                    } catch { }
                }
            }
        }

        public async Task TryStartPendingRoute() {
            if (PendingRouteFoodId.HasValue && _isMapLoaded) {
                int id = PendingRouteFoodId.Value; PendingRouteFoodId = null;
                await Task.Delay(300); await mapView.EvaluateJavaScriptAsync($"window.routeToPoi({id})");
            }
        }
    }
}
