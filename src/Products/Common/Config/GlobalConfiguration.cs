using GroupDocs.Viewer.MVC.Products.Viewer.Config;

namespace GroupDocs.Viewer.MVC.Products.Common.Config
{
    /// <summary>
    /// Global configuration.
    /// </summary>
    public class GlobalConfiguration
    {
        public ServerConfiguration Server;
        public ApplicationConfiguration Application;
        public CommonConfiguration Common;
        public ViewerConfiguration Viewer;

        /// <summary>
        /// Get all configurations.
        /// </summary>
        public GlobalConfiguration()
        {
            this.Server = new ServerConfiguration();
            this.Application = new ApplicationConfiguration();
            this.Viewer = new ViewerConfiguration();
            this.Common = new CommonConfiguration();
        }
    }
}