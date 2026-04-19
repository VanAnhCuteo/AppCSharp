using System.Diagnostics;
using System.Text.Json;
using System.Net.Http.Json;
using FoodMapApp.Services;
using FoodMapApp.Models;

namespace FoodMapApp
{
    public partial class MainPage : ContentPage
    {
        public static MainPage? Instance { get; private set; }
        public static int? PendingOpenFoodId { get; set; } = null;
        public static int? PendingRouteFoodId { get; set; } = null;

        private bool _isMapLoaded = false;
        private static string? _foodsJson = null;

        public MainPage()
        {
            InitializeComponent();
            Instance = this;

            SetupWebViewHandlers();
            
            mapView.Source = "map.html";
            LoadFoods();
            StartLocationTracking();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_manualSession.IsActive)
            {
                _manualSession.IsPaused = true;
                _activeSession = _manualSession;
                _ = SyncPlayerUI(_manualSession);
            }

            _ = Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            
            if (_isMapLoaded) 
            { 
                string currentLang = _manualSession.Language?.Split('-')[0] ?? "vi";
                _ = LoadFoods(currentLang); 
                await TryOpenPendingDetail(); 
            }

            _ = ReportCurrentLocationAsync();
            await LocalizeUI();
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

        private async Task<Dictionary<string, string>> GetUISetAsync(string lang)
        {
            string shortLang = lang.Split('-')[0].ToLower();
            var sourceStrings = new Dictionary<string, string>
            {
                ["title"] = "Thông báo", ["msg"] = "Bạn muốn đổi ngôn ngữ?", ["msg_audio"] = "Bạn muốn đổi ngôn ngữ? Âm thanh sẽ phát lại từ đầu.",
                ["ok"] = "Đồng ý", ["cancel"] = "Hủy", ["explore"] = "Khám phá", ["directions"] = "Chỉ đường đến quán",
                ["search_ph"] = "Tìm quán ăn, địa chỉ...", ["cancel_nav"] = "Hủy dẫn đường", ["navigating_to"] = "Đang đến:",
                ["select_lang"] = "Chọn ngôn ngữ", ["done"] = "Xong", ["loading_lang"] = "Đang tải ngôn ngữ...",
                ["hours_not_available"] = "Không có giờ mở cửa", ["unknown_loc"] = "Vị trí chưa xác định",
                ["no_addr"] = "Không có địa chỉ", ["hour_short"] = "h", ["min_short"] = "phút"
            };
            await LocalizationService.Instance.InitializeAsync(shortLang, sourceStrings);
            return sourceStrings.ToDictionary(kv => kv.Key, kv => LocalizationService.Instance.Get(kv.Key));
        }

        private async Task LocalizeUI()
        {
            var source = new Dictionary<string, string>
            {
                ["main_tour_title"] = "Danh Sách Tour", ["main_tour_loading"] = "Đang tải danh sách Tour...",
                ["main_tour_empty"] = "Không có Tour nào", ["main_tour_view_btn"] = "Xem Tour này",
                ["main_tour_time"] = "⏳ Thời gian:", ["main_tour_price"] = "💰 Giá xấp xỉ:",
                ["main_tour_status"] = "📍 Trạng thái:", ["main_tour_start"] = "▶ BẮT ĐẦU TOUR",
                ["main_tour_next"] = "Tới điểm tiếp theo ⏭", ["main_tour_end"] = "Kết thúc Tour",
                ["main_tour_not_moved"] = "Chưa di chuyển", ["main_tour_error"] = "Lỗi kết nối",
                ["main_tour_io_error"] = "Lỗi đường truyền thiết bị", ["main_audio_listening"] = "BẠN ĐANG NGHE CHỈ DẪN",
                ["main_locating"] = "Đang xác định vị trí của bạn...", ["main_calculating"] = "Đang tính toán..."
            };
            await LocalizationService.Instance.InitializeAsync(Preferences.Default.Get("app_lang", "vi"), source);
            tourDrawerTitleLabel.Text = LocalizationService.Instance.Get("main_tour_title");
            simTimeLabelPrefix.Text = LocalizationService.Instance.Get("main_tour_time");
            simPriceLabelPrefix.Text = LocalizationService.Instance.Get("main_tour_price");
            simStatusLabelPrefix.Text = LocalizationService.Instance.Get("main_tour_status");
            btnSimulateStart.Text = LocalizationService.Instance.Get("main_tour_start");
            btnSimulateNext.Text = LocalizationService.Instance.Get("main_tour_next");
            btnEndTour.Text = LocalizationService.Instance.Get("main_tour_end");
            manualModeLabel.Text = LocalizationService.Instance.Get("main_audio_listening");
        }

        private string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            return System.Text.RegularExpressions.Regex.Replace(text.Trim().ToLower(), @"\s+", " ");
        }

        async Task LoadFoods(string lang = "vi")
        {
            try {
                _foodsJson = await HttpService.GetStringWithCacheAsync($"{AppConfig.FoodApiUrl}?lang={lang}", $"map_foods_{lang}");
                if (_foodsJson != null && _isMapLoaded) {
                    int userId = Preferences.Default.Get("user_id", 0);
                    await mapView.EvaluateJavaScriptAsync($"loadFoods({_foodsJson}, {userId});");
                    await TryOpenPendingDetail();
                }
            } catch (Exception ex) { Debug.WriteLine($"LoadFoods error: {ex.Message}"); }
        }

        public void ResumeAudio()
        {
            if (_activeSession != null) {
                _activeSession.IsPaused = false;
                _pauseTcs?.TrySetResult(true);
            }
        }

        public void StopAudioWithFade() => StopSpeech(fullReset: false, clearQueue: false);
    }
}
