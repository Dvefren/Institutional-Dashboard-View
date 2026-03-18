using Microsoft.AspNetCore.Mvc;

namespace UTTN.Dashboard.Controllers
{
    public class ManagementController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
