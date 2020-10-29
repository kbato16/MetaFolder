using System;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security;
using System.Windows;
using System.Windows.Data;
using FileExplorer.DataModels;

namespace FileExplorer.Converters
{
    public class BannerToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool visibility = (value.ToString() == DBServices.NoBanner.BANNER_CODE ) ? true : false;
            return visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
