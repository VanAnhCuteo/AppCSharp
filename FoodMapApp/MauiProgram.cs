using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;

namespace FoodMapApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .UseBarcodeReader()
                .ConfigureMauiHandlers(handlers =>
                {
#if ANDROID
                    handlers.AddHandler<WebView, Microsoft.Maui.Handlers.WebViewHandler>();
    				Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("MyGeoHandler", (handler, view) =>
    				{
    					handler.PlatformView.SetWebChromeClient(new MyWebChromeClient());
    				});
#endif
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }

#if ANDROID
    public class MyWebChromeClient : Android.Webkit.WebChromeClient
    {
        public override void OnGeolocationPermissionsShowPrompt(string? origin, Android.Webkit.GeolocationPermissions.ICallback? callback)
        {
            callback?.Invoke(origin, true, false);
        }
    }
#endif
}
