namespace UTTN.Dashboard.Models.Management
{
    public class PermissionModel
    {
        public int management_permission_ID { get; set; }
        public string management_permission_Key { get; set; } = string.Empty;
        public string? management_permission_Description { get; set; }
        public bool management_permission_status { get; set; }
        public DateTime management_permission_createdDate { get; set; }
    }
}