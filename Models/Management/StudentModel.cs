namespace UTTN.Dashboard.Models.Management
{
    public class StudentModel
    {
        public int management_student_ID { get; set; }
        public int management_student_PersonID { get; set; }
        public int? management_student_CareerID { get; set; }
        public int? management_student_GroupID { get; set; }
        public string? management_student_Matricula { get; set; }
        public string? management_student_EnrollmentFolio { get; set; }
        public bool management_student_IsFolio { get; set; }
        public string management_student_StatusCode { get; set; } = string.Empty;
        public bool management_student_status { get; set; }
        public DateTime management_student_createdDate { get; set; }
    }
}