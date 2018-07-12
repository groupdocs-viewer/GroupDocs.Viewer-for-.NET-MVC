using System.Collections.Specialized;
using System.Configuration;

namespace GroupDocs.Viewer.MVC.Products.Common.Config
{
    /// <summary>
    /// Application configuration
    /// </summary>
    public class ApplicationConfiguration : ConfigurationSection
    {
        public string LicensePath { get; set; }
        private NameValueCollection applicationConfiguration = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("applicationConfiguration");
       
        /// <summary>
        /// Get license path from the application configuration section of the web.config
        /// </summary>
        public ApplicationConfiguration()
        {
            LicensePath = applicationConfiguration["licensePath"];
        }
    }
}