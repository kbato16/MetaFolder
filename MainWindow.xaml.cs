﻿using LiteDB;
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

namespace FileExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DBServices DB = DBServices.Instance;
        //private ObservableCollection<TreeViewItem> RootFolders { get; set; } = new ObservableCollection<TreeViewItem>();
        private ObservableConcurrentDictionary<string, TreeViewItem> RootFolders { get; set; } = new ObservableConcurrentDictionary<string, TreeViewItem>();
        private DBFileWatch dbFileWatch = new DBFileWatch();
        private static bool HasSearched = false;
        
        public MainWindow()
        {
            InitializeComponent();
            DB.InitializeDB();

            InitRootFolders();
            InitTopBar();
            dbFileWatch.FileChanged += DbFileWatch_FileChanged;
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
                                itemsFound.Add(CreateTreeViewItem(dir.Value.GetDirectoryInfo()));
                            }
                            fileTree.ItemsSource = itemsFound;
                            HasSearched = true;
                        }
                        else
                        {
                            RootFolders[item.Key] = TreeTraverse(item.Value, dirs);
                        }
                    }));
                }
            }
            else
            {
                DB.IsApplicationUpdate = false;
            }
        }
        private void InitRootFolders()
        {
            if (RootFolders.Count == 0)
            {
                foreach (var dir in Directory.GetDirectories(@"C:\Users\kenne\Documents\Dev Projects\TestDirectory"))
                {
                    RootFolders.TryAdd(dir, CreateTreeViewItem(new DirectoryInfo(dir)));
                }
            }
            fileTree.ItemsSource = RootFolders.Values;
            RootFolders.CollectionChanged += RootFolders_CollectionChanged;
        }
        private void InitTopBar()
        {
            DB.Banners.AsParallel().ForAll(banner =>
            {
                if (!String.IsNullOrEmpty(banner.Value.BANNER_NAME))
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)(() =>
                    {
                        Chip tagChip = new Chip() { Name ="btn_"+banner.Key, Tag = banner.Value, Icon = new PackIcon() { Kind = PackIconKind.Tag }, Content = banner.Value.BANNER_NAME, Margin = new Thickness(5) };
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
                        itemsFound.Add(CreateTreeViewItem(dir.Value.GetDirectoryInfo()));
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
            if (parentNode.Items.Count > 0 && parentNode.Items[0] != null )
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
                    return this.Dispatcher.Invoke(new Func<TreeViewItem>(() => { return CreateTreeViewItem(dbData.GetDirectoryInfo()); }));
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
                        item = CreateTreeViewItem(dir.GetDirectoryInfo());
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
                                itemsFound.Add(CreateTreeViewItem(directory.Value.GetDirectoryInfo()));
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
                DirectoryInfo dir = (node.Tag as DirectoryMeta).GetDirectoryInfo();
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
