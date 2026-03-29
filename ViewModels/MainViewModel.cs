using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecipeApp.Models;
using RecipeApp.Services;

namespace RecipeApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly RecipeService _service = new();

    [ObservableProperty] private ObservableCollection<Recipe> _recipes = new();
    [ObservableProperty] private Recipe? _selectedRecipe;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedCategory = "Все";
    [ObservableProperty] private ObservableCollection<string> _categories = new() { "Все" };
    [ObservableProperty] private bool _isLoading;

    public MainViewModel()
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
            Recipes = new ObservableCollection<Recipe>(list);

            var cats = await _service.GetCategoriesAsync();
            Categories = new ObservableCollection<string>(new[] { "Все" }.Concat(cats));
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task DeleteRecipeAsync(Recipe? recipe)
    {
        if (recipe == null) return;
        bool confirm = await Shell.Current.DisplayAlert(
            "Удалить рецепт", $"Удалить \u00ab{recipe.Title}\u00bb?", "Удалить", "Отмена");
        if (!confirm) return;
        await _service.DeleteAsync(recipe.Id);
        Recipes.Remove(recipe);
        if (SelectedRecipe == recipe) SelectedRecipe = null;
    }

    [RelayCommand]
    public async Task OpenRecipeAsync(Recipe? recipe)
    {
        if (recipe == null) return;
        await Shell.Current.GoToAsync(nameof(Views.RecipeDetailPage),
            new Dictionary<string, object> { ["Recipe"] = recipe });
        SelectedRecipe = null;
    }

    [RelayCommand]
    public async Task AddRecipeAsync()
    {
        await Shell.Current.GoToAsync($"{nameof(Views.EditRecipePage)}?mode=new");
    }

    [RelayCommand]
    public async Task ImportRecipeAsync()
    {
        await Shell.Current.GoToAsync(nameof(Views.ImportRecipePage));
    }

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();
    partial void OnSelectedCategoryChanged(string value) => _ = LoadAsync();
}

