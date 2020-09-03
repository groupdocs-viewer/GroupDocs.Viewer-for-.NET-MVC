using GroupDocs.Viewer.MVC.Products.Common.Config;
using GroupDocs.Viewer.MVC.Products.Common.Entity.Web;
using GroupDocs.Viewer.MVC.Products.Common.Util.Comparator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Entity.Web
{
    public class UrlFileWrapper : IFileWrapper
    {
        private GlobalConfiguration globalConfiguration;

        public UrlFileWrapper(GlobalConfiguration globalConfiguration)
        {
            this.globalConfiguration = globalConfiguration;
        }

        public string GetFileFolderName(string guid)
        {
            Uri uri = new Uri(guid);

            string filename = System.IO.Path.GetFileName(uri.LocalPath);

            return filename.Replace(".", "_");
        }

        public string GetFilePath(string guid)
        {
            return this.GetFileFolderName(guid);
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
            using (var client = new WebClient())
            {
                var content = client.DownloadData(guid);
                return new MemoryStream(content);
            }
        }

        public string GetId(string guid)
        {
            return guid;
        }

        public string GetFileName(string guid)
        {
            return Path.GetFileName(guid);
        }

        public string SetId()
        {
            throw new NotImplementedException();
        }

        public string SetFileName()
        {
            throw new NotImplementedException();
        }
    }
}