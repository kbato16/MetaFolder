using LiteDB;
using MaterialDesignThemes.Wpf.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FileExplorer.DataModels
{
    public class DirectoryMeta
    {
        [BsonIgnore]
        private ObservableCollection<DirectoryMeta> _childItems = new ObservableCollection<DirectoryMeta>();
        [BsonIgnore]
        public ObservableCollection<DirectoryMeta> ChildItems
        {
            get
            {
                return _childItems;
            }
            private set { _childItems = value; }
        }
        [BsonId]
        public int Id { get; private set; }
        [BsonField("StoreBanner")]
        [BsonRef("storebanners")]
        public StoreBanner StoreBanner { get; set; }
        [BsonField("IsRevitProject")]
        public bool IsRevitProject { get; set; }
        [BsonField("DirectoryPath")]
        public string DirectoryPath { get; set; }
        [BsonField("Name")]
        public string Name { get; set; }
        [BsonIgnore]
        public FileSystemInfo ItemFSInfo { get; set; }
        [BsonIgnore]
        public bool IsDirectory { get { return File.GetAttributes(DirectoryPath) == FileAttributes.Directory; } }
        [BsonIgnore]
        public string Type { get { return IsDirectory ? "Directory" : "File"; } }
        [BsonCtor]
        public DirectoryMeta()
        {
            ChildItems.Add(null);
        }
        public DirectoryMeta(string path)
        {
            DirectoryPath = path;
            ItemFSInfo = IsDirectory ? new DirectoryInfo(DirectoryPath) : (new FileInfo(DirectoryPath)) as FileSystemInfo;
            Name = ItemFSInfo.Name;
            if(IsDirectory)
            {
                StoreBanner = DBServices.NoBanner;
                ChildItems.Add(null);
            }
        }
        public DirectoryMeta(FileSystemInfo fsinfo)
        {
            DirectoryPath = fsinfo.FullName;
            ItemFSInfo = fsinfo;
            Name = ItemFSInfo.Name;
            if (IsDirectory)
            {
                StoreBanner = DBServices.NoBanner;
                ChildItems.Add(null);
            }
        }
        public DirectoryMeta(string path, StoreBanner banner)
        {
            DirectoryPath = path;
            ItemFSInfo = IsDirectory ? new DirectoryInfo(DirectoryPath) : (new FileInfo(DirectoryPath)) as FileSystemInfo;
            Name = ItemFSInfo.Name;
            if(IsDirectory)
            {
                StoreBanner = banner;
                ChildItems.Add(null);
            }
        }

        public DirectoryMeta(string directoryPath, string name, StoreBanner storeBanner, bool isRevitProject)
        {
            DirectoryPath = directoryPath;
            IsRevitProject = isRevitProject;
            ItemFSInfo = IsDirectory ? new DirectoryInfo(DirectoryPath) : (new FileInfo(DirectoryPath)) as FileSystemInfo;
            Name = ItemFSInfo.Name;
            if(IsDirectory)
            {
                StoreBanner = storeBanner;
                ChildItems.Add(null);
            }
        }
        public DirectoryInfo GetDirectoryInfo()
        {
            if (ItemFSInfo != null)
                return IsDirectory ? ItemFSInfo as DirectoryInfo : (ItemFSInfo as FileInfo).Directory;   
            return IsDirectory ? new DirectoryInfo(DirectoryPath) : (new FileInfo(DirectoryPath)).Directory; 
        }
        public void LoadChildItems(Dispatcher dispatcher)
        {
            if (ChildItems[0] != null)
                return;
            ChildItems.Clear();
            GetDirectoryInfo().GetFileSystemInfos().ToList().ForEach(x =>
            {
                if (!Regex.IsMatch(x.Name, "^[.]"))
                {
                        DirectoryMeta meta = x is DirectoryInfo ? DBServices.Instance.GetInsertFolderData(x as DirectoryInfo) : new DirectoryMeta(x.FullName);
                        if (meta.IsDirectory)
                            meta.ChildItems.Add(null);
                        ChildItems.Add(meta);
                   
                }
            });
        }
        public static implicit operator DirectoryMeta(DirectoryInfo data)
        {
            return new DirectoryMeta(data);
        }
        public static implicit operator DirectoryInfo(DirectoryMeta data)
        {
            return (DirectoryInfo)data;
        }
    }

    public class FolderTreeData : FileSystemInfo
    {
        private List<FolderTreeData> _childDirs;
        private List<FileInfo> _dirFiles;
        public DirectoryInfo ParentDirectory { get; set; }
        public List<FolderTreeData> ChildDirectories
        {
            get
            {
                if (_dirFiles != null)
                    return _childDirs;
                else
                    return new List<FolderTreeData>();
            }
            private set { _childDirs = value; }
        }
        public List<FileInfo> DirectoryFiles
        {
            get
            {
                if (_dirFiles != null)
                    return _dirFiles;
                else
                    return new List<FileInfo>();
            }
            private set { _dirFiles = value; }
        }

        public override string Name { get { return ParentDirectory.Name; } }
        public override bool Exists { get { return ParentDirectory.Exists; } }
        public override void Delete()
        {
            ParentDirectory.Delete();
        }
        public FolderTreeData() { }
        public FolderTreeData(FileSystemInfo fsinfo)
        {
            base.FullPath = fsinfo.FullName;
            base.OriginalPath = fsinfo.FullName;
            ParentDirectory = fsinfo as DirectoryInfo;

            ChildDirectories = ParentDirectory.GetDirectories().ToList().ConvertAll(x => (FolderTreeData)x);

            DirectoryFiles = ParentDirectory.GetFiles().ToList();
        }
        public static implicit operator FolderTreeData(DirectoryInfo data)
        {
            return new FolderTreeData(data);
        }

        public static implicit operator DirectoryInfo(FolderTreeData data)
        {
            return (DirectoryInfo)data;
        }
    }

}
