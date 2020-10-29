using System;
using System.Globalization;
using System.Windows.Data;
using FileExplorer.DataModels;

namespace FileExplorer.Converters
{
    public class TagToBannerConverter : IValueConverter
    {
        private DBServices DB = DBServices.Instance;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((value as DirectoryMeta).StoreBanner.BANNER_CODE != "NAN")
                return (value as DirectoryMeta).StoreBanner;
            else
                return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
