namespace RecipeApp.Models;

public class UserProfile
{
    public int    Id         { get; set; } = 1;
    public string Name       { get; set; } = string.Empty;
    public int    Age        { get; set; }
    public double Weight     { get; set; }  // кг
    public double Height     { get; set; }  // см
    public string Gender     { get; set; } = "Мужской";
    public string Activity   { get; set; } = "Полуактивный";

    // Рассчитанные нормы
    public double DailyCalories => CalcCalories();
    public double DailyProtein  => Math.Round(DailyCalories * 0.25 / 4, 1);
    public double DailyFat      => Math.Round(DailyCalories * 0.30 / 9, 1);
    public double DailyCarbs    => Math.Round(DailyCalories * 0.45 / 4, 1);

    private double CalcCalories()
    {
        if (Weight <= 0 || Height <= 0 || Age <= 0) return 0;
        // Формула Миффлина-Сан Жеора
        double bmr = 10 * Weight + 6.25 * Height - 5 * Age
                   + (Gender == "Мужской" ? 5 : -161);

        double factor = Activity switch
        {
            "Сидячий"      => 1.2,
            "Полуактивный" => 1.375,
            "Активный"     => 1.55,
            "Спортивный"   => 1.725,
            _              => 1.375
        };
        return Math.Round(bmr * factor, 0);
    }
}

public class FoodDiaryEntry
{
    public int      Id         { get; set; }
    public DateTime Date       { get; set; } = DateTime.Today;
    public string   FoodName   { get; set; } = string.Empty;
    public double   Amount     { get; set; }
    public string   Unit       { get; set; } = "г";
    public double   Calories   { get; set; }
    public double   Protein    { get; set; }
    public double   Fat        { get; set; }
    public double   Carbs      { get; set; }
    public string   MealType   { get; set; } = "Завтрак"; // Завтрак, Обед, Ужин, Перекус
}
