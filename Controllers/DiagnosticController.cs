using Microsoft.AspNetCore.Mvc;
using Dapper;
using UTTN.Dashboard.Data;

namespace UTTN.Dashboard.Controllers
{
    public class DiagnosticController : Controller
    {
        private readonly DapperContext _context;

        public DiagnosticController(DapperContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            using var connection = _context.CreateConnection();
            var report = new Dictionary<string, object>();

            try
            {
                // ═══════════════════════════════════════
                // ROW COUNTS FOR EVERY TABLE
                // ═══════════════════════════════════════
                var tables = new[]
                {
                    "management_person_table",
                    "management_user_table",
                    "management_role_table",
                    "management_permission_table",
                    "management_rolepermission_table",
                    "management_userrole_table",
                    "management_career_table",
                    "management_group_table",
                    "management_student_table",
                    "management_teacher_table",
                    "management_usercareer_table",
                    "management_studentcareer_history_table",
                    "management_studentgroup_history_table",
                    "Preinscripciones",
                    "PreinscripcionDatosPersonales",
                    "PreinscripcionDomicilio",
                    "PreinscripcionEscolar",
                    "PreinscripcionSalud",
                    "PreinscripcionTutor",
                    "PreinscripcionOtros",
                    "Inscripciones",
                    "Aspirantes",
                    "AspiranteDatosGenerales",
                    "AspiranteDomicilio",
                    "AspiranteEscolar",
                    "AspiranteOtros",
                    "AspiranteTutor",
                    "CE_TramitesCategoria",
                    "CE_TramitesRequisitos",
                    "CE_TramitesSolicitud",
                    "CE_TramitesDetalleDocumentos",
                    "Visitas",
                    "VisitasPsicologicas",
                    "operational_organization_table",
                    "operational_program_table",
                    "operational_studentassignment_table",
                    "operational_document_table"
                };

                var counts = new Dictionary<string, int>();
                foreach (var table in tables)
                {
                    try
                    {
                        var count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM [{table}]");
                        counts[table] = count;
                    }
                    catch
                    {
                        counts[table] = -1; // table doesn't exist or error
                    }
                }
                report["counts"] = counts;

                // ═══════════════════════════════════════
                // SAMPLE DATA (Top 5 per key table)
                // ═══════════════════════════════════════

                // Careers
                report["careers"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM management_career_table WHERE management_career_status = 1")).ToList();

                // Groups
                report["groups"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM management_group_table WHERE management_group_status = 1")).ToList();

                // Students
                report["students"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM management_student_table WHERE management_student_status = 1")).ToList();

                // Teachers
                report["teachers"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM management_teacher_table WHERE management_teacher_status = 1")).ToList();

                // Persons
                report["persons"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM management_person_table WHERE management_person_status = 1")).ToList();

                // Preinscripciones
                report["preinscripciones"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM Preinscripciones ORDER BY FechaPreinscripcion DESC")).ToList();

                // Inscripciones
                report["inscripciones"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM Inscripciones ORDER BY FechaInscripcion DESC")).ToList();

                // Tramites Categorias
                report["tramite_categorias"] = (await connection.QueryAsync(
                    "SELECT * FROM CE_TramitesCategoria")).ToList();

                // Tramites Solicitudes
                report["tramite_solicitudes"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM CE_TramitesSolicitud ORDER BY tramites_solicitud_fecha DESC")).ToList();

                // Visitas
                report["visitas"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM Visitas ORDER BY FechaVisita DESC")).ToList();

                // Preinscripcion Salud
                report["salud"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM PreinscripcionSalud")).ToList();

                // Student career history
                report["career_history"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM management_studentcareer_history_table WHERE management_studentcareer_history_status = 1")).ToList();

                // Student group history
                report["group_history"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM management_studentgroup_history_table WHERE management_studentgroup_history_status = 1")).ToList();

                // Distinct statuses
                report["student_statuses"] = (await connection.QueryAsync(
                    "SELECT management_student_StatusCode, COUNT(*) AS Total FROM management_student_table WHERE management_student_status = 1 GROUP BY management_student_StatusCode")).ToList();

                report["preinscripcion_statuses"] = (await connection.QueryAsync(
                    "SELECT EstadoPreinscripcion, COUNT(*) AS Total FROM Preinscripciones GROUP BY EstadoPreinscripcion")).ToList();

                report["inscripcion_statuses"] = (await connection.QueryAsync(
                    "SELECT EstadoInscripcion, COUNT(*) AS Total FROM Inscripciones GROUP BY EstadoInscripcion")).ToList();

                report["tramite_statuses"] = (await connection.QueryAsync(
                    "SELECT tramites_solicitud_estatus, COUNT(*) AS Total FROM CE_TramitesSolicitud GROUP BY tramites_solicitud_estatus")).ToList();

                // Visitas diagnosticos (distinct)
                report["diagnosticos"] = (await connection.QueryAsync(
                    "SELECT TOP 15 Diagnostico, COUNT(*) AS Total FROM Visitas GROUP BY Diagnostico ORDER BY Total DESC")).ToList();

                // Aspirantes
                report["aspirantes"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM Aspirantes ORDER BY FechaRegistro DESC")).ToList();

                // Aspirante Datos Generales
                report["aspirante_datos"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM AspiranteDatosGenerales")).ToList();

                // Aspirante Otros
                report["aspirante_otros"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM AspiranteOtros")).ToList();

                // PreinscripcionOtros
                report["preinscripcion_otros"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM PreinscripcionOtros")).ToList();

                // Operational
                report["organizations"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM operational_organization_table ORDER BY operational_organization_createdDate DESC")).ToList();

                report["programs"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM operational_program_table ORDER BY operational_program_createdDate DESC")).ToList();

                report["assignments"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM operational_studentassignment_table ORDER BY operational_studentassignment_createdDate DESC")).ToList();

                report["documents"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM operational_document_table ORDER BY operational_document_createdDate DESC")).ToList();

                // Psicologicas
                report["visitas_psicologicas"] = (await connection.QueryAsync(
                    "SELECT TOP 5 * FROM VisitasPsicologicas ORDER BY FechaVisita DESC")).ToList();

            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
            }

            return View(report);
        }
    }
}