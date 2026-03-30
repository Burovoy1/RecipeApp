using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.ViewModels;

public partial class FoodDatabaseViewModel : ObservableObject
{
    private readonly FoodItemService _service = new();

    [ObservableProperty] private ObservableCollection<FoodItem> _items = new();
    [ObservableProperty] private FoodItem? _selectedItem;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedCategory = "Все";
    [ObservableProperty] private ObservableCollection<string> _categories = new() { "Все" };
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isLoading;

    // Поля формы редактирования
    [ObservableProperty] private int    _editId;
    [ObservableProperty] private string _editName        = string.Empty;
    [ObservableProperty] private string _editCategory    = string.Empty;
    [ObservableProperty] private string _editCalories    = string.Empty;
    [ObservableProperty] private string _editProtein     = string.Empty;
    [ObservableProperty] private string _editFat         = string.Empty;
    [ObservableProperty] private string _editCarbs       = string.Empty;
    [ObservableProperty] private string _editDefaultUnit = "г";

    public event Action<string>? OnSaveError;
    // Raised after StartAdd/StartEdit so code-behind can open the modal dialog
    public event Action? OnEditRequested;

    public static IEnumerable<string> UnitOptions => new[]
        { "г", "мл", "шт", "ст.л.", "ч.л." };

    public static IEnumerable<string> FoodCategories => new[]
    {
        "Мясо и птица", "Рыба и морепродукты", "Молочные продукты",
        "Яйца", "Крупы и злаки", "Бобовые", "Овощи", "Фрукты и ягоды",
        "Орехи и семена", "Масла и жиры", "Хлеб и выпечка",
        "Сладкое и десерты", "Напитки", "Специи и приправы", "Другое"
    };

    public FoodDatabaseViewModel()
    {
        _ = LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _service.GetAllAsync(SearchText, SelectedCategory);
            Items = new ObservableCollection<FoodItem>(list);

            var cats = await _service.GetCategoriesAsync();
            var allCats = new[] { "Все" }.Concat(cats).ToList();
            Categories = new ObservableCollection<string>(allCats);

            if (!allCats.Contains(SelectedCategory)) SelectedCategory = "Все";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void StartAdd()
    {
        EditId = 0;
        EditName = EditCategory = EditCalories = EditProtein = EditFat = EditCarbs = string.Empty;
        EditDefaultUnit = "г";
        IsEditing = true;
        OnEditRequested?.Invoke();
    }

    [RelayCommand]
    public void StartEdit(FoodItem? item)
    {
        if (item == null) return;
        EditId          = item.Id;
        EditName        = item.Name;
        EditCategory    = item.Category;
        EditCalories    = item.CaloriesPer100.ToString("0.#");
        EditProtein     = item.ProteinPer100.ToString("0.#");
        EditFat         = item.FatPer100.ToString("0.#");
        EditCarbs       = item.CarbsPer100.ToString("0.#");
        EditDefaultUnit = item.DefaultUnit;
        IsEditing       = true;
        OnEditRequested?.Invoke();
    }

    [RelayCommand]
    public void CancelEdit() => IsEditing = false;

    [RelayCommand]
    public async Task SaveEditAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName)) return;

        try
        {
            var item = new FoodItem
            {
                Id             = EditId,
                Name           = EditName.Trim(),
                Category       = EditCategory ?? string.Empty,
                CaloriesPer100 = Parse(EditCalories),
                ProteinPer100  = Parse(EditProtein),
                FatPer100      = Parse(EditFat),
                CarbsPer100    = Parse(EditCarbs),
                DefaultUnit    = EditDefaultUnit ?? "г",
            };

            await _service.SaveAsync(item);
            IsEditing = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            OnSaveError?.Invoke($"{ex.Message}\n{ex.InnerException?.Message}");
        }
    }

    [RelayCommand]
    public async Task DeleteItemAsync(FoodItem? item)
    {
        if (item == null) return;
        await _service.DeleteAsync(item.Id);
        Items.Remove(item);
        if (SelectedItem == item) SelectedItem = null;
    }

    partial void OnSearchTextChanged(string value)       => _ = LoadAsync();
    partial void OnSelectedCategoryChanged(string value) => _ = LoadAsync();

    private static double Parse(string s)
    {
        double.TryParse(s.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out double v);
        return v;
    }
}
