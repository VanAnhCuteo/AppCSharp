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
            string content = qrContent.Trim();
            System.Diagnostics.Debug.WriteLine($"QR Content Detected: {content}");

            // 1. Tự động đăng nhập khách nếu chưa đăng nhập
            if (!_authService.IsLoggedIn)
            {
                int guestId = new Random().Next(100000, 999999);
                _authService.LoginAsGuest(guestId);
            }

            // 2. Phân loại nội dung QR để điều hướng
            bool isGuestQr = content.Equals("FOODMAP_GUEST", StringComparison.OrdinalIgnoreCase)
                          || content.Equals("foodmap://guest", StringComparison.OrdinalIgnoreCase)
                          || content.Contains("foodmap.app/guest", StringComparison.OrdinalIgnoreCase);

            if (isGuestQr)
            {
                // Chuyển về Trang Chủ
                await Shell.Current.GoToAsync("//MainTabs/HomePage");
            }
            else if (content.Contains("foodmap://audio/", StringComparison.OrdinalIgnoreCase) || 
                     content.Contains("foodmap.app/audio/", StringComparison.OrdinalIgnoreCase))
            {
                // Xử lý link Audio (Ví dụ: foodmap://audio/5 hoặc https://foodmap.app/audio/5)
                try
                {
                    string idPart = "";
                    if (content.Contains("audio/"))
                    {
                        idPart = content.Substring(content.IndexOf("audio/") + 6).Split('/')[0].Split('?')[0];
                    }

                    if (int.TryParse(idPart, out int id))
                    {
                        // Chuyển đến trang xem mã QR và tự động phát audio
                        await Shell.Current.GoToAsync($"QRViewerPage?id={id}&auto=true");
                    }
                    else
                    {
                        await DisplayAlert("Lỗi", "ID âm thanh không hợp lệ", "OK");
                        _isProcessing = false;
                    }
                }
                catch
                {
                    await DisplayAlert("Lỗi", "Không thể xử lý mã QR Audio này", "OK");
                    _isProcessing = false;
                }
            }
            else if (content.Contains("foodmap://poi/", StringComparison.OrdinalIgnoreCase) || 
                     content.Contains("foodmap.app/poi/", StringComparison.OrdinalIgnoreCase))
            {
                // Xử lý link POI (ví dụ: foodmap://poi/123 hoặc https://foodmap.app/poi/123)
                try
                {
                    string idPart = "";
                    if (content.Contains("poi/"))
                    {
                        idPart = content.Substring(content.IndexOf("poi/") + 4).Split('/')[0].Split('?')[0];
                    }

                    if (int.TryParse(idPart, out int id))
                    {
                        // Set pending ID để trang Map tự mở khi xuất hiện
                        MainPage.PendingOpenFoodId = id;
                        await Shell.Current.GoToAsync("//MainTabs/MainPage");
                    }
                    else
                    {
                        await DisplayAlert("Lỗi", "Mã địa điểm không hợp lệ", "OK");
                        _isProcessing = false;
                    }
                }
                catch
                {
                    await DisplayAlert("Lỗi", "Không thể xử lý mã QR này", "OK");
                    _isProcessing = false;
                }
            }
            else
            {
                // Trường hợp mã không thuộc hệ thống FoodMap
                await DisplayAlert("Thông báo", $"Nội dung QR: {content}", "OK");
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
                    await Task.Delay(500); 
                    
                    string testContent = await DisplayPromptAsync("Giả lập QR (Debug)", "Nhập nội dung mã QR muốn kiểm tra:", "Quét", "Mặc định (Guest)", "foodmap://audio/14");
                    
                    if (string.IsNullOrEmpty(testContent))
                        await HandleQrCode("FOODMAP_GUEST");
                    else
                        await HandleQrCode(testContent);
                    
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
