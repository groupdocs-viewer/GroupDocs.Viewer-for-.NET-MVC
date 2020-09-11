﻿using GroupDocs.Viewer.MVC.Products.Common.Config;
using GroupDocs.Viewer.MVC.Products.Viewer.Cache;
using System.IO;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Util
{
    public class LocalCacheHandler : ICacheHandler
    {
        private readonly GlobalConfiguration globalConfiguration;

        public LocalCacheHandler(GlobalConfiguration globalConfiguration)
        {
            this.globalConfiguration = globalConfiguration;
        }

        public string GetFileCachePath(string fileName)
        {
            return Path.Combine(this.globalConfiguration.Viewer.GetFilesDirectory(), this.globalConfiguration.Viewer.GetCacheFolderName(), Path.GetFileName(fileName).Replace('.', '_'));
        }

        public void CreateCache(ICustomViewer customCache)
        {
            customCache.GenerateFileCache();
        }
    }
}