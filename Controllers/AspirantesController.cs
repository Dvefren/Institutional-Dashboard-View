using Microsoft.AspNetCore.Mvc;
using UTTN.Dashboard.Services.Interfaces;

namespace UTTN.Dashboard.Controllers
{
    public class AspirantesController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public AspirantesController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index(int? year, int? cuatrimestre)
        {
            try
            {
                var model = await _dashboardService.GetAspirantesDataAsync(year, cuatrimestre);
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new ViewModels.Dashboard.AspirantesViewModel());
            }
        }
    }
}