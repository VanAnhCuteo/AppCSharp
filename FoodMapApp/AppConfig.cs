using Microsoft.Maui.Devices;

namespace FoodMapApp
{
    public static class AppConfig
    {
        // For Android Emulator (AVD): use 10.0.2.2
        // For physical device on same WiFi: use your PC's local IP (e.g. 192.168.31.209)
        private const string PhysicalDeviceIp = "172.20.10.3";

        public static string BackendIp =>
            DeviceInfo.Platform == DevicePlatform.Android &&
            DeviceInfo.DeviceType == DeviceType.Virtual
                ? "10.0.2.2"           // Android Emulator
                : (DeviceInfo.Platform == DevicePlatform.WinUI 
                    ? "localhost"      // Windows (Local)
                    : PhysicalDeviceIp); // Physical device on same WiFi

        public static string BaseUrl    => $"http://{BackendIp}:5000/api";
        public static string FoodApiUrl => $"{BaseUrl}/Food";
        public static string AuthApiUrl => $"{BaseUrl}/Auth";
    }
}
