using Microsoft.AspNetCore.Mvc;
using UTTN.Dashboard.Services.Interfaces;

namespace UTTN.Dashboard.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public HomeController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var model = await _dashboardService.GetRectorateDataAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new ViewModels.Dashboard.RectorateViewModel());
            }
        }
    }
}