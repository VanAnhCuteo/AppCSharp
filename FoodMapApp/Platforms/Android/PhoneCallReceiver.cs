using Android.App;
using Android.Content;
using Android.Telephony;
using FoodMapApp.Services;

namespace FoodMapApp.Platforms.Android
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { "android.intent.action.PHONE_STATE" })]
    public class PhoneCallReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action == "android.intent.action.PHONE_STATE")
            {
                string state = intent.GetStringExtra(TelephonyManager.ExtraState);
                if (state == TelephonyManager.ExtraStateRinging || state == TelephonyManager.ExtraStateOffhook)
                {
                    // Call incoming or active
                    AutoAudioService.Instance.SetCallStatus(true);
                }
                else if (state == TelephonyManager.ExtraStateIdle)
                {
                    // Call ended
                    AutoAudioService.Instance.SetCallStatus(false);
                }
            }
        }
    }
}
