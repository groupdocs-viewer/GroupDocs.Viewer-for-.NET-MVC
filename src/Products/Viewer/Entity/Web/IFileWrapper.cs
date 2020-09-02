using GroupDocs.Viewer.MVC.Products.Common.Config;
using GroupDocs.Viewer.MVC.Products.Common.Entity.Web;
using System.Collections.Generic;
using System.IO;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Entity.Web
{
    interface IFileWrapper
    {
        string GetId(string guid);

        string GetFileFolderName(string guid);

        string GetFilePath(string guid);

        Stream GetFileStream(string guid);

        List<FileDescriptionEntity> GetFilesList();

        string GetFileName(string guid);

        string SetId();

        string SetFileName();
    }
}
