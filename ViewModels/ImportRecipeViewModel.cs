using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.ViewModels;

public partial class ImportRecipeViewModel : ObservableObject
{
    private readonly RecipeImportService _importer = new();
    private readonly RecipeService       _saver    = new();
    private ImportedRecipe?              _imported;

    [ObservableProperty] private string  _url          = string.Empty;
    [ObservableProperty] private bool    _isLoading;
    [ObservableProperty] private bool    _hasResult;
    [ObservableProperty] private string  _errorMessage = string.Empty;
    [ObservableProperty] private bool    _hasError;

    // Поля предпросмотра (редактируемые)
    [ObservableProperty] private string  _title        = string.Empty;
    [ObservableProperty] private string  _description  = string.Empty;
    [ObservableProperty] private string  _prepTime     = string.Empty;
    [ObservableProperty] private string  _cookTime     = string.Empty;
    [ObservableProperty] private string  _servings     = string.Empty;
    [ObservableProperty] private string? _imagePath;
    [ObservableProperty] private ObservableCollection<ImportedIngredient> _ingredients = new();
    [ObservableProperty] private double _calories;
    [ObservableProperty] private double _protein;
    [ObservableProperty] private double _fat;
    [ObservableProperty] private double _carbs;
    [ObservableProperty] private bool   _hasNutrition;
    [ObservableProperty] private ObservableCollection<string>             _steps       = new();

    public bool IsSaved { get; private set; }

    [RelayCommand]
    public async Task ImportAsync()
    {
        if (string.IsNullOrWhiteSpace(Url)) return;

        IsLoading  = true;
        HasError   = false;
        HasResult  = false;
        ErrorMessage = string.Empty;

        try
        {
            var url = Url.Trim();
            if (!url.StartsWith("http")) url = "https://" + url;

            _imported   = await _importer.ImportFromUrlAsync(url);
            Title       = _imported.Title;
            Description = _imported.Description;
            PrepTime    = _imported.PrepTime > 0 ? _imported.PrepTime.ToString() : string.Empty;
            CookTime    = _imported.CookTime > 0 ? _imported.CookTime.ToString() : string.Empty;
            Servings    = _imported.Servings > 0 ? _imported.Servings.ToString() : string.Empty;
            ImagePath   = _imported.LocalImagePath;
            Ingredients  = new ObservableCollection<ImportedIngredient>(_imported.Ingredients);
            Steps        = new ObservableCollection<string>(_imported.Steps);
            Calories     = _imported.Calories;
            Protein      = _imported.Protein;
            Fat          = _imported.Fat;
            Carbs        = _imported.Carbs;
            HasNutrition = _imported.Calories > 0;
            HasResult    = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            HasError     = true;
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public void RemoveIngredient(ImportedIngredient? item)
    {
        if (item != null) Ingredients.Remove(item);
    }

    [RelayCommand]
    public void RemoveStep(string? step)
    {
        if (step != null) Steps.Remove(step);
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Title)) return;

        int.TryParse(PrepTime, out int prep);
        int.TryParse(CookTime, out int cook);
        int.TryParse(Servings,  out int serv);

        var recipe = new Recipe
        {
            Title           = Title,
            Description     = Description,
            PrepTimeMinutes = prep,
            CookTimeMinutes = cook,
            Servings        = serv,
            ImagePath       = ImagePath,
            Ingredients     = Ingredients.Select((ing, i) => new Ingredient
            {
                Name      = ing.Name,
                Amount    = ing.Amount,
                AmountGrams = double.TryParse(ing.Amount, out var g) ? g : 0,
                Unit      = ing.Unit,
            }).ToList(),
            Steps = Steps.Select((s, i) => new Step
            {
                Order       = i + 1,
                Description = s
            }).ToList()
        };

        await _saver.SaveAsync(recipe);
        IsSaved = true;
    }
}
