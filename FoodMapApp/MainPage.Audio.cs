using System.Diagnostics;
using System.Text.Json;
using System.Net.Http.Json;
using FoodMapApp.Services;
using FoodMapApp.Models;

namespace FoodMapApp
{
    public partial class MainPage
    {
        // Unified Audio States
        private static AudioSession _manualSession = new() { IsManual = true };
        private static AudioSession? _activeSession = null;
        private string? _pendingManualPoiId = null;

        private static bool _isCleaningUp = false; 
        private static bool _isActuallySpeaking = false; 
        private static TaskCompletionSource<bool>? _pauseTcs;
        private static Stopwatch _audioStopwatch = new Stopwatch();
        private static CancellationTokenSource? _ttsCts;
        private static CancellationTokenSource? _currentSentenceCts;
        private readonly SemaphoreSlim _ttsSemaphore = new SemaphoreSlim(1, 1);
        private static IEnumerable<Locale>? _cachedLocales = null;

        public class AudioSession
        {
            public string PoiId { get; set; } = "";
            public string Name { get; set; } = "Quán ăn";
            public string[] Sentences { get; set; } = Array.Empty<string>();
            public int SentenceIndex { get; set; } = 0;
            public int TotalSentences { get; set; } = 0;
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

        public async void StopGlobalAudio()
        {
            StopSpeech(true);
            manualMiniPlayer.IsVisible = false;
            if (mapView != null) {
                await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
            }
        }

        private async Task SendAudioLogAsync(string poiId, int duration)
        {
            if (string.IsNullOrEmpty(poiId)) return;
            try
            {
                if (new AuthService().IsGuest) return;
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
                
                _ = TextToSpeech.Default.SpeakAsync("", new SpeechOptions { Volume = 0 });

                if (fullReset)
                {
                    _manualSession.Reset();
                    _activeSession = null;
                }
            }
            catch (Exception ex) { Debug.WriteLine($"StopSpeech error: {ex.Message}"); }
            
            TrackingService.Stop();
            _ = ReportCurrentLocationAsync();
        }

        private async Task SpeakWithChunksAsync(AudioSession session)
        {
            if (!await _ttsSemaphore.WaitAsync(500)) return;
            _isActuallySpeaking = true;
            _ttsCts = new CancellationTokenSource();
            _audioStopwatch.Restart();

            if (int.TryParse(session.PoiId, out int pid))
                TrackingService.UpdateStatus(true, pid);
            else
                TrackingService.UpdateStatus(true);
            
            _ = ReportCurrentLocationAsync(); 

            try
            {
                var sentences = session.Sentences;
                if (sentences == null || sentences.Length == 0) return;

                if (_cachedLocales == null) _cachedLocales = await TextToSpeech.Default.GetLocalesAsync();
                var locale = _cachedLocales?.FirstOrDefault(l => l.Language.Equals(session.Language, StringComparison.OrdinalIgnoreCase)) ??
                             _cachedLocales?.FirstOrDefault(l => l.Language.ToLower().StartsWith(session.Language.Split('-')[0].ToLower()));
                var options = new SpeechOptions { Locale = locale, Pitch = AppConfig.AudioPitch };

                _ = AnimateVisualizer(session);

                while (session.SentenceIndex < sentences.Length)
                {
                    if (_ttsCts == null || _ttsCts.Token.IsCancellationRequested || !_isActuallySpeaking || !session.IsActive || _activeSession != session) break;

                    int index = session.SentenceIndex;
                    if (session.IsManual) await mapView.EvaluateJavaScriptAsync($"if(window.onTtsProgress) window.onTtsProgress({index}, {sentences.Length});");
                    else if (AutoAudioService.Instance.CurrentItem != null) {
                        AutoAudioService.Instance.CurrentItem.CurrentSentenceIndex = index;
                    }

                    if (_isActuallySpeaking && session.IsPaused)
                    {
                        _pauseTcs = new TaskCompletionSource<bool>();
                        bool resume = await _pauseTcs.Task;
                        if (!resume || _ttsCts == null || _ttsCts.Token.IsCancellationRequested || !_isActuallySpeaking || !session.IsActive || _activeSession != session) break;
                    }

                    if (_ttsCts == null || _ttsCts.Token.IsCancellationRequested || !_isActuallySpeaking || !session.IsActive || _activeSession != session) break;
                    _currentSentenceCts = CancellationTokenSource.CreateLinkedTokenSource(_ttsCts.Token);
                    string sentence = sentences[index];
                    if (!string.IsNullOrWhiteSpace(sentence)) 
                    {
                        await TextToSpeech.Default.SpeakAsync(sentence, options, _currentSentenceCts.Token);
                    }
                    
                    if (_ttsCts == null || _ttsCts.Token.IsCancellationRequested || !_isActuallySpeaking || !session.IsActive || _activeSession != session) break;
                    session.SentenceIndex++;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Debug.WriteLine($"TTS Error: {ex.Message}"); }
            finally 
            { 
                _isActuallySpeaking = false; 
                _ttsCts = null;
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
                        var cts = new CancellationTokenSource();
                        cts.CancelAfter(TimeSpan.FromSeconds(5));
                        try {
                            bool continueAuto = await DisplayAlert("Thông báo", "Tiếp tục nghe các quán gần đây?", "Đồng ý", "Bỏ qua");
                            if (continueAuto) AutoAudioService.Instance.SetPaused(false);
                        } catch (TaskCanceledException) { }
                    });
                }
                else
                {
                    AutoAudioService.Instance.MarkAsHeard(int.Parse(session.PoiId));
                }
            }
        }

        public async Task TriggerAutoAudioAsync(AutoAudioService.AudioQueueItem item)
        {
            try {
                using HttpClient client = new HttpClient();
                var res = await client.GetAsync($"{AppConfig.FoodApiUrl}/{item.Poi.id}/guide?lang={item.Language}");
                string text = "";
                if (res.IsSuccessStatusCode) {
                    var guide = await res.Content.ReadFromJsonAsync<dynamic>();
                    text = $"{guide?.GetProperty("title").GetString()}. {guide?.GetProperty("description").GetString()}";
                } else {
                    text = $"{item.Poi.name}. {item.Poi.description}";
                }

                if (!string.IsNullOrWhiteSpace(text)) {
                    string normalizedText = NormalizeText(text);
                    var sentences = normalizedText.Split(new[] { '.', '!', '?', ';', ',', '。', '！', '？' }, StringSplitOptions.RemoveEmptyEntries)
                                                  .Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

                    _activeSession = new AudioSession
                    {
                        PoiId = item.Poi.id.ToString(),
                        Name = item.Poi.name,
                        Sentences = sentences,
                        SentenceIndex = item.CurrentSentenceIndex,
                        TotalSentences = sentences.Length,
                        Language = item.Language,
                        IsPaused = false,
                        IsManual = false,
                        IsActive = true
                    };
                    _ = SpeakWithChunksAsync(_activeSession);
                }
            } catch (Exception ex) { Debug.WriteLine($"TriggerAudio error: {ex.Message}"); }
        }

        private async Task SyncPlayerUI(AudioSession? session)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() => {
                    bool isActive = session != null && session.IsActive;
                    bool isPlaying = isActive && _isActuallySpeaking && !session.IsPaused;
                    bool isPaused = isActive && session.IsPaused;

                    if (mapView != null) 
                    {
                        _ = mapView.EvaluateJavaScriptAsync($"if(window.updateAudioState) window.updateAudioState({isPlaying.ToString().ToLower()}, {isPaused.ToString().ToLower()}, {isActive.ToString().ToLower()});");
                    }

                    if (!isActive)
                    {
                        manualMiniPlayer.IsVisible = false;
                        queueBadge.IsVisible = false;
                        return;
                    }

                    string name = session.Name ?? "Quán ăn";
                    manualMiniPlayer.IsVisible = true;
                    mPlayIcon.IsVisible = session.IsPaused;
                    mPauseIcon.IsVisible = !session.IsPaused;
                    manualShopLabel.Text = name;
                    manualModeLabel.Text = session.IsManual ? "BẠN ĐANG NGHE THỦ CÔNG" : "BẠN ĐANG NGHE TỰ ĐỘNG";

                    int queueCount = AutoAudioService.Instance.Queue.Count;
                    int waitingCount = queueCount - 1;
                    if (waitingCount > 0) 
                    {
                        queueBadge.IsVisible = true;
                        queueBadgeLabel.Text = waitingCount.ToString();
                    }
                    else 
                    {
                        queueBadge.IsVisible = false;
                    }
                });
            }
            catch (Exception ex) { Debug.WriteLine($"SyncUI error: {ex.Message}"); }
        }

        private async Task AnimateVisualizer(AudioSession session)
        {
            Random rnd = new Random();
            while (_isActuallySpeaking && (_activeSession == session || _manualSession == session) && !session.IsPaused)
            {
                await MainThread.InvokeOnMainThreadAsync(() => {
                    mWave1.HeightRequest = rnd.Next(8, 20); mWave2.HeightRequest = rnd.Next(10, 25);
                    mWave3.HeightRequest = rnd.Next(5, 15); mWave4.HeightRequest = rnd.Next(12, 28);
                    mWave5.HeightRequest = rnd.Next(8, 22); mWave6.HeightRequest = rnd.Next(10, 24);
                });
                await Task.Delay(130);
            }
            await MainThread.InvokeOnMainThreadAsync(() => {
                mWave1.HeightRequest = 12; mWave2.HeightRequest = 18; mWave3.HeightRequest = 10;
                mWave4.HeightRequest = 22; mWave5.HeightRequest = 14; mWave6.HeightRequest = 19;
            });
        }

        private async void OnManualPlayClicked(object sender, EventArgs e)
        {
            var session = _activeSession ?? (_manualSession.IsActive ? _manualSession : null);
            if (session == null || string.IsNullOrEmpty(session.PoiId)) return;
            
            if (AutoAudioService.Instance.IsCallActive) {
                _pendingManualPoiId = session.PoiId;
                await DisplayAlert("Thông báo", "Yêu cầu đã được ghi lại. Audio sẽ phát sau khi cuộc gọi kết thúc.", "OK");
                return;
            }

            if (session.IsManual && !session.IsActive) {
                AutoAudioService.Instance.SetPaused(true);
                if (int.TryParse(session.PoiId, out int mid)) {
                    AutoAudioService.Instance.RemoveFromQueue(mid);
                    AutoAudioService.Instance.MarkAsHeard(mid);
                }
            }

            session.IsPaused = !session.IsPaused;
            if (!session.IsManual) AutoAudioService.Instance.SetPaused(session.IsPaused);

            await SyncPlayerUI(session);

            if (!session.IsPaused)
            {
                if (!session.IsActive && _isActuallySpeaking) StopSpeech(false, false);
                session.IsActive = true;
                _activeSession = session;
                if (!_isActuallySpeaking) _ = SpeakWithChunksAsync(session);
                else {
                    _pauseTcs?.TrySetResult(true);
                    _ = AnimateVisualizer(session);
                }
            }
            else { _pauseTcs?.TrySetResult(false); }
            _ = ReportCurrentLocationAsync();
        }

        private async void OnManualReplayClicked(object sender, EventArgs e)
        {
            var session = _activeSession ?? (_manualSession.IsActive ? _manualSession : null);
            if (session == null || string.IsNullOrEmpty(session.PoiId)) return;
            
            AutoAudioService.Instance.SetPaused(true);
            if (int.TryParse(session.PoiId, out int mid)) {
                AutoAudioService.Instance.ResetCooldown(mid);
                AutoAudioService.Instance.MarkAsHeard(mid);
            }

            session.IsPaused = false;
            session.SentenceIndex = 0;
            if (!session.IsManual && AutoAudioService.Instance.CurrentItem != null)
                AutoAudioService.Instance.CurrentItem.CurrentSentenceIndex = 0;

            await SyncPlayerUI(session);

            if (!_isActuallySpeaking) {
                session.IsActive = true;
                _activeSession = session;
                _ = SpeakWithChunksAsync(session);
            }
            else if (_activeSession == session) _pauseTcs?.TrySetResult(true);
            else {
                StopSpeech(false, false);
                session.IsActive = true;
                _activeSession = session;
                _ = SpeakWithChunksAsync(session);
            }
            _ = ReportCurrentLocationAsync();
        }

        private async void OnManualCloseClicked(object sender, EventArgs e)
        {
            var session = _activeSession ?? _manualSession;
            session.IsActive = false;
            if (!session.IsManual && int.TryParse(session.PoiId, out int mid)) {
                AutoAudioService.Instance.RemoveFromQueue(mid);
                AutoAudioService.Instance.MarkAsHeard(mid);
            }

            StopSpeech(false, false);
            await SyncPlayerUI(null);
            _activeSession = null;
            if (mapView != null) await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
            _ = ReportCurrentLocationAsync();
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

        public async Task HandleCallEndAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (!string.IsNullOrEmpty(_pendingManualPoiId))
                {
                    string pid = _pendingManualPoiId;
                    _pendingManualPoiId = null;
                    await OpenShopDetailAsync(pid);
                    return;
                }

                if (_activeSession != null && !_activeSession.IsManual)
                {
                    var foods = JsonSerializer.Deserialize<List<FoodModel>>(_foodsJson);
                    var food = foods?.FirstOrDefault(f => f.id.ToString() == _activeSession.PoiId);
                    if (food != null)
                    {
                        double progress = _activeSession.TotalSentences > 0 ? (double)_activeSession.SentenceIndex / _activeSession.TotalSentences : 0;
                        if (!AutoAudioService.Instance.IsStillInRange(food))
                        {
                            if (progress > 0.5) AutoAudioService.Instance.MarkAsHeard(food.id);
                            else {
                                AutoAudioService.Instance.RemoveFromQueue(food.id);
                                await SyncPlayerUI(null);
                            }
                        }
                        else ResumeAudio();
                    }
                }
            });
        }
    }
}
