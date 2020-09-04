using GroupDocs.Viewer.MVC.Products.Common.Config;
using GroupDocs.Viewer.MVC.Products.Common.Entity.Web;
using GroupDocs.Viewer.MVC.Products.Common.Resources;
using GroupDocs.Viewer.MVC.Products.Common.Util.Comparator;
using GroupDocs.Viewer.MVC.Products.Viewer.Cache;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Entity.Web
{
    public class LocalFileWrapper : IFileWrapper
    {
        private readonly GlobalConfiguration globalConfiguration;
        private string FileName;

        public LocalFileWrapper(GlobalConfiguration globalConfiguration)
        {
            this.globalConfiguration = globalConfiguration;
        }

        public void CreateCache(ICustomViewer customCache)
        {
            customCache.CreateCache();
        }

        public string GetFileCachePath(string fileName)
        {
            return Path.Combine(this.globalConfiguration.Viewer.GetFilesDirectory(), this.globalConfiguration.Viewer.GetCacheFolderName(), this.GetFileName(fileName).Replace('.', '_'));
        }

        public string GetFileName(string guid)
        {
            return Path.GetFileName(guid);
        }

        public List<FileDescriptionEntity> GetFilesList()
        {
            var filesList = new List<FileDescriptionEntity>();

            if (!string.IsNullOrEmpty(globalConfiguration.Viewer.GetFilesDirectory()))
            {
                var currentPath = globalConfiguration.Viewer.GetFilesDirectory();
                List<string> allFiles = new List<string>(Directory.GetFiles(currentPath));
                allFiles.AddRange(Directory.GetDirectories(currentPath));

                string cacheFolderName = globalConfiguration.Viewer.GetCacheFolderName();

                allFiles.Sort(new FileNameComparator());
                allFiles.Sort(new FileDateComparator());

                foreach (string file in allFiles)
                {
                    System.IO.FileInfo fileInfo = new System.IO.FileInfo(file);

                    // check if current file/folder is hidden
                    if (!(cacheFolderName.Equals(Path.GetFileName(file)) ||
                          Path.GetFileName(file).StartsWith(".") ||
                          fileInfo.Attributes.HasFlag(FileAttributes.Hidden) ||
                          Path.GetFileName(file).Equals(Path.GetFileName(globalConfiguration.Viewer.GetFilesDirectory()))))
                    {
                        FileDescriptionEntity fileDescription = new FileDescriptionEntity
                        {
                            guid = Path.GetFullPath(file),
                            name = Path.GetFileName(file),

                            // set is directory true/false
                            isDirectory = fileInfo.Attributes.HasFlag(FileAttributes.Directory),
                        };

                        // set file size
                        if (!fileDescription.isDirectory)
                        {
                            fileDescription.size = fileInfo.Length;
                        }

                        // add object to array list
                        filesList.Add(fileDescription);
                    }
                }
            }

            return filesList;
        }

        public Stream GetFileStream(string guid)
        {
            return File.OpenRead(guid);
        }

        public void SetFileName(string fileName)
        {
            this.FileName = fileName;
        }

        public UploadedDocumentEntity UploadFile()
        {
            string url = HttpContext.Current.Request.Form["url"];

            // get documents storage path
            string documentStoragePath = globalConfiguration.Viewer.GetFilesDirectory();
            bool rewrite = bool.Parse(HttpContext.Current.Request.Form["rewrite"]);
            string fileSavePath = string.Empty;
            if (string.IsNullOrEmpty(url))
            {
                if (HttpContext.Current.Request.Files.AllKeys != null)
                {
                    // Get the uploaded document from the Files collection
                    var httpPostedFile = HttpContext.Current.Request.Files["file"];
                    if (httpPostedFile != null)
                    {
                        if (rewrite)
                        {
                            // Get the complete file path
                            fileSavePath = Path.Combine(documentStoragePath, httpPostedFile.FileName);
                        }
                        else
                        {
                            fileSavePath = Resources.GetFreeFileName(documentStoragePath, httpPostedFile.FileName);
                        }

                        // Save the uploaded file to "UploadedFiles" folder
                        httpPostedFile.SaveAs(fileSavePath);
                    }
                }
            }
            else
            {
                using (WebClient client = new WebClient())
                {
                    // get file name from the URL
                    Uri uri = new Uri(url);
                    string fileName = Path.GetFileName(uri.LocalPath);
                    if (rewrite)
                    {
                        // Get the complete file path
                        fileSavePath = Path.Combine(documentStoragePath, fileName);
                    }
                    else
                    {
                        fileSavePath = Resources.GetFreeFileName(documentStoragePath, fileName);
                    }

                    // Download the Web resource and save it into the current filesystem folder.
                    client.DownloadFile(url, fileSavePath);
                }
            }

            UploadedDocumentEntity uploadedDocument = new UploadedDocumentEntity
            {
                guid = fileSavePath,
            };

            return uploadedDocument;
        }

        public HttpResponseMessage DownloadFile(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (File.Exists(path))
                {
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    var fileStream = new FileStream(path, FileMode.Open);
                    response.Content = new StreamContent(fileStream);
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                    response.Content.Headers.ContentDisposition.FileName = Path.GetFileName(path);
                    return response;
                }
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
}