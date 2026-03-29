using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.ViewModels;

public partial class EditRecipeViewModel : ObservableObject
{
    private readonly RecipeService   _service     = new();
    private readonly FoodItemService _foodService = new();

    [ObservableProperty] private int     _id;
    [ObservableProperty] private string  _title       = string.Empty;
    [ObservableProperty] private string  _description = string.Empty;
    [ObservableProperty] private string  _category    = string.Empty;
    [ObservableProperty] private string  _prepTimeText  = string.Empty;
    [ObservableProperty] private string  _cookTimeText  = string.Empty;
    [ObservableProperty] private string  _servingsText  = string.Empty;
    [ObservableProperty] private string  _difficulty  = "Средняя";
    [ObservableProperty] private string? _imagePath;
    [ObservableProperty] private ObservableCollection<Ingredient> _ingredients = new();
    [ObservableProperty] private ObservableCollection<Step>       _steps       = new();

    // ── Выбор из базы: категория → ингредиент ────────────────────
    [ObservableProperty] private ObservableCollection<string>   _dbCategories     = new();
    [ObservableProperty] private string?                        _selectedDbCategory;
    [ObservableProperty] private ObservableCollection<FoodItem> _dbItemsInCategory = new();
    [ObservableProperty] private FoodItem?                      _selectedFoodItem;

    // ── Поля нового ингредиента ───────────────────────────────────
    [ObservableProperty] private string _newIngredientName     = string.Empty;
    [ObservableProperty] private string _newIngredientAmount   = string.Empty;
    [ObservableProperty] private string _newIngredientUnit     = "г";
    [ObservableProperty] private string _newIngredientCalories = string.Empty;
    [ObservableProperty] private string _newIngredientProtein  = string.Empty;
    [ObservableProperty] private string _newIngredientFat      = string.Empty;
    [ObservableProperty] private string _newIngredientCarbs    = string.Empty;
    [ObservableProperty] private bool   _showSaveToDb;

    // ── Шаги ──────────────────────────────────────────────────────
    [ObservableProperty] private string _newStepDescription = string.Empty;

    // ── Итоговое КБЖУ ────────────────────────────────────────────
    [ObservableProperty] private double _totalCalories;
    [ObservableProperty] private double _totalProtein;
    [ObservableProperty] private double _totalFat;
    [ObservableProperty] private double _totalCarbs;

    public static IEnumerable<string> Difficulties    => new[] { "Лёгкая", "Средняя", "Сложная" };
    public static IEnumerable<string> CategoryOptions => new[]
        { "Завтрак", "Обед", "Ужин", "Десерт", "Закуска", "Суп", "Салат", "Выпечка", "Напиток", "Другое" };
    public static IEnumerable<string> UnitOptions => new[] { "г", "мл", "шт", "ст.л.", "ч.л." };

    public bool IsSaved { get; private set; }
    public event Action<string>? OnIngredientSavedToDb;

    public EditRecipeViewModel()
    {
        _ = LoadDbCategoriesAsync();
    }

    // Загрузить категории из базы
    private async Task LoadDbCategoriesAsync()
    {
        var cats = await _foodService.GetCategoriesAsync();
        DbCategories = new ObservableCollection<string>(cats);
    }

    // При смене категории — загружаем ингредиенты из неё
    async partial void OnSelectedDbCategoryChanged(string? value)
    {
        SelectedFoodItem = null;
        if (string.IsNullOrEmpty(value))
        {
            DbItemsInCategory.Clear();
            return;
        }
        var items = await _foodService.GetAllAsync(category: value);
        DbItemsInCategory = new ObservableCollection<FoodItem>(items);
    }

    // При выборе ингредиента — заполняем поля
    partial void OnSelectedFoodItemChanged(FoodItem? value)
    {
        if (value == null) return;
        NewIngredientName     = value.Name;
        NewIngredientUnit     = value.DefaultUnit;
        NewIngredientCalories = value.CaloriesPer100.ToString("0.#");
        NewIngredientProtein  = value.ProteinPer100.ToString("0.#");
        NewIngredientFat      = value.FatPer100.ToString("0.#");
        NewIngredientCarbs    = value.CarbsPer100.ToString("0.#");
        NewIngredientAmount   = string.Empty;
        ShowSaveToDb          = false;
    }

    // Ручной ввод — предлагаем сохранить в базу
    partial void OnNewIngredientNameChanged(string value)
    {
        if (SelectedFoodItem != null && SelectedFoodItem.Name == value) return;
        ShowSaveToDb = !string.IsNullOrWhiteSpace(value) && SelectedFoodItem == null;
    }

    public void LoadRecipe(Recipe recipe)
    {
        Id            = recipe.Id;
        Title         = recipe.Title;
        Description   = recipe.Description;
        Category      = recipe.Category;
        PrepTimeText  = recipe.PrepTimeMinutes > 0 ? recipe.PrepTimeMinutes.ToString() : string.Empty;
        CookTimeText  = recipe.CookTimeMinutes > 0 ? recipe.CookTimeMinutes.ToString() : string.Empty;
        ServingsText  = recipe.Servings > 0 ? recipe.Servings.ToString() : string.Empty;
        Difficulty    = recipe.Difficulty;
        ImagePath     = recipe.ImagePath;
        Ingredients   = new ObservableCollection<Ingredient>(recipe.Ingredients);
        Steps         = new ObservableCollection<Step>(recipe.Steps.OrderBy(s => s.Order));
        RecalcNutrition();
    }

    [RelayCommand]
    public void ClearSelection()
    {
        SelectedFoodItem      = null;
        SelectedDbCategory    = null;
        NewIngredientName     = NewIngredientAmount = string.Empty;
        NewIngredientCalories = NewIngredientProtein =
        NewIngredientFat      = NewIngredientCarbs = string.Empty;
        NewIngredientUnit     = "г";
        ShowSaveToDb          = false;
    }

    [RelayCommand]
    public async Task SaveIngredientToDbAsync()
    {
        if (string.IsNullOrWhiteSpace(NewIngredientName)) return;
        var item = new FoodItem
        {
            Name           = NewIngredientName.Trim(),
            Category       = "Другое",
            DefaultUnit    = NewIngredientUnit,
            CaloriesPer100 = Parse(NewIngredientCalories),
            ProteinPer100  = Parse(NewIngredientProtein),
            FatPer100      = Parse(NewIngredientFat),
            CarbsPer100    = Parse(NewIngredientCarbs),
        };
        await _foodService.SaveAsync(item);
        SelectedFoodItem = item;
        ShowSaveToDb     = false;
        await LoadDbCategoriesAsync();
        OnIngredientSavedToDb?.Invoke(item.Name);
    }

    [RelayCommand]
    public void AddIngredient()
    {
        if (string.IsNullOrWhiteSpace(NewIngredientName)) return;

        Ingredients.Add(new Ingredient
        {
            Name           = NewIngredientName,
            Amount         = NewIngredientAmount,
            AmountGrams    = Parse(NewIngredientAmount),
            Unit           = NewIngredientUnit,
            CaloriesPer100 = Parse(NewIngredientCalories),
            ProteinPer100  = Parse(NewIngredientProtein),
            FatPer100      = Parse(NewIngredientFat),
            CarbsPer100    = Parse(NewIngredientCarbs),
        });

        // Сброс
        SelectedFoodItem   = null;
        SelectedDbCategory = null;
        NewIngredientName  = NewIngredientAmount =
        NewIngredientCalories = NewIngredientProtein =
        NewIngredientFat   = NewIngredientCarbs = string.Empty;
        NewIngredientUnit  = "г";
        ShowSaveToDb       = false;
        DbItemsInCategory.Clear();

        RecalcNutrition();
    }

    [RelayCommand]
    public void RemoveIngredient(Ingredient? ingredient)
    {
        if (ingredient == null) return;
        Ingredients.Remove(ingredient);
        RecalcNutrition();
    }

    [RelayCommand]
    public void AddStep()
    {
        if (string.IsNullOrWhiteSpace(NewStepDescription)) return;
        Steps.Add(new Step { Order = Steps.Count + 1, Description = NewStepDescription });
        NewStepDescription = string.Empty;
    }

    [RelayCommand]
    public void RemoveStep(Step? step)
    {
        if (step == null) return;
        Steps.Remove(step);
        for (int i = 0; i < Steps.Count; i++) Steps[i].Order = i + 1;
    }

    [RelayCommand]
    public async Task PickImageAsync()
    {
        try
        {
            var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Выберите изображение рецепта"
            });
            if (result != null)
            {
                // Copy to app data directory so the path persists
                var destDir = FileSystem.AppDataDirectory;
                var destPath = Path.Combine(destDir, result.FileName);
                using var src = await result.OpenReadAsync();
                using var dst = File.OpenWrite(destPath);
                await src.CopyToAsync(dst);
                ImagePath = destPath;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PickImage error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Title)) return;
        var recipe = new Recipe
        {
            Id              = Id,
            Title           = Title,
            Description     = Description,
            Category        = Category,
            PrepTimeMinutes = (int)Parse(PrepTimeText),
            CookTimeMinutes = (int)Parse(CookTimeText),
            Servings        = (int)Parse(ServingsText),
            Difficulty      = Difficulty,
            ImagePath       = ImagePath,
            Ingredients     = Ingredients.ToList(),
            Steps           = Steps.ToList()
        };
        await _service.SaveAsync(recipe);
        IsSaved = true;
    }

    private void RecalcNutrition()
    {
        TotalCalories = Ingredients.Sum(i => i.TotalCalories);
        TotalProtein  = Ingredients.Sum(i => i.TotalProtein);
        TotalFat      = Ingredients.Sum(i => i.TotalFat);
        TotalCarbs    = Ingredients.Sum(i => i.TotalCarbs);
    }

    private static double Parse(string s)
    {
        double.TryParse(s.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out double v);
        return v;
    }
}

