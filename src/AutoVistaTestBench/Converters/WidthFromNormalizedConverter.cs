using System.Globalization;
using System.Windows.Data;

namespace AutoVistaTestBench.Converters
{
    /// <summary>
    /// Converts a normalized value (0.0–1.0) and a total width into a pixel width.
    /// Used for the channel level bar in the channel monitor.
    /// MultiBinding: [0] = NormalizedValue (double), [1] = ContainerWidth (double)
    /// </summary>
    public class WidthFromNormalizedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2
                && values[0] is double normalizedValue
                && values[1] is double containerWidth)
            {
                double width = normalizedValue * containerWidth;
                return Math.Max(2.0, Math.Min(width, containerWidth));
            }
            return 2.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}