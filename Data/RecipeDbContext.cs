using System.IO;
using Microsoft.EntityFrameworkCore;
using RecipeApp.Models;

namespace RecipeApp.Data;

public class RecipeDbContext : DbContext
{
    public DbSet<Recipe>    Recipes     => Set<Recipe>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Step>      Steps       => Set<Step>();
    public DbSet<FoodItem>       FoodItems       => Set<FoodItem>();
    public DbSet<UserProfile>   UserProfiles    => Set<UserProfile>();
    public DbSet<FoodDiaryEntry> FoodDiaryEntries => Set<FoodDiaryEntry>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // FileSystem.AppDataDirectory works on Android, iOS, Windows, macOS
        var dataDir = FileSystem.AppDataDirectory;
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "recipes.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Recipe>()
            .HasMany(r => r.Ingredients)
            .WithOne()
            .HasForeignKey(i => i.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Recipe>()
            .HasMany(r => r.Steps)
            .WithOne()
            .HasForeignKey(s => s.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// Создаёт БД и добавляет недостающие таблицы без удаления данных
    public void EnsureSchema()
    {
        // Создаёт таблицы если БД новая
        Database.EnsureCreated();

        // Добавляет таблицу FoodItems если её ещё нет (для старых БД)
        Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "FoodItems" (
                "Id"             INTEGER NOT NULL CONSTRAINT "PK_FoodItems" PRIMARY KEY AUTOINCREMENT,
                "Name"           TEXT    NOT NULL,
                "Category"       TEXT    NOT NULL,
                "CaloriesPer100" REAL    NOT NULL,
                "ProteinPer100"  REAL    NOT NULL,
                "FatPer100"      REAL    NOT NULL,
                "CarbsPer100"    REAL    NOT NULL,
                "DefaultUnit"    TEXT    NOT NULL
            );
            """);

        // Добавляет колонки AmountGrams и *Per100 в Ingredients если их нет
        // UserProfiles table
        Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "UserProfiles" (
                "Id"       INTEGER NOT NULL CONSTRAINT "PK_UserProfiles" PRIMARY KEY,
                "Name"     TEXT NOT NULL DEFAULT '',
                "Age"      INTEGER NOT NULL DEFAULT 0,
                "Weight"   REAL NOT NULL DEFAULT 0,
                "Height"   REAL NOT NULL DEFAULT 0,
                "Gender"   TEXT NOT NULL DEFAULT 'Мужской',
                "Activity" TEXT NOT NULL DEFAULT 'Полуактивный'
            );
            """);

        // FoodDiaryEntries table
        Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "FoodDiaryEntries" (
                "Id"       INTEGER NOT NULL CONSTRAINT "PK_FoodDiaryEntries" PRIMARY KEY AUTOINCREMENT,
                "Date"     TEXT NOT NULL,
                "FoodName" TEXT NOT NULL DEFAULT '',
                "Amount"   REAL NOT NULL DEFAULT 0,
                "Unit"     TEXT NOT NULL DEFAULT 'г',
                "Calories" REAL NOT NULL DEFAULT 0,
                "Protein"  REAL NOT NULL DEFAULT 0,
                "Fat"      REAL NOT NULL DEFAULT 0,
                "Carbs"    REAL NOT NULL DEFAULT 0,
                "MealType" TEXT NOT NULL DEFAULT 'Завтрак'
            );
            """);

        TryAddColumn("Ingredients", "AmountGrams",    "REAL NOT NULL DEFAULT 0");
        TryAddColumn("Ingredients", "CaloriesPer100", "REAL NOT NULL DEFAULT 0");
        TryAddColumn("Ingredients", "ProteinPer100",  "REAL NOT NULL DEFAULT 0");
        TryAddColumn("Ingredients", "FatPer100",      "REAL NOT NULL DEFAULT 0");
        TryAddColumn("Ingredients", "CarbsPer100",    "REAL NOT NULL DEFAULT 0");
    }

    private void TryAddColumn(string table, string column, string definition)
    {
        try
        {
            Database.ExecuteSqlRaw(
                $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {definition};");
        }
        catch { /* колонка уже существует — игнорируем */ }
    }
}

