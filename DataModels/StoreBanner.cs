
using LiteDB;
using System;
using System.Collections;

namespace FileExplorer.DataModels
{
    public class StoreBanner
    {
        
        [BsonId]
        public string BANNER_CODE { get; set; }
        [BsonField("BANNER_NAME")]
        public string BANNER_NAME { get; set; }
        [BsonField("CLIENT")]
        public string CLIENT { get; set; }

        [BsonCtor]
        public StoreBanner(string banner_code, string banner_name, string client)
        {
            BANNER_CODE = banner_code;
            BANNER_NAME = banner_name;
            CLIENT = client;
        }
        public StoreBanner() { }

    }
}
