using GroupDocs.Viewer.MVC.Products.Common.Config;
using GroupDocs.Viewer.MVC.Products.Common.Entity.Web;
using GroupDocs.Viewer.MVC.Products.Common.Resources;
using GroupDocs.Viewer.MVC.Products.Common.Util.Comparator;
using System;
using System.Collections.Generic;
using System.IO;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Util
{ 
    public class LocalInputHandler : IInputHandler
    {
        private readonly GlobalConfiguration globalConfiguration;

        public LocalInputHandler(GlobalConfiguration globalConfiguration)
        {
            this.globalConfiguration = globalConfiguration;
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

        public Stream GetFile(string guid)
        {
            if (File.Exists(guid))
            {
                return File.OpenRead(guid);
            }

            else return new MemoryStream();
        }

        public string StoreFile(Stream inputStream, string fileName, bool rewrite)
        {
            string fileSavePath;

            if (rewrite)
            {
                // Get the complete file path.
                fileSavePath = Path.Combine(globalConfiguration.Viewer.GetFilesDirectory(), fileName);
            }
            else
            {
                fileSavePath = Resources.GetFreeFileName(globalConfiguration.Viewer.GetFilesDirectory(), fileName);
            }

            using (var fileStream = File.Create(fileSavePath))
            {
                inputStream.Seek(0, SeekOrigin.Begin);
                inputStream.CopyTo(fileStream);
            }

            return fileSavePath;
        }
    }
}