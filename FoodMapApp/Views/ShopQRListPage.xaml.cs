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
        await LoadShopsAsync();
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
            await DisplayAlert("Lỗi", "Không thể tải danh sách quán: " + ex.Message, "OK");
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
