using RecipeApp.Models;

namespace RecipeApp.Views;

[QueryProperty(nameof(Recipe), "Recipe")]
public partial class RecipeDetailPage : ContentPage
{
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
        var result = await Shell.Current.GoToAsync(nameof(EditRecipePage),
            new Dictionary<string, object> { ["Recipe"] = _recipe });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Refresh recipe data if returning from edit
        if (_recipe != null)
            BindingContext = null;  // force refresh
        BindingContext = _recipe;
    }
}
