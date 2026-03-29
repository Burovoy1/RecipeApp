using RecipeApp.ViewModels;

namespace RecipeApp.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _vm;

    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _vm.LoadAsync();
    }

    private async void OnMenuClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet(
            "Дополнительно", "Отмена", null,
            "База продуктов", "Дневник питания");

        switch (action)
        {
            case "База продуктов":
                await Shell.Current.GoToAsync("//fooddb");
                break;
            case "Дневник питания":
                await Shell.Current.GoToAsync("//nutrition");
                break;
        }
    }
}
