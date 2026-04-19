using System.Collections.ObjectModel;
using System.Text.Json;
using FoodMapApp.Models;
using FoodMapApp.Services;

namespace FoodMapApp.Views;

public partial class ShopQRListPage : ContentPage
{
	public ShopQRListPage()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LocalizeUI();
        await LoadShopsAsync();
    }

    private async Task LocalizeUI()
    {
        var source = new Dictionary<string, string>
        {
            ["qr_list_title"] = "Danh sách quán ăn",
            ["qr_list_instruction"] = "Chọn quán để xem mã QR",
            ["qr_list_sub"] = "Mã QR giúp bạn nhận diện quán nhanh chóng",
            ["err_load_shops"] = "Không thể tải danh sách quán"
        };

        await LocalizationService.Instance.InitializeAsync(Preferences.Default.Get("app_lang", "vi"), source);

        this.Title = LocalizationService.Instance.Get("qr_list_title");
        InstructionsTitleLabel.Text = LocalizationService.Instance.Get("qr_list_instruction");
        InstructionsSubTitleLabel.Text = LocalizationService.Instance.Get("qr_list_sub");
    }

    private async Task LoadShopsAsync()
    {
        loadingIndicator.IsRunning = true;
        loadingIndicator.IsVisible = true;

        try
        {
            var foods = await HttpService.GetWithCacheAsync<List<Models.FoodModel>>($"{AppConfig.FoodApiUrl}?lang=vi", "shop_qr_list_cache");
            shopsCollection.ItemsSource = foods;
        }
        catch (Exception ex)
        {
            await DisplayAlert(LocalizationService.Instance.Get("err_title", "Lỗi"), LocalizationService.Instance.Get("err_load_shops") + ": " + ex.Message, "OK");
        }
        finally
        {
            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;
        }
    }

    private async void OnShopTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Models.FoodModel selectedShop)
        {
            try
            {
                await Shell.Current.GoToAsync("QRViewerPage", new Dictionary<string, object>
                {
                    { "Shop", selectedShop }
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi điều hướng", ex.Message, "OK");
            }
        }
    }
}
