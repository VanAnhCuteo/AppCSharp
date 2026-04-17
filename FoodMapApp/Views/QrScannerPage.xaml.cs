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
            if (string.IsNullOrWhiteSpace(qrContent)) return;
            
            // Làm sạch nội dung mã QR
            string cleanQr = qrContent.Trim();
            string qrLower = cleanQr.ToLower();
            Debug.WriteLine($"QR Cleaned: {cleanQr}");

            // 1. Xử lý đăng nhập khách (Link guest)
            if (qrLower.Contains("foodmap_guest") || qrLower.Contains("foodmap://guest") || qrLower.Contains("foodmap.app/guest"))
            {
                await DisplayAlert("Thành công", "Đang đăng nhập chế độ khách...", "Tiếp tục");
                
                int guestId = new Random().Next(100000, 999999);
                if (cleanQr.Contains("_")) {
                    string idPart = cleanQr.Split('_').Last();
                    int pid;
                    if (int.TryParse(idPart, out pid)) guestId = pid;
                }

                _authService.LoginAsGuest(guestId);
                await Shell.Current.GoToAsync("//HomePage");
            }
            // 2. Xử lý quét mã Quán ăn (Link POI / Audio)
            else if (qrLower.Contains("foodmap://poi/") || qrLower.Contains("foodmap.app/poi/") || qrLower.Contains("foodmap_poi_"))
            {
                // Trích xuất ID từ bất kỳ định dạng nào (url hoặc vắn tắt)
                string idStr = "";
                if (qrLower.Contains("/")) idStr = qrLower.Split('/').Last();
                else if (qrLower.Contains("_")) idStr = qrLower.Split('_').Last();

                if (int.TryParse(idStr, out int id))
                {
                    // Tự động đăng nhập khách nếu chưa có tài khoản
                    if (!_authService.IsLoggedIn)
                    {
                        int guestId = new Random().Next(100000, 999999);
                        _authService.LoginAsGuest(guestId);
                    }

                    // Chuyển hướng đến bản đồ và mở chi tiết quán
                    FoodMapApp.MainPage.PendingOpenFoodId = id;
                    
                    // Nếu đang ở trang login, dùng //MainPage. Nếu đang ở trong app, Navigation.PopAsync() hoặc GoToAsync.
                    if (Shell.Current.CurrentPage is LoginPage) {
                        await Shell.Current.GoToAsync("//MainPage");
                    } else {
                        await Navigation.PopToRootAsync();
                        await Shell.Current.GoToAsync("//MainPage");
                    }
                    
                    if (FoodMapApp.MainPage.Instance != null)
                    {
                        await FoodMapApp.MainPage.Instance.TryOpenPendingDetail();
                    }
                }
                else
                {
                    await DisplayAlert("Lỗi", "Mã quán ăn không hợp lệ.", "Thử lại");
                    _isProcessing = false;
                }
            }
            else
            {
                await DisplayAlert("Thông báo", $"Mã QR không hợp lệ: {cleanQr}", "Thử lại");
                _isProcessing = false;
            }
        }

        private void SetGuestSession(int guestId)
        {
            Preferences.Default.Set("user_id", -guestId);
            Preferences.Default.Set("username", $"Khách {guestId}");
            Preferences.Default.Set("role", "guest");
            Preferences.Default.Set("is_logged_in", true);
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
            
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status == PermissionStatus.Granted)
            {
                barcodeReader.IsDetecting = true;
            }
            else
            {
                await DisplayAlert("Quyền truy cập camera", "Ứng dụng cần quyền truy cập camera để quét mã QR. Vui lòng cấp quyền trong cài đặt.", "Đóng");
                await Navigation.PopAsync();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            barcodeReader.IsDetecting = false;
        }
    }
}
