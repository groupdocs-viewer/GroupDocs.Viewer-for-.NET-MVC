using System;
using System.Collections.Specialized;
using System.Web;

namespace Viewer.Config
{
    public class ViewerConfig
    {
        private Application application;

        private Resources resources;

        private Server server;

        private NameValueCollection viewerConfig = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("viewerConfig");

        public ViewerConfig()
        {
            application = new Application();
            resources = new Resources();
            server = new Server();
        }

        public Application getApplication()
        {
            return application;
        }

        public Resources getResources()
        {
            return resources;
        }

        public Server getServer()
        {
            return server;
        }

        /**
         * Application related configurations
         */
        public class Application
        {
            private string filesDirectory;

            private string licensePath;

            private string fontsDirectory;

            public string getFilesDirectory()
            {
                filesDirectory = new ViewerConfig().viewerConfig["filesDirectory"];
                return filesDirectory;
            }

            public void setFilesDirectory(string filesDirectory)
            {
                this.filesDirectory = filesDirectory;
            }

            public string getLicensePath()
            {
                licensePath = new ViewerConfig().viewerConfig["licensePath"];
                return licensePath;
            }

            public void setLicensePath(string licensePath)
            {
                this.licensePath = licensePath;
            }

            public string getFontsDirectory()
            {
                fontsDirectory = new ViewerConfig().viewerConfig["fontsDirectory"];
                return fontsDirectory;
            }

            public void setFontsDirectory(string fontsDirectory)
            {
                this.fontsDirectory = fontsDirectory;
            }
        }

        /**
         * Resources related configuration
         */
        public class Resources
        {
            private string resourcesUrl;

            private int preloadPageCount;

            private bool zoom;

            private bool pageSelector;

            private bool search;

            private bool thumbnails;

            private bool rotate;

            private bool download;

            private bool upload;

            private bool print;

            private string defaultDocument;

            private bool browse;

            private bool htmlMode;

            private bool rewrite;

            private bool offlineMode;

            public string getResourcesUrl()
            {
                resourcesUrl = new ViewerConfig().viewerConfig["resourcesUrl"];
                return resourcesUrl;
            }

            public void setResourcesUrl(string resourcesUrl)
            {
                this.resourcesUrl = resourcesUrl;
            }

            public int getPreloadPageCount()
            {
                preloadPageCount = Convert.ToInt32(new ViewerConfig().viewerConfig["preloadPageCount"]);
                return preloadPageCount;
            }

            public void setPreloadPageCount(int preloadPageCount)
            {
                this.preloadPageCount = preloadPageCount;
            }

            public bool isZoom()
            {
                zoom = bool.Parse(new ViewerConfig().viewerConfig["zoom"]);
                return zoom;
            }

            public void setZoom(bool zoom)
            {
                this.zoom = zoom;
            }

            public bool isPageSelector()
            {
                pageSelector = bool.Parse(new ViewerConfig().viewerConfig["pageSelector"]);
                return pageSelector;
            }

            public void setPageSelector(bool pageSelector)
            {
                this.pageSelector = pageSelector;
            }

            public bool isSearch()
            {
                search = bool.Parse(new ViewerConfig().viewerConfig["search"]);
                return search;
            }

            public void setSearch(bool search)
            {
                this.search = search;
            }

            public bool isThumbnails()
            {
                thumbnails = bool.Parse(new ViewerConfig().viewerConfig["thumbnails"]);
                return thumbnails;
            }

            public void setThumbnails(bool thumbnails)
            {
                this.thumbnails = thumbnails;
            }

            public bool isRotate()
            {
                rotate = bool.Parse(new ViewerConfig().viewerConfig["rotate"]);
                return rotate;
            }

            public void setRotate(bool rotate)
            {
                this.rotate = rotate;
            }

            public bool isDownload()
            {
                download = bool.Parse(new ViewerConfig().viewerConfig["download"]);
                return download;
            }

            public void setDownload(bool download)
            {
                this.download = download;
            }

            public bool isUpload()
            {
                upload = bool.Parse(new ViewerConfig().viewerConfig["upload"]);
                return upload;
            }

            public void setUpload(bool upload)
            {
                this.upload = upload;
            }

            public bool isPrint()
            {
                print = bool.Parse(new ViewerConfig().viewerConfig["print"]);
                return print;
            }

            public void setPrint(bool print)
            {
                this.print = print;
            }

            public string getDefaultDocument()
            {
                defaultDocument = new ViewerConfig().viewerConfig["defaultDocument"];
                return defaultDocument;
            }

            public void setDefaultDocument(string defaultDocument)
            {
                this.defaultDocument = defaultDocument;
            }

            public bool isBrowse()
            {
                browse = bool.Parse(new ViewerConfig().viewerConfig["browse"]);
                return browse;
            }

            public void setBrowse(bool browse)
            {
                this.browse = browse;
            }

            public bool isHtmlMode()
            {
                htmlMode = bool.Parse(new ViewerConfig().viewerConfig["htmlMode"]);
                return htmlMode;
            }

            public void setHtmlMode(bool htmlMode)
            {
                this.htmlMode = htmlMode;
            }

            public bool isRewrite()
            {
                rewrite = bool.Parse(new ViewerConfig().viewerConfig["rewrite"]);
                return rewrite;
            }

            public void setRewrite(bool rewrite)
            {
                this.rewrite = rewrite;
            }

            public bool isOfflineMode()
            {
                offlineMode = bool.Parse(new ViewerConfig().viewerConfig["offlineMode"]);
                return offlineMode;
            }

            public void setOfflineMode(bool offlineMode) { this.offlineMode = offlineMode; }
        }

        /**
         * Server related configurations
         */
        public class Server
        {
            private int httpPort;
            private string hostAddress;

            public int getHttpPort()
            {
                httpPort = HttpContext.Current.Request.Url.Port;
                return httpPort;
            }

            public void setHttpPort(int httpPort)
            {
                this.httpPort = httpPort;
            }

            public string getHostAddress()
            {
                hostAddress = HttpContext.Current.Request.Url.Host;
                return hostAddress;
            }

            public void setHostAddress(string hostAddress)
            {
                this.hostAddress = hostAddress;
            }
        }

    }
}