using RecipeApp.Models;
using RecipeApp.ViewModels;

namespace RecipeApp.Views;

[QueryProperty(nameof(Recipe), "Recipe")]
[QueryProperty(nameof(Mode), "mode")]
public partial class EditRecipePage : ContentPage
{
    private readonly EditRecipeViewModel _vm;

    public string? Mode { get; set; }

    private Recipe? _recipe;
    public Recipe? Recipe
    {
        get => _recipe;
        set
        {
            _recipe = value;
            if (value != null)
                _vm.LoadRecipe(value);
        }
    }

    public EditRecipePage(EditRecipeViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;

        _vm.OnIngredientSavedToDb += async name =>
        {
            await DisplayAlert("Сохранено", $"«{name}» добавлен в базу продуктов", "OK");
        };

        _vm.OnPickImageError += async msg =>
        {
            await DisplayAlert("Ошибка фото", msg, "OK");
        };
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_vm.Title))
        {
            await DisplayAlert("Ошибка", "Введите название рецепта", "OK");
            return;
        }
        await _vm.SaveAsync();
        if (_vm.IsSaved)
            await Shell.Current.GoToAsync("..");
    }
}
