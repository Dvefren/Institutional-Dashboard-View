namespace UTTN.Dashboard.Models.Management
{
    public class TeacherModel
    {
        public int management_teacher_ID { get; set; }
        public int management_teacher_PersonID { get; set; }
        public string? management_teacher_EmployeeNumber { get; set; }
        public string management_teacher_StatusCode { get; set; } = string.Empty;
        public bool management_teacher_status { get; set; }
        public DateTime management_teacher_createdDate { get; set; }
    }
}