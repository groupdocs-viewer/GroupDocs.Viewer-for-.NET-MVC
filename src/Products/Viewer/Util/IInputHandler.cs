using GroupDocs.Viewer.MVC.Products.Common.Entity.Web;
using System.Collections.Generic;
using System.IO;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Util
{
    interface IInputHandler
    {
        Stream GetFile(string guid);

        string GetFileName(string guid);

        string StoreFile(Stream inputStream, string fileName, bool rewrite);

        List<FileDescriptionEntity> GetFilesList();
    }
}
