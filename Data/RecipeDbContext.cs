using System.IO;
using Microsoft.EntityFrameworkCore;
using RecipeApp.Models;

namespace RecipeApp.Data;

public class RecipeDbContext : DbContext
{
    public DbSet<Recipe>         Recipes          => Set<Recipe>();
    public DbSet<Ingredient>     Ingredients      => Set<Ingredient>();
    public DbSet<Step>           Steps            => Set<Step>();
    public DbSet<FoodItem>       FoodItems        => Set<FoodItem>();
    public DbSet<UserProfile>    UserProfiles     => Set<UserProfile>();
    public DbSet<FoodDiaryEntry> FoodDiaryEntries => Set<FoodDiaryEntry>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dataDir = FileSystem.AppDataDirectory;
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "recipes.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Recipe>()
            .HasMany(r => r.Ingredients).WithOne()
            .HasForeignKey(i => i.RecipeId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Recipe>()
            .HasMany(r => r.Steps).WithOne()
            .HasForeignKey(s => s.RecipeId).OnDelete(DeleteBehavior.Cascade);
    }

    public void EnsureSchema()
    {
        Database.EnsureCreated();

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

        SeedFoodItems();
    }

    private void SeedFoodItems()
    {
        if (FoodItems.Count() > 0) return;

        var items = new[]
        {
            // ── Мясо и птица ─────────────────────────────────────────────
            new FoodItem { Name="Куриная грудка",        Category="Мясо и птица",        CaloriesPer100=113, ProteinPer100=23.6, FatPer100=1.9,  CarbsPer100=0.4,  DefaultUnit="г" },
            new FoodItem { Name="Куриное бедро",         Category="Мясо и птица",        CaloriesPer100=185, ProteinPer100=15.0, FatPer100=14.0, CarbsPer100=0,    DefaultUnit="г" },
            new FoodItem { Name="Куриное яйцо",          Category="Мясо и птица",        CaloriesPer100=157, ProteinPer100=12.7, FatPer100=11.5, CarbsPer100=0.7,  DefaultUnit="шт" },
            new FoodItem { Name="Говядина (вырезка)",    Category="Мясо и птица",        CaloriesPer100=187, ProteinPer100=18.9, FatPer100=12.4, CarbsPer100=0,    DefaultUnit="г" },
            new FoodItem { Name="Свинина (корейка)",     Category="Мясо и птица",        CaloriesPer100=259, ProteinPer100=16.0, FatPer100=21.6, CarbsPer100=0,    DefaultUnit="г" },
            new FoodItem { Name="Индейка (грудка)",      Category="Мясо и птица",        CaloriesPer100=84,  ProteinPer100=19.2, FatPer100=0.7,  CarbsPer100=0,    DefaultUnit="г" },
            new FoodItem { Name="Фарш куриный",          Category="Мясо и птица",        CaloriesPer100=143, ProteinPer100=17.4, FatPer100=8.1,  CarbsPer100=0,    DefaultUnit="г" },
            new FoodItem { Name="Фарш говяжий",          Category="Мясо и птица",        CaloriesPer100=254, ProteinPer100=14.0, FatPer100=22.0, CarbsPer100=0,    DefaultUnit="г" },
            new FoodItem { Name="Бекон",                 Category="Мясо и птица",        CaloriesPer100=458, ProteinPer100=11.6, FatPer100=45.0, CarbsPer100=0.7,  DefaultUnit="г" },
            new FoodItem { Name="Колбаса варёная",       Category="Мясо и птица",        CaloriesPer100=257, ProteinPer100=13.7, FatPer100=22.8, CarbsPer100=0,    DefaultUnit="г" },

            // ── Рыба и морепродукты ───────────────────────────────────────
            new FoodItem { Name="Лосось",                Category="Рыба и морепродукты", CaloriesPer100=208, ProteinPer100=20.0, FatPer100=13.6, CarbsPer100=0,    DefaultUnit="г" },
            new FoodItem { Name="Тунец (консервы)",      Category="Рыба и морепродукты", CaloriesPer100=96,  ProteinPer100=22.0, FatPer100=1.0,  CarbsPer100=0,    DefaultUnit="г" },
            new FoodItem { Name="Треска",                Category="Рыба и морепродукты", CaloriesPer100=69,  ProteinPer100=16.0, FatPer100=0.6,  CarbsPer100=0,    DefaultUnit="г" },
            new FoodItem { Name="Скумбрия",              Category="Рыба и морепродукты", CaloriesPer100=191, ProteinPer100=18.0, FatPer100=13.2, CarbsPer100=0,    DefaultUnit="г" },
            new FoodItem { Name="Тилапия",               Category="Рыба и морепродукты", CaloriesPer100=96,  ProteinPer100=20.1, FatPer100=1.7,  CarbsPer100=0,    DefaultUnit="г" },
            new FoodItem { Name="Креветки",              Category="Рыба и морепродукты", CaloriesPer100=95,  ProteinPer100=18.9, FatPer100=2.2,  CarbsPer100=0,    DefaultUnit="г" },
            new FoodItem { Name="Кальмар",               Category="Рыба и морепродукты", CaloriesPer100=92,  ProteinPer100=18.0, FatPer100=1.4,  CarbsPer100=2.0,  DefaultUnit="г" },
            new FoodItem { Name="Сельдь",                Category="Рыба и морепродукты", CaloriesPer100=217, ProteinPer100=17.7, FatPer100=15.5, CarbsPer100=0,    DefaultUnit="г" },

            // ── Молочные продукты ─────────────────────────────────────────
            new FoodItem { Name="Молоко 3.2%",           Category="Молочные продукты",   CaloriesPer100=60,  ProteinPer100=2.9,  FatPer100=3.2,  CarbsPer100=4.7,  DefaultUnit="мл" },
            new FoodItem { Name="Молоко 2.5%",           Category="Молочные продукты",   CaloriesPer100=52,  ProteinPer100=2.8,  FatPer100=2.5,  CarbsPer100=4.7,  DefaultUnit="мл" },
            new FoodItem { Name="Творог 5%",             Category="Молочные продукты",   CaloriesPer100=121, ProteinPer100=17.2, FatPer100=5.0,  CarbsPer100=1.8,  DefaultUnit="г" },
            new FoodItem { Name="Творог 0%",             Category="Молочные продукты",   CaloriesPer100=71,  ProteinPer100=16.5, FatPer100=0.6,  CarbsPer100=1.3,  DefaultUnit="г" },
            new FoodItem { Name="Сметана 20%",           Category="Молочные продукты",   CaloriesPer100=206, ProteinPer100=2.8,  FatPer100=20.0, CarbsPer100=3.2,  DefaultUnit="г" },
            new FoodItem { Name="Кефир 1%",              Category="Молочные продукты",   CaloriesPer100=40,  ProteinPer100=3.4,  FatPer100=1.0,  CarbsPer100=3.8,  DefaultUnit="мл" },
            new FoodItem { Name="Йогурт натуральный",    Category="Молочные продукты",   CaloriesPer100=68,  ProteinPer100=5.0,  FatPer100=3.2,  CarbsPer100=3.5,  DefaultUnit="г" },
            new FoodItem { Name="Сыр Российский",        Category="Молочные продукты",   CaloriesPer100=364, ProteinPer100=23.0, FatPer100=29.5, CarbsPer100=0,    DefaultUnit="г" },
            new FoodItem { Name="Сыр Моцарелла",         Category="Молочные продукты",   CaloriesPer100=280, ProteinPer100=18.0, FatPer100=22.0, CarbsPer100=2.2,  DefaultUnit="г" },
            new FoodItem { Name="Сыр Творожный",         Category="Молочные продукты",   CaloriesPer100=253, ProteinPer100=7.1,  FatPer100=24.2, CarbsPer100=2.8,  DefaultUnit="г" },
            new FoodItem { Name="Сливки 10%",            Category="Молочные продукты",   CaloriesPer100=119, ProteinPer100=2.8,  FatPer100=10.0, CarbsPer100=4.0,  DefaultUnit="мл" },
            new FoodItem { Name="Масло сливочное 82.5%", Category="Молочные продукты",   CaloriesPer100=748, ProteinPer100=0.8,  FatPer100=82.5, CarbsPer100=0.8,  DefaultUnit="г" },

            // ── Крупы и злаки ─────────────────────────────────────────────
            new FoodItem { Name="Гречка",                Category="Крупы и злаки",       CaloriesPer100=313, ProteinPer100=12.6, FatPer100=3.3,  CarbsPer100=57.1, DefaultUnit="г" },
            new FoodItem { Name="Рис белый",             Category="Крупы и злаки",       CaloriesPer100=344, ProteinPer100=6.7,  FatPer100=0.7,  CarbsPer100=78.9, DefaultUnit="г" },
            new FoodItem { Name="Рис бурый",             Category="Крупы и злаки",       CaloriesPer100=337, ProteinPer100=7.4,  FatPer100=2.7,  CarbsPer100=72.9, DefaultUnit="г" },
            new FoodItem { Name="Овсянка (хлопья)",      Category="Крупы и злаки",       CaloriesPer100=342, ProteinPer100=12.3, FatPer100=6.1,  CarbsPer100=59.5, DefaultUnit="г" },
            new FoodItem { Name="Перловка",              Category="Крупы и злаки",       CaloriesPer100=315, ProteinPer100=9.3,  FatPer100=1.1,  CarbsPer100=66.9, DefaultUnit="г" },
            new FoodItem { Name="Пшено",                 Category="Крупы и злаки",       CaloriesPer100=334, ProteinPer100=11.5, FatPer100=3.3,  CarbsPer100=66.5, DefaultUnit="г" },
            new FoodItem { Name="Макароны",              Category="Крупы и злаки",       CaloriesPer100=338, ProteinPer100=10.4, FatPer100=1.1,  CarbsPer100=69.7, DefaultUnit="г" },
            new FoodItem { Name="Булгур",                Category="Крупы и злаки",       CaloriesPer100=342, ProteinPer100=12.3, FatPer100=1.3,  CarbsPer100=75.9, DefaultUnit="г" },
            new FoodItem { Name="Кускус",                Category="Крупы и злаки",       CaloriesPer100=376, ProteinPer100=12.8, FatPer100=0.6,  CarbsPer100=77.4, DefaultUnit="г" },
            new FoodItem { Name="Мука пшеничная",        Category="Крупы и злаки",       CaloriesPer100=342, ProteinPer100=10.3, FatPer100=1.1,  CarbsPer100=70.6, DefaultUnit="г" },

            // ── Бобовые ───────────────────────────────────────────────────
            new FoodItem { Name="Чечевица красная",      Category="Бобовые",             CaloriesPer100=295, ProteinPer100=24.0, FatPer100=1.5,  CarbsPer100=42.7, DefaultUnit="г" },
            new FoodItem { Name="Нут",                   Category="Бобовые",             CaloriesPer100=364, ProteinPer100=19.0, FatPer100=6.0,  CarbsPer100=61.0, DefaultUnit="г" },
            new FoodItem { Name="Фасоль белая",          Category="Бобовые",             CaloriesPer100=337, ProteinPer100=21.0, FatPer100=2.0,  CarbsPer100=52.0, DefaultUnit="г" },
            new FoodItem { Name="Горох",                 Category="Бобовые",             CaloriesPer100=298, ProteinPer100=23.0, FatPer100=1.6,  CarbsPer100=50.8, DefaultUnit="г" },

            // ── Овощи ─────────────────────────────────────────────────────
            new FoodItem { Name="Картофель",             Category="Овощи",               CaloriesPer100=77,  ProteinPer100=2.0,  FatPer100=0.4,  CarbsPer100=16.3, DefaultUnit="г" },
            new FoodItem { Name="Морковь",               Category="Овощи",               CaloriesPer100=32,  ProteinPer100=1.3,  FatPer100=0.1,  CarbsPer100=6.9,  DefaultUnit="г" },
            new FoodItem { Name="Лук репчатый",          Category="Овощи",               CaloriesPer100=41,  ProteinPer100=1.4,  FatPer100=0,    CarbsPer100=8.2,  DefaultUnit="г" },
            new FoodItem { Name="Чеснок",                Category="Овощи",               CaloriesPer100=149, ProteinPer100=6.4,  FatPer100=0.5,  CarbsPer100=29.9, DefaultUnit="г" },
            new FoodItem { Name="Помидор",               Category="Овощи",               CaloriesPer100=20,  ProteinPer100=1.1,  FatPer100=0.2,  CarbsPer100=3.7,  DefaultUnit="г" },
            new FoodItem { Name="Огурец",                Category="Овощи",               CaloriesPer100=14,  ProteinPer100=0.8,  FatPer100=0.1,  CarbsPer100=2.5,  DefaultUnit="г" },
            new FoodItem { Name="Капуста белокочанная",  Category="Овощи",               CaloriesPer100=27,  ProteinPer100=1.8,  FatPer100=0.1,  CarbsPer100=4.7,  DefaultUnit="г" },
            new FoodItem { Name="Брокколи",              Category="Овощи",               CaloriesPer100=34,  ProteinPer100=2.8,  FatPer100=0.4,  CarbsPer100=6.6,  DefaultUnit="г" },
            new FoodItem { Name="Болгарский перец",      Category="Овощи",               CaloriesPer100=27,  ProteinPer100=1.3,  FatPer100=0,    CarbsPer100=5.7,  DefaultUnit="г" },
            new FoodItem { Name="Кабачок",               Category="Овощи",               CaloriesPer100=24,  ProteinPer100=1.5,  FatPer100=0.3,  CarbsPer100=4.6,  DefaultUnit="г" },
            new FoodItem { Name="Баклажан",              Category="Овощи",               CaloriesPer100=25,  ProteinPer100=1.2,  FatPer100=0.1,  CarbsPer100=5.9,  DefaultUnit="г" },
            new FoodItem { Name="Свёкла",                Category="Овощи",               CaloriesPer100=43,  ProteinPer100=1.5,  FatPer100=0.1,  CarbsPer100=9.6,  DefaultUnit="г" },
            new FoodItem { Name="Шпинат",                Category="Овощи",               CaloriesPer100=23,  ProteinPer100=2.9,  FatPer100=0.4,  CarbsPer100=3.6,  DefaultUnit="г" },
            new FoodItem { Name="Сельдерей",             Category="Овощи",               CaloriesPer100=12,  ProteinPer100=1.0,  FatPer100=0.1,  CarbsPer100=2.1,  DefaultUnit="г" },

            // ── Фрукты и ягоды ────────────────────────────────────────────
            new FoodItem { Name="Яблоко",                Category="Фрукты и ягоды",      CaloriesPer100=47,  ProteinPer100=0.4,  FatPer100=0.4,  CarbsPer100=9.8,  DefaultUnit="г" },
            new FoodItem { Name="Банан",                 Category="Фрукты и ягоды",      CaloriesPer100=96,  ProteinPer100=1.5,  FatPer100=0.5,  CarbsPer100=21.8, DefaultUnit="г" },
            new FoodItem { Name="Апельсин",              Category="Фрукты и ягоды",      CaloriesPer100=43,  ProteinPer100=0.9,  FatPer100=0.2,  CarbsPer100=8.1,  DefaultUnit="г" },
            new FoodItem { Name="Клубника",              Category="Фрукты и ягоды",      CaloriesPer100=33,  ProteinPer100=0.8,  FatPer100=0.4,  CarbsPer100=7.5,  DefaultUnit="г" },
            new FoodItem { Name="Виноград",              Category="Фрукты и ягоды",      CaloriesPer100=65,  ProteinPer100=0.6,  FatPer100=0.2,  CarbsPer100=16.8, DefaultUnit="г" },
            new FoodItem { Name="Черника",               Category="Фрукты и ягоды",      CaloriesPer100=44,  ProteinPer100=0.7,  FatPer100=0.5,  CarbsPer100=11.4, DefaultUnit="г" },
            new FoodItem { Name="Груша",                 Category="Фрукты и ягоды",      CaloriesPer100=42,  ProteinPer100=0.4,  FatPer100=0.3,  CarbsPer100=10.9, DefaultUnit="г" },

            // ── Орехи и семена ────────────────────────────────────────────
            new FoodItem { Name="Грецкий орех",          Category="Орехи и семена",      CaloriesPer100=654, ProteinPer100=15.2, FatPer100=65.2, CarbsPer100=7.0,  DefaultUnit="г" },
            new FoodItem { Name="Миндаль",               Category="Орехи и семена",      CaloriesPer100=579, ProteinPer100=21.2, FatPer100=49.9, CarbsPer100=21.6, DefaultUnit="г" },
            new FoodItem { Name="Семена подсолнечника",  Category="Орехи и семена",      CaloriesPer100=584, ProteinPer100=20.7, FatPer100=51.5, CarbsPer100=10.5, DefaultUnit="г" },
            new FoodItem { Name="Кешью",                 Category="Орехи и семена",      CaloriesPer100=553, ProteinPer100=18.2, FatPer100=43.9, CarbsPer100=30.2, DefaultUnit="г" },
            new FoodItem { Name="Семена чиа",            Category="Орехи и семена",      CaloriesPer100=486, ProteinPer100=16.5, FatPer100=30.7, CarbsPer100=42.1, DefaultUnit="г" },

            // ── Масла и жиры ──────────────────────────────────────────────
            new FoodItem { Name="Масло подсолнечное",    Category="Масла и жиры",        CaloriesPer100=899, ProteinPer100=0,    FatPer100=99.9, CarbsPer100=0,    DefaultUnit="мл" },
            new FoodItem { Name="Масло оливковое",       Category="Масла и жиры",        CaloriesPer100=884, ProteinPer100=0,    FatPer100=100,  CarbsPer100=0,    DefaultUnit="мл" },

            // ── Хлеб и выпечка ────────────────────────────────────────────
            new FoodItem { Name="Хлеб ржаной",          Category="Хлеб и выпечка",      CaloriesPer100=174, ProteinPer100=6.6,  FatPer100=1.2,  CarbsPer100=34.2, DefaultUnit="г" },
            new FoodItem { Name="Хлеб пшеничный",       Category="Хлеб и выпечка",      CaloriesPer100=242, ProteinPer100=8.0,  FatPer100=1.0,  CarbsPer100=48.8, DefaultUnit="г" },
            new FoodItem { Name="Батон",                 Category="Хлеб и выпечка",      CaloriesPer100=262, ProteinPer100=7.4,  FatPer100=2.9,  CarbsPer100=51.4, DefaultUnit="г" },

            // ── Сладкое ───────────────────────────────────────────────────
            new FoodItem { Name="Мёд",                   Category="Сладкое и десерты",   CaloriesPer100=304, ProteinPer100=0.3,  FatPer100=0,    CarbsPer100=75.0, DefaultUnit="ст.л." },
            new FoodItem { Name="Шоколад тёмный 70%",    Category="Сладкое и десерты",   CaloriesPer100=599, ProteinPer100=7.8,  FatPer100=42.6, CarbsPer100=45.9, DefaultUnit="г" },
            new FoodItem { Name="Варенье",               Category="Сладкое и десерты",   CaloriesPer100=238, ProteinPer100=0.4,  FatPer100=0.1,  CarbsPer100=63.0, DefaultUnit="ст.л." },

            // ── Специи и приправы ─────────────────────────────────────────
            new FoodItem { Name="Соль",                  Category="Специи и приправы",   CaloriesPer100=0,   ProteinPer100=0,    FatPer100=0,    CarbsPer100=0,    DefaultUnit="ч.л." },
            new FoodItem { Name="Сахар",                 Category="Специи и приправы",   CaloriesPer100=399, ProteinPer100=0,    FatPer100=0,    CarbsPer100=99.8, DefaultUnit="ч.л." },
            new FoodItem { Name="Перец чёрный",          Category="Специи и приправы",   CaloriesPer100=251, ProteinPer100=10.4, FatPer100=3.3,  CarbsPer100=64.8, DefaultUnit="ч.л." },
            new FoodItem { Name="Лавровый лист",         Category="Специи и приправы",   CaloriesPer100=313, ProteinPer100=7.6,  FatPer100=8.4,  CarbsPer100=74.9, DefaultUnit="шт" },
            new FoodItem { Name="Томатная паста",        Category="Специи и приправы",   CaloriesPer100=82,  ProteinPer100=4.8,  FatPer100=0.5,  CarbsPer100=18.6, DefaultUnit="ст.л." },
            new FoodItem { Name="Соевый соус",           Category="Специи и приправы",   CaloriesPer100=53,  ProteinPer100=8.1,  FatPer100=0,    CarbsPer100=4.9,  DefaultUnit="ст.л." },
        };

        FoodItems.AddRange(items);
        SaveChanges();
    }

    private void TryAddColumn(string table, string column, string definition)
    {
        try { Database.ExecuteSqlRaw($"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {definition};"); }
        catch { }
    }
}
