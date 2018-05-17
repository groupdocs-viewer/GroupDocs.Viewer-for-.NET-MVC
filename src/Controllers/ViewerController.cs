using Viewer.Config;
using System.Web.Mvc;

namespace Viewer.Controllers
{
    public class ViewerController : Controller
    {
        public ActionResult Index()
        {
          return View("Viewer");
        }
    }
}
