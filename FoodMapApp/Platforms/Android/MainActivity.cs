using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;

namespace FoodMapApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(new[] { Android.Content.Intent.ActionView },
                  Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
                  DataScheme = "foodmap",
                  DataHost = "poi")]
    [IntentFilter(new[] { Android.Content.Intent.ActionView },
                  Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
                  DataScheme = "foodmap",
                  DataHost = "guest")]
    public class MainActivity : MauiAppCompatActivity, AudioManager.IOnAudioFocusChangeListener
    {
        private AudioManager? _audioManager;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _audioManager = (AudioManager?)GetSystemService(Context.AudioService);
            _audioManager?.RequestAudioFocus(this, Android.Media.Stream.Music, AudioFocus.Gain);
        }

        public void OnAudioFocusChange(AudioFocus focusChange)
        {
            if (focusChange == AudioFocus.Loss || focusChange == AudioFocus.LossTransient || focusChange == AudioFocus.LossTransientCanDuck)
            {
                MainPage.Instance?.HandleSystemInterruption();
            }
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            if (intent != null)
            {
                this.Intent = intent;
            }
        }
    }
}
