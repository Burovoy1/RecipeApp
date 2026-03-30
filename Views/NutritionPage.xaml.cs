using RecipeApp.ViewModels;

namespace RecipeApp.Views;

public partial class NutritionPage : ContentPage
{
    private readonly NutritionViewModel _vm;

    public NutritionPage(NutritionViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;

        _vm.OnAddError += async msg =>
            await DisplayAlert("Ошибка", msg, "OK");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _vm.LoadAsync();
    }

    private void OnModeIngredient(object sender, EventArgs e) => _vm.AddMode = "ingredient";
    private void OnModeRecipe(object sender, EventArgs e)     => _vm.AddMode = "recipe";
    private void OnModeManual(object sender, EventArgs e)     => _vm.AddMode = "manual";
}
