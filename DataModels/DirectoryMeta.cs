using LiteDB;
using System.IO;
using System.Runtime.InteropServices;

namespace FileExplorer.DataModels
{
    public class DirectoryMeta
    {
        [BsonId]
        public int Id { get; private set; }
        [BsonRef("storebanners")]
        public StoreBanner StoreBanner { get; set; }   
        public bool IsRevitProject { get; set; }
        public string DirectoryPath { get; set; }
        public string Name { get; set; }
        [BsonCtor()]
        public DirectoryMeta(int id, StoreBanner storeBanner, bool isRevitProject, string directoryPath, string name)
        {
            Id = id;
            StoreBanner = storeBanner;
            IsRevitProject = isRevitProject;
            DirectoryPath = directoryPath;
            Name = name;
        }
        public DirectoryMeta(string path, StoreBanner banner)
        {
            Name = (new DirectoryInfo(path)).Name;
            
            StoreBanner = banner;
            DirectoryPath = path;
        }
        public DirectoryMeta() {  }
        public DirectoryMeta(string path) 
        { 
            DirectoryPath = path;
            Name = this.GetDirectoryInfo().Name;
            StoreBanner = DBServices.NoBanner;
        }

        public DirectoryInfo GetDirectoryInfo()
        {
            return new DirectoryInfo(DirectoryPath);
        }
    }
   
}
