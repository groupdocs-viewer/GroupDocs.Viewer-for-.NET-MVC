using System;
using System.Collections.Specialized;
using System.Configuration;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Config
{
    /// <summary>
    /// ViewerConfiguration
    /// </summary>
    public class ViewerConfiguration : ConfigurationSection
    {
        public string FilesDirectory { get; set; }
        public string FontsDirectory { get; set; }
        public string DefaultDocument { get; set; }
        public int PreloadPageCount { get; set; }
        public bool isZoom { get; set; }
        public bool isPageSelector { get; set; }
        public bool isSearch { get; set; }
        public bool isThumbnails { get; set; }
        public bool isRotate { get; set; }
        public bool isDownload { get; set; }
        public bool isUpload { get; set; }
        public bool isPrint { get; set; }
        public bool isBrowse { get; set; }
        public bool isHtmlMode { get; set; }
        public bool isRewrite { get; set; }
        private NameValueCollection viewerConfiguration = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("viewerConfiguration");

        /// <summary>
        /// Constructor
        /// </summary>
        public ViewerConfiguration()
        {
            // get Viewer configuration section from the web.config
            FilesDirectory = viewerConfiguration["filesDirectory"];
            FontsDirectory = viewerConfiguration["fontsDirectory"];
            DefaultDocument = viewerConfiguration["defaultDocument"];
            PreloadPageCount = Convert.ToInt32(viewerConfiguration["preloadPageCount"]);
            isZoom = Convert.ToBoolean(viewerConfiguration["isZoom"]);
            isPageSelector = Convert.ToBoolean(viewerConfiguration["isPageSelector"]);
            isSearch = Convert.ToBoolean(viewerConfiguration["isSearch"]);
            isThumbnails = Convert.ToBoolean(viewerConfiguration["isThumbnails"]);
            isRotate = Convert.ToBoolean(viewerConfiguration["isRotate"]);
            isDownload = Convert.ToBoolean(viewerConfiguration["isDownload"]);
            isUpload = Convert.ToBoolean(viewerConfiguration["isUpload"]);
            isPrint = Convert.ToBoolean(viewerConfiguration["isPrint"]);
            isBrowse = Convert.ToBoolean(viewerConfiguration["isBrowse"]);
            isHtmlMode = Convert.ToBoolean(viewerConfiguration["isHtmlMode"]);
            isRewrite = Convert.ToBoolean(viewerConfiguration["isRewrite"]);
        }
    }
}