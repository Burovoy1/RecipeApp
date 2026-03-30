using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.Views;

[QueryProperty(nameof(Recipe), "Recipe")]
public partial class RecipeDetailPage : ContentPage
{
    private readonly RecipeService _service = new();
    private Recipe? _recipe;

    public Recipe? Recipe
    {
        get => _recipe;
        set
        {
            _recipe = value;
            BindingContext = value;
        }
    }

    public RecipeDetailPage()
    {
        InitializeComponent();
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        if (_recipe == null) return;
        await Shell.Current.GoToAsync(nameof(EditRecipePage),
            new Dictionary<string, object> { ["Recipe"] = _recipe });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Reload from DB every time page appears — picks up edits and fresh data
        if (_recipe?.Id > 0)
        {
            var updated = await _service.GetByIdAsync(_recipe.Id);
            if (updated != null)
            {
                _recipe = updated;
                BindingContext = null;
                BindingContext = _recipe;
            }
        }
    }
}
