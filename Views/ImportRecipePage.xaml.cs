using RecipeApp.ViewModels;

namespace RecipeApp.Views;

public partial class ImportRecipePage : ContentPage
{
    private readonly ImportRecipeViewModel _vm;

    public ImportRecipePage(ImportRecipeViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (!_vm.HasResult)
        {
            await DisplayAlert("Нет данных", "Сначала загрузите рецепт по ссылке", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(_vm.Title))
        {
            await DisplayAlert("Ошибка", "Введите название рецепта", "OK");
            return;
        }
        await _vm.SaveAsync();
        if (_vm.IsSaved)
        {
            await DisplayAlert("Готово", "Рецепт сохранён", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }
}
