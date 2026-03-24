using Microsoft.Maui.Devices;

namespace FoodMapApp
{
    public static class AppConfig
    {
        // Using 127.0.0.1 for Android via ADB Reverse (tcp:5000 tcp:5000)
        // This is the most reliable way to bypass firewall issues.
        public static string BackendIp => DeviceInfo.Platform == DevicePlatform.Android ? "10.0.2.2" : "localhost";
        
        public static string BaseUrl => $"http://{BackendIp}:5000/api";
        public static string FoodApiUrl => $"{BaseUrl}/Food";
        public static string AuthApiUrl => $"{BaseUrl}/Auth";
    }
}
