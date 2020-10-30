using LiteDB;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace FileExplorer.DataModels
{
    public class DirectoryMeta : FileSystemInfo
    {
        private List<DirectoryMeta> _childDirs;
        private List<FileInfo> _dirFiles;
        public DirectoryInfo DirectoryInfo { get; set; }
        public List<DirectoryMeta> ChildDirectories
        {
            get
            {
                if (_dirFiles != null)
                    return _childDirs;
                else
                    return new List<DirectoryMeta>();
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
        public string _Name { get; set; }
        public override bool Exists { get { return DirectoryInfo.Exists; } }
        public override string Name { get { return _Name; } }
        
        [BsonCtor]
        public DirectoryMeta() 
        { 
            if (!string.IsNullOrEmpty(DirectoryPath)) 
            {
                DirectoryInfo = new DirectoryInfo(DirectoryPath);
                FullPath = DirectoryPath;
                base.FullPath = DirectoryPath;
            } 
        }
        public DirectoryMeta(string path)
        { 
            DirectoryPath = path;
            base.FullPath = DirectoryPath;
            DirectoryInfo = new DirectoryInfo(DirectoryPath);
            _Name = DirectoryInfo.Name;
            StoreBanner = DBServices.NoBanner;
            ChildDirectories = DirectoryInfo.GetDirectories().ToList().ConvertAll(x => (DirectoryMeta)x);
            DirectoryFiles = DirectoryInfo.GetFiles().ToList();
        }
        public DirectoryMeta(DirectoryInfo dinfo)
        {
            DirectoryPath = dinfo.FullName;
            base.FullPath = DirectoryPath;
            DirectoryInfo = dinfo;
            _Name = DirectoryInfo.Name;
            StoreBanner = DBServices.NoBanner;
        }
        public DirectoryMeta(string path, StoreBanner banner)
        {
            _Name = (new DirectoryInfo(path)).Name;
            DirectoryPath = path;
            base.FullPath = DirectoryPath;
            StoreBanner = banner;
            DirectoryInfo = new DirectoryInfo(DirectoryPath);
        }
        public DirectoryMeta(int id, StoreBanner storeBanner, bool isRevitProject, string directoryPath)
        {
            Id = id;
            StoreBanner = storeBanner;
            IsRevitProject = isRevitProject;
            DirectoryPath = directoryPath;
            base.FullPath = DirectoryPath;
            DirectoryInfo = new DirectoryInfo(DirectoryPath);
            _Name = DirectoryInfo.Name;
        }
        public DirectoryInfo GetDirectoryInfo()
        {
            if (this.DirectoryInfo == null)
                DirectoryInfo = new DirectoryInfo(DirectoryPath);
            return DirectoryInfo;
        }
        public override void Delete()
        {
            DirectoryInfo.Delete();
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
