using Dapper;
using System.Data;
using UTTN.Dashboard.Data;
using UTTN.Dashboard.Models.Management;
using UTTN.Dashboard.ViewModels.Auth;
using UTTN.Dashboard.Services.Interfaces;

namespace UTTN.Dashboard.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly DapperContext _context;

        public AuthService(DapperContext context)
        {
            _context = context;
        }

        public async Task<UserSessionViewModel?> AuthenticateAsync(string username, string password)
        {
            using var connection = _context.CreateConnection();

            // 1. Find user by username
            var users = await connection.QueryAsync<UserModel>(
                "sp_management",
                new { Option = "management_user_get" },
                commandType: CommandType.StoredProcedure
            );

            var user = users.FirstOrDefault(u =>
                u.management_user_Username.Equals(username, StringComparison.OrdinalIgnoreCase)
                && u.management_user_status);

            if (user == null) return null;

            // 2. Verify password (BCrypt recommended)
            if (!BCryptVerify(password, user.management_user_PasswordHash))
                return null;

            // 3. Check if locked
            if (user.management_user_IsLocked) return null;

            // 4. Get person info
            PersonModel? person = null;
            if (user.management_user_PersonID.HasValue)
            {
                person = await connection.QueryFirstOrDefaultAsync<PersonModel>(
                    "sp_management",
                    new { Option = "management_person_getbyid", ID = user.management_user_PersonID.Value },
                    commandType: CommandType.StoredProcedure
                );
            }

            // 5. Get role info
            string roleName = "Sin rol";
            if (user.management_user_RoleID.HasValue)
            {
                var role = await connection.QueryFirstOrDefaultAsync<RoleModel>(
                    "sp_management",
                    new { Option = "management_role_getbyid", ID = user.management_user_RoleID.Value },
                    commandType: CommandType.StoredProcedure
                );
                if (role != null) roleName = role.management_role_Name;
            }

            // 6. Get permissions through role
            var permissions = new List<string>();
            if (user.management_user_RoleID.HasValue)
            {
                var rolePermissions = await connection.QueryAsync<dynamic>(
                    @"SELECT p.management_permission_Key 
                      FROM management_rolepermission_table rp
                      INNER JOIN management_permission_table p 
                          ON rp.management_rolepermission_PermissionID = p.management_permission_ID
                      WHERE rp.management_rolepermission_RoleID = @RoleID
                        AND rp.management_rolepermission_status = 1
                        AND p.management_permission_status = 1",
                    new { RoleID = user.management_user_RoleID.Value }
                );
                permissions = rolePermissions.Select(rp => (string)rp.management_permission_Key).ToList();
            }

            return new UserSessionViewModel
            {
                UserID = user.management_user_ID,
                PersonID = user.management_user_PersonID,
                Username = user.management_user_Username,
                Email = user.management_user_Email,
                FullName = person?.FullName ?? username,
                RoleName = roleName,
                RoleID = user.management_user_RoleID,
                Permissions = permissions
            };
        }

        public async Task<bool> UpdateLastLoginAsync(int userId)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_management",
                new { Option = "management_user_update", ID = userId, LastLoginDate = DateTime.Now },
                commandType: CommandType.StoredProcedure
            );
            return result?.affected_rows > 0;
        }

        private bool BCryptVerify(string password, string hash)
        {
            // For initial dev: plain text comparison
            // TODO: Replace with BCrypt.Net-Next package
            // return BCrypt.Net.BCrypt.Verify(password, hash);
            return password == hash;
        }
    }
}