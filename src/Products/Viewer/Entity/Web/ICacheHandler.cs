using GroupDocs.Viewer.MVC.Products.Viewer.Cache;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Entity.Web
{
    interface ICacheHandler
    {
        void CreateCache(ICustomViewer customCache);
    }
}
