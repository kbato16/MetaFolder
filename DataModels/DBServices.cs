
using LiteDB;
using FileExplorer.DataModels;
using System.Configuration;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using System;
using FileExplorer.Properties;
using System.Windows.Input;
using System.Runtime.InteropServices.WindowsRuntime;

namespace FileExplorer
{
    public sealed class DBServices
    {
        public static readonly StoreBanner NoBanner = new StoreBanner() { BANNER_NAME = "None", BANNER_CODE = "000" };
        public bool IsApplicationUpdate = false;
        public ObservableConcurrentDictionary<string, StoreBanner> Banners { get; set; }
        public ObservableConcurrentDictionary<string, DirectoryMeta> Folders { get; set; }
        private void UpdateServiceCollection()
        {
            Folders = GetFoldersData();
            Banners = GetStoreBanners();
        }
        public void AddBanner(StoreBanner storeBanner)
        {
            IsApplicationUpdate = true;
            using (var db = new LiteDatabase(Settings.Default.ConnectionString))
            {
                var collection = db.GetCollection<StoreBanner>("storebanners");
                if (!collection.Exists(x => x.BANNER_CODE == storeBanner.BANNER_CODE || x.BANNER_NAME == storeBanner.BANNER_NAME))
                    collection.Insert(storeBanner);
                db.Commit();
                Banners = collection.FindAll().ToDictionary(banner => banner.BANNER_CODE);
            }
        }
        public StoreBanner DB_CheckAndInsertUpdateBannerData(StoreBanner data)
        {
            IsApplicationUpdate = true;
            using (var db = new LiteDatabase(Settings.Default.ConnectionString))
            {
                db.Timeout = TimeSpan.FromSeconds(2);
                var collection = db.GetCollection<StoreBanner>("storebanners");
                if (!collection.Exists(x => x.BANNER_CODE == data.BANNER_CODE || x.BANNER_NAME == data.BANNER_NAME))
                    collection.Insert(data);
                else if (!collection.Exists(x => x.BANNER_CODE == data.BANNER_CODE && x.BANNER_NAME == data.BANNER_NAME && x.CLIENT == data.CLIENT))
                    collection.Update(data);
                db.Commit();

            }
            UpdateServiceCollection();
            return Banners[data.BANNER_CODE];
        }
        public DirectoryMeta CheckAndInsertUpdateData(DirectoryMeta data)
        {
            IsApplicationUpdate = true;
            using (var db = new LiteDatabase(Settings.Default.ConnectionString))
            {
                db.Timeout = TimeSpan.FromSeconds(2);
                var collection = db.GetCollection<DirectoryMeta>("projdirs");
                if (!collection.Exists(x => x.DirectoryPath == data.DirectoryPath))
                    collection.Insert(data);
                else if (!collection.Exists(x => x.DirectoryPath == data.DirectoryPath && x.StoreBanner.BANNER_CODE == data.StoreBanner.BANNER_CODE))
                    collection.Update(data);
                db.Commit();
            }
            UpdateServiceCollection();
            return Folders[data.DirectoryPath];
        }
        public DirectoryMeta GetInsertFolderData(string Path)
        {
            DirectoryMeta meta = GetFoldersData().ContainsKey(Path) ? Folders[Path] : CheckAndInsertUpdateData(new DirectoryMeta(Path, NoBanner));
            return meta;
        }
        public DirectoryMeta GetInsertFolderData(DirectoryInfo dirinfo)
        {
            DirectoryMeta meta = GetFoldersData().ContainsKey(dirinfo.FullName) ? Folders[dirinfo.FullName] : CheckAndInsertUpdateData(new DirectoryMeta(dirinfo.FullName, NoBanner));
            return meta;
        }
        public Dictionary<string, DirectoryMeta> GetFoldersData()
        {
            using (var db = new LiteDatabase(Settings.Default.ConnectionString))
            {
                db.Timeout = TimeSpan.FromSeconds(2);
                try
                {
                    Folders = db.GetCollection<DirectoryMeta>("projdirs").Include<StoreBanner>(x => x.StoreBanner).FindAll().ToDictionary(folder => folder.DirectoryPath);
                    IsApplicationUpdate = true;
                    foreach (var folder in Folders)
                    {
                        if (!folder.Value.GetDirectoryInfo().Exists)
                        {
                            db.GetCollection<DirectoryMeta>("projdirs").Delete(folder.Value.Id);
                        }
                    }
                    db.Commit();
                }
                catch (ArgumentNullException)
                {
                    return Folders;
                }
            }
            return Folders;
        }
        public Dictionary<string, StoreBanner> GetStoreBanners()
        {
            using (var db = new LiteDatabase(Settings.Default.ConnectionString))
            {
                db.Timeout = TimeSpan.FromSeconds(2);
                Banners = db.GetCollection<StoreBanner>("storebanners").FindAll().ToDictionary(banner => banner.BANNER_CODE);
                db.Commit();
            }
            return Banners;
        }
        public Dictionary<string, DirectoryMeta> SearchFoldersByTag(StoreBanner banner)
        {
            using (var db = new LiteDatabase(Settings.Default.ConnectionString))
            {
                db.Timeout = TimeSpan.FromSeconds(2);
                Folders = db.GetCollection<DirectoryMeta>("projdirs").Include<StoreBanner>(x => x.StoreBanner).Query().Where(x => x.StoreBanner.BANNER_CODE == banner.BANNER_CODE).ToList().ToDictionary(folder => folder.DirectoryPath);
                db.Commit();
            }
            return Folders;
        }
        public void InitializeDB()
        {
            IsApplicationUpdate = true;
            AddBanner(NoBanner);
            AddBanner(new StoreBanner() { BANNER_CODE = "SWY", BANNER_NAME = "Safeway", CLIENT = "Sobeys" });
            AddBanner(new StoreBanner() { BANNER_CODE = "FDL", BANNER_NAME = "Foodland", CLIENT = "Sobeys" });
            AddBanner(new StoreBanner() { BANNER_CODE = "FCO", BANNER_NAME = "FreschCo", CLIENT = "Sobeys" });
            AddBanner(new StoreBanner() { BANNER_CODE = "CHL", BANNER_NAME = "Chalo", CLIENT = "Sobeys" });

            GetStoreBanners();
            GetFoldersData();
        }

        #region Singleton pattern
        private DBServices()
        {
            Banners = new Dictionary<string, StoreBanner>();
            Folders = new Dictionary<string, DirectoryMeta>();
        }
        private static readonly DBServices instance = new DBServices();
        public static DBServices Instance { get { return instance; } }
        #endregion
    }
}
