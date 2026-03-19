using Dapper;
using UTTN.Dashboard.Data;
using UTTN.Dashboard.Services.Interfaces;
using UTTN.Dashboard.ViewModels.Dashboard;

namespace UTTN.Dashboard.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly DapperContext _context;

        public DashboardService(DapperContext context)
        {
            _context = context;
        }

        public async Task<RectorateViewModel> GetRectorateDataAsync()
        {
            using var connection = _context.CreateConnection();
            var vm = new RectorateViewModel();

            // ═══════════════════════════════════════
            // KPI COUNTS
            // ═══════════════════════════════════════
            vm.TotalStudents = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM management_student_table WHERE management_student_status = 1");

            vm.ActiveStudents = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM management_student_table WHERE management_student_status = 1 AND management_student_StatusCode = 'INSCRITO'");

            vm.Preinscritos = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM management_student_table WHERE management_student_status = 1 AND management_student_StatusCode = 'PREINSCRITO'");

            vm.Inscritos = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Inscripciones");

            vm.TotalTeachers = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM management_teacher_table WHERE management_teacher_status = 1");

            vm.TotalCareers = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM management_career_table WHERE management_career_status = 1");

            vm.TotalGroups = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM management_group_table WHERE management_group_status = 1");

            vm.TotalTramitesPendientes = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM CE_TramitesSolicitud WHERE tramites_solicitud_estatus = 'Pendiente'");

            vm.TotalVisitasMedicas = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Visitas");

            // ═══════════════════════════════════════
            // STUDENTS BY CAREER
            // ═══════════════════════════════════════
            var studentsByCareers = await connection.QueryAsync<dynamic>(@"
                SELECT 
                    ISNULL(c.management_career_Name, 'Sin carrera') AS CareerName,
                    ISNULL(c.management_career_Code, '—') AS CareerCode,
                    COUNT(*) AS Count
                FROM management_student_table s
                LEFT JOIN management_career_table c 
                    ON s.management_student_CareerID = c.management_career_ID
                WHERE s.management_student_status = 1
                GROUP BY c.management_career_Name, c.management_career_Code
                ORDER BY Count DESC");

            var total = studentsByCareers.Sum(x => (int)x.Count);
            vm.StudentsByCareers = studentsByCareers.Select(x => new CareerStatItem
            {
                CareerName = (string)x.CareerName,
                CareerCode = (string)x.CareerCode,
                Count = (int)x.Count,
                Percentage = total > 0 ? Math.Round((decimal)(int)x.Count / total * 100, 1) : 0
            }).ToList();

            // ═══════════════════════════════════════
            // STUDENTS BY STATUS
            // ═══════════════════════════════════════
            var studentsByStatus = await connection.QueryAsync<dynamic>(@"
                SELECT 
                    management_student_StatusCode AS Status,
                    COUNT(*) AS Count
                FROM management_student_table
                WHERE management_student_status = 1
                GROUP BY management_student_StatusCode
                ORDER BY Count DESC");

            var totalStatus = studentsByStatus.Sum(x => (int)x.Count);
            vm.StudentsByStatus = studentsByStatus.Select(x => new StatusStatItem
            {
                Status = (string)x.Status,
                Count = (int)x.Count,
                Percentage = totalStatus > 0 ? Math.Round((decimal)(int)x.Count / totalStatus * 100, 1) : 0
            }).ToList();

            // ═══════════════════════════════════════
            // GENDER DISTRIBUTION
            // ═══════════════════════════════════════
            var genderData = await connection.QueryAsync<dynamic>(@"
                SELECT 
                    ISNULL(p.management_person_Gender, 'No especificado') AS Gender,
                    COUNT(*) AS Count
                FROM management_student_table s
                INNER JOIN management_person_table p 
                    ON s.management_student_PersonID = p.management_person_ID
                WHERE s.management_student_status = 1
                GROUP BY p.management_person_Gender");

            foreach (var g in genderData)
            {
                string gender = ((string)g.Gender).ToLower();
                int count = (int)g.Count;
                if (gender.Contains("masculino") || gender.Contains("hombre") || gender == "m")
                    vm.MaleCount += count;
                else if (gender.Contains("femenino") || gender.Contains("mujer") || gender == "f")
                    vm.FemaleCount += count;
                else
                    vm.OtherGenderCount += count;
            }

            // ═══════════════════════════════════════
            // PREINSCRIPCIONES BY CAREER
            // ═══════════════════════════════════════
            var preByCareers = await connection.QueryAsync<dynamic>(@"
                SELECT 
                    CarreraSolicitada AS CareerName,
                    COUNT(*) AS Count
                FROM Preinscripciones
                GROUP BY CarreraSolicitada
                ORDER BY Count DESC");

            var totalPre = preByCareers.Sum(x => (int)x.Count);
            vm.PreinscripcionesByCareer = preByCareers.Select(x => new CareerStatItem
            {
                CareerName = (string)x.CareerName,
                CareerCode = "",
                Count = (int)x.Count,
                Percentage = totalPre > 0 ? Math.Round((decimal)(int)x.Count / totalPre * 100, 1) : 0
            }).ToList();

            // ═══════════════════════════════════════
            // PREINSCRIPCIONES BY STATUS
            // ═══════════════════════════════════════
            var preByStatus = await connection.QueryAsync<dynamic>(@"
                SELECT 
                    EstadoPreinscripcion AS Status,
                    COUNT(*) AS Count
                FROM Preinscripciones
                GROUP BY EstadoPreinscripcion
                ORDER BY Count DESC");

            var totalPreStatus = preByStatus.Sum(x => (int)x.Count);
            vm.PreinscripcionesByStatus = preByStatus.Select(x => new StatusStatItem
            {
                Status = (string)x.Status,
                Count = (int)x.Count,
                Percentage = totalPreStatus > 0 ? Math.Round((decimal)(int)x.Count / totalPreStatus * 100, 1) : 0
            }).ToList();

            // ═══════════════════════════════════════
            // GROUPS OVERVIEW
            // ═══════════════════════════════════════
            vm.GroupsOverview = (await connection.QueryAsync<GroupStatItem>(@"
                SELECT 
                    g.management_group_Code AS GroupCode,
                    ISNULL(c.management_career_Name, 'Sin carrera') AS CareerName,
                    ISNULL(g.management_group_Shift, '—') AS Shift,
                    COUNT(s.management_student_ID) AS StudentCount
                FROM management_group_table g
                LEFT JOIN management_career_table c 
                    ON g.management_group_CareerID = c.management_career_ID
                LEFT JOIN management_student_table s 
                    ON g.management_group_ID = s.management_student_GroupID 
                    AND s.management_student_status = 1
                WHERE g.management_group_status = 1
                GROUP BY g.management_group_Code, c.management_career_Name, g.management_group_Shift
                ORDER BY c.management_career_Name, g.management_group_Code")).ToList();

            // ═══════════════════════════════════════
            // MONTHLY PREINSCRIPCIONES TREND
            // ═══════════════════════════════════════
            vm.MonthlyPreinscripciones = (await connection.QueryAsync<MonthlyStatItem>(@"
                SELECT 
                    FORMAT(FechaPreinscripcion, 'MMM', 'es-MX') AS Month,
                    YEAR(FechaPreinscripcion) AS Year,
                    COUNT(*) AS Count
                FROM Preinscripciones
                GROUP BY FORMAT(FechaPreinscripcion, 'MMM', 'es-MX'), 
                         YEAR(FechaPreinscripcion),
                         MONTH(FechaPreinscripcion)
                ORDER BY YEAR(FechaPreinscripcion), MONTH(FechaPreinscripcion)")).ToList();

            // ═══════════════════════════════════════
            // CAREERS OVERVIEW (aggregated stats)
            // ═══════════════════════════════════════
            vm.CareersOverview = (await connection.QueryAsync<CareerOverviewItem>(@"
                SELECT 
                    c.management_career_Name AS CareerName,
                    c.management_career_Code AS CareerCode,
                    COUNT(s.management_student_ID) AS TotalStudents,
                    SUM(CASE WHEN s.management_student_StatusCode = 'INSCRITO' THEN 1 ELSE 0 END) AS Inscritos,
                    SUM(CASE WHEN s.management_student_StatusCode = 'PREINSCRITO' THEN 1 ELSE 0 END) AS Preinscritos,
                    SUM(CASE WHEN s.management_student_StatusCode = 'BAJA' THEN 1 ELSE 0 END) AS Bajas,
                    COUNT(DISTINCT s.management_student_GroupID) AS Groups,
                    CAST(0 AS DECIMAL(5,1)) AS Percentage
                FROM management_career_table c
                LEFT JOIN management_student_table s 
                    ON c.management_career_ID = s.management_student_CareerID 
                    AND s.management_student_status = 1
                WHERE c.management_career_status = 1
                GROUP BY c.management_career_Name, c.management_career_Code
                ORDER BY TotalStudents DESC")).ToList();

                    var totalCareerStudents = vm.CareersOverview.Sum(x => x.TotalStudents);
                    foreach (var c in vm.CareersOverview)
                    {
                        c.Percentage = totalCareerStudents > 0
                            ? Math.Round((decimal)c.TotalStudents / totalCareerStudents * 100, 1)
                            : 0;
                    }

                    return vm;

        }

        public async Task<AdmissionsViewModel> GetAdmissionsDataAsync()
        {
            using var connection = _context.CreateConnection();
            var vm = new AdmissionsViewModel();

            // ═══════════════════════════════════════
            // KPI COUNTS
            // ═══════════════════════════════════════
            vm.TotalPreinscripciones = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Preinscripciones");

            vm.TotalInscripciones = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Inscripciones");

            vm.ConversionRate = vm.TotalPreinscripciones > 0
                ? Math.Round((decimal)vm.TotalInscripciones / vm.TotalPreinscripciones * 100, 1)
                : 0;

            vm.PromedioGeneral = await connection.ExecuteScalarAsync<decimal?>(
                "SELECT AVG(Promedio) FROM Preinscripciones") ?? 0;
            vm.PromedioGeneral = Math.Round(vm.PromedioGeneral, 2);

            // ═══════════════════════════════════════
            // BY CAREER
            // ═══════════════════════════════════════
            var byCareers = await connection.QueryAsync<dynamic>(@"
        SELECT CarreraSolicitada AS CareerName, COUNT(*) AS Count
        FROM Preinscripciones
        GROUP BY CarreraSolicitada
        ORDER BY Count DESC");

            var totalC = byCareers.Sum(x => (int)x.Count);
            vm.PreinscripcionesByCareer = byCareers.Select(x => new CareerStatItem
            {
                CareerName = (string)x.CareerName,
                Count = (int)x.Count,
                Percentage = totalC > 0 ? Math.Round((decimal)(int)x.Count / totalC * 100, 1) : 0
            }).ToList();

            // ═══════════════════════════════════════
            // BY STATUS (Preinscripciones)
            // ═══════════════════════════════════════
            var byStatus = await connection.QueryAsync<dynamic>(@"
        SELECT EstadoPreinscripcion AS Status, COUNT(*) AS Count
        FROM Preinscripciones
        GROUP BY EstadoPreinscripcion
        ORDER BY Count DESC");

            var totalS = byStatus.Sum(x => (int)x.Count);
            vm.PreinscripcionesByStatus = byStatus.Select(x => new StatusStatItem
            {
                Status = (string)x.Status,
                Count = (int)x.Count,
                Percentage = totalS > 0 ? Math.Round((decimal)(int)x.Count / totalS * 100, 1) : 0
            }).ToList();

            // ═══════════════════════════════════════
            // BY STATUS (Inscripciones)
            // ═══════════════════════════════════════
            var byInsStatus = await connection.QueryAsync<dynamic>(@"
        SELECT EstadoInscripcion AS Status, COUNT(*) AS Count
        FROM Inscripciones
        GROUP BY EstadoInscripcion
        ORDER BY Count DESC");

            var totalIS = byInsStatus.Sum(x => (int)x.Count);
            vm.InscripcionesByStatus = byInsStatus.Select(x => new StatusStatItem
            {
                Status = (string)x.Status,
                Count = (int)x.Count,
                Percentage = totalIS > 0 ? Math.Round((decimal)(int)x.Count / totalIS * 100, 1) : 0
            }).ToList();

            // ═══════════════════════════════════════
            // GEOGRAPHIC: BY ESTADO
            // ═══════════════════════════════════════
            var byEstado = await connection.QueryAsync<dynamic>(@"
        SELECT d.Estado AS Name, COUNT(*) AS Count
        FROM PreinscripcionDomicilio d
        INNER JOIN Preinscripciones p ON d.PreinscripcionId = p.Id
        GROUP BY d.Estado
        ORDER BY Count DESC");

            var totalE = byEstado.Sum(x => (int)x.Count);
            vm.ByEstado = byEstado.Select(x => new GeoStatItem
            {
                Name = (string)x.Name,
                Count = (int)x.Count,
                Percentage = totalE > 0 ? Math.Round((decimal)(int)x.Count / totalE * 100, 1) : 0
            }).ToList();

            // ═══════════════════════════════════════
            // GEOGRAPHIC: BY MUNICIPIO (Top 10)
            // ═══════════════════════════════════════
            var byMunicipio = await connection.QueryAsync<dynamic>(@"
        SELECT TOP 10 d.Municipio AS Name, COUNT(*) AS Count
        FROM PreinscripcionDomicilio d
        INNER JOIN Preinscripciones p ON d.PreinscripcionId = p.Id
        GROUP BY d.Municipio
        ORDER BY Count DESC");

            var totalM = byMunicipio.Sum(x => (int)x.Count);
            vm.ByMunicipio = byMunicipio.Select(x => new GeoStatItem
            {
                Name = (string)x.Name,
                Count = (int)x.Count,
                Percentage = totalM > 0 ? Math.Round((decimal)(int)x.Count / totalM * 100, 1) : 0
            }).ToList();

            // ═══════════════════════════════════════
            // TOP ESCUELAS DE PROCEDENCIA
            // ═══════════════════════════════════════
            vm.TopEscuelas = (await connection.QueryAsync<EscuelaStatItem>(@"
        SELECT TOP 10
            e.EscuelaProcedencia AS EscuelaNombre,
            ISNULL(e.EstadoEscuela, '—') AS Estado,
            COUNT(*) AS Count
        FROM PreinscripcionEscolar e
        INNER JOIN Preinscripciones p ON e.PreinscripcionId = p.Id
        GROUP BY e.EscuelaProcedencia, e.EstadoEscuela
        ORDER BY Count DESC")).ToList();

            // ═══════════════════════════════════════
            // MEDIO DE DIFUSION
            // ═══════════════════════════════════════
            var byMedio = await connection.QueryAsync<dynamic>(@"
        SELECT ISNULL(MedioDifusion, 'No especificado') AS Status, COUNT(*) AS Count
        FROM Preinscripciones
        GROUP BY MedioDifusion
        ORDER BY Count DESC");

            var totalMd = byMedio.Sum(x => (int)x.Count);
            vm.ByMedioDifusion = byMedio.Select(x => new StatusStatItem
            {
                Status = (string)x.Status,
                Count = (int)x.Count,
                Percentage = totalMd > 0 ? Math.Round((decimal)(int)x.Count / totalMd * 100, 1) : 0
            }).ToList();

            // ═══════════════════════════════════════
            // MONTHLY TRENDS
            // ═══════════════════════════════════════
            vm.MonthlyPreinscripciones = (await connection.QueryAsync<MonthlyStatItem>(@"
        SELECT FORMAT(FechaPreinscripcion, 'MMM', 'es-MX') AS Month,
               YEAR(FechaPreinscripcion) AS Year,
               COUNT(*) AS Count
        FROM Preinscripciones
        GROUP BY FORMAT(FechaPreinscripcion, 'MMM', 'es-MX'),
                 YEAR(FechaPreinscripcion),
                 MONTH(FechaPreinscripcion)
        ORDER BY YEAR(FechaPreinscripcion), MONTH(FechaPreinscripcion)")).ToList();

            vm.MonthlyInscripciones = (await connection.QueryAsync<MonthlyStatItem>(@"
        SELECT FORMAT(FechaInscripcion, 'MMM', 'es-MX') AS Month,
               YEAR(FechaInscripcion) AS Year,
               COUNT(*) AS Count
        FROM Inscripciones
        GROUP BY FORMAT(FechaInscripcion, 'MMM', 'es-MX'),
                 YEAR(FechaInscripcion),
                 MONTH(FechaInscripcion)
        ORDER BY YEAR(FechaInscripcion), MONTH(FechaInscripcion)")).ToList();

            // ═══════════════════════════════════════
            // HEALTH / SOCIAL INDICATORS
            // ═══════════════════════════════════════
            vm.TotalSaludRecords = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM PreinscripcionSalud");

            vm.ConDiscapacidad = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM PreinscripcionSalud WHERE TieneDiscapacidad = 1");

            vm.ComunidadIndigena = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM PreinscripcionSalud WHERE ComunidadIndigena = 1");

            vm.ConHijos = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM PreinscripcionSalud WHERE TieneHijos = 1");

            // ═══════════════════════════════════════
            // GENDER FROM PREINSCRIPCION
            // ═══════════════════════════════════════
            var genderData = await connection.QueryAsync<dynamic>(@"
        SELECT d.Sexo AS Gender, COUNT(*) AS Count
        FROM PreinscripcionDatosPersonales d
        INNER JOIN Preinscripciones p ON d.PreinscripcionId = p.Id
        GROUP BY d.Sexo");

            foreach (var g in genderData)
            {
                string gender = ((string)g.Gender).ToLower();
                int count = (int)g.Count;
                if (gender.Contains("masculino") || gender.Contains("hombre") || gender == "m")
                    vm.MaleCount += count;
                else if (gender.Contains("femenino") || gender.Contains("mujer") || gender == "f")
                    vm.FemaleCount += count;
                else
                    vm.OtherGenderCount += count;
            }

            // ═══════════════════════════════════════
            // PROMEDIO DISTRIBUTION
            // ═══════════════════════════════════════
            vm.PromedioDistribution = (await connection.QueryAsync<PromedioRangeItem>(@"
        SELECT 
            CASE 
                WHEN Promedio >= 9.0 THEN '9.0 — 10.0'
                WHEN Promedio >= 8.0 THEN '8.0 — 8.9'
                WHEN Promedio >= 7.0 THEN '7.0 — 7.9'
                WHEN Promedio >= 6.0 THEN '6.0 — 6.9'
                ELSE 'Menor a 6.0'
            END AS Range,
            COUNT(*) AS Count
        FROM Preinscripciones
        GROUP BY 
            CASE 
                WHEN Promedio >= 9.0 THEN '9.0 — 10.0'
                WHEN Promedio >= 8.0 THEN '8.0 — 8.9'
                WHEN Promedio >= 7.0 THEN '7.0 — 7.9'
                WHEN Promedio >= 6.0 THEN '6.0 — 6.9'
                ELSE 'Menor a 6.0'
            END
        ORDER BY Range DESC")).ToList();

            // ═══════════════════════════════════════
            // RECENT PREINSCRIPCIONES (Last 50)
            // ═══════════════════════════════════════
            vm.RecentPreinscripciones = (await connection.QueryAsync<PreinscripcionDetailItem>(@"
        SELECT TOP 50
            ISNULL(p.Folio, '—') AS Folio,
            ISNULL(d.Nombre + ' ' + d.ApellidoPaterno, '—') AS Nombre,
            p.CarreraSolicitada AS Carrera,
            p.Promedio,
            ISNULL(dom.Estado, '—') AS Estado,
            p.EstadoPreinscripcion AS Estatus,
            p.FechaPreinscripcion AS Fecha
        FROM Preinscripciones p
        LEFT JOIN PreinscripcionDatosPersonales d ON p.Id = d.PreinscripcionId
        LEFT JOIN PreinscripcionDomicilio dom ON p.Id = dom.PreinscripcionId
        ORDER BY p.FechaPreinscripcion DESC")).ToList();

            return vm;
        }

        public async Task<TramitesViewModel> GetTramitesDataAsync()
        {
            using var connection = _context.CreateConnection();
            var vm = new TramitesViewModel();

            // ═══════════════════════════════════════
            // KPI COUNTS
            // ═══════════════════════════════════════
            vm.TotalSolicitudes = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM CE_TramitesSolicitud");

            vm.Pendientes = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM CE_TramitesSolicitud WHERE tramites_solicitud_estatus = 'Pendiente'");

            vm.Completadas = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM CE_TramitesSolicitud WHERE tramites_solicitud_estatus IN ('Completado','Completada','Aprobado','Aprobada','Entregado','Entregada')");

            vm.Rechazadas = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM CE_TramitesSolicitud WHERE tramites_solicitud_estatus IN ('Rechazado','Rechazada')");

            vm.TasaCompletado = vm.TotalSolicitudes > 0
                ? Math.Round((decimal)vm.Completadas / vm.TotalSolicitudes * 100, 1)
                : 0;

            // Average resolution time (completed only)
            var avgDays = await connection.ExecuteScalarAsync<double?>(
                @"SELECT AVG(CAST(DATEDIFF(DAY, tramites_solicitud_fecha, GETDATE()) AS FLOAT))
          FROM CE_TramitesSolicitud 
          WHERE tramites_solicitud_estatus IN ('Completado','Completada','Aprobado','Aprobada','Entregado','Entregada')");
            vm.PromedioResolucionDias = Math.Round(avgDays ?? 0, 1);

            // ═══════════════════════════════════════
            // BY STATUS
            // ═══════════════════════════════════════
            var byStatus = await connection.QueryAsync<dynamic>(@"
        SELECT ISNULL(tramites_solicitud_estatus, 'Pendiente') AS Status, COUNT(*) AS Count
        FROM CE_TramitesSolicitud
        GROUP BY tramites_solicitud_estatus
        ORDER BY Count DESC");

            var totalS = byStatus.Sum(x => (int)x.Count);
            vm.ByStatus = byStatus.Select(x => new StatusStatItem
            {
                Status = (string)x.Status,
                Count = (int)x.Count,
                Percentage = totalS > 0 ? Math.Round((decimal)(int)x.Count / totalS * 100, 1) : 0
            }).ToList();

            // ═══════════════════════════════════════
            // BY TRAMITE TYPE
            // ═══════════════════════════════════════
            vm.ByTipoTramite = (await connection.QueryAsync<TramiteTipoItem>(@"
        SELECT 
            t.nombre_tramite AS TipoNombre,
            COUNT(*) AS Total,
            SUM(CASE WHEN s.tramites_solicitud_estatus = 'Pendiente' THEN 1 ELSE 0 END) AS Pendientes,
            SUM(CASE WHEN s.tramites_solicitud_estatus IN ('Completado','Completada','Aprobado','Aprobada','Entregado','Entregada') THEN 1 ELSE 0 END) AS Completadas,
            SUM(CASE WHEN s.tramites_solicitud_estatus IN ('Rechazado','Rechazada') THEN 1 ELSE 0 END) AS Rechazadas
        FROM CE_TramitesSolicitud s
        INNER JOIN CE_TramitesCategoria t ON s.id_tramite = t.id_tramite
        GROUP BY t.nombre_tramite
        ORDER BY Total DESC")).ToList();

            // ═══════════════════════════════════════
            // DOCUMENT VALIDATION STATUS
            // ═══════════════════════════════════════
            vm.DocsAprobados = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM CE_TramitesDetalleDocumentos WHERE estatus_documento IN ('Aprobado','Aprobada','Validado','Validada')");

            vm.DocsPendientes = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM CE_TramitesDetalleDocumentos WHERE estatus_documento = 'Pendiente'");

            vm.DocsRechazados = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM CE_TramitesDetalleDocumentos WHERE estatus_documento IN ('Rechazado','Rechazada')");

            // ═══════════════════════════════════════
            // MONTHLY TREND
            // ═══════════════════════════════════════
            vm.MonthlyTrend = (await connection.QueryAsync<MonthlyStatItem>(@"
        SELECT FORMAT(tramites_solicitud_fecha, 'MMM', 'es-MX') AS Month,
               YEAR(tramites_solicitud_fecha) AS Year,
               COUNT(*) AS Count
        FROM CE_TramitesSolicitud
        GROUP BY FORMAT(tramites_solicitud_fecha, 'MMM', 'es-MX'),
                 YEAR(tramites_solicitud_fecha),
                 MONTH(tramites_solicitud_fecha)
        ORDER BY YEAR(tramites_solicitud_fecha), MONTH(tramites_solicitud_fecha)")).ToList();

            // ═══════════════════════════════════════
            // RESOLUTION TIME BY TYPE
            // ═══════════════════════════════════════
            vm.ResolucionByTipo = (await connection.QueryAsync<TramiteResolucionItem>(@"
        SELECT 
            t.nombre_tramite AS TipoNombre,
            AVG(CAST(DATEDIFF(DAY, s.tramites_solicitud_fecha, GETDATE()) AS FLOAT)) AS PromedioDias,
            COUNT(*) AS TotalResueltos
        FROM CE_TramitesSolicitud s
        INNER JOIN CE_TramitesCategoria t ON s.id_tramite = t.id_tramite
        WHERE s.tramites_solicitud_estatus IN ('Completado','Completada','Aprobado','Aprobada','Entregado','Entregada')
        GROUP BY t.nombre_tramite
        ORDER BY PromedioDias DESC")).ToList();

            // ═══════════════════════════════════════
            // RECENT SOLICITUDES (Last 50)
            // ═══════════════════════════════════════
            vm.RecentSolicitudes = (await connection.QueryAsync<SolicitudDetailItem>(@"
        SELECT TOP 50
            s.tramites_solicitud_id AS Id,
            ISNULL(p.management_person_FirstName + ' ' + p.management_person_LastNamePaternal, '—') AS Nombre,
            ISNULL(st.management_student_Matricula, '—') AS Matricula,
            t.nombre_tramite AS TipoTramite,
            ISNULL(s.tramites_solicitud_estatus, 'Pendiente') AS Estatus,
            ISNULL(s.tramites_solicitud_observaciones, '') AS Observaciones,
            s.tramites_solicitud_fecha AS Fecha,
            DATEDIFF(DAY, s.tramites_solicitud_fecha, GETDATE()) AS DiasTranscurridos
        FROM CE_TramitesSolicitud s
        INNER JOIN CE_TramitesCategoria t ON s.id_tramite = t.id_tramite
        LEFT JOIN management_user_table u ON s.id_usuario_propietario = u.management_user_ID
        LEFT JOIN management_person_table p ON u.management_user_PersonID = p.management_person_ID
        LEFT JOIN management_student_table st ON p.management_person_ID = st.management_student_PersonID
        ORDER BY s.tramites_solicitud_fecha DESC")).ToList();

            // ═══════════════════════════════════════
            // BOTTLENECK: OLDEST PENDING (Top 10)
            // ═══════════════════════════════════════
            vm.OldestPending = (await connection.QueryAsync<SolicitudDetailItem>(@"
        SELECT TOP 10
            s.tramites_solicitud_id AS Id,
            ISNULL(p.management_person_FirstName + ' ' + p.management_person_LastNamePaternal, '—') AS Nombre,
            ISNULL(st.management_student_Matricula, '—') AS Matricula,
            t.nombre_tramite AS TipoTramite,
            'Pendiente' AS Estatus,
            ISNULL(s.tramites_solicitud_observaciones, '') AS Observaciones,
            s.tramites_solicitud_fecha AS Fecha,
            DATEDIFF(DAY, s.tramites_solicitud_fecha, GETDATE()) AS DiasTranscurridos
        FROM CE_TramitesSolicitud s
        INNER JOIN CE_TramitesCategoria t ON s.id_tramite = t.id_tramite
        LEFT JOIN management_user_table u ON s.id_usuario_propietario = u.management_user_ID
        LEFT JOIN management_person_table p ON u.management_user_PersonID = p.management_person_ID
        LEFT JOIN management_student_table st ON p.management_person_ID = st.management_student_PersonID
        WHERE s.tramites_solicitud_estatus = 'Pendiente'
        ORDER BY s.tramites_solicitud_fecha ASC")).ToList();

            return vm;
        }
    }
}