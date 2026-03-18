namespace UTTN.Dashboard.Models.Management
{
    public class PersonModel
    {
        public int management_person_ID { get; set; }
        public string management_person_FirstName { get; set; } = string.Empty;
        public string management_person_LastNamePaternal { get; set; } = string.Empty;
        public string? management_person_LastNameMaternal { get; set; }
        public DateTime? management_person_BirthDate { get; set; }
        public string? management_person_Gender { get; set; }
        public string? management_person_CURP { get; set; }
        public string? management_person_Email { get; set; }
        public string? management_person_Phone { get; set; }
        public bool management_person_status { get; set; }
        public DateTime management_person_createdDate { get; set; }

        // Computed
        public string FullName => $"{management_person_FirstName} {management_person_LastNamePaternal} {management_person_LastNameMaternal}".Trim();
    }
}