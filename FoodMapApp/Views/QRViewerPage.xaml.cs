using FoodMapApp.Models;
using FoodMapApp.Services;
using System.Threading;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using ZXing;

namespace FoodMapApp.Views;

[QueryProperty(nameof(Shop), "Shop")]
public partial class QRViewerPage : ContentPage
{
    private FoodModel _shop;
    private bool _isScanning = false;
    private bool _hasDetected = false;
    private CancellationTokenSource _ttsCts;
    private CancellationTokenSource _currentSentenceCts;
    private string _qrDecodedText = string.Empty;
    
    // Audio Resume State
    private string[] _chunks = Array.Empty<string>();
    private int _currentChunkIndex = 0;
    private bool _isPaused = false;
    private readonly SemaphoreSlim _ttsSemaphore = new SemaphoreSlim(1, 1);
    private Locale _vietnameseLocale;
    private bool _shouldResumeOnAppearing = false;
    private Stopwatch _audioStopwatch = new Stopwatch();

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
            detectedShopLabel.Text = _shop.name;
            
            string qrUrl = _shop.qr_code_url;
            qrImage.Source = string.IsNullOrEmpty(qrUrl) ? null : qrUrl;

            // Prepare audio chunks
            _chunks = PrepareAudioChunks($"{_shop.name}. {_shop.description}");
            _currentChunkIndex = 0;
            _isPaused = false;
        }
    }

    private string[] PrepareAudioChunks(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();

        // Đồng bộ 100% với logic tại MainPage.xaml.cs (Trang chi tiết)
        // Chia theo các dấu câu và ký tự ngắt nghỉ đặc biệt để đảm bảo không sót chữ
        return text.Split(new[] { '.', '!', '?', ';', ',', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim())
                   .Where(s => s.Length > 0)
                   .ToArray();
    }

    private async void OnScanButtonClicked(object sender, EventArgs e)
    {
        await PrepareScannerAsync();

        _isScanning = true;
        _hasDetected = false;
        magicLens.IsVisible = true;
        scanButton.IsVisible = false;
        miniPlayer.IsVisible = false;
        
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
            using HttpClient client = new HttpClient();
            var imageBytes = await client.GetByteArrayAsync(_shop.qr_code_url);
            _qrDecodedText = $"foodmap://poi/{_shop.id}"; 
            
            // Pre-fetch Vietnamese Locale
            if (_vietnameseLocale == null)
            {
                var locales = await TextToSpeech.Default.GetLocalesAsync();
                _vietnameseLocale = locales.FirstOrDefault(l => l.Language.Equals("vi", StringComparison.OrdinalIgnoreCase)) ??
                                    locales.FirstOrDefault(l => l.Language.StartsWith("vi", StringComparison.OrdinalIgnoreCase));
            }
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
            await laserLine.TranslateTo(0, 290, 1500, Easing.Linear);
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
        bool isCenteredOverImage = magicLens.TranslationY < -220 && Math.Abs(magicLens.TranslationX) < 50;
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
        
        magicLens.IsVisible = false;
        miniPlayer.IsVisible = true;
        
        _currentChunkIndex = 0;
        _isPaused = false;
        _ = PlayAudioWithChunksAsync();
    }

    private async Task PlayAudioWithChunksAsync()
    {
        await _ttsSemaphore.WaitAsync();
        try
        {
            _ttsCts = new CancellationTokenSource();
            _isPaused = false;
            
            // Sync to Global Tracking
            TrackingService.IsListening = true;
            TrackingService.UpdateStatus(true, _shop?.id);

            // Toggle icons
            playIcon.IsVisible = false;
            pauseIcon.IsVisible = true;

            var options = new SpeechOptions { Locale = _vietnameseLocale };
            _ = AnimateVisualizer(); // Start visualizer

            while (_currentChunkIndex < _chunks.Length && !_ttsCts.Token.IsCancellationRequested)
            {
                if (_isPaused) break;

                _currentSentenceCts = CancellationTokenSource.CreateLinkedTokenSource(_ttsCts.Token);
                string text = _chunks[_currentChunkIndex];
                
                try
                {
                    _audioStopwatch.Start();
                    await TextToSpeech.Default.SpeakAsync(text, options, cancelToken: _currentSentenceCts.Token);
                    _audioStopwatch.Stop();
                    _currentChunkIndex++;
                }
                catch (OperationCanceledException)
                {
                    if (!_isPaused) break;
                }
                finally
                {
                    _currentSentenceCts?.Dispose();
                    _currentSentenceCts = null;
                }
            }

            if (_currentChunkIndex >= _chunks.Length)
            {
                ReportAndResetAudioStats();
                _currentChunkIndex = 0;
                playIcon.IsVisible = true;
                pauseIcon.IsVisible = false;
            }
        }
        finally
        {
            _ttsSemaphore.Release();
        }
    }

    private async Task AnimateVisualizer()
    {
        Random rnd = new Random();
        while (!_isPaused && pauseIcon.IsVisible)
        {
            // Smooth random scaling for 6 wave bars
            wave1.HeightRequest = rnd.Next(8, 20);
            wave2.HeightRequest = rnd.Next(10, 25);
            wave3.HeightRequest = rnd.Next(5, 15);
            wave4.HeightRequest = rnd.Next(12, 28);
            wave5.HeightRequest = rnd.Next(8, 22);
            wave6.HeightRequest = rnd.Next(10, 24);
            await Task.Delay(130);
        }
        
        // Reset to initial heights when paused
        wave1.HeightRequest = 12;
        wave2.HeightRequest = 18;
        wave3.HeightRequest = 10;
        wave4.HeightRequest = 22;
        wave5.HeightRequest = 14;
        wave6.HeightRequest = 19;
    }

    private void OnPlayAudioClicked(object sender, EventArgs e)
    {
        if (_isPaused)
        {
            _isPaused = false;
            playIcon.IsVisible = false;
            pauseIcon.IsVisible = true;
            _ = PlayAudioWithChunksAsync();
        }
        else if (pauseIcon.IsVisible)
        {
            _isPaused = true;
            playIcon.IsVisible = true;
            pauseIcon.IsVisible = false;
            _audioStopwatch.Stop();
            _currentSentenceCts?.Cancel();
            _ttsCts?.Cancel();
            TrackingService.Stop();
        }
        else
        {
            _ = PlayAudioWithChunksAsync();
        }
    }

    private void OnReplayAudioClicked(object sender, EventArgs e)
    {
        ReportAndResetAudioStats();
        _ttsCts?.Cancel();
        _currentChunkIndex = 0;
        _isPaused = false;
        _ = PlayAudioWithChunksAsync();
    }

    private void OnClosePlayerClicked(object sender, EventArgs e)
    {
        ReportAndResetAudioStats();
        _ttsCts?.Cancel();
        miniPlayer.IsVisible = false;
        scanButton.IsVisible = true;
        _isScanning = false;
        _hasDetected = false;
        _currentChunkIndex = 0;
        magicLens.Stroke = new SolidColorBrush(Color.FromArgb("#FF6B81"));
    }

    private void OnDetailClicked(object sender, EventArgs e)
    {
        if (_shop == null) return;
        ReportAndResetAudioStats();
        _ttsCts?.Cancel();
        MainPage.PendingOpenFoodId = _shop.id;
        Shell.Current.GoToAsync("//MainPage");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LocalizeUI();
        if (_shouldResumeOnAppearing && _shop != null && _chunks.Length > 0)
        {
            _ = PlayAudioWithChunksAsync();
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
        AudioGuideHeaderLabel.Text = LocalizationService.Instance.Get("audio_guide_header");
        UpdateScanButtonText();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ReportAndResetAudioStats();
        // Record if it was playing (not paused and pauseIcon is visible)
        _shouldResumeOnAppearing = !_isPaused && pauseIcon.IsVisible;
        _ttsCts?.Cancel();
    }

    private void ReportAndResetAudioStats()
    {
        _audioStopwatch.Stop();
        if (_audioStopwatch.Elapsed.TotalSeconds >= 1 && _shop != null)
        {
            _ = SendAudioLogAsync(_shop.id, (int)_audioStopwatch.Elapsed.TotalSeconds);
        }
        _audioStopwatch.Reset();
        TrackingService.Stop();
    }

    private async Task SendAudioLogAsync(int poiId, int duration)
    {
        try
        {
            int userId = Preferences.Default.Get("user_id", 1);
            var payload = new { user_id = userId, duration_seconds = duration };
            
            using HttpClient client = new HttpClient();
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            await client.PostAsync($"{AppConfig.FoodApiUrl}/{poiId}/audio-log", content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG: Error sending QR audio log: {ex.Message}");
        }
    }
}
