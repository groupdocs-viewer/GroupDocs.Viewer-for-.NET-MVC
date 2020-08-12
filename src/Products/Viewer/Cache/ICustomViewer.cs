namespace GroupDocs.Viewer.MVC.Products.Viewer.Cache
{
    interface ICustomViewer
    {
        GroupDocs.Viewer.Viewer GetViewer();
        void CreateCache();
        void Dispose();
        Results.FileInfo GetFileInfo();
        System.IO.FileInfo GetPageFile(int pageNumber);
    }
}