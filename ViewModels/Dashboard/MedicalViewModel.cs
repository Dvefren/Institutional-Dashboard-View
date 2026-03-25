namespace UTTN.Dashboard.ViewModels.Dashboard
{
    public class MedicalViewModel
    {
        // Filters
        public int SelectedYear { get; set; }
        public int SelectedCuatrimestre { get; set; }
        public List<int> AvailableYears { get; set; } = new();

        // KPIs
        public int TotalVisitas { get; set; }
        public int TotalPsicologicas { get; set; }
        public int ConAlergias { get; set; }
        public int ConEnfermedadesCronicas { get; set; }
        public int ConTerapiaPrevia { get; set; }
        public int ConMedicacion { get; set; }
        public decimal PromedioEdad { get; set; }

        // Top diagnosticos
        public List<StatusStatItem> TopDiagnosticos { get; set; } = new();

        // Top motivos psicologicos
        public List<StatusStatItem> TopMotivosPsicologicos { get; set; } = new();

        // Monthly trends
        public List<MonthlyStatItem> MonthlyVisitas { get; set; } = new();
        public List<MonthlyStatItem> MonthlyPsicologicas { get; set; } = new();

        // By age range
        public List<PromedioRangeItem> ByEdad { get; set; } = new();

        // Vital signs averages
        public decimal PromedioTemperatura { get; set; }
        public decimal PromedioIMC { get; set; }

        // Recent visitas
        public List<VisitaDetailItem> RecentVisitas { get; set; } = new();

        // Recent psicologicas
        public List<PsicologicaDetailItem> RecentPsicologicas { get; set; } = new();
    }

    public class VisitaDetailItem
    {
        public int Id { get; set; }
        public string Matricula { get; set; } = string.Empty;
        public DateTime FechaVisita { get; set; }
        public int Edad { get; set; }
        public string Diagnostico { get; set; } = string.Empty;
        public string Temperatura { get; set; } = string.Empty;
        public string PresionArterial { get; set; } = string.Empty;
        public string Saturacion { get; set; } = string.Empty;
        public bool TieneAlergias { get; set; }
        public string Alergias { get; set; } = string.Empty;
    }

    public class PsicologicaDetailItem
    {
        public int Id { get; set; }
        public string Matricula { get; set; } = string.Empty;
        public DateTime FechaVisita { get; set; }
        public int Edad { get; set; }
        public string MotivoConsulta { get; set; } = string.Empty;
        public bool TerapiaPrevia { get; set; }
        public string Medicacion { get; set; } = string.Empty;
    }
}