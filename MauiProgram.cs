using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using RecipeApp.Data;
using RecipeApp.Views;
using RecipeApp.ViewModels;

namespace RecipeApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts => { }); // add custom fonts to Resources/Fonts/ if needed

        // Database — singleton so schema is created once
        builder.Services.AddSingleton<RecipeDbContext>(sp =>
        {
            var ctx = new RecipeDbContext();
            ctx.EnsureSchema();
            return ctx;
        });

        // Pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<RecipeDetailPage>();
        builder.Services.AddTransient<EditRecipePage>();
        builder.Services.AddTransient<FoodDatabasePage>();
        builder.Services.AddTransient<ImportRecipePage>();
        builder.Services.AddTransient<NutritionPage>();

        // ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<EditRecipeViewModel>();
        builder.Services.AddTransient<FoodDatabaseViewModel>();
        builder.Services.AddTransient<ImportRecipeViewModel>();
        builder.Services.AddTransient<NutritionViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
