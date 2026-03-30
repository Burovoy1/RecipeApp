using RecipeApp.ViewModels;

namespace RecipeApp.Views;

public partial class FoodDatabasePage : ContentPage
{
    private readonly FoodDatabaseViewModel _vm;

    public FoodDatabasePage(FoodDatabaseViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;

        _vm.OnSaveError += async msg =>
            await DisplayAlert("Ошибка", msg, "OK");

        // Both StartAddCommand and StartEditCommand fire OnEditRequested after
        // populating the edit fields — we open the modal dialog here
        _vm.OnEditRequested += async () =>
            await ShowEditDialog();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _vm.LoadAsync();
    }

    private async Task ShowEditDialog()
    {
        var page = new EditFoodItemPage(_vm);
        await Navigation.PushModalAsync(new NavigationPage(page)
        {
            BarBackgroundColor = (Color)Application.Current!.Resources["Primary"],
            BarTextColor       = Colors.White
        });
    }
}

// Inline modal page for editing/adding food items
public class EditFoodItemPage : ContentPage
{
    private readonly FoodDatabaseViewModel _vm;

    public EditFoodItemPage(FoodDatabaseViewModel vm)
    {
        _vm   = vm;
        Title = vm.EditId == 0 ? "Новый продукт" : "Редактировать";
        BindingContext = vm;

        var nameEntry      = CreateEntry("Название *",       nameof(vm.EditName));
        var categoryPicker = CreatePicker("Категория",       nameof(vm.EditCategory),
                                          FoodDatabaseViewModel.FoodCategories);
        var unitPicker     = CreatePicker("Ед. изм.",        nameof(vm.EditDefaultUnit),
                                          FoodDatabaseViewModel.UnitOptions);
        var calEntry       = CreateEntry("Калории / 100 г",  nameof(vm.EditCalories), Keyboard.Numeric);
        var protEntry      = CreateEntry("Белки / 100 г",    nameof(vm.EditProtein),  Keyboard.Numeric);
        var fatEntry       = CreateEntry("Жиры / 100 г",     nameof(vm.EditFat),      Keyboard.Numeric);
        var carbEntry      = CreateEntry("Углеводы / 100 г", nameof(vm.EditCarbs),    Keyboard.Numeric);

        var saveBtn = new Button
        {
            Text            = "Сохранить",
            BackgroundColor = (Color)Application.Current!.Resources["Primary"],
            TextColor       = Colors.White,
            CornerRadius    = 8,
            Margin          = new Thickness(0, 16, 0, 0)
        };
        saveBtn.Clicked += async (_, _) =>
        {
            await vm.SaveEditAsync();
            if (!vm.IsEditing)
                await Navigation.PopModalAsync();
        };

        var cancelBtn = new Button
        {
            Text            = "Отмена",
            BackgroundColor = Colors.Transparent,
            TextColor       = (Color)Application.Current!.Resources["Primary"],
            BorderColor     = (Color)Application.Current!.Resources["Primary"],
            BorderWidth     = 1,
            CornerRadius    = 8,
        };
        cancelBtn.Clicked += async (_, _) =>
        {
            vm.CancelEdit();
            await Navigation.PopModalAsync();
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding  = new Thickness(16),
                Spacing  = 8,
                Children =
                {
                    nameEntry, categoryPicker, unitPicker,
                    new Label
                    {
                        Text      = "КБЖУ указывается на 100 г/мл",
                        FontSize  = 12,
                        TextColor = (Color)Application.Current!.Resources["TextSecondary"]
                    },
                    calEntry, protEntry, fatEntry, carbEntry,
                    saveBtn, cancelBtn
                }
            }
        };
    }

    private static VerticalStackLayout CreateEntry(string label, string binding, Keyboard? keyboard = null)
    {
        var entry = new Entry { Placeholder = label };
        entry.SetBinding(Entry.TextProperty, binding);
        if (keyboard != null) entry.Keyboard = keyboard;
        return new VerticalStackLayout
        {
            Spacing  = 4,
            Children =
            {
                new Label
                {
                    Text      = label,
                    FontSize  = 12,
                    TextColor = (Color)Application.Current!.Resources["TextSecondary"]
                },
                entry
            }
        };
    }

    private static VerticalStackLayout CreatePicker(string label, string binding, IEnumerable<string> items)
    {
        var picker = new Picker { ItemsSource = items.ToList() };
        picker.SetBinding(Picker.SelectedItemProperty, binding);
        return new VerticalStackLayout
        {
            Spacing  = 4,
            Children =
            {
                new Label
                {
                    Text      = label,
                    FontSize  = 12,
                    TextColor = (Color)Application.Current!.Resources["TextSecondary"]
                },
                picker
            }
        };
    }
}
