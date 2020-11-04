using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using FileExplorer.DataModels;

namespace FileExplorer.Converters
{
    public class MetaToTreeViewItemConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DirectoryMeta meta = value as DirectoryMeta;
            ContextMenu menu = new ContextMenu();
            if (meta.IsDirectory)
            {
                foreach (StoreBanner banner in DBServices.Instance.Banners.Values)
                {
                    MenuItem item = new MenuItem() { Header = banner.BANNER_NAME, Tag = banner };
                    item.Click += delegate (object sender, RoutedEventArgs e)
                    {
                        meta.StoreBanner = banner;
                        DBServices.Instance.CheckAndInsertUpdateData(meta);
                    };
                    menu.Items.Add(item);
                }
            }
            else
            {
                MenuItem openFile = new MenuItem() { Header = "Open File" };
                MenuItem openDir = new MenuItem() { Header = "Open File Directory" };
                openFile.Click += delegate (object sender, RoutedEventArgs e)
                {
                    
                };
                openDir.Click += delegate (object sender, RoutedEventArgs e)
                {
                    Process.Start("explorer.exe", ((FileInfo)meta.ItemFSInfo).DirectoryName);
                };
                menu.Items.Add(openFile);
                menu.Items.Add(openDir);
            }
            return menu;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
