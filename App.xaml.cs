using FileExplorer.DataModels;
using FileExplorer.Properties;
using Microsoft.Win32;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
            DBServices.Instance.InitializeDB();
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
                    DirectoryMeta directoryMeta = new DirectoryMeta(dir);
                    main.RootFolders.Add(directoryMeta);
                }
            }
            else
            {
                foreach (var dir in Directory.GetDirectories(@"C:\Users\kenne\Documents\Dev Projects\TestDirectory"))
                {
                    DirectoryInfo i = new DirectoryInfo(dir);
                    if (Regex.IsMatch(i.Name, "."))
                    {
                        DirectoryMeta directoryMeta = DBServices.Instance.GetInsertFolderData(dir);
                        //directoryMeta.LoadChildItems(Dispatcher);
                        main.RootFolders.Add(directoryMeta);
                    }
                }
                Parallel.ForEach(main.RootFolders, (folder, state) => 
                {
                    folder.LoadChildItems();
                    state.Break();
                });
            }
            main.Show();
        }
    }
}
