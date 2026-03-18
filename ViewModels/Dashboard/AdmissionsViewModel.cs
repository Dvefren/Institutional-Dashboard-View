namespace UTTN.Dashboard.ViewModels.Dashboard
{
    public class AdmissionsViewModel
    {
        // KPI Cards
        public int TotalPreinscripciones { get; set; }
        public int TotalInscripciones { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal PromedioGeneral { get; set; }

        // Preinscripciones by Career
        public List<CareerStatItem> PreinscripcionesByCareer { get; set; } = new();

        // Preinscripciones by Status
        public List<StatusStatItem> PreinscripcionesByStatus { get; set; } = new();

        // Inscripciones by Status
        public List<StatusStatItem> InscripcionesByStatus { get; set; } = new();

        // Geographic origin (Estado)
        public List<GeoStatItem> ByEstado { get; set; } = new();

        // Geographic origin (Municipio - top 10)
        public List<GeoStatItem> ByMunicipio { get; set; } = new();

        // Escuelas de procedencia (top 10)
        public List<EscuelaStatItem> TopEscuelas { get; set; } = new();

        // Medio de difusion
        public List<StatusStatItem> ByMedioDifusion { get; set; } = new();

        // Monthly trend
        public List<MonthlyStatItem> MonthlyPreinscripciones { get; set; } = new();
        public List<MonthlyStatItem> MonthlyInscripciones { get; set; } = new();

        // Health/Social indicators from PreinscripcionSalud
        public int ConDiscapacidad { get; set; }
        public int ComunidadIndigena { get; set; }
        public int ConHijos { get; set; }
        public int TotalSaludRecords { get; set; }

        // Gender from preinscripcion
        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }
        public int OtherGenderCount { get; set; }

        // Promedio distribution
        public List<PromedioRangeItem> PromedioDistribution { get; set; } = new();

        // Detail table
        public List<PreinscripcionDetailItem> RecentPreinscripciones { get; set; } = new();
    }

    public class GeoStatItem
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class EscuelaStatItem
    {
        public string EscuelaNombre { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class PromedioRangeItem
    {
        public string Range { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class PreinscripcionDetailItem
    {
        public string Folio { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Carrera { get; set; } = string.Empty;
        public decimal Promedio { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Estatus { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }
}