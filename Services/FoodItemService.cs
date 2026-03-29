using Microsoft.EntityFrameworkCore;
using RecipeApp.Data;
using RecipeApp.Models;

namespace RecipeApp.Services;

public class FoodItemService
{
    public FoodItemService()
    {
        using var db = new RecipeDbContext();
        db.EnsureSchema();
    }

    // Каждый метод создаёт свой контекст — избегаем конфликт отслеживания
    public async Task<List<FoodItem>> GetAllAsync(string? search = null, string? category = null)
    {
        using var db = new RecipeDbContext();
        var q = db.FoodItems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(f => f.Name.Contains(search));

        if (!string.IsNullOrWhiteSpace(category) && category != "Все")
            q = q.Where(f => f.Category == category);

        return await q.OrderBy(f => f.Category).ThenBy(f => f.Name).ToListAsync();
    }

    public async Task<List<FoodItem>> SearchByNameAsync(string name)
    {
        using var db = new RecipeDbContext();
        return await db.FoodItems
            .Where(f => f.Name.Contains(name))
            .OrderBy(f => f.Name)
            .Take(10)
            .ToListAsync();
    }

    public async Task SaveAsync(FoodItem item)
    {
        using var db = new RecipeDbContext();
        if (item.Id == 0)
        {
            db.FoodItems.Add(item);
        }
        else
        {
            // Загружаем существующую запись и обновляем поля
            var existing = await db.FoodItems.FindAsync(item.Id);
            if (existing != null)
            {
                existing.Name           = item.Name;
                existing.Category       = item.Category;
                existing.CaloriesPer100 = item.CaloriesPer100;
                existing.ProteinPer100  = item.ProteinPer100;
                existing.FatPer100      = item.FatPer100;
                existing.CarbsPer100    = item.CarbsPer100;
                existing.DefaultUnit    = item.DefaultUnit;
            }
            else
            {
                db.FoodItems.Add(item);
            }
        }
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = new RecipeDbContext();
        var item = await db.FoodItems.FindAsync(id);
        if (item != null) { db.FoodItems.Remove(item); await db.SaveChangesAsync(); }
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        using var db = new RecipeDbContext();
        var cats = await db.FoodItems.Select(f => f.Category).Distinct().ToListAsync();
        return cats.Where(c => !string.IsNullOrEmpty(c)).OrderBy(c => c).ToList();
    }
}
