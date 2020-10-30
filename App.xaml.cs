using FileExplorer.DataModels;
using FileExplorer.Properties;
using Microsoft.Win32;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Windows;

namespace FileExplorer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_Start(object sender, StartupEventArgs e)
        {
            if (Settings.Default.IsFirstRun)
            {
                MessageBoxResult result = MessageBox.Show("Is it a GR Machine?", "For Root Directories", MessageBoxButton.YesNo);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        Settings.Default.IsOnGRServer = true;
                        break;
                    case MessageBoxResult.No:
                        Settings.Default.IsOnGRServer = false;
                        break;
                    default:
                        Settings.Default.IsOnGRServer = false;
                        break;
                }
            }

            Settings.Default.IsFirstRun = false;
            Settings.Default.Save();
            
            MainWindow main = new MainWindow();
            if (Settings.Default.IsOnGRServer)
            {
                foreach (var dir in Settings.Default.ProjectRoot)
                {
                    //RootFolders.TryAdd(dir, CreateTreeViewItem(new DirectoryInfo(dir)));
                    DirectoryMeta directoryMeta = new DirectoryMeta(dir);
                    FolderTreeData fData = new FolderTreeData(directoryMeta.DirectoryInfo);
                    var cd = fData.ChildDirectories;
                    var cf = fData.DirectoryFiles;
                    main.RootFolders.TryAdd(directoryMeta, fData);
                    main.FD.Add(directoryMeta.DirectoryInfo);
                }
            }
            main.Show();
        }
    }
}
