using RecipeApp.Views;

namespace RecipeApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register modal/push routes
        Routing.RegisterRoute(nameof(RecipeDetailPage), typeof(RecipeDetailPage));
        Routing.RegisterRoute(nameof(EditRecipePage),   typeof(EditRecipePage));
        Routing.RegisterRoute(nameof(ImportRecipePage), typeof(ImportRecipePage));
    }
}
