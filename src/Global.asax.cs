using Viewer.Config;
using Viewer.Manager;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

namespace Viewer
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            ViewerConfig config = new ViewerConfig();
            if (!config.getResources().isOfflineMode())
            {
                WebAssetsManager webAssetsManager = new WebAssetsManager();
                webAssetsManager.Update(config.getResources().getResourcesUrl());
            }
        }
    }
}
