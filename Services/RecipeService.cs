using Microsoft.EntityFrameworkCore;
using RecipeApp.Data;
using RecipeApp.Models;

namespace RecipeApp.Services;

public class RecipeService
{
    public RecipeService()
    {
        using var db = new RecipeDbContext();
        db.EnsureSchema();
    }

    public async Task<List<Recipe>> GetAllAsync(string? searchTerm = null, string? category = null)
    {
        using var db = new RecipeDbContext();
        var query = db.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(r => r.Title.Contains(searchTerm) || r.Description.Contains(searchTerm));

        if (!string.IsNullOrWhiteSpace(category) && category != "Все")
            query = query.Where(r => r.Category == category);

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<Recipe?> GetByIdAsync(int id)
    {
        using var db = new RecipeDbContext();
        return await db.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Steps.OrderBy(s => s.Order))
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Recipe> SaveAsync(Recipe recipe)
    {
        using var db = new RecipeDbContext();
        if (recipe.Id == 0)
        {
            db.Recipes.Add(recipe);
        }
        else
        {
            // Удаляем старые ингредиенты и шаги, добавляем новые
            var existing = await db.Recipes
                .Include(r => r.Ingredients)
                .Include(r => r.Steps)
                .FirstOrDefaultAsync(r => r.Id == recipe.Id);

            if (existing != null)
            {
                existing.Title           = recipe.Title;
                existing.Description     = recipe.Description;
                existing.Category        = recipe.Category;
                existing.PrepTimeMinutes = recipe.PrepTimeMinutes;
                existing.CookTimeMinutes = recipe.CookTimeMinutes;
                existing.Servings        = recipe.Servings;
                existing.Difficulty      = recipe.Difficulty;
                existing.ImagePath       = recipe.ImagePath;

                // Обновляем ингредиенты
                db.Ingredients.RemoveRange(existing.Ingredients);
                existing.Ingredients = recipe.Ingredients;

                // Обновляем шаги
                db.Steps.RemoveRange(existing.Steps);
                existing.Steps = recipe.Steps;
            }
            else
            {
                db.Recipes.Add(recipe);
            }
        }

        await db.SaveChangesAsync();
        return recipe;
    }

    public async Task DeleteAsync(int id)
    {
        using var db = new RecipeDbContext();
        var recipe = await db.Recipes.FindAsync(id);
        if (recipe != null)
        {
            db.Recipes.Remove(recipe);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        using var db = new RecipeDbContext();
        var cats = await db.Recipes.Select(r => r.Category).Distinct().ToListAsync();
        return cats.Where(c => !string.IsNullOrEmpty(c)).OrderBy(c => c).ToList();
    }
}
