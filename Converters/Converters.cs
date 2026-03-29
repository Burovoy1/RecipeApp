using System.Globalization;

namespace RecipeApp.Converters;

/// Null or empty string → false, non-null/non-empty → true. Parameter="Inverse" to flip.
public class NullToBoolConverter : IValueConverter
{
    public static readonly NullToBoolConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool hasValue = value != null && value.ToString() != string.Empty;
        bool inverse  = parameter?.ToString() == "Inverse";
        return hasValue ^ inverse;
    }
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => throw new NotImplementedException();
}

/// true → false, false → true
public class InverseBoolConverter : IValueConverter
{
    public static readonly InverseBoolConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => throw new NotImplementedException();
}

/// int minutes → human-readable Russian string
public class MinutesToTimeConverter : IValueConverter
{
    public static readonly MinutesToTimeConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int minutes)
        {
            if (minutes <= 0) return "—";
            if (minutes < 60) return $"{minutes} мин";
            int h = minutes / 60, m = minutes % 60;
            return m == 0 ? $"{h} ч" : $"{h} ч {m} мин";
        }
        return "—";
    }
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => throw new NotImplementedException();
}

/// File path string → ImageSource (null if file missing)
public class ImagePathConverter : IValueConverter
{
    public static readonly ImagePathConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path && File.Exists(path))
        {
            try { return ImageSource.FromFile(path); }
            catch { }
        }
        return null;
    }
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => throw new NotImplementedException();
}

/// string empty/null → false, non-empty → true
public class StringNotEmptyConverter : IValueConverter
{
    public static readonly StringNotEmptyConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !string.IsNullOrWhiteSpace(value as string);
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => throw new NotImplementedException();
}

/// int > 0 → true
public class CountToBoolConverter : IValueConverter
{
    public static readonly CountToBoolConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int i && i > 0;
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => throw new NotImplementedException();
}

/// double > 0 → true
public class PositiveToBoolConverter : IValueConverter
{
    public static readonly PositiveToBoolConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double d && d > 0;
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => throw new NotImplementedException();
}

/// DateTime before today → true
public class IsBeforeTodayConverter : IValueConverter
{
    public static readonly IsBeforeTodayConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateTime d && d.Date < DateTime.Today;
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => throw new NotImplementedException();
}

/// double formatted as string
public class DoubleFormatConverter : IValueConverter
{
    public static readonly DoubleFormatConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string fmt = parameter as string ?? "0.#";
        return value is double d ? d.ToString(fmt) : "0";
    }
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => throw new NotImplementedException();
}
