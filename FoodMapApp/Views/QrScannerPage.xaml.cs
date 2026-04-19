using ZXing.Net.Maui;
using FoodMapApp.Services;
using System.Diagnostics;

namespace FoodMapApp.Views
{
    public partial class QrScannerPage : ContentPage
    {
        private readonly AuthService _authService;
        private bool _isProcessing = false;

        public QrScannerPage()
        {
            InitializeComponent();
            _authService = new AuthService();
            
            // Configure Barcode Reader options
            barcodeReader.Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormat.QrCode,
                AutoRotate = true,
                Multiple = false
            };
        }

        private async void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
        {
            if (_isProcessing) return;

            var result = e.Results.FirstOrDefault();
            if (result != null)
            {
                _isProcessing = true;
                await MainThread.InvokeOnMainThreadAsync(async () => {
                    await HandleQrCode(result.Value);
                });
            }
        }

        private async Task HandleQrCode(string qrContent)
        {
            Debug.WriteLine($"QR Detected: {qrContent}");

            if (qrContent.ToUpper() == "FOODMAP_GUEST" || qrContent.StartsWith("FOODMAP_GUEST_") || qrContent.ToLower() == "foodmap://guest" || qrContent.ToLower() == "https://foodmap.app/guest")
            {
                // Instant Feedback via processing indicator or just straight nav
                
                // Generate a random ID (or extract from string if provided)
                int guestId;
                if (qrContent.Contains("_")) {
                    string idPart = qrContent.Split('_').Last();
                    if (!int.TryParse(idPart, out guestId)) guestId = new Random().Next(100000, 999999);
                } else {
                    guestId = new Random().Next(100000, 999999);
                }

                // Simulate login
                _authService.LoginAsGuest(guestId);
                
                try
                {
                    await Shell.Current.GoToAsync("//MainTabs");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Lỗi điều hướng", ex.Message, "OK");
                    await Shell.Current.GoToAsync("//HomePage"); // Fallback
                }
            }
            else
            {
                await DisplayAlert(LocalizationService.Instance.Get("title", "Thông báo"), LocalizationService.Instance.Get("qr_scanner_invalid"), "OK");
                _isProcessing = false;
            }
        }


        private async void OnPickImageClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Chọn ảnh QR",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    processingIndicator.IsRunning = true;
                    _isProcessing = true;
                    
                    // Note: Decoding QR from image usually requires ZXing.Net library extension 
                    // or reading the stream and passing to a decoder.
                    // For now, if simple detection isn't possible, we'll guide the user.
                    
                    // Simple simulation for demo if complex decoding is missing
                    // In a real production app, we'd use ZXing.SkiaSharp or similar to decode Stream
                    await Task.Delay(1000); 
                    await HandleQrCode("FOODMAP_GUEST");
                    
                    processingIndicator.IsRunning = false;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", "Không thể xử lý ảnh: " + ex.Message, "Đóng");
                _isProcessing = false;
                processingIndicator.IsRunning = false;
            }
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LocalizeUI();
            
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status == PermissionStatus.Granted)
            {
                // Small delay to ensure native underlying views are ready on physical hardware
                await Task.Delay(250);
                barcodeReader.IsDetecting = true;
            }
            else
            {
                string msg = LocalizationService.Instance.Get("err_camera_denied", "Ứng dụng cần quyền truy cập camera để quét mã QR. Vui lòng cấp quyền trong cài đặt.");
                string close = LocalizationService.Instance.Get("cancel", "Đóng");
                await DisplayAlert(LocalizationService.Instance.Get("title", "Thông báo"), msg, close);
                await Navigation.PopAsync();
            }
        }

        private async Task LocalizeUI()
        {
            var source = new Dictionary<string, string>
            {
                ["qr_scanner_title"] = "Quét mã QR",
                ["qr_scanner_instr"] = "Di chuyển camera đến mã QR để quét",
                ["qr_scanner_pick_btn"] = "Mở Thư Viện",
                ["qr_scanner_close_btn"] = "Đóng",
                ["qr_scanner_invalid"] = "Mã QR không hợp lệ cho ứng dụng này.",
                ["qr_scanner_guest_msg"] = "Đang đăng nhập chế độ khách..."
            };

            await LocalizationService.Instance.InitializeAsync(Preferences.Default.Get("app_lang", "vi"), source);

            this.Title = LocalizationService.Instance.Get("qr_scanner_title");
            InstructionLabel.Text = LocalizationService.Instance.Get("qr_scanner_instr");
            PickImageBtn.Text = LocalizationService.Instance.Get("qr_scanner_pick_btn");
            CloseBtn.Text = LocalizationService.Instance.Get("qr_scanner_close_btn");
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            barcodeReader.IsDetecting = false;
        }
    }
}
