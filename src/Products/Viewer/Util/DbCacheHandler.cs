using GroupDocs.Viewer.MVC.Products.Common.Config;
using GroupDocs.Viewer.MVC.Products.Viewer.Cache;
using System.IO;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Util
{
    public class DbCacheHandler : ICacheHandler
    {
        private readonly GlobalConfiguration globalConfiguration;

        public DbCacheHandler(GlobalConfiguration globalConfiguration)
        {
            this.globalConfiguration = globalConfiguration;
        }

        public string GetFileCachePath(string fileName)
        {
            return Path.Combine(this.globalConfiguration.Viewer.GetFilesDirectory(), this.globalConfiguration.Viewer.GetCacheFolderName(), fileName);
        }

        public void CreateCache(ICustomViewer customCache)
        {
            customCache.GenerateFileCache();
        }
    }
}