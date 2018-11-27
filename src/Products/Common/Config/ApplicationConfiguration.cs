using GroupDocs.Viewer.MVC.Products.Common.Util.Parser;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;

namespace GroupDocs.Viewer.MVC.Products.Common.Config
{
    /// <summary>
    /// Application configuration
    /// </summary>
    public class ApplicationConfiguration
    {
        public string LicensePath = "Licenses";

        /// <summary>
        /// Get license path from the application configuration section of the web.config
        /// </summary>
        public ApplicationConfiguration()
        {
            YamlParser parser = new YamlParser();
            dynamic configuration = parser.GetConfiguration("application");
            ConfigurationValuesGetter valuesGetter = new ConfigurationValuesGetter(configuration);
            LicensePath = valuesGetter.GetStringPropertyValue("licensePath", LicensePath);            
            if (!IsFullPath(LicensePath))
            {
                LicensePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LicensePath);
                if (!Directory.Exists(Path.GetDirectoryName(LicensePath)))
                {                    
                    Directory.CreateDirectory(Path.GetDirectoryName(LicensePath));
                }
            }
            if (!File.Exists(LicensePath))
            {
                LicensePath = "";
            }
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