using System;
using System.Collections.Specialized;
using System.Configuration;

namespace GroupDocs.Viewer.MVC.Products.Common.Config
{
    /// <summary>
    /// Server configuration
    /// </summary>
    public class ServerConfiguration : ConfigurationSection
    {
        public int HttpPort { get; set; }
        public string HostAddress { get; set; }
        private NameValueCollection serverConfiguration = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("serverConfiguration");

        /// <summary>
        /// Get server configuration section of the web.config
        /// </summary>
        public ServerConfiguration() {
            HttpPort = Convert.ToInt32(serverConfiguration["httpPort"]);
            HostAddress = serverConfiguration["hostAddress"];
        }
    }
}