using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace TWCF_BibleApp.Presentation.Components.Converters
{
    public class MultiplyFontSizeConverter : IValueConverter
    {
        private const double DefaultBaseFontSize = 14.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value expected to be the font scale (e.g. 1.0)
            if (value is double scale)
            {
                double factor = 1.0;
                if (parameter is string paramStr && double.TryParse(paramStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var p))
                {
                    factor = p;
                }

                return scale * DefaultBaseFontSize * factor;
            }

            // fallback
            return DefaultBaseFontSize;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
