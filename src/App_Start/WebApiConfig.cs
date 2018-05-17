using System.Web.Http;

namespace Viewer
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // enable CORS
            config.EnableCors();

            // Web API routes
            config.MapHttpAttributeRoutes();
          
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{action}",
                defaults: new { controller = "ViewerApiController" }
            );
        }
    }
}
