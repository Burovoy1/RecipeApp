using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.ViewModels;

public partial class NutritionViewModel : ObservableObject
{
    private readonly NutritionService _service  = new();
    private readonly FoodItemService  _foodSvc  = new();
    private readonly RecipeService    _recipeSvc = new();

    // ── Профиль ──────────────────────────────────────────────────────
    [ObservableProperty] private string _profileName   = string.Empty;
    [ObservableProperty] private string _profileAge    = string.Empty;
    [ObservableProperty] private string _profileWeight = string.Empty;
    [ObservableProperty] private string _profileHeight = string.Empty;
    [ObservableProperty] private string _profileGender = "Мужской";
    [ObservableProperty] private string _profileActivity = "Полуактивный";

    // Нормы
    [ObservableProperty] private double _targetCalories;
    [ObservableProperty] private double _targetProtein;
    [ObservableProperty] private double _targetFat;
    [ObservableProperty] private double _targetCarbs;
    [ObservableProperty] private bool   _hasProfile;

    // ── Дневник ──────────────────────────────────────────────────────
    [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
    [ObservableProperty] private ObservableCollection<FoodDiaryEntry> _entries = new();

    // Факт за день
    [ObservableProperty] private double _eatenCalories;
    [ObservableProperty] private double _eatenProtein;
    [ObservableProperty] private double _eatenFat;
    [ObservableProperty] private double _eatenCarbs;

    // Прогресс (0..1)
    [ObservableProperty] private double _calProgress;
    [ObservableProperty] private double _protProgress;
    [ObservableProperty] private double _fatProgress;
    [ObservableProperty] private double _carbProgress;

    // Оставшееся
    [ObservableProperty] private double _remainCalories;
    [ObservableProperty] private double _remainProtein;
    [ObservableProperty] private double _remainFat;
    [ObservableProperty] private double _remainCarbs;

    // ── Форма добавления ─────────────────────────────────────────────
    [ObservableProperty] private bool   _showAddPanel;
    [ObservableProperty] private string _newFoodName    = string.Empty;
    [ObservableProperty] private string _newFoodAmount  = string.Empty;
    [ObservableProperty] private string _newFoodUnit    = "г";
    [ObservableProperty] private string _newFoodCalories = string.Empty;
    [ObservableProperty] private string _newFoodProtein  = string.Empty;
    [ObservableProperty] private string _newFoodFat      = string.Empty;
    [ObservableProperty] private string _newFoodCarbs    = string.Empty;
    [ObservableProperty] private string _newMealType    = "Завтрак";

    // Поиск по базе ингредиентов
    [ObservableProperty] private ObservableCollection<string>   _dbCategories     = new();
    [ObservableProperty] private string?                        _selectedDbCategory;
    [ObservableProperty] private ObservableCollection<FoodItem> _dbItemsInCategory = new();
    [ObservableProperty] private FoodItem?                      _selectedFoodItem;

    // Поиск по рецептам
    [ObservableProperty] private ObservableCollection<Recipe>   _recipes          = new();
    [ObservableProperty] private Recipe?                        _selectedRecipe;

    // Режим добавления
    [ObservableProperty] private string _addMode = "ingredient"; // "ingredient" | "recipe" | "manual"

    public bool IsIngredientMode => AddMode == "ingredient";
    public bool IsRecipeMode     => AddMode == "recipe";
    public bool IsManualMode     => AddMode == "manual";

    public IEnumerable<IGrouping<string, FoodDiaryEntry>> MealGroups =>
        Entries.GroupBy(e => e.MealType)
               .OrderBy(g => g.Key switch {
                   "Завтрак" => 0, "Обед" => 1, "Ужин" => 2, _ => 3 });

    public static IList<string> GenderOptions   => new[] { "Мужской", "Женский" };
    public static IList<string> ActivityOptions => new[] { "Сидячий", "Малоактивный", "Полуактивный", "Активный", "Очень активный" };
    public static IList<string> MealTypeOptions => new[] { "Завтрак", "Обед", "Ужин", "Перекус" };
    public static IList<string> UnitOptions     => new[] { "г", "мл", "шт", "ст.л.", "ч.л." };

    // Instance wrappers for XAML {Binding} (avoids XC0009 type mismatch with {x:Static})
    public IList<string> Genders    => GenderOptions;
    public IList<string> Activities => ActivityOptions;
    public IList<string> MealTypes  => MealTypeOptions;
    public IList<string> Units      => UnitOptions;

    public NutritionViewModel()
    {
        _ = InitAsync();
    }

    public async Task LoadAsync() => await InitAsync();

    private async Task InitAsync()
    {
        await LoadProfileAsync();
        await LoadDiaryAsync();
        await LoadDbCategoriesAsync();
        await LoadRecipesAsync();
    }

    // ── Профиль ──────────────────────────────────────────────────────
    private async Task LoadProfileAsync()
    {
        var p = await _service.GetProfileAsync();
        ProfileName     = p.Name;
        ProfileAge      = p.Age > 0     ? p.Age.ToString()    : string.Empty;
        ProfileWeight   = p.Weight > 0  ? p.Weight.ToString() : string.Empty;
        ProfileHeight   = p.Height > 0  ? p.Height.ToString() : string.Empty;
        ProfileGender   = p.Gender;
        ProfileActivity = p.Activity;
        UpdateTargets(p);
    }

    private void UpdateTargets(UserProfile p)
    {
        TargetCalories = p.DailyCalories;
        TargetProtein  = p.DailyProtein;
        TargetFat      = p.DailyFat;
        TargetCarbs    = p.DailyCarbs;
        HasProfile     = TargetCalories > 0;
        UpdateProgress();
    }

    [RelayCommand]
    public async Task SaveProfileAsync()
    {
        var p = new UserProfile
        {
            Name     = ProfileName,
            Age      = (int)Parse(ProfileAge),
            Weight   = Parse(ProfileWeight),
            Height   = Parse(ProfileHeight),
            Gender   = ProfileGender,
            Activity = ProfileActivity,
        };
        await _service.SaveProfileAsync(p);
        UpdateTargets(p);
    }

    // ── Дневник ──────────────────────────────────────────────────────
    private async Task LoadDiaryAsync()
    {
        var list = await _service.GetEntriesForDateAsync(SelectedDate);
        Entries = new ObservableCollection<FoodDiaryEntry>(list);
        RecalcTotals();
        OnPropertyChanged(nameof(Entries));
    }

    private void RecalcTotals()
    {
        EatenCalories = Entries.Sum(e => e.Calories);
        EatenProtein  = Entries.Sum(e => e.Protein);
        EatenFat      = Entries.Sum(e => e.Fat);
        EatenCarbs    = Entries.Sum(e => e.Carbs);
        UpdateProgress();
    }

    private void UpdateProgress()
    {
        CalProgress  = TargetCalories > 0 ? Math.Min(EatenCalories / TargetCalories, 1) : 0;
        ProtProgress = TargetProtein  > 0 ? Math.Min(EatenProtein  / TargetProtein,  1) : 0;
        FatProgress  = TargetFat      > 0 ? Math.Min(EatenFat      / TargetFat,      1) : 0;
        CarbProgress = TargetCarbs    > 0 ? Math.Min(EatenCarbs    / TargetCarbs,    1) : 0;

        RemainCalories = Math.Max(TargetCalories - EatenCalories, 0);
        RemainProtein  = Math.Max(TargetProtein  - EatenProtein,  0);
        RemainFat      = Math.Max(TargetFat      - EatenFat,      0);
        RemainCarbs    = Math.Max(TargetCarbs    - EatenCarbs,    0);
    }

    partial void OnSelectedDateChanged(DateTime value) => _ = LoadDiaryAsync();

    [RelayCommand] public void PrevDay() { SelectedDate = SelectedDate.AddDays(-1); }
    [RelayCommand] public void NextDay() { SelectedDate = SelectedDate.AddDays(1);  }

    [RelayCommand]
    public async Task DeleteEntryAsync(FoodDiaryEntry? e)
    {
        if (e == null) return;
        await _service.DeleteEntryAsync(e.Id);
        Entries.Remove(e);
        RecalcTotals();
        OnPropertyChanged(nameof(Entries));
    }

    // ── Добавление ───────────────────────────────────────────────────
    [RelayCommand] public void OpenAddPanel()  { ShowAddPanel = true;  ResetForm(); }
    [RelayCommand] public void CloseAddPanel() { ShowAddPanel = false; }

    private async Task LoadDbCategoriesAsync()
    {
        var cats = await _foodSvc.GetCategoriesAsync();
        DbCategories = new ObservableCollection<string>(cats);
    }

    private async Task LoadRecipesAsync()
    {
        var list = await _recipeSvc.GetAllAsync();
        Recipes = new ObservableCollection<Recipe>(list);
    }

    async partial void OnSelectedDbCategoryChanged(string? value)
    {
        SelectedFoodItem = null;
        if (string.IsNullOrEmpty(value)) { DbItemsInCategory.Clear(); return; }
        var items = await _foodSvc.GetAllAsync(category: value);
        DbItemsInCategory = new ObservableCollection<FoodItem>(items);
    }

    partial void OnSelectedFoodItemChanged(FoodItem? value)
    {
        if (value == null) return;
        NewFoodName     = value.Name;
        NewFoodUnit     = value.DefaultUnit;
        NewFoodCalories = value.CaloriesPer100.ToString("0.#");
        NewFoodProtein  = value.ProteinPer100.ToString("0.#");
        NewFoodFat      = value.FatPer100.ToString("0.#");
        NewFoodCarbs    = value.CarbsPer100.ToString("0.#");
        NewFoodAmount   = string.Empty;
    }

    partial void OnSelectedRecipeChanged(Recipe? value)
    {
        if (value == null) return;
        NewFoodName     = value.Title;
        NewFoodCalories = value.TotalCalories.ToString("0.#");
        NewFoodProtein  = value.TotalProtein.ToString("0.#");
        NewFoodFat      = value.TotalFat.ToString("0.#");
        NewFoodCarbs    = value.TotalCarbs.ToString("0.#");
        NewFoodUnit     = "порц.";
        NewFoodAmount   = value.Servings > 0 ? "1" : string.Empty;
    }

    [RelayCommand]
    public async Task AddEntryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewFoodName)) return;

        double amount = Parse(NewFoodAmount);
        double cal100 = Parse(NewFoodCalories);
        double pro100 = Parse(NewFoodProtein);
        double fat100 = Parse(NewFoodFat);
        double carb100= Parse(NewFoodCarbs);

        // Если выбран ингредиент из базы — пересчитываем на количество
        double cal, pro, fat, carb;
        if (AddMode == "ingredient" && SelectedFoodItem != null && amount > 0)
        {
            cal  = cal100 * amount / 100;
            pro  = pro100 * amount / 100;
            fat  = fat100 * amount / 100;
            carb = carb100 * amount / 100;
        }
        else if (AddMode == "recipe" && SelectedRecipe != null)
        {
            // Рецепт: умножаем на количество порций
            double portions = amount > 0 ? amount : 1;
            int servings = SelectedRecipe.Servings > 0 ? SelectedRecipe.Servings : 1;
            cal  = cal100 / servings * portions;
            pro  = pro100 / servings * portions;
            fat  = fat100 / servings * portions;
            carb = carb100/ servings * portions;
        }
        else
        {
            cal = cal100; pro = pro100; fat = fat100; carb = carb100;
        }

        var entry = new FoodDiaryEntry
        {
            Date     = SelectedDate,
            FoodName = NewFoodName,
            Amount   = amount,
            Unit     = NewFoodUnit,
            Calories = Math.Round(cal, 1),
            Protein  = Math.Round(pro, 1),
            Fat      = Math.Round(fat, 1),
            Carbs    = Math.Round(carb, 1),
            MealType = NewMealType,
        };

        await _service.AddEntryAsync(entry);
        Entries.Add(entry);
        RecalcTotals();
        // Уведомляем View о смене коллекции чтобы обновить группировку
        OnPropertyChanged(nameof(Entries));
        ResetForm();
        ShowAddPanel = false;
    }

    partial void OnAddModeChanged(string value)
    {
        OnPropertyChanged(nameof(IsIngredientMode));
        OnPropertyChanged(nameof(IsRecipeMode));
        OnPropertyChanged(nameof(IsManualMode));
    }

    partial void OnEntriesChanged(System.Collections.ObjectModel.ObservableCollection<FoodDiaryEntry> value)
    {
        OnPropertyChanged(nameof(MealGroups));
    }

    private void ResetForm()
    {
        NewFoodName = NewFoodAmount = NewFoodCalories =
        NewFoodProtein = NewFoodFat = NewFoodCarbs = string.Empty;
        NewFoodUnit     = "г";
        NewMealType     = "Завтрак";
        SelectedFoodItem   = null;
        SelectedRecipe     = null;
        SelectedDbCategory = null;
        AddMode = "ingredient";
    }

    private static double Parse(string s)
    {
        double.TryParse(s?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out double v);
        return v;
    }
}
