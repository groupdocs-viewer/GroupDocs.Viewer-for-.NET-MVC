namespace GroupDocs.Viewer.MVC.Products.Viewer.Util
{
    public interface ICustomViewer
    {
        GroupDocs.Viewer.Viewer GetViewer();

        void GenerateFileCache();
    }
}