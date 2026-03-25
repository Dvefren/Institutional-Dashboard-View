namespace UTTN.Dashboard.ViewModels.Dashboard
{
    public class RectorateViewModel
    {
        // Filters
        public int SelectedYear { get; set; }
        public int SelectedCuatrimestre { get; set; } // 0=All, 1=Ene-Abr, 2=May-Ago, 3=Sep-Dic
        public List<int> AvailableYears { get; set; } = new();

        // KPI Cards
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int Preinscritos { get; set; }
        public int Inscritos { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalCareers { get; set; }
        public int TotalGroups { get; set; }
        public int TotalTramitesPendientes { get; set; }
        public int TotalVisitasMedicas { get; set; }

        // Students by career
        public List<CareerStatItem> StudentsByCareers { get; set; } = new();

        // Students by status
        public List<StatusStatItem> StudentsByStatus { get; set; } = new();

        // Gender distribution
        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }
        public int OtherGenderCount { get; set; }

        // Preinscription by career
        public List<CareerStatItem> PreinscripcionesByCareer { get; set; } = new();

        // Preinscription by status
        public List<StatusStatItem> PreinscripcionesByStatus { get; set; } = new();

        // Groups with student counts
        public List<GroupStatItem> GroupsOverview { get; set; } = new();

        // Monthly trends (preinscriptions)
        public List<MonthlyStatItem> MonthlyPreinscripciones { get; set; } = new();
        public List<CareerOverviewItem> CareersOverview { get; set; } = new();
        public List<CareerChangeItem> CareerChanges { get; set; } = new();
        public List<GroupChangeItem> GroupChanges { get; set; } = new();
    }

    public class CareerStatItem
    {
        public string CareerName { get; set; } = string.Empty;
        public string CareerCode { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class StatusStatItem
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class GroupStatItem
    {
        public string GroupCode { get; set; } = string.Empty;
        public string CareerName { get; set; } = string.Empty;
        public string Shift { get; set; } = string.Empty;
        public int StudentCount { get; set; }
    }

    public class MonthlyStatItem
    {
        public string Month { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Count { get; set; }
    }
    public class CareerOverviewItem
    {
        public string CareerName { get; set; } = string.Empty;
        public string CareerCode { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int Inscritos { get; set; }
        public int Preinscritos { get; set; }
        public int Bajas { get; set; }
        public int Groups { get; set; }
        public decimal Percentage { get; set; }
    }

    public class CareerChangeItem
    {
        public string StudentName { get; set; } = string.Empty;
        public string Matricula { get; set; } = string.Empty;
        public string CareerName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class GroupChangeItem
    {
        public string StudentName { get; set; } = string.Empty;
        public string Matricula { get; set; } = string.Empty;
        public string GroupCode { get; set; } = string.Empty;
        public string CareerName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}