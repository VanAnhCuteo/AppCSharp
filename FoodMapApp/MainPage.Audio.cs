using System;
using System.Diagnostics;
using System.Text.Json;
using System.Net.Http.Json;
using FoodMapApp.Services;
using FoodMapApp.Models;

namespace FoodMapApp
{
    public partial class MainPage
    {
        // ═══════════════════════════════════════════
        //  SESSION STATE
        // ═══════════════════════════════════════════
        private static AudioSession _manualSession = new() { IsManual = true };
        private static AudioSession? _activeSession = null;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        
        /// Case 1: Nhớ trạng thái auto trước khi manual chiếm quyền
        private static bool _hadAutoBeforeManual = false;
        private string? _pendingManualPoiId = null;

        // ═══════════════════════════════════════════
        //  TTS ENGINE STATE
        // ═══════════════════════════════════════════
        private static bool _isCleaningUp = false;
        private static double _sessionAccumulatedSeconds = 0;
        private static string? _lastLoggedPoiId = null;
        private static bool _isActuallySpeaking = false;
        private static TaskCompletionSource<bool>? _pauseTcs;
        private static Stopwatch _audioStopwatch = new Stopwatch();
        private static CancellationTokenSource? _ttsCts;
        private static CancellationTokenSource? _currentSentenceCts;
        private readonly SemaphoreSlim _ttsSemaphore = new SemaphoreSlim(1, 1);
        private static IEnumerable<Locale>? _cachedLocales = null;

        // ═══════════════════════════════════════════
        //  AUDIO SESSION MODEL
        // ═══════════════════════════════════════════
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

        // ═══════════════════════════════════════════
        //  GLOBAL STOP (dùng khi cần dọn sạch hoàn toàn)
        // ═══════════════════════════════════════════
        public async void StopGlobalAudio()
        {
            FinalizeAndSendLog();
            StopSpeech(true);
            manualMiniPlayer.IsVisible = false;
            if (mapView != null)
                await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
        }

        // ═══════════════════════════════════════════
        //  AUDIO LOGGING (thống kê thời gian nghe)
        // ═══════════════════════════════════════════
        private async Task SendAudioLogAsync(string poiId, int duration)
        {
            if (string.IsNullOrEmpty(poiId) || duration < 1) return;
            try
            {
                // Khách cũng được tính thống kê lượt nghe (chỉ ẩn lịch sử ở ProfilePage)
                int userId = Preferences.Default.Get("user_id", 1);
                var payload = new { user_id = userId, duration_seconds = duration };
                var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await _httpClient.PostAsync($"{AppConfig.FoodApiUrl}/{poiId}/audio-log", content);
                sw.Stop();
                Debug.WriteLine($"[AudioLog] POST /audio-log POI={poiId}, duration={duration}s, status={response.StatusCode}, latency={sw.ElapsedMilliseconds}ms");
            }
            catch (Exception ex) { Debug.WriteLine($"[AudioLog] ERROR POI={poiId}: {ex.Message}"); }
        }

        private void FinalizeAndSendLog()
        {
            if (_activeSession == null || string.IsNullOrEmpty(_activeSession.PoiId)) return;

            _audioStopwatch.Stop();
            double totalSeconds = _sessionAccumulatedSeconds + _audioStopwatch.Elapsed.TotalSeconds;
            int finalSeconds = (int)Math.Round(totalSeconds);

            if (finalSeconds > 0)
            {
                string poiId = _activeSession.PoiId;
                _ = SendAudioLogAsync(poiId, finalSeconds);
            }

            _sessionAccumulatedSeconds = 0;
            _audioStopwatch.Reset();
        }

        // ═══════════════════════════════════════════
        //  TTS ENGINE CORE
        // ═══════════════════════════════════════════
        private void StopSpeech(bool fullReset = false, bool clearQueue = true)
        {
            try
            {
                _isActuallySpeaking = false;
                _currentSentenceCts?.Cancel();
                _ttsCts?.Cancel();
                _pauseTcs?.TrySetResult(false);

                if (_audioStopwatch.IsRunning)
                {
                    _audioStopwatch.Stop();
                    _sessionAccumulatedSeconds += _audioStopwatch.Elapsed.TotalSeconds;
                    _audioStopwatch.Reset();
                }

                _ = TextToSpeech.Default.SpeakAsync("", new SpeechOptions { Volume = 0 });

                if (fullReset)
                {
                    _manualSession.Reset();
                    _activeSession = null;
                    _hadAutoBeforeManual = false;
                    _sessionAccumulatedSeconds = 0;
                    _audioStopwatch.Reset();
                }
            }
            catch (Exception ex) { Debug.WriteLine($"StopSpeech error: {ex.Message}"); }

            TrackingService.Stop();
            _ = ReportCurrentLocationAsync();
        }

        private async Task SpeakWithChunksAsync(AudioSession session)
        {
            Debug.WriteLine($"[TTS] SpeakWithChunks: chờ semaphore... (POI={session.PoiId})");
            if (!await _ttsSemaphore.WaitAsync(500))
            {
                Debug.WriteLine($"[TTS] ⚠ Semaphore TIMEOUT 500ms – SKIP (POI={session.PoiId}). Đã có 1 session khác đang chạy.");
                return;
            }
            Debug.WriteLine($"[TTS] ✓ Semaphore acquired (POI={session.PoiId}, {session.Sentences.Length} câu, isManual={session.IsManual})");
            _isActuallySpeaking = true;
            _ttsCts = new CancellationTokenSource();

            // Nếu là POI mới, reset bộ đếm thời gian nghe
            if (_lastLoggedPoiId != session.PoiId)
            {
                _sessionAccumulatedSeconds = 0;
                _lastLoggedPoiId = session.PoiId;
            }
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
                    if (session.IsManual)
                        await mapView.EvaluateJavaScriptAsync($"if(window.onTtsProgress) window.onTtsProgress({index}, {sentences.Length});");
                    else if (AutoAudioService.Instance.CurrentItem != null)
                        AutoAudioService.Instance.CurrentItem.CurrentSentenceIndex = index;

                    // Xử lý tạm dừng
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
                        await TextToSpeech.Default.SpeakAsync(sentence, options, _currentSentenceCts.Token);

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
                Debug.WriteLine($"[TTS] Semaphore released (POI={session.PoiId}, SentenceIndex={session.SentenceIndex}/{session.Sentences.Length})");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (mapView != null) await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");
                    _ = ReportCurrentLocationAsync();
                });
            }

            if (_isCleaningUp) return;

            // ── Xử lý khi audio kết thúc tự nhiên (hết câu) ──
            if (session.SentenceIndex >= session.Sentences.Length)
            {
                FinalizeAndSendLog();

                if (session.IsManual)
                {
                    // Case 1: Nghe thủ công xong → hỏi tiếp tục auto nếu có
                    session.IsActive = false;
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await SyncPlayerUI(null);
                        await HandleManualAudioEnded();
                    });
                }
                else
                {
                    // Case 4+5: Auto xong → đánh dấu đã nghe (cooldown) → phát quán tiếp theo
                    AutoAudioService.Instance.MarkAsHeard(int.Parse(session.PoiId));
                    // MarkAsHeard sẽ tự gọi PlayNextAsync
                }
            }
        }

        // ═══════════════════════════════════════════
        //  Case 1: XỬ LÝ KHI AUDIO THỦ CÔNG KẾT THÚC
        //  Hỏi tiếp tục auto NẾU có hàng đợi auto
        // ═══════════════════════════════════════════
        private async Task HandleManualAudioEnded()
        {
            bool hasAutoQueue = _hadAutoBeforeManual &&
                (AutoAudioService.Instance.Queue.Count > 0 || AutoAudioService.Instance.CurrentItem != null);

            _hadAutoBeforeManual = false;

            if (hasAutoQueue)
            {
                bool continueAuto = await DisplayAlert(
                    LocalizationService.Instance.Get("main_audio_resume_title"),
                    LocalizationService.Instance.Get("main_audio_resume_msg"),
                    LocalizationService.Instance.Get("main_audio_resume_btn"),
                    LocalizationService.Instance.Get("main_audio_cancel_btn")
                );

                if (continueAuto)
                {
                    AutoAudioService.Instance.SetPaused(false);
                    if (AutoAudioService.Instance.CurrentItem != null)
                        await TriggerAutoAudioAsync(AutoAudioService.Instance.CurrentItem);
                }
                else
                {
                    AutoAudioService.Instance.ClearQueue();
                    AutoAudioService.Instance.SetPaused(false); // Cho phép auto mới trong tương lai
                }
            }
            else
            {
                // Không có auto queue → mở khóa auto cho POI mới
                AutoAudioService.Instance.SetPaused(false);
            }
        }

        // ═══════════════════════════════════════════
        //  TRIGGER AUTO AUDIO (từ AutoAudioService)
        // ═══════════════════════════════════════════
        public async Task TriggerAutoAudioAsync(AutoAudioService.AudioQueueItem item)
        {
            try
            {
                Debug.WriteLine($"[TTS] TriggerAutoAudio: POI={item.Poi.id} ({item.Poi.name})");
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var res = await _httpClient.GetAsync($"{AppConfig.FoodApiUrl}/{item.Poi.id}/guide?lang={item.Language}");
                sw.Stop();
                Debug.WriteLine($"[TTS] GET /guide POI={item.Poi.id}, status={res.StatusCode}, latency={sw.ElapsedMilliseconds}ms");
                string text = "";
                if (res.IsSuccessStatusCode)
                {
                    var guide = await res.Content.ReadFromJsonAsync<dynamic>();
                    text = $"{guide?.GetProperty("title").GetString()}. {guide?.GetProperty("description").GetString()}";
                }
                else
                {
                    text = $"{item.Poi.name}. {item.Poi.description}";
                    Debug.WriteLine($"[TTS] ⚠ Guide API failed, dùng fallback text cho POI={item.Poi.id}");
                }

                if (!string.IsNullOrWhiteSpace(text))
                {
                    string normalizedText = NormalizeText(text);
                    var sentences = normalizedText.Split(new[] { '.', '!', '?', ';', ',', '。', '！', '？' }, StringSplitOptions.RemoveEmptyEntries)
                                                  .Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

                    Debug.WriteLine($"[TTS] POI={item.Poi.id}: text={text.Length} chars → {sentences.Length} câu");
                    FinalizeAndSendLog();

                    // Case 4: Đồng bộ TotalSentences cho AutoAudioService (tránh chia 0)
                    item.TotalSentences = sentences.Length;

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
                    await SyncPlayerUI(_activeSession);
                    _ = SpeakWithChunksAsync(_activeSession);
                }
            }
            catch (Exception ex) { Debug.WriteLine($"TriggerAudio error: {ex.Message}"); }
        }

        // ═══════════════════════════════════════════
        //  SYNC MINI-PLAYER UI
        // ═══════════════════════════════════════════
        private async Task SyncPlayerUI(AudioSession? session)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    bool isActive = session != null && session.IsActive;
                    bool isPlaying = isActive && _isActuallySpeaking && !session.IsPaused;
                    bool isPaused = isActive && session.IsPaused;

                    if (mapView != null)
                        _ = mapView.EvaluateJavaScriptAsync($"if(window.updateAudioState) window.updateAudioState({isPlaying.ToString().ToLower()}, {isPaused.ToString().ToLower()}, {isActive.ToString().ToLower()});");

                    if (!isActive)
                    {
                        manualMiniPlayer.IsVisible = false;
                        queueBadge.IsVisible = false;
                        return;
                    }

                    manualMiniPlayer.IsVisible = true;
                    mPlayIcon.IsVisible = session.IsPaused;
                    mPauseIcon.IsVisible = !session.IsPaused;
                    manualShopLabel.Text = session.Name ?? "Quán ăn";
                    manualModeLabel.Text = session.IsManual
                        ? LocalizationService.Instance.Get("main_audio_manual_label", "BẠN ĐANG NGHE THỦ CÔNG")
                        : LocalizationService.Instance.Get("main_audio_auto_label", "BẠN ĐANG NGHE TỰ ĐỘNG");

                    // Case 5: Hiển thị số lượng hàng đợi
                    int waitingCount = AutoAudioService.Instance.Queue.Count - 1;
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

        // ═══════════════════════════════════════════
        //  AUDIO WAVE ANIMATION
        // ═══════════════════════════════════════════
        private async Task AnimateVisualizer(AudioSession session)
        {
            Random rnd = new Random();
            while (_isActuallySpeaking && _activeSession == session && !session.IsPaused)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    mWave1.HeightRequest = rnd.Next(8, 20); mWave2.HeightRequest = rnd.Next(10, 25);
                    mWave3.HeightRequest = rnd.Next(5, 15); mWave4.HeightRequest = rnd.Next(12, 28);
                    mWave5.HeightRequest = rnd.Next(8, 22); mWave6.HeightRequest = rnd.Next(10, 24);
                });
                await Task.Delay(130);
            }
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                mWave1.HeightRequest = 12; mWave2.HeightRequest = 18; mWave3.HeightRequest = 10;
                mWave4.HeightRequest = 22; mWave5.HeightRequest = 14; mWave6.HeightRequest = 19;
            });
        }

        // ═══════════════════════════════════════════
        //  NÚT PLAY/PAUSE (▶ / ⏸)
        // ═══════════════════════════════════════════
        private async void OnManualPlayClicked(object sender, EventArgs e)
        {
            var session = _activeSession ?? (_manualSession.IsActive ? _manualSession : null);
            if (session == null || string.IsNullOrEmpty(session.PoiId)) return;

            // Chặn khi đang có cuộc gọi
            if (AutoAudioService.Instance.IsCallActive)
            {
                _pendingManualPoiId = session.PoiId;
                await DisplayAlert("Thông báo", "Audio sẽ phát sau khi cuộc gọi kết thúc.", "OK");
                return;
            }

            // Toggle pause/resume
            session.IsPaused = !session.IsPaused;

            // Đồng bộ trạng thái pause với AutoAudioService cho auto session
            if (!session.IsManual)
                AutoAudioService.Instance.SetPaused(session.IsPaused);

            await SyncPlayerUI(session);

            if (!session.IsPaused)
            {
                // RESUME
                session.IsActive = true;
                _activeSession = session;
                if (!_isActuallySpeaking)
                    _ = SpeakWithChunksAsync(session);
                else
                {
                    _pauseTcs?.TrySetResult(true);
                    _ = AnimateVisualizer(session);
                }

                // Khởi động lại stopwatch
                _audioStopwatch.Restart();
            }
            else
            {
                // PAUSE
                _pauseTcs?.TrySetResult(false);
                if (_audioStopwatch.IsRunning)
                {
                    _audioStopwatch.Stop();
                    _sessionAccumulatedSeconds += _audioStopwatch.Elapsed.TotalSeconds;
                    _audioStopwatch.Reset();
                }
            }
            _ = ReportCurrentLocationAsync();
        }

        // ═══════════════════════════════════════════
        //  NÚT REPLAY (🔄)
        // ═══════════════════════════════════════════
        private async void OnManualReplayClicked(object sender, EventArgs e)
        {
            var session = _activeSession ?? (_manualSession.IsActive ? _manualSession : null);
            if (session == null || string.IsNullOrEmpty(session.PoiId)) return;

            // Case 4: Replay KHÔNG reset cooldown cho auto (chỉ manual replay được)
            if (!session.IsManual)
                AutoAudioService.Instance.SetPaused(false);

            // Reset về câu đầu
            session.IsPaused = false;
            session.SentenceIndex = 0;
            if (!session.IsManual && AutoAudioService.Instance.CurrentItem != null)
                AutoAudioService.Instance.CurrentItem.CurrentSentenceIndex = 0;

            await SyncPlayerUI(session);

            // Dừng engine cũ và khởi động lại
            if (_isActuallySpeaking)
                StopSpeech(false, false);

            FinalizeAndSendLog();
            session.IsActive = true;
            _activeSession = session;
            _ = SpeakWithChunksAsync(session);
            _ = ReportCurrentLocationAsync();
        }

        // ═══════════════════════════════════════════
        //  NÚT ĐÓNG (✕) - Case 1 + Case 6
        // ═══════════════════════════════════════════
        private async void OnManualCloseClicked(object sender, EventArgs e)
        {
            var session = _activeSession ?? _manualSession;
            bool wasManual = session.IsManual;

            // Dừng audio hiện tại
            session.IsActive = false;
            FinalizeAndSendLog();
            StopSpeech(false, false);
            await SyncPlayerUI(null);
            _activeSession = null;
            if (mapView != null)
                await mapView.EvaluateJavaScriptAsync("if(window.onTtsFinished) window.onTtsFinished();");

            if (wasManual)
            {
                // ══ Case 1: Tắt audio thủ công ══
                // Hỏi tiếp tục auto NẾU có hàng đợi auto
                await HandleManualAudioEnded();
            }
            else
            {
                // ══ Case 6: Tắt audio tự động ══
                // Đánh dấu quán hiện tại đã nghe (cooldown 15 phút - Case 4)
                if (int.TryParse(session.PoiId, out int poiId))
                    AutoAudioService.Instance.MarkAsHeardOnly(poiId);

                int waitingCount = AutoAudioService.Instance.Queue.Count;
                if (waitingCount > 0)
                {
                    // Hàng đợi còn → hỏi tắt hàng đợi
                    bool clearQueue = await DisplayAlert(
                        LocalizationService.Instance.Get("main_audio_clear_queue_title"),
                        string.Format(LocalizationService.Instance.Get("main_audio_clear_queue_msg"), waitingCount),
                        LocalizationService.Instance.Get("main_audio_clear_btn"),
                        LocalizationService.Instance.Get("main_audio_skip_btn")
                    );

                    if (clearQueue)
                    {
                        AutoAudioService.Instance.ClearQueue();
                        AutoAudioService.Instance.SetPaused(false); // Mở khóa auto cho POI mới
                    }
                    else
                    {
                        // Bỏ qua quán này, phát quán tiếp theo
                        _ = AutoAudioService.Instance.PlayNextAsync();
                    }
                }
                else
                {
                    // Không có hàng đợi → mở khóa auto cho POI mới
                    AutoAudioService.Instance.SetPaused(false);
                }
            }

            _ = ReportCurrentLocationAsync();
        }

        // ═══════════════════════════════════════════
        //  XỬ LÝ GIÁN ĐOẠN HỆ THỐNG (cuộc gọi, mất focus)
        // ═══════════════════════════════════════════
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
                // Nếu có yêu cầu thủ công đang chờ
                if (!string.IsNullOrEmpty(_pendingManualPoiId))
                {
                    string pid = _pendingManualPoiId;
                    _pendingManualPoiId = null;
                    await OpenShopDetailAsync(pid);
                    return;
                }

                // Nếu có auto session đang tạm dừng
                if (_activeSession != null && !_activeSession.IsManual)
                {
                    var foods = JsonSerializer.Deserialize<List<FoodModel>>(_foodsJson);
                    var food = foods?.FirstOrDefault(f => f.id.ToString() == _activeSession.PoiId);
                    if (food != null)
                    {
                        double progress = _activeSession.TotalSentences > 0
                            ? (double)_activeSession.SentenceIndex / _activeSession.TotalSentences : 0;

                        if (!AutoAudioService.Instance.IsStillInRange(food))
                        {
                            if (progress > 0.5)
                                AutoAudioService.Instance.MarkAsHeard(food.id);
                            else
                            {
                                AutoAudioService.Instance.RemoveFromQueue(food.id);
                                await SyncPlayerUI(null);
                            }
                        }
                        else
                        {
                            ResumeAudio();
                        }
                    }
                }
            });
        }
    }
}
