using GroupDocs.Viewer.MVC.Products.Common.Config;
using GroupDocs.Viewer.MVC.Products.Common.Util.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Config
{
    /// <summary>
    /// ViewerConfiguration
    /// </summary>
    public class ViewerConfiguration
    {
        public string FilesDirectory = "DocumentSamples/Viewer";
        public string FontsDirectory = "";
        public string DefaultDocument = "";
        public int PreloadPageCount = 0;
        public bool isZoom = true;
        public bool isSearch = true;
        public bool isThumbnails = true;
        public bool isRotate = true;
        public bool isHtmlMode = true;
        public bool Cache = true;       

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
        }

        private static bool IsFullPath(string path)
        {
            return !String.IsNullOrWhiteSpace(path)
                && path.IndexOfAny(System.IO.Path.GetInvalidPathChars().ToArray()) == -1
                && Path.IsPathRooted(path)
                && !Path.GetPathRoot(path).Equals(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        }        
    }
}