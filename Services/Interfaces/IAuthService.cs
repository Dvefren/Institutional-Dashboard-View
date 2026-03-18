using UTTN.Dashboard.ViewModels.Auth;

namespace UTTN.Dashboard.Services.Interfaces
{
    public interface IAuthService
    {
        Task<UserSessionViewModel?> AuthenticateAsync(string username, string password);
        Task<bool> UpdateLastLoginAsync(int userId);
    }
}