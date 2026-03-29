using Microsoft.EntityFrameworkCore;
using RecipeApp.Data;
using RecipeApp.Models;

namespace RecipeApp.Services;

public class NutritionService
{
    private readonly RecipeDbContext _db;

    public NutritionService()
    {
        _db = new RecipeDbContext();
        _db.EnsureSchema();
    }

    // ── Профиль пользователя ─────────────────────────────────────────
    public async Task<UserProfile> GetProfileAsync()
    {
        var p = await _db.UserProfiles.FirstOrDefaultAsync();
        return p ?? new UserProfile();
    }

    public async Task SaveProfileAsync(UserProfile profile)
    {
        try
        {
            var existing = await _db.UserProfiles.FirstOrDefaultAsync();

            if (existing == null)
            {
                var newProfile = new UserProfile
                {
                    Name     = profile.Name,
                    Age      = profile.Age,
                    Weight   = profile.Weight,
                    Height   = profile.Height,
                    Gender   = profile.Gender,
                    Activity = profile.Activity,
                };
                _db.UserProfiles.Add(newProfile);
            }
            else
            {
                existing.Name     = profile.Name;
                existing.Age      = profile.Age;
                existing.Weight   = profile.Weight;
                existing.Height   = profile.Height;
                existing.Gender   = profile.Gender;
                existing.Activity = profile.Activity;
                _db.UserProfiles.Update(existing);
            }

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка сохранения профиля: {ex.Message}");
        }
    }

    // ── Дневник питания ──────────────────────────────────────────────
    public async Task<List<FoodDiaryEntry>> GetEntriesForDateAsync(DateTime date)
    {
        return await _db.FoodDiaryEntries
            .Where(e => e.Date.Date == date.Date)
            .OrderBy(e => e.MealType)
            .ToListAsync();
    }

    public async Task AddEntryAsync(FoodDiaryEntry entry)
    {
        entry.Date = entry.Date.Date;
        _db.FoodDiaryEntries.Add(entry);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteEntryAsync(int id)
    {
        var e = await _db.FoodDiaryEntries.FindAsync(id);
        if (e != null)
        {
            _db.FoodDiaryEntries.Remove(e);
            await _db.SaveChangesAsync();
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
