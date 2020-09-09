using GroupDocs.Viewer.MVC.Products.Common.Entity.Web;
using GroupDocs.Viewer.MVC.Products.Viewer.Cache;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Entity.Web
{
    interface IInputHandler
    {
        Stream GetFileStream(string guid);

        List<FileDescriptionEntity> GetFilesList();

        string GetFileName(string guid);

        void SetFileName(string fileName);

        UploadedDocumentEntity UploadFile();

        HttpResponseMessage DownloadFile(string path);
    }
}
