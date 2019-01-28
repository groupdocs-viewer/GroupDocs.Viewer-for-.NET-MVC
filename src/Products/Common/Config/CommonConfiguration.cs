using GroupDocs.Viewer.MVC.Products.Common.Util.Parser;
using System;
using System.Collections.Specialized;
using System.Configuration;

namespace GroupDocs.Viewer.MVC.Products.Common.Config
{
    /// <summary>
    /// CommonConfiguration
    /// </summary>
    public class CommonConfiguration : ConfigurationSection
    {
        public bool isPageSelector { get; set; }    
        public bool isDownload { get; set; }
        public bool isUpload { get; set; }
        public bool isPrint { get; set; }
        public bool isBrowse { get; set; }
        public bool isRewrite { get; set; }
        public bool enableRightClick { get; set; }
        private NameValueCollection commonConfiguration = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("commonConfiguration");

        /// <summary>
        /// Constructor
        /// </summary>
        public CommonConfiguration()
        {
            YamlParser parser = new YamlParser();
            dynamic configuration = parser.GetConfiguration("common");
            ConfigurationValuesGetter valuesGetter = new ConfigurationValuesGetter(configuration);
            isPageSelector = valuesGetter.GetBooleanPropertyValue("pageSelector", Convert.ToBoolean(commonConfiguration["isPageSelector"]));
            isDownload = valuesGetter.GetBooleanPropertyValue("download", Convert.ToBoolean(commonConfiguration["isDownload"]));
            isUpload = valuesGetter.GetBooleanPropertyValue("upload", Convert.ToBoolean(commonConfiguration["isUpload"]));
            isPrint = valuesGetter.GetBooleanPropertyValue("print", Convert.ToBoolean(commonConfiguration["isPrint"]));
            isBrowse = valuesGetter.GetBooleanPropertyValue("browse", Convert.ToBoolean(commonConfiguration["isBrowse"]));
            isRewrite = valuesGetter.GetBooleanPropertyValue("rewrite", Convert.ToBoolean(commonConfiguration["isRewrite"]));
            enableRightClick = valuesGetter.GetBooleanPropertyValue("enableRightClick", Convert.ToBoolean(commonConfiguration["enableRightClick"]));
        }
    }
}