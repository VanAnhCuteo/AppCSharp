using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;

namespace FoodMapApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    
    // Custom Deep Link: foodmap://
    [IntentFilter(new[] { Android.Content.Intent.ActionView },
                  Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
                  DataScheme = "foodmap")]

    // App Link (HTTPS) for Zalo Compatibility and Web redirection
    [IntentFilter(new[] { Android.Content.Intent.ActionView },
                  Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
                  DataScheme = "https",
                  DataHost = "foodmap.app",
                  DataPathPrefix = "/guest",
                  AutoVerify = true)]
    [IntentFilter(new[] { Android.Content.Intent.ActionView },
                  Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
                  DataScheme = "https",
                  DataHost = "foodmap.app",
                  DataPathPrefix = "/poi",
                  AutoVerify = true)]
    [IntentFilter(new[] { Android.Content.Intent.ActionView },
                  Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
                  DataScheme = "https",
                  DataHost = "foodmap.app",
                  DataPathPrefix = "/audio",
                  AutoVerify = true)]
    public class MainActivity : MauiAppCompatActivity, AudioManager.IOnAudioFocusChangeListener
    {
        private AudioManager? _audioManager;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _audioManager = (AudioManager?)GetSystemService(Context.AudioService);
            _audioManager?.RequestAudioFocus(this, Android.Media.Stream.Music, AudioFocus.Gain);

            // Handle deep link on cold start
            HandleDeepLinkIntent(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);

            // Handle deep link when app is already running (SingleTask)
            if (intent != null)
            {
                HandleDeepLinkIntent(intent);
            }
        }

        private void HandleDeepLinkIntent(Intent? intent)
        {
            var data = intent?.Data;
            if (data == null) return;

            string? uri = data.ToString();
            if (string.IsNullOrEmpty(uri)) return;

            System.Diagnostics.Debug.WriteLine($"[DeepLink] Intent data: {uri}");

            // Store URI and let App.xaml.cs process it
            App.PendingDeepLinkUri = uri;
        }

        public void OnAudioFocusChange(AudioFocus focusChange)
        {
            if (focusChange == AudioFocus.Loss || focusChange == AudioFocus.LossTransient || focusChange == AudioFocus.LossTransientCanDuck)
            {
                MainPage.Instance?.HandleSystemInterruption();
            }
        }
    }
}
