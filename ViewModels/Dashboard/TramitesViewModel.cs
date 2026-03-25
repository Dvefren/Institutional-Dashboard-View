namespace UTTN.Dashboard.ViewModels.Dashboard
{
    public class TramitesViewModel
    {

        // Filters
        public int SelectedYear { get; set; }
        public int SelectedCuatrimestre { get; set; }
        public List<int> AvailableYears { get; set; } = new();

        // KPI Cards
        public int TotalSolicitudes { get; set; }
        public int Pendientes { get; set; }
        public int Completadas { get; set; }
        public int Rechazadas { get; set; }
        public decimal TasaCompletado { get; set; }
        public double PromedioResolucionDias { get; set; }

        // By Status
        public List<StatusStatItem> ByStatus { get; set; } = new();

        // By Tramite Type
        public List<TramiteTipoItem> ByTipoTramite { get; set; } = new();

        // Monthly trend
        public List<MonthlyStatItem> MonthlyTrend { get; set; } = new();

        // Document validation status
        public int DocsAprobados { get; set; }
        public int DocsPendientes { get; set; }
        public int DocsRechazados { get; set; }

        // Average resolution by type
        public List<TramiteResolucionItem> ResolucionByTipo { get; set; } = new();

        // Recent solicitudes table
        public List<SolicitudDetailItem> RecentSolicitudes { get; set; } = new();

        // Bottleneck: oldest pending
        public List<SolicitudDetailItem> OldestPending { get; set; } = new();
    }

    public class TramiteTipoItem
    {
        public string TipoNombre { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Pendientes { get; set; }
        public int Completadas { get; set; }
        public int Rechazadas { get; set; }
    }

    public class TramiteResolucionItem
    {
        public string TipoNombre { get; set; } = string.Empty;
        public double PromedioDias { get; set; }
        public int TotalResueltos { get; set; }
    }

    public class SolicitudDetailItem
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Matricula { get; set; } = string.Empty;
        public string TipoTramite { get; set; } = string.Empty;
        public string Estatus { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public int DiasTranscurridos { get; set; }
    }
}