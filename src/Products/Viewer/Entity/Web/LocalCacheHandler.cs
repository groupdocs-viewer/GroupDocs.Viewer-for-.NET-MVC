using GroupDocs.Viewer.MVC.Products.Viewer.Cache;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Entity.Web
{
    public class LocalCacheHandler : ICacheHandler
    {
        public void CreateCache(ICustomViewer customCache)
        {
            customCache.CreateCache();
        }
    }
}