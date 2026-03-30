using Microsoft.EntityFrameworkCore;
using RecipeApp.Data;
using RecipeApp.Models;

namespace RecipeApp.Services;

public class NutritionService
{
    public NutritionService()
    {
        // Ensure schema exists once on construction
        using var db = new RecipeDbContext();
        db.EnsureSchema();
    }

    // ── Профиль пользователя ─────────────────────────────────────────
    public async Task<UserProfile> GetProfileAsync()
    {
        using var db = new RecipeDbContext();
        var p = await db.UserProfiles.FirstOrDefaultAsync();
        return p ?? new UserProfile();
    }

    public async Task SaveProfileAsync(UserProfile profile)
    {
        try
        {
            using var db = new RecipeDbContext();
            var existing = await db.UserProfiles.FirstOrDefaultAsync();

            if (existing == null)
            {
                db.UserProfiles.Add(new UserProfile
                {
                    Name     = profile.Name,
                    Age      = profile.Age,
                    Weight   = profile.Weight,
                    Height   = profile.Height,
                    Gender   = profile.Gender,
                    Activity = profile.Activity,
                });
            }
            else
            {
                existing.Name     = profile.Name;
                existing.Age      = profile.Age;
                existing.Weight   = profile.Weight;
                existing.Height   = profile.Height;
                existing.Gender   = profile.Gender;
                existing.Activity = profile.Activity;
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка сохранения профиля: {ex.Message}");
        }
    }

    // ── Дневник питания ──────────────────────────────────────────────
    public async Task<List<FoodDiaryEntry>> GetEntriesForDateAsync(DateTime date)
    {
        using var db = new RecipeDbContext();
        // Загружаем все записи в память и фильтруем там — SQLite DateTime может
        // хранить дату как строку, и LINQ-to-SQL не всегда корректно переводит .Date
        var all = await db.FoodDiaryEntries.ToListAsync();
        return all
            .Where(e => e.Date.Date == date.Date)
            .OrderBy(e => e.MealType switch {
                "Завтрак" => 0,
                "Обед"    => 1,
                "Ужин"    => 2,
                _         => 3
            })
            .ToList();
    }

    public async Task AddEntryAsync(FoodDiaryEntry entry)
    {
        using var db = new RecipeDbContext();
        entry.Date = entry.Date.Date;
        db.FoodDiaryEntries.Add(entry);
        await db.SaveChangesAsync();
    }

    public async Task DeleteEntryAsync(int id)
    {
        using var db = new RecipeDbContext();
        var e = await db.FoodDiaryEntries.FindAsync(id);
        if (e != null)
        {
            db.FoodDiaryEntries.Remove(e);
            await db.SaveChangesAsync();
        }
    }

    public async Task<(double cal, double prot, double fat, double carbs)>
        GetTotalsForDateAsync(DateTime date)
    {
        var entries = await GetEntriesForDateAsync(date);
        return (
            entries.Sum(e => e.Calories),
            entries.Sum(e => e.Protein),
            entries.Sum(e => e.Fat),
            entries.Sum(e => e.Carbs)
        );
    }
}
