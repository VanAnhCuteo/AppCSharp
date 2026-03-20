namespace FoodMapApp;

public partial class FoodTourPage : ContentPage
{
    public FoodTourPage()
    {
        InitializeComponent();
    }

    private async void StartTour(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//HomePage");
    }
}