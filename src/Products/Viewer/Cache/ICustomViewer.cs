namespace GroupDocs.Viewer.MVC.Products.Viewer.Cache
{
    public interface ICustomViewer
    {
        GroupDocs.Viewer.Viewer GetViewer();

        void CreateCache();
    }
}