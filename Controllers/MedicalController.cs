using Microsoft.AspNetCore.Mvc;
using UTTN.Dashboard.Services.Interfaces;

namespace UTTN.Dashboard.Controllers
{
    public class MedicalController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public MedicalController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index(int? year, int? cuatrimestre)
        {
            try
            {
                var model = await _dashboardService.GetMedicalDataAsync(year, cuatrimestre);
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new ViewModels.Dashboard.MedicalViewModel());
            }
        }
    }
}