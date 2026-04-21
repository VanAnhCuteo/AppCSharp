using FoodMapApp.Models;
using FoodMapApp.Services;
using System.Threading;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace FoodMapApp.Views;

[QueryProperty(nameof(Shop), "Shop")]
[QueryProperty(nameof(ShopId), "id")]
[QueryProperty(nameof(IsAutoPlay), "auto")]
public partial class QRViewerPage : ContentPage
{
    private FoodModel _shop;
    private bool _isScanning = false;
    private bool _hasDetected = false;
    private string _qrDecodedText = string.Empty;
    
    public string ShopId { set => LoadShopByIdAsync(value); }
    public string IsAutoPlay { set => _autoAudioEnabled = value?.ToLower() == "true"; }
    private bool _autoAudioEnabled = false;

    public FoodModel Shop
    {
        get => _shop;
        set
        {
            _shop = value;
            UpdateUI();
        }
    }

	public QRViewerPage()
	{
		InitializeComponent();
	}

    private void UpdateUI()
    {
        if (_shop != null)
        {
            shopNameLabel.Text = _shop.name;
            
            string qrUrl = _shop.qr_code_url;
            qrImage.Source = string.IsNullOrEmpty(qrUrl) ? null : qrUrl;
        }
    }

    private async void LoadShopByIdAsync(string idStr)
    {
        if (int.TryParse(idStr, out int id))
        {
            try
            {
                var foods = await HttpService.GetWithCacheAsync<List<FoodModel>>($"{AppConfig.FoodApiUrl}?lang=vi", "shop_qr_list_cache");
                var target = foods?.FirstOrDefault(f => f.id == id);
                if (target != null)
                {
                    Shop = target;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading shop by ID: {ex.Message}");
            }
        }
    }

    private async void OnScanButtonClicked(object sender, EventArgs e)
    {
        await PrepareScannerAsync();

        _isScanning = true;
        _hasDetected = false;
        magicLens.IsVisible = true;
        scanButton.IsVisible = false;
        
        magicLens.TranslationX = 0;
        magicLens.TranslationY = 0;

        AnimateLaser();
    }

    private void UpdateScanButtonText()
    {
        string key = _isScanning ? "qr_scan_btn_off" : "qr_scan_btn_on";
        string fallback = _isScanning ? "Tắt Kính Quét" : "Bật Kính Quét";
        scanButton.Text = LocalizationService.Instance.Get(key, fallback);
    }

    private async Task PrepareScannerAsync()
    {
        if (_shop == null || string.IsNullOrEmpty(_shop.qr_code_url)) return;

        try
        {
            _qrDecodedText = $"foodmap://poi/{_shop.id}"; 
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Scanner Prep Error: {ex.Message}");
            _qrDecodedText = "error";
        }
    }

    private async void AnimateLaser()
    {
        while (_isScanning && !_hasDetected)
        {
            await laserLine.TranslateTo(0, 240, 1500, Easing.Linear);
            await laserLine.TranslateTo(0, 0, 1500, Easing.Linear);
        }
    }

    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (!_isScanning || _hasDetected) return;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                magicLens.TranslationX += e.TotalX;
                magicLens.TranslationY += e.TotalY;
                CheckCollision();
                break;
        }
    }

    private void CheckCollision()
    {
        // Điều chỉnh vùng va chạm cho khớp với UI mới
        bool isCenteredOverImage = magicLens.TranslationY < -180 && Math.Abs(magicLens.TranslationX) < 80;
        bool isDataValid = _qrDecodedText.Contains($"foodmap://poi/{_shop.id}");

        if (isCenteredOverImage && isDataValid)
        {
            OnScanSuccess();
        }
    }

    private async void OnScanSuccess()
    {
        _hasDetected = true;
        HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

        magicLens.Stroke = new SolidColorBrush(Colors.Green);
        await magicLens.ScaleTo(1.2, 200);
        await magicLens.ScaleTo(1.0, 200);
        
        // Điều hướng trực tiếp sang Bản Đồ
        if (_shop != null)
        {
            MainPage.PendingOpenFoodId = _shop.id;
            await Shell.Current.GoToAsync("//MainPage");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LocalizeUI();

        if (_autoAudioEnabled && _shop != null)
        {
            _autoAudioEnabled = false; // Run only once
            await Task.Delay(500); // Wait for page to settle
            
            // 1. Bật kính quét
            OnScanButtonClicked(null, EventArgs.Empty);
            
            // 2. Chờ hiệu ứng laser chạy (mô phỏng quét tự động)
            await Task.Delay(1500);
            
            // 3. Kết thúc quét thành công
            if (_isScanning) OnScanSuccess();
        }
    }

    private async Task LocalizeUI()
    {
        var source = new Dictionary<string, string>
        {
            ["qr_viewer_title"] = "Mã QR của quán",
            ["qr_viewer_drag_instr"] = "Kéo kính quét bên dưới đè lên ảnh QR để xem",
            ["qr_scan_btn_on"] = "Bật Kính Quét",
            ["qr_scan_btn_off"] = "Tắt Kính Quét",
            ["audio_guide_header"] = "AUDIO GUIDE",
            ["err_find_shop"] = "Không thể tìm thấy thông tin quán.",
            ["err_camera_denied"] = "Ứng dụng cần quyền truy cập camera để quét mã QR."
        };

        await LocalizationService.Instance.InitializeAsync(Preferences.Default.Get("app_lang", "vi"), source);

        this.Title = LocalizationService.Instance.Get("qr_viewer_title");
        DragInstructionsLabel.Text = LocalizationService.Instance.Get("qr_viewer_drag_instr");
        UpdateScanButtonText();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isScanning = false;
        _hasDetected = false;
        magicLens.IsVisible = false;
        scanButton.IsVisible = true;
        magicLens.Stroke = new SolidColorBrush(Color.FromArgb("#FF6B81"));
    }
}
