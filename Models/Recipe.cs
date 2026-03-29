namespace RecipeApp.Models;

public class Recipe
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; }
    public string Difficulty { get; set; } = "Средняя";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<Ingredient> Ingredients { get; set; } = new();
    public List<Step> Steps { get; set; } = new();
    public string? ImagePath { get; set; }

    public int TotalTimeMinutes => PrepTimeMinutes + CookTimeMinutes;

    // Суммарное КБЖУ — сумма пересчитанных значений каждого ингредиента
    public double TotalCalories => Ingredients.Sum(i => i.TotalCalories);
    public double TotalProtein  => Ingredients.Sum(i => i.TotalProtein);
    public double TotalFat      => Ingredients.Sum(i => i.TotalFat);
    public double TotalCarbs    => Ingredients.Sum(i => i.TotalCarbs);
}

public class Ingredient
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;   // строка для отображения
    public double AmountGrams { get; set; }              // граммы для пересчёта
    public string Unit { get; set; } = string.Empty;
    public int RecipeId { get; set; }

    // КБЖУ на 100 г
    public double CaloriesPer100 { get; set; }
    public double ProteinPer100  { get; set; }
    public double FatPer100      { get; set; }
    public double CarbsPer100    { get; set; }

    // Пересчитанные значения на AmountGrams
    public double TotalCalories => CaloriesPer100 * AmountGrams / 100.0;
    public double TotalProtein  => ProteinPer100  * AmountGrams / 100.0;
    public double TotalFat      => FatPer100      * AmountGrams / 100.0;
    public double TotalCarbs    => CarbsPer100    * AmountGrams / 100.0;
}

public class Step
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
    public int RecipeId { get; set; }
}
