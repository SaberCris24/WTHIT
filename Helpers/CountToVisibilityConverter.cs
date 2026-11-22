using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace WTHIT.Helpers
{
    /// <summary>
    /// Converts count to Visibility: Visible when 0, Collapsed otherwise.
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) =>
            value is int count && count == 0 ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            throw new NotImplementedException();
    }
}
