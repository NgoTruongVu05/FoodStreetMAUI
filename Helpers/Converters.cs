using System.Globalization;
using FoodStreetGuide.Models;

namespace FoodStreetGuide.Helpers;

// ── Bool → Color (generic, ConverterParameter = "trueHex|falseHex") ──────────
public sealed class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool b = value is bool bv && bv;
        var parts = (parameter as string ?? "#22C55E|#993C1D").Split('|');
        return Color.FromArgb(b ? parts[0] : parts[1]);
    }
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotImplementedException();
}

// ── Bool → "Dừng tour | Bắt đầu tour" ───────────────────────────────────────
public sealed class BoolToTourLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && b ? "Dừng" : "Bắt đầu";
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotImplementedException();
}

// ── Bool → Green/Red ─────────────────────────────────────────────────────────
public sealed class BoolToGreenRedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && b
            ? Color.FromArgb("#4ADE80")
            : Color.FromArgb("#F87171");
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotImplementedException();
}

// ── AppLanguage → Button BackgroundColor (active highlight) ──────────────────
// ConverterParameter = language index as string "0","1","2","3"
public sealed class LanguageToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AppLanguage current) return Color.FromArgb("#1A1A2E");
        if (!int.TryParse(parameter as string, out int target)) return Color.FromArgb("#1A1A2E");
        return (int)current == target
            ? Color.FromArgb("#185FA5")
            : Color.FromArgb("#1A1A2E");
    }
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotImplementedException();
}

// ── Tab index → text color ────────────────────────────────────────────────────
public sealed class TabColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!int.TryParse(value?.ToString(), out int current)) return Color.FromArgb("#888780");
        if (!int.TryParse(parameter as string, out int target)) return Color.FromArgb("#888780");
        return current == target
            ? Color.FromArgb("#85B7EB")
            : Color.FromArgb("#888780");
    }
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotImplementedException();
}

// ── Tab index → IsVisible ────────────────────────────────────────────────────
public sealed class TabVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!int.TryParse(value?.ToString(), out int current)) return false;
        if (!int.TryParse(parameter as string, out int target)) return false;
        return current == target;
    }
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotImplementedException();
}

// ── GeofenceState → Color ────────────────────────────────────────────────────
public sealed class GeofenceStateToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is GeofenceState s
            ? s switch
            {
                GeofenceState.Inside => Color.FromArgb("#22C55E"),
                GeofenceState.Nearby => Color.FromArgb("#378ADD"),
                _                    => Color.FromArgb("#888780")
            }
            : Color.FromArgb("#888780");
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotImplementedException();
}
