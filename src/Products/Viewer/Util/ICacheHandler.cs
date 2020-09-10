using GroupDocs.Viewer.MVC.Products.Viewer.Cache;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Util
{
    interface ICacheHandler
    {
        string GetFileCachePath(string guid);

        void CreateCache(ICustomViewer customCache);
    }
}
