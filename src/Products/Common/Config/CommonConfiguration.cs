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
        public bool isPageSelector = true;
        public bool isDownload = true;
        public bool isUpload = true;
        public bool isPrint = true;
        public bool isBrowse = true;
        public bool isRewrite = true;
        private NameValueCollection commonConfiguration = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("commonConfiguration");

        /// <summary>
        /// Constructor
        /// </summary>
        public CommonConfiguration()
        {
            YamlParser parser = new YamlParser();
            dynamic configuration = parser.GetConfiguration("common");
            ConfigurationValuesGetter valuesGetter = new ConfigurationValuesGetter(configuration);
            isPageSelector = valuesGetter.GetBooleanPropertyValue("pageSelector", isPageSelector);
            isDownload = valuesGetter.GetBooleanPropertyValue("download", isDownload);
            isUpload = valuesGetter.GetBooleanPropertyValue("upload", isUpload);
            isPrint = valuesGetter.GetBooleanPropertyValue("print", isPrint);
            isBrowse = valuesGetter.GetBooleanPropertyValue("browse", isBrowse);
            isRewrite = valuesGetter.GetBooleanPropertyValue("rewrite", isRewrite);
        }
    }
}