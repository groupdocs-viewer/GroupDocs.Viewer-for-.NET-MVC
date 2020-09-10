using GroupDocs.Viewer.MVC.Products.Viewer.Cache;
using GroupDocs.Viewer.Options;
using GroupDocs.Viewer.Results;
using System;
using System.Collections.Generic;
using System.IO;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Util
{
    class HtmlViewer : IDisposable, ICustomViewer
    {
        private readonly string fileName;
        private readonly IViewerCache cache;
        private readonly GroupDocs.Viewer.Viewer viewer;
        private readonly HtmlViewOptions viewOptions;
        private readonly ViewInfoOptions viewInfoOptions;
        private static readonly Common.Config.GlobalConfiguration globalConfiguration = new Common.Config.GlobalConfiguration();

        public HtmlViewer(GroupDocs.Viewer.Common.Func<Stream> getFileStream, string fileName, IViewerCache cache, GroupDocs.Viewer.Common.Func<LoadOptions> getLoadOptions, int pageNumber = -1, int newAngle = 0)
        {
            this.cache = cache;
            this.fileName = fileName;
            this.viewer = new GroupDocs.Viewer.Viewer(getFileStream, getLoadOptions);
            this.viewOptions = this.CreateHtmlViewOptions(pageNumber, newAngle);
            this.viewInfoOptions = ViewInfoOptions.FromHtmlViewOptions(this.viewOptions);
        }

        public GroupDocs.Viewer.Viewer GetViewer()
        {
            return this.viewer;
        }

        private HtmlViewOptions CreateHtmlViewOptions(int passedPageNumber = -1, int newAngle = 0)
        {
            HtmlViewOptions htmlViewOptions = HtmlViewOptions.ForExternalResources(
                pageNumber =>
            {
                string resourceFileName = $"p{pageNumber}.html";
                string cacheFilePath = this.cache.GetCacheFilePath(resourceFileName);

                return File.Create(cacheFilePath);
            },
                (pageNumber, resource) =>
                {
                    string resourceFileName = $"p{pageNumber}_{resource.FileName}";
                    string cacheFilePath = this.cache.GetCacheFilePath(resourceFileName);

                    return File.Create(cacheFilePath);
                },
                (pageNumber, resource) =>
                {
                    var urlPrefix = "/viewer/resources/" + this.fileName.Replace(".", "_");
                    return $"{urlPrefix}/p{pageNumber}_{resource.FileName}";
                });

            htmlViewOptions.SpreadsheetOptions.TextOverflowMode = TextOverflowMode.HideText;
            htmlViewOptions.SpreadsheetOptions.SkipEmptyColumns = true;
            htmlViewOptions.SpreadsheetOptions.SkipEmptyRows = true;
            SetWatermarkOptions(htmlViewOptions);

            if (passedPageNumber >= 0 && newAngle != 0)
            {
                Rotation rotationAngle = GetRotationByAngle(newAngle);
                htmlViewOptions.RotatePage(passedPageNumber, rotationAngle);
            }

            return htmlViewOptions;
        }

        /// <summary>
        /// Gets enumeration member by rotation angle value.
        /// </summary>
        /// <param name="newAngle">New rotation angle value.</param>
        /// <returns>Rotation enumeration member.</returns>
        private static Rotation GetRotationByAngle(int newAngle)
        {
            switch (newAngle)
            {
                case 90:
                    return Rotation.On90Degree;
                case 180:
                    return Rotation.On180Degree;
                case 270:
                    return Rotation.On270Degree;
                default:
                    return Rotation.On90Degree;
            }
        }

        public Results.FileInfo GetFileInfo()
        {
            string cacheKey = "file_info.dat";

            Results.FileInfo viewInfo = this.cache.GetValue(cacheKey, () => this.ReadFileInfo());

            return viewInfo;
        }

        public System.IO.FileInfo GetPageFile(int pageNumber)
        {
            this.GenerateFileCache();

            string pageKey = $"p{pageNumber}.html";
            string cacheFilePath = this.cache.GetCacheFilePath(pageKey);

            return new System.IO.FileInfo(cacheFilePath);
        }

        public void GenerateFileCache()
        {
            ViewInfo viewInfo = this.GetViewInfo();

            using (new CrossProcessLock(this.fileName))
            {
                int[] missingPages = this.GetPagesMissingFromCache(viewInfo.Pages);

                if (missingPages.Length > 0)
                {
                    this.viewer.View(this.viewOptions, missingPages);
                }
            }
        }

        public void Dispose()
        {
            this.viewer?.Dispose();
        }

        /// <summary>
        /// Adds watermark on document if its specified in configuration file.
        /// </summary>
        /// <param name="options">View options.</param>
        private static void SetWatermarkOptions(ViewOptions options)
        {
            Watermark watermark = null;

            if (!string.IsNullOrEmpty(globalConfiguration.Viewer.GetWatermarkText()))
            {
                // Set watermark properties
                watermark = new Watermark(globalConfiguration.Viewer.GetWatermarkText())
                {
                    Color = System.Drawing.Color.Blue,
                    Position = Position.Diagonal,
                };
            }

            if (watermark != null)
            {
                options.Watermark = watermark;
            }
        }

        private Results.FileInfo ReadFileInfo()
        {
            using (new CrossProcessLock(this.fileName))
            {
                Results.FileInfo fileInfo = this.viewer.GetFileInfo();
                return fileInfo;
            }
        }

        private ViewInfo GetViewInfo()
        {
            string cacheKey = "view_info.dat";

            if (!this.cache.Contains(cacheKey))
            {
                using (new CrossProcessLock(this.fileName))
                {
                    if (!this.cache.Contains(cacheKey))
                    {
                        return this.cache.GetValue(cacheKey, () => this.ReadViewInfo());
                    }
                }
            }

            return this.cache.GetValue<ViewInfo>(cacheKey);
        }

        private ViewInfo ReadViewInfo()
        {
            ViewInfo viewInfo = this.viewer.GetViewInfo(this.viewInfoOptions);
            return viewInfo;
        }

        private int[] GetPagesMissingFromCache(IList<Page> pages)
        {
            List<int> missingPages = new List<int>();
            foreach (Page page in pages)
            {
                string pageKey = $"p{page.Number}.html";
                if (!this.cache.Contains(pageKey))
                {
                    missingPages.Add(page.Number);
                }
            }

            return missingPages.ToArray();
        }
    }
}