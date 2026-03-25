namespace UTTN.Dashboard.ViewModels.Dashboard
{
    public class VinculacionViewModel
    {
        // Filters
        public int SelectedYear { get; set; }
        public int SelectedCuatrimestre { get; set; }
        public List<int> AvailableYears { get; set; } = new();

        // KPIs
        public int TotalPrograms { get; set; }
        public int TotalOrganizations { get; set; }
        public int TotalAssignments { get; set; }
        public int TotalDocuments { get; set; }
        public int Completados { get; set; }
        public int EnProceso { get; set; }
        public int Asignados { get; set; }
        public int Cancelados { get; set; }
        public decimal PromedioHoras { get; set; }
        public decimal PromedioEvaluacion { get; set; }

        // By Status
        public List<StatusStatItem> ByStatus { get; set; } = new();

        // By Program Type
        public List<StatusStatItem> ByProgramType { get; set; } = new();

        // By Organization
        public List<StatusStatItem> ByOrganization { get; set; } = new();

        // Documents by status
        public List<StatusStatItem> DocsByStatus { get; set; } = new();

        // Programs list
        public List<ProgramDetailItem> Programs { get; set; } = new();

        // Organizations list
        public List<OrganizationDetailItem> Organizations { get; set; } = new();

        // Recent assignments
        public List<AssignmentDetailItem> RecentAssignments { get; set; } = new();

        // Document Detailed Item

        public List<DocumentDetailItem> RecentDocuments { get; set; } = new();
    }

    public class ProgramDetailItem
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public int Year { get; set; }
        public int RequiredHours { get; set; }
        public int TotalStudents { get; set; }
        public bool IsActive { get; set; }
    }

    public class OrganizationDetailItem
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
    }

    public class AssignmentDetailItem
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Matricula { get; set; } = string.Empty;
        public string ProgramName { get; set; } = string.Empty;
        public string ProgramType { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public decimal ApprovedHours { get; set; }
        public decimal? EvaluationScore { get; set; }
        public DateTime? StartDate { get; set; }
    }
    public class DocumentDetailItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string ProgramName { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public string ReviewComments { get; set; } = string.Empty;
    }
}