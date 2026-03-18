using UTTN.Dashboard.ViewModels.Dashboard;

namespace UTTN.Dashboard.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<RectorateViewModel> GetRectorateDataAsync();
        Task<AdmissionsViewModel> GetAdmissionsDataAsync();
        Task<TramitesViewModel> GetTramitesDataAsync();
    }
}