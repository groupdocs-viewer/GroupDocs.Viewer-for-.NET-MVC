using GroupDocs.Viewer.MVC.Products.Common.Config;
using GroupDocs.Viewer.MVC.Products.Common.Util.Parser;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Config
{
    /// <summary>
    /// ViewerConfiguration
    /// </summary>
    public class ViewerConfiguration : CommonConfiguration
    {
        [JsonProperty]
        private string filesDirectory = "DocumentSamples/Viewer";

        [JsonProperty]
        private string fontsDirectory = "";

        [JsonProperty]
        private string defaultDocument = "";

        [JsonProperty]
        private string watermarkText = "";

        [JsonProperty]
        private int preloadPageCount;

        [JsonProperty]
        private bool zoom = true;

        [JsonProperty]
        private bool search = true;

        [JsonProperty]
        private bool thumbnails = true;

        [JsonProperty]
        private bool rotate = true;

        [JsonProperty]
        private bool htmlMode = true;

        [JsonProperty]
        private bool cache = true;

        [JsonProperty]
        private bool saveRotateState = true;

        [JsonProperty]
        private bool printAllowed = true;

        [JsonProperty]
        private bool showGridLines = true;

        [JsonProperty]
        private string cacheFolderName = "cache";

        /// <summary>
        /// Constructor
        /// </summary>
        public ViewerConfiguration()
        {
            YamlParser parser = new YamlParser();
            dynamic configuration = parser.GetConfiguration("viewer");
            ConfigurationValuesGetter valuesGetter = new ConfigurationValuesGetter(configuration);

            // get Viewer configuration section from the web.config
            filesDirectory = valuesGetter.GetStringPropertyValue("filesDirectory", filesDirectory);
            if (!IsFullPath(filesDirectory))
            {
                filesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filesDirectory);
                if (!Directory.Exists(filesDirectory))
                {
                    Directory.CreateDirectory(filesDirectory);
                }
            }
            cacheFolderName = valuesGetter.GetStringPropertyValue("cacheFolderName", cacheFolderName);
            if (!IsFullPath(cacheFolderName))
            {
                var cacheDirectory = Path.Combine(filesDirectory, cacheFolderName);
                if (!Directory.Exists(cacheDirectory))
                {
                    Directory.CreateDirectory(cacheDirectory);
                }
            }
            fontsDirectory = valuesGetter.GetStringPropertyValue("fontsDirectory", fontsDirectory);
            defaultDocument = valuesGetter.GetStringPropertyValue("defaultDocument", defaultDocument);
            preloadPageCount = valuesGetter.GetIntegerPropertyValue("preloadPageCount", preloadPageCount);
            zoom = valuesGetter.GetBooleanPropertyValue("zoom", zoom);
            search = valuesGetter.GetBooleanPropertyValue("search", search);
            thumbnails = valuesGetter.GetBooleanPropertyValue("thumbnails", thumbnails);
            rotate = valuesGetter.GetBooleanPropertyValue("rotate", rotate);
            htmlMode = valuesGetter.GetBooleanPropertyValue("htmlMode", htmlMode);
            cache = valuesGetter.GetBooleanPropertyValue("cache", cache);
            saveRotateState = valuesGetter.GetBooleanPropertyValue("saveRotateState", saveRotateState);
            watermarkText = valuesGetter.GetStringPropertyValue("watermarkText", watermarkText);
            printAllowed = valuesGetter.GetBooleanPropertyValue("printAllowed", printAllowed);
            showGridLines = valuesGetter.GetBooleanPropertyValue("showGridLines", showGridLines);
        }

        private static bool IsFullPath(string path)
        {
            return !String.IsNullOrWhiteSpace(path)
                && path.IndexOfAny(System.IO.Path.GetInvalidPathChars().ToArray()) == -1
                && Path.IsPathRooted(path)
                && !Path.GetPathRoot(path).Equals(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        }

        public void SetFilesDirectory(string filesDirectory)
        {
            this.filesDirectory = filesDirectory;
        }

        public string GetFilesDirectory()
        {
            return filesDirectory;
        }

        public void SetCacheFolderName(string cacheFolderName)
        {
            this.cacheFolderName = cacheFolderName;
        }

        public string GetCacheFolderName()
        {
            return cacheFolderName;
        }

        public void SetFontsDirectory(string fontsDirectory)
        {
            this.fontsDirectory = fontsDirectory;
        }

        public string GetFontsDirectory()
        {
            return fontsDirectory;
        }

        public void SetDefaultDocument(string defaultDocument)
        {
            this.defaultDocument = defaultDocument;
        }

        public string GetDefaultDocument()
        {
            return defaultDocument;
        }

        public void SetPreloadPageCount(int preloadPageCount)
        {
            this.preloadPageCount = preloadPageCount;
        }

        public int GetPreloadPageCount()
        {
            return preloadPageCount;
        }

        public void SetIsZoom(bool isZoom)
        {
            this.zoom = isZoom;
        }

        public bool GetIsZoom()
        {
            return zoom;
        }

        public void SetIsSearch(bool isSearch)
        {
            this.search = isSearch;
        }

        public bool GetIsSearch()
        {
            return search;
        }

        public void SetIsThumbnails(bool isThumbnails)
        {
            this.thumbnails = isThumbnails;
        }

        public bool GetIsThumbnails()
        {
            return thumbnails;
        }

        public void SetIsRotate(bool isRotate)
        {
            this.rotate = isRotate;
        }

        public bool GetIsRotate()
        {
            return rotate;
        }

        public void SetIsHtmlMode(bool isHtmlMode)
        {
            this.htmlMode = isHtmlMode;
        }

        public bool GetIsHtmlMode()
        {
            return htmlMode;
        }

        public void SetCache(bool Cache)
        {
            this.cache = Cache;
        }

        public bool GetCache()
        {
            return cache;
        }

        public void SetSaveRotateState(bool saveRotateState)
        {
            this.saveRotateState = saveRotateState;
        }

        public bool GetSaveRotateState()
        {
            return saveRotateState;
        }

        public void SetWatermarkText(string watermarkText)
        {
            this.watermarkText = watermarkText;
        }

        public string GetWatermarkText()
        {
            return watermarkText;
        }

        public void SetPrintAllowed(bool printAllowed)
        {
            this.printAllowed = printAllowed;
        }

        public bool GetPrintAllowed()
        {
            return printAllowed;
        }

        public void SetShowGridLines(bool showGridLines)
        {
            this.showGridLines = showGridLines;
        }

        public bool GetShowGridLines()
        {
            return showGridLines;
        }
    }
}