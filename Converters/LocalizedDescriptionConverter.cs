using System;
using System.Globalization;
using FoodStreetMAUI.Models;
using Microsoft.Maui.Controls;

namespace FoodStreetMAUI.Converters
{
    public class LocalizedDescriptionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
            {
                return string.Empty;
            }

            if (values[0] is not PointOfInterest poi)
            {
                return string.Empty;
            }

            var lang = values[1] as string ?? string.Empty;
            var content = poi.GetContent(lang);
            if (!string.IsNullOrWhiteSpace(content?.Description))
            {
                return content!.Description;
            }

            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
