namespace UTTN.Dashboard.Models.Management
{
    public class GroupModel
    {
        public int management_group_ID { get; set; }
        public int? management_group_CareerID { get; set; }
        public string management_group_Code { get; set; } = string.Empty;
        public string? management_group_Name { get; set; }
        public string? management_group_Shift { get; set; }
        public bool management_group_status { get; set; }
        public DateTime management_group_createdDate { get; set; }
    }
}