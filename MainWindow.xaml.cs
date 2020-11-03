using LiteDB;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FileExplorer.DataModels;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Net;
using System.Security;
using System.Xaml;
using System.Diagnostics;
using System.Collections;
using FileExplorer.Properties;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Windows.Input;
using System.Threading.Tasks;

namespace FileExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DBServices DB = DBServices.Instance;
        public ObservableCollection<DirectoryMeta> RootFolders = new ObservableCollection<DirectoryMeta>();
        private DBFileWatch dbFileWatch = new DBFileWatch();
        private static bool HasSearched = false;

        public MainWindow()
        {
            InitializeComponent();
            InitRootFolders();
            InitTopBar();
            //dbFileWatch.FileChanged += DbFileWatch_FileChanged;
        }
        private void InitRootFolders()
        {
            fileTree.ItemsSource = RootFolders;
        }
        private void FileTree_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)e.OriginalSource;
            var dm = item.DataContext as DirectoryMeta;
            //item.ContextMenu = CreateBannerContextMenu(dm);
            if (dm.IsDirectory)
                dm.LoadChildItems(Dispatcher);
            item.Items.SortDescriptions.Add(new SortDescription("Type", ListSortDirection.Ascending));
            item.Items.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }
        private void TextSearch()
        {
            List<DirectoryMeta> dirsFound = new List<DirectoryMeta>();
            string searchCondition = String.Format(".*{0}.*", txt_Search.Text.ToLower());
            foreach (DirectoryMeta item in RootFolders)
            {
                if (Regex.IsMatch(item.Name.ToLower(), searchCondition))
                    dirsFound.Add(item);
                TraversalSearch(ref dirsFound, item, searchCondition);
            }
            fileTree.ItemsSource = dirsFound;
            fileTree.Items.SortDescriptions.Add(new SortDescription("Type", ListSortDirection.Ascending));
            fileTree.Items.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }
        private void TraversalSearch(ref List<DirectoryMeta> dirsFound, DirectoryMeta parent, string searchCondition)
        {
            foreach (DirectoryMeta sub in parent.ChildItems.Where(x => x != null))
            {
                if (Regex.IsMatch(sub.Name.ToLower(), searchCondition))
                    dirsFound.Add(sub);
                TraversalSearch(ref dirsFound, sub, searchCondition);
            }
        }
        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(txt_Search.Text))
                TextSearch();
            else
                fileTree.ItemsSource = RootFolders;
        }
        private void txt_Search_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!String.IsNullOrEmpty(txt_Search.Text))
                    TextSearch();
                else
                    fileTree.ItemsSource = RootFolders;
            }
        }
        private ContextMenu CreateBannerContextMenu(DirectoryMeta fs)
        {
            ContextMenu menu = new ContextMenu();
            foreach (StoreBanner banner in DB.Banners.Values)
            {
                MenuItem item = new MenuItem() { Header = banner.BANNER_NAME, Tag = banner };
                item.Click += delegate (object sender, RoutedEventArgs e)
                {
                    fs.StoreBanner = banner;
                    DB.CheckAndInsertUpdateData(fs);
                };
                menu.Items.Add(item);
            }
            return menu;
        }

        private void DbFileWatch_FileChanged(string filePath)
        {
            if (!DB.IsApplicationUpdate)
            {
                Dictionary<string, DirectoryMeta> dirs = DB.GetFoldersData();
                foreach (var item in RootFolders)
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
                    {
                        StoreBanner banner = ((fileTree.Items.CurrentItem as TreeViewItem).Tag as DirectoryMeta).StoreBanner;
                        if (HasSearched && banner.BANNER_CODE != DBServices.NoBanner.BANNER_CODE)
                        {
                            ObservableCollection<TreeViewItem> itemsFound = new ObservableCollection<TreeViewItem>();
                            foreach (var dir in DB.SearchFoldersByTag(banner))
                            {
                                itemsFound.Add(CreateTreeViewItem(dir.Value.ItemFSInfo));
                            }
                            fileTree.ItemsSource = itemsFound;
                            HasSearched = true;
                        }
                        else
                        {
                            //RootFolders[item.Key] = TreeTraverse(item.Value, dirs);
                        }
                    }));
                }
            }
            else
            {
                DB.IsApplicationUpdate = false;
            }
        }
        private void InitTopBar()
        {
            DB.Banners.AsParallel().ForAll(banner =>
            {
                if (!String.IsNullOrEmpty(banner.Value.BANNER_NAME))
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)(() =>
                    {
                        Chip tagChip = new Chip() { Name = "btn_" + banner.Key, Tag = banner.Value, Icon = new PackIcon() { Kind = PackIconKind.Tag }, Content = banner.Value.BANNER_NAME, Margin = new Thickness(5) };
                        tagChip.Click += TagChip_Click;
                        TopBar.Children.Add(tagChip);
                    }));
            });
        }
        private void TagChip_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                StoreBanner banner = (sender is Chip) ? (sender as Chip).Tag as StoreBanner : sender as StoreBanner;
                if (banner.BANNER_CODE == DBServices.NoBanner.BANNER_CODE)
                {
                    InitRootFolders();
                    HasSearched = false;
                }
                else
                {
                    ObservableCollection<TreeViewItem> itemsFound = new ObservableCollection<TreeViewItem>();
                    foreach (var dir in DB.SearchFoldersByTag(banner))
                    {
                        itemsFound.Add(CreateTreeViewItem(dir.Value.ItemFSInfo));
                    }
                    fileTree.ItemsSource = itemsFound;
                    HasSearched = true;
                }
                fileTree.Items.Refresh();
                fileTree.UpdateLayout();
            });
        }
        private TreeViewItem TreeTraverse(TreeViewItem node, Dictionary<string, DirectoryMeta> dirs)
        {
            DirectoryMeta nodeItem = this.Dispatcher.Invoke(new Func<DirectoryMeta>(() => { return node.Tag as DirectoryMeta; }));
            DirectoryMeta dbData = dirs[nodeItem.DirectoryPath];
            TreeViewItem parentNode = node;
            if (parentNode.Items.Count > 0 && parentNode.Items[0] != null)
            {
                for (int i = 0; i < parentNode.Items.Count; i++)
                {
                    this.Dispatcher.Invoke(() => { parentNode.Items[i] = TreeTraverse((parentNode.Items[i] as TreeViewItem), dirs); });
                }
                return parentNode;
            }
            else
            {
                if (nodeItem.GetHashCode() != dbData.GetHashCode())
                    return this.Dispatcher.Invoke(new Func<TreeViewItem>(() => { return CreateTreeViewItem(dbData.ItemFSInfo); }));
                else
                    return parentNode;
            }

        }
        private void RootFolders_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            fileTree.Items.Refresh();
        }
        private TreeViewItem CreateTreeViewItem(FileSystemInfo fsinfo)
        {
            TreeViewItem item = new TreeViewItem();

            DirectoryMeta dirMeta = DB.GetInsertFolderData(fsinfo.FullName);
            StoreBanner banner = dirMeta.StoreBanner;

            //Build StackPanel as Header
            StackPanel stack = new StackPanel() { Orientation = Orientation.Horizontal };
            Label folderName = new Label() { Content = fsinfo.Name, VerticalContentAlignment = VerticalAlignment.Center };

            Chip chipBanner = new Chip()
            {
                Content = banner.BANNER_NAME,
                Visibility = banner.BANNER_CODE != DBServices.NoBanner.BANNER_CODE ? Visibility.Visible : Visibility.Hidden
            };
            Chip chipClient = new Chip()
            {
                Content = banner.CLIENT,
                Visibility = banner.BANNER_CODE != DBServices.NoBanner.BANNER_CODE ? Visibility.Visible : Visibility.Hidden
            };

            stack.Children.Add(folderName);
            stack.Children.Add(chipBanner);
            stack.Children.Add(chipClient);

            item.Header = stack;
            item.ToolTip = fsinfo.FullName;
            item.Tag = dirMeta;
            item.Items.Add(null);
            item.Expanded += Item_Expanded;
            //Build ContextMenu 
            ContextMenu menu = new ContextMenu();
            Button btnAddOrChangeBanner = new Button() { Content = "Select Banner", ContextMenu = menu };
            btnAddOrChangeBanner.Click += delegate (object sender, RoutedEventArgs e) { (sender as Button).ContextMenu.IsOpen = true; };
            stack.Children.Add(btnAddOrChangeBanner);
            this.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)(() =>
            {
                DB.GetStoreBanners().ToList()
                .ForEach(x =>
                {
                    MenuItem menuItem = new MenuItem() { Header = x.Value.BANNER_NAME, Tag = x.Value };

                    //Update DB to change Banner
                    menuItem.Click += delegate (object sender, RoutedEventArgs e)
                    {
                        DirectoryMeta dir = dirMeta;
                        dirMeta.StoreBanner = x.Value;
                        DB.CheckAndInsertUpdateData(dir);
                        item = CreateTreeViewItem(dir.ItemFSInfo);
                        if (x.Value.BANNER_CODE != DBServices.NoBanner.BANNER_CODE)
                        {
                            chipBanner.Content = x.Value.BANNER_NAME;
                            chipClient.Content = x.Value.CLIENT;
                            chipBanner.Visibility = Visibility.Visible;
                            chipClient.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            chipBanner.Content = x.Value.BANNER_NAME;
                            chipClient.Content = x.Value.CLIENT;
                            chipBanner.Visibility = Visibility.Hidden;
                            chipClient.Visibility = Visibility.Hidden;
                        }
                        if (HasSearched)
                        {
                            ObservableCollection<TreeViewItem> itemsFound = new ObservableCollection<TreeViewItem>();
                            foreach (var directory in DB.SearchFoldersByTag(banner))
                            {
                                itemsFound.Add(CreateTreeViewItem(directory.Value.ItemFSInfo));
                            }
                            fileTree.ItemsSource = itemsFound;
                            HasSearched = true;
                        }
                    };
                    menu.Items.Add(menuItem);
                });
            }));
            return item;
        }
        private void Item_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem node = sender as TreeViewItem;
            if (node.Items.Count != 1 || node.Items[0] != null)
                return;
            node.Items.Clear();

            if (node.Tag != null)
            {
                DirectoryInfo dir = (node.Tag as DirectoryMeta).ItemFSInfo as DirectoryInfo;
                foreach (DirectoryInfo sub in dir.GetDirectories())
                {
                    node.Items.Add(CreateTreeViewItem(sub));
                }
            }
        }
    }
    public class DBFileWatch
    {
        public delegate void DBFileChangeEventArgs(string filePath);
        public event DBFileChangeEventArgs FileChanged;
        public DBFileWatch(string filePath = @"C:\Users\kenne\Documents\Dev Projects\TestDirectory", string filefilter = "*?.db")
        {
            var watcher = new FileSystemWatcher
            {
                Path = filePath,
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = filefilter,
                EnableRaisingEvents = true
            };

            watcher.Changed += (sender, args) => { FileChanged?.Invoke(args.FullPath); };
        }
    }


}
