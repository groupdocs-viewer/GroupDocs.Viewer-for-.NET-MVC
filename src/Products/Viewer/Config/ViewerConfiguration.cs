using GroupDocs.Viewer.MVC.Products.Common.Config;
using GroupDocs.Viewer.MVC.Products.Common.Util.Parser;
using System;
using System.IO;
using System.Linq;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Config
{
    /// <summary>
    /// ViewerConfiguration
    /// </summary>
    public class ViewerConfiguration
    {
        private string FilesDirectory = "DocumentSamples/Viewer";
        private string FontsDirectory = "";
        private string DefaultDocument = "";
        private string WatermarkText = "";
        private int PreloadPageCount = 0;
        private bool isZoom = true;
        private bool isSearch = true;
        private bool isThumbnails = true;
        private bool isRotate = true;
        private bool isHtmlMode = true;
        private bool Cache = true;
        private bool SaveRotateState = true;

        /// <summary>
        /// Constructor
        /// </summary>
        public ViewerConfiguration()
        {
            YamlParser parser = new YamlParser();
            dynamic configuration = parser.GetConfiguration("viewer");
            ConfigurationValuesGetter valuesGetter = new ConfigurationValuesGetter(configuration);

            // get Viewer configuration section from the web.config
            FilesDirectory = valuesGetter.GetStringPropertyValue("filesDirectory", FilesDirectory);
            if (!IsFullPath(FilesDirectory))
            {
                FilesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FilesDirectory);
                if (!Directory.Exists(FilesDirectory))
                {
                    Directory.CreateDirectory(FilesDirectory);
                }
            }
            FontsDirectory = valuesGetter.GetStringPropertyValue("fontsDirectory", FontsDirectory);
            DefaultDocument = valuesGetter.GetStringPropertyValue("defaultDocument", DefaultDocument);
            PreloadPageCount = valuesGetter.GetIntegerPropertyValue("preloadPageCount", PreloadPageCount);
            isZoom = valuesGetter.GetBooleanPropertyValue("zoom", isZoom);
            isSearch = valuesGetter.GetBooleanPropertyValue("search", isSearch);
            isThumbnails = valuesGetter.GetBooleanPropertyValue("thumbnails", isThumbnails);
            isRotate = valuesGetter.GetBooleanPropertyValue("rotate", isRotate);
            isHtmlMode = valuesGetter.GetBooleanPropertyValue("htmlMode", isHtmlMode);
            Cache = valuesGetter.GetBooleanPropertyValue("cache", Cache);
            SaveRotateState = valuesGetter.GetBooleanPropertyValue("saveRotateState", SaveRotateState);
            WatermarkText = valuesGetter.GetStringPropertyValue("watermarkText", WatermarkText);
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
            this.FilesDirectory = filesDirectory;
        }

        public string GetFilesDirectory()
        {
            return FilesDirectory;
        }

        public void SetFontsDirectory(string fontsDirectory)
        {
            this.FontsDirectory = fontsDirectory;
        }

        public string GetFontsDirectory()
        {
            return FontsDirectory;
        }

        public void SetDefaultDocument(string defaultDocument)
        {
            this.DefaultDocument = defaultDocument;
        }

        public string GetDefaultDocument()
        {
            return DefaultDocument;
        }

        public void SetPreloadPageCount(int preloadPageCount)
        {
            this.PreloadPageCount = preloadPageCount;
        }

        public int GetPreloadPageCount()
        {
            return PreloadPageCount;
        }

        public void SetIsZoom(bool isZoom)
        {
            this.isZoom = isZoom;
        }

        public bool GetIsZoom()
        {
            return isZoom;
        }

        public void SetIsSearch(bool isSearch)
        {
            this.isSearch = isSearch;
        }

        public bool GetIsSearch()
        {
            return isSearch;
        }

        public void SetIsThumbnails(bool isThumbnails)
        {
            this.isThumbnails = isThumbnails;
        }

        public bool GetIsThumbnails()
        {
            return isThumbnails;
        }

        public void SetIsRotate(bool isRotate)
        {
            this.isRotate = isRotate;
        }

        public bool GetIsRotate()
        {
            return isRotate;
        }

        public void SetIsHtmlMode(bool isHtmlMode)
        {
            this.isHtmlMode = isHtmlMode;
        }

        public bool GetIsHtmlMode()
        {
            return isHtmlMode;
        }

        public void SetCache(bool Cache)
        {
            this.Cache = Cache;
        }

        public bool GetCache()
        {
            return Cache;
        }

        public void SetSaveRotateState(bool saveRotateState)
        {
            this.SaveRotateState = saveRotateState;
        }

        public bool GetSaveRotateState()
        {
            return SaveRotateState;
        }

        public void SetWatermarkText(string watermarkText)
        {
            this.WatermarkText = watermarkText;
        }

        public string GetWatermarkText()
        {
            return WatermarkText;
        }
    }
}