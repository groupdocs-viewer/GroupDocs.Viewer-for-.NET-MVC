using GroupDocs.Viewer.MVC.Products.Common.Entity.Web;
using GroupDocs.Viewer.MVC.Products.Viewer.Cache;
using System.Collections.Generic;
using System.IO;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Entity.Web
{
    interface IFileWrapper
    {
        Stream GetFileStream(string guid);

        string GetFileCachePath(string guid);

        List<FileDescriptionEntity> GetFilesList();

        string GetFileName(string guid);

        void SetFileName(string fileName);

        void CreateCache(ICustomViewer customCache);
    }
}
