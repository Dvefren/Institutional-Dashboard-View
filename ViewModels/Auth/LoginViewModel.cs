namespace UTTN.Dashboard.ViewModels.Auth
{
    public class UserSessionViewModel
    {
        public int UserID { get; set; }
        public int? PersonID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public int? RoleID { get; set; }
        public List<string> Permissions { get; set; } = new();
    }
}