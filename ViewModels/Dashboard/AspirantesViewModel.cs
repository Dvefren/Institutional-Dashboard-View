namespace UTTN.Dashboard.ViewModels.Dashboard
{
    public class AspirantesViewModel
    {
        // KPIs
        public int TotalAspirantes { get; set; }
        public int FichasPagadas { get; set; }
        public int ExamenPresentado { get; set; }
        public int Aceptados { get; set; }
        public int Pendientes { get; set; }
        public int DocIncompleta { get; set; }
        public int Rechazados { get; set; }
        public decimal PromedioGeneral { get; set; }

        // By Status
        public List<StatusStatItem> ByStatus { get; set; } = new();

        // By Career
        public List<CareerStatItem> ByCareer { get; set; } = new();

        // Gender
        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }

        // Geographic
        public List<GeoStatItem> ByEstado { get; set; } = new();
        public List<GeoStatItem> ByMunicipio { get; set; } = new();

        // Escuelas
        public List<EscuelaStatItem> TopEscuelas { get; set; } = new();

        // Tipo Preparatoria
        public List<StatusStatItem> ByTipoPrepa { get; set; } = new();

        // Sistema Estudio
        public List<StatusStatItem> BySistemaEstudio { get; set; } = new();

        // Medio difusion
        public List<StatusStatItem> ByMedioDifusion { get; set; } = new();

        // Social indicators
        public int Trabajan { get; set; }
        public int ConBeca { get; set; }
        public int OrigenIndigena { get; set; }
        public int HablaLenguaIndigena { get; set; }
        public int ConDiscapacidad { get; set; }
        public int ConEnfermedad { get; set; }
        public int TotalOtros { get; set; }

        // Promedio distribution
        public List<PromedioRangeItem> PromedioDistribution { get; set; } = new();

        // Monthly trend
        public List<MonthlyStatItem> MonthlyTrend { get; set; } = new();

        // Nacionalidad
        public List<StatusStatItem> ByNacionalidad { get; set; } = new();

        // Recent table
        public List<AspiranteDetailItem> RecentAspirantes { get; set; } = new();
    }

    public class AspiranteDetailItem
    {
        public string Folio { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Carrera { get; set; } = string.Empty;
        public decimal Promedio { get; set; }
        public string Preparatoria { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string Estatus { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }
}