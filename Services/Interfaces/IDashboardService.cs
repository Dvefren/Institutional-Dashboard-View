using UTTN.Dashboard.ViewModels.Dashboard;

namespace UTTN.Dashboard.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<RectorateViewModel> GetRectorateDataAsync(int? year = null, int? cuatrimestre = null);
        Task<AdmissionsViewModel> GetAdmissionsDataAsync(int? year = null, int? cuatrimestre = null);
        Task<TramitesViewModel> GetTramitesDataAsync(int? year = null, int? cuatrimestre = null);
        Task<AspirantesViewModel> GetAspirantesDataAsync(int? year = null, int? cuatrimestre = null);
        Task<MedicalViewModel> GetMedicalDataAsync(int? year = null, int? cuatrimestre = null);
        Task<VinculacionViewModel> GetVinculacionDataAsync(int? year = null, int? cuatrimestre = null);
        Task<AcademicQualityViewModel> GetAcademicQualityDataAsync(int? year = null, int? cuatrimestre = null);
    }
}