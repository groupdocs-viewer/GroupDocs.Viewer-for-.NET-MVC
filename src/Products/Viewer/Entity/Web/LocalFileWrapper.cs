using GroupDocs.Viewer.MVC.Products.Common.Config;
using GroupDocs.Viewer.MVC.Products.Common.Entity.Web;
using GroupDocs.Viewer.MVC.Products.Common.Util.Comparator;
using GroupDocs.Viewer.MVC.Products.Viewer.Cache;
using System;
using System.Collections.Generic;
using System.IO;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Entity.Web
{
    public class LocalFileWrapper : IFileWrapper
    {
        private GlobalConfiguration globalConfiguration;
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
    }
}