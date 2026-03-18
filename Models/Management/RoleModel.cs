namespace UTTN.Dashboard.Models.Management
{
    public class RoleModel
    {
        public int management_role_ID { get; set; }
        public string management_role_Name { get; set; } = string.Empty;
        public string? management_role_Description { get; set; }
        public bool management_role_status { get; set; }
        public DateTime management_role_createdDate { get; set; }
    }
}