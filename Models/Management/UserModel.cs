namespace UTTN.Dashboard.Models.Management
{
    public class UserModel
    {
        public int management_user_ID { get; set; }
        public int? management_user_PersonID { get; set; }
        public int? management_user_RoleID { get; set; }
        public string management_user_Username { get; set; } = string.Empty;
        public string? management_user_Email { get; set; }
        public string management_user_PasswordHash { get; set; } = string.Empty;
        public bool management_user_IsLocked { get; set; }
        public string? management_user_LockReason { get; set; }
        public DateTime? management_user_LastLoginDate { get; set; }
        public bool management_user_status { get; set; }
        public DateTime management_user_createdDate { get; set; }
    }
}