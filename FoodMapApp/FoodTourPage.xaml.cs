namespace FoodMapApp;

public partial class FoodTourPage : ContentPage
{
    public FoodTourPage()
    {
        InitializeComponent();
    }

    private async void StartTour(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MainPage());
    }
}