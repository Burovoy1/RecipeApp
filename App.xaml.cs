using System.Globalization;

namespace RecipeApp;

public partial class App : Application
{
    public App()
    {
        var culture = new CultureInfo("ru-RU");
        CultureInfo.DefaultThreadCurrentCulture   = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        InitializeComponent();
        MainPage = new AppShell();
    }
}
