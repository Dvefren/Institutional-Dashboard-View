namespace UTTN.Dashboard.ViewModels.Dashboard
{
    public class AcademicQualityViewModel
    {
        public int SelectedYear { get; set; }
        public int SelectedCuatrimestre { get; set; }
        public List<int> AvailableYears { get; set; } = new();

        // KPIs
        public int TotalSubjects { get; set; }
        public int TotalTeacherAssignments { get; set; }
        public int TotalGradeRecords { get; set; }
        public int TotalFinalGrades { get; set; }
        public int Aprobados { get; set; }
        public int Reprobados { get; set; }
        public decimal TasaAprobacion { get; set; }
        public decimal PromedioGeneral { get; set; }
        public int TotalOpportunities { get; set; }
        public int ActivePeriods { get; set; }

        public List<SubjectStatItem> SubjectsByCareer { get; set; } = new();
        public List<StatusStatItem> ByPassStatus { get; set; } = new();
        public List<GradeBySubjectItem> GradesBySubject { get; set; } = new();
        public List<GradeByTeacherItem> GradesByTeacher { get; set; } = new();
        public List<PromedioRangeItem> GradeDistribution { get; set; } = new();
        public List<PeriodItem> Periods { get; set; } = new();
        public List<SubjectCatalogItem> SubjectCatalog { get; set; } = new();
        public List<TeacherAssignmentItem> TeacherAssignments { get; set; } = new();
        public List<FinalGradeDetailItem> RecentFinalGrades { get; set; } = new();
        public List<OpportunityDetailItem> RecentOpportunities { get; set; } = new();
    }

    public class SubjectStatItem
    {
        public string CareerName { get; set; } = string.Empty;
        public int SubjectCount { get; set; }
        public int TotalHours { get; set; }
    }

    public class GradeBySubjectItem
    {
        public string SubjectName { get; set; } = string.Empty;
        public string CareerName { get; set; } = string.Empty;
        public decimal Average { get; set; }
        public int TotalStudents { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public decimal PassRate { get; set; }
    }

    public class GradeByTeacherItem
    {
        public string TeacherName { get; set; } = string.Empty;
        public int TotalSubjects { get; set; }
        public int TotalStudents { get; set; }
        public decimal AverageGrade { get; set; }
        public decimal PassRate { get; set; }
    }

    public class PeriodItem
    {
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public int Assignments { get; set; }
    }

    public class SubjectCatalogItem
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CareerName { get; set; } = string.Empty;
        public int Semester { get; set; }
        public int WeeklyHours { get; set; }
    }

    public class TeacherAssignmentItem
    {
        public string TeacherName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string GroupCode { get; set; } = string.Empty;
        public string PeriodName { get; set; } = string.Empty;
        public int CriteriaCount { get; set; }
        public int StudentCount { get; set; }
    }

    public class FinalGradeDetailItem
    {
        public string StudentName { get; set; } = string.Empty;
        public string Matricula { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public decimal FinalValue { get; set; }
        public string PassStatus { get; set; } = string.Empty;
        public string PeriodName { get; set; } = string.Empty;
    }

    public class OpportunityDetailItem
    {
        public string StudentName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal OriginalGrade { get; set; }
        public decimal OpportunityGrade { get; set; }
        public decimal? MaxAllowed { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}