namespace RecipeApp.Models;

public class FoodItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    // КБЖУ на 100 г
    public double CaloriesPer100 { get; set; }
    public double ProteinPer100  { get; set; }
    public double FatPer100      { get; set; }
    public double CarbsPer100    { get; set; }

    public string DefaultUnit { get; set; } = "г";

    // Используется ComboBox для отображения выбранного элемента
    public override string ToString() => Name;
}
