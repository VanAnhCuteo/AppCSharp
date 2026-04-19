using FoodMapApp.Models;
using FoodMapApp.Services;

namespace FoodMapApp.Views
{
    public partial class AudioHistoryPage : ContentPage
    {
        public AudioHistoryPage()
        {
            InitializeComponent();
            LoadHistory();

            refreshView.Command = new Command(async () => {
                await LoadHistory();
                refreshView.IsRefreshing = false;
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LocalizeUI();
        }

        private async Task LocalizeUI()
        {
            var source = new Dictionary<string, string>
            {
                ["audio_history_title"] = "Lịch sử nghe Audio",
                ["audio_history_empty"] = "Bạn chưa nghe audio nào"
            };

            await LocalizationService.Instance.InitializeAsync(Preferences.Default.Get("app_lang", "vi"), source);

            HistoryTitleLabel.Text = LocalizationService.Instance.Get("audio_history_title");
            EmptyHistoryLabel.Text = LocalizationService.Instance.Get("audio_history_empty");
        }

        private async Task LoadHistory()
        {
            if (loadingOverlay == null) return;
            
            loadingOverlay.IsVisible = true;
            try
            {
                int userId = AuthService.CurrentUserId;
                if (userId != 0)
                {
                    string url = $"{AppConfig.FoodApiUrl}/audio-history/{userId}";
                    var history = await HttpService.GetAsync<List<AudioLogModel>>(url);
                    
                    if (history != null)
                    {
                        historyListView.ItemsSource = history;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading audio history: {ex.Message}");
            }
            finally
            {
                loadingOverlay.IsVisible = false;
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
