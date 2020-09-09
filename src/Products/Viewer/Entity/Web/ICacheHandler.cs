using GroupDocs.Viewer.MVC.Products.Viewer.Cache;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Entity.Web
{
    interface ICacheHandler
    {
        string GetFileCachePath(string guid);

        void CreateCache(ICustomViewer customCache);
    }
}
