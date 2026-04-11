using AVFoundation;
using Foundation;
using UIKit;

namespace FoodMapApp
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            var result = base.FinishedLaunching(application, launchOptions);

            NSNotificationCenter.DefaultCenter.AddObserver(AVAudioSession.InterruptionNotification, (notification) =>
            {
                var userInfo = notification.UserInfo;
                if (userInfo != null && userInfo.ContainsKey(AVAudioSession.InterruptionTypeKey))
                {
                    var type = (AVAudioSessionInterruptionType)((NSNumber)userInfo[AVAudioSession.InterruptionTypeKey]).UInt32Value;
                    if (type == AVAudioSessionInterruptionType.Began)
                    {
                        MainPage.Instance?.HandleSystemInterruption();
                    }
                }
            });

            return result;
        }
    }
}
