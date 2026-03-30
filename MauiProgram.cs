using CommunityToolkit.Maui;
using RecipeApp.Data;
using RecipeApp.Views;
using RecipeApp.ViewModels;

namespace RecipeApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Required for EF Core SQLite on Android — prevents native library crash
        SQLitePCL.Batteries_V2.Init();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts => { });

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

        return builder.Build();
    }
}
