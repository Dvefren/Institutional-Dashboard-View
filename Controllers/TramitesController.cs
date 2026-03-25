using Microsoft.AspNetCore.Mvc;
using UTTN.Dashboard.Services.Interfaces;

namespace UTTN.Dashboard.Controllers
{
    public class TramitesController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public TramitesController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index(int? year, int? cuatrimestre)
        {
            try
            {
                var model = await _dashboardService.GetTramitesDataAsync(year, cuatrimestre);
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new ViewModels.Dashboard.TramitesViewModel());
            }
        }
    }
}