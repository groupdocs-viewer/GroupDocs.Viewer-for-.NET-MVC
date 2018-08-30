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
        private NameValueCollection commonConfiguration = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("commonConfiguration");

        /// <summary>
        /// Constructor
        /// </summary>
        public CommonConfiguration()
        {
            // get Common configuration section from the web.config           
            isPageSelector = Convert.ToBoolean(commonConfiguration["isPageSelector"]);
            isDownload = Convert.ToBoolean(commonConfiguration["isDownload"]);
            isUpload = Convert.ToBoolean(commonConfiguration["isUpload"]);
            isPrint = Convert.ToBoolean(commonConfiguration["isPrint"]);
            isBrowse = Convert.ToBoolean(commonConfiguration["isBrowse"]);
            isRewrite = Convert.ToBoolean(commonConfiguration["isRewrite"]);
        }
    }
}