namespace UTTN.Dashboard.Models.Management
{
    public class CareerModel
    {
        public int management_career_ID { get; set; }
        public string management_career_Code { get; set; } = string.Empty;
        public string management_career_Name { get; set; } = string.Empty;
        public bool management_career_status { get; set; }
        public DateTime management_career_createdDate { get; set; }
    }
}