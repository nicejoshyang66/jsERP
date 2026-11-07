// 2026-10-28 15:00 added: 角色資料維護 (USERS / SYS_ROLES / SYS_ROLEGROUPS / SYS_ROLEGROUPS_D)
// 2026-10-29 10:51 modified: 2 pages - List (users) and ListDet (roles / role groups of one user)
// 2026-10-30 19:32 modified: one page List, tabs Users / Roles. Role groups moved to RoleGroupRepository (ZSYSRoleGroup)
// Note: these tables have no primary key, so we check "already exists" in code.
using Dapper;
using Microsoft.Data.SqlClient;

namespace jsERP.Services
{
    public class RoleRepository
    {
        private readonly IConfiguration _configuration;

        public RoleRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Create a connection; Dapper opens it when the query runs.
        private SqlConnection Connect()
        {
            string? connectionString = _configuration.GetConnectionString("jserp");
            if (connectionString == null)
            {
                throw new InvalidOperationException("Missing ConnectionStrings:jserp.");
            }
            return new SqlConnection(connectionString);
        }

        // ==================== Users (List page) ====================

        // All users
        // 2026-10-29 10:51 modified: add TEL, MOBILE
        // 2026-10-29 13:13 modified: add IS_ACTIVE; PWD2 / AUTOLOGIN are not sent to the page
        public async Task<List<UserRow>> UserList()
        {
            using SqlConnection connection = Connect();

            // 2026-10-30 21:00 modified: add Groups (e.g. "ACC,MIS"), joined by STRING_AGG, no loop
            //   USERS -> SYS_ROLES -> SYS_ROLEGROUPS_D ; DISTINCT because one user may have many roles
            // 2026-10-31 13:35 modified: remove WITHIN GROUP (ORDER BY ...) for simpler syntax; order does not matter
            string sql = """
                SELECT USERID AS UserId, USERNAME AS UserName,
                       EMAIL AS Email, DESCRIPTION AS Description,
                       TEL AS Tel, MOBILE AS Mobile, IS_ACTIVE AS IsActive,
                       ISNULL((
                           SELECT STRING_AGG(G.ROLEGROUP_ID, ',')
                           FROM (
                               SELECT DISTINCT SYS_ROLEGROUPS_D.ROLEGROUP_ID
                               FROM SYS_ROLES
                               JOIN SYS_ROLEGROUPS_D ON SYS_ROLEGROUPS_D.ROLE_ID = SYS_ROLES.ROLE_ID
                               WHERE SYS_ROLES.USER_ID = USERS.USERID
                           ) AS G
                       ), '') AS Groups
                FROM USERS
                ORDER BY USERID
                """;

            IEnumerable<UserRow> rows = await connection.QueryAsync<UserRow>(sql);
            return rows.ToList();
        }

        // 2026-10-30 19:32 removed: UserOne (ListDet page is removed)

        // Is this USERID used already?
        public async Task<bool> UserExists(string userId)
        {
            using SqlConnection connection = Connect();

            string sql = "SELECT COUNT(*) FROM USERS WHERE USERID = @userId";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { userId = userId });
            return count > 0;
        }

        // Insert one user. Password is plain text in PWD2 (for testing). PWD (old encrypted) is not used.
        // 2026-10-29 10:51 modified: add TEL, MOBILE
        // 2026-10-29 13:13 modified: default password = USERID, IS_ACTIVE = 1 (AUTOLOGIN is not used)
        public async Task<bool> UserInsert(UserRow user)
        {
            using SqlConnection connection = Connect();

            string sql = """
                INSERT INTO USERS (USERID, USERNAME, PWD2, EMAIL, DESCRIPTION, TEL, MOBILE, IS_ACTIVE)
                VALUES (@UserId, @UserName, @UserId, @Email, @Description, @Tel, @Mobile, 1)
                """;

            int affected = await connection.ExecuteAsync(sql, user);
            return affected > 0;
        }

        // Update one user (USERID does not change)
        // 2026-10-29 10:51 modified: add TEL, MOBILE
        // 2026-10-29 13:13 modified: add IS_ACTIVE; PWD2 / AUTOLOGIN are not changed here
        public async Task<bool> UserUpdate(UserRow user)
        {
            using SqlConnection connection = Connect();

            string sql = """
                UPDATE USERS SET
                    USERNAME = @UserName,
                    EMAIL = @Email,
                    DESCRIPTION = @Description,
                    TEL = @Tel,
                    MOBILE = @Mobile,
                    IS_ACTIVE = @IsActive
                WHERE USERID = @UserId
                """;

            int affected = await connection.ExecuteAsync(sql, user);
            return affected > 0;
        }

        // 2026-10-29 13:13 added: change password (plain text in PWD2, encryption later)
        public async Task<bool> UserChangePassword(string userId, string password)
        {
            using SqlConnection connection = Connect();

            string sql = "UPDATE USERS SET PWD2 = @password WHERE USERID = @userId";

            int affected = await connection.ExecuteAsync(sql, new { userId = userId, password = password });
            return affected > 0;
        }

        // How many roles use this user? (can not delete a user who still has roles)
        public async Task<int> UserRoleCount(string userId)
        {
            using SqlConnection connection = Connect();

            string sql = "SELECT COUNT(*) FROM SYS_ROLES WHERE USER_ID = @userId";

            return await connection.ExecuteScalarAsync<int>(sql, new { userId = userId });
        }

        // Delete one user
        public async Task<bool> UserDelete(string userId)
        {
            using SqlConnection connection = Connect();

            string sql = "DELETE FROM USERS WHERE USERID = @userId";

            int affected = await connection.ExecuteAsync(sql, new { userId = userId });
            return affected > 0;
        }

        // ==================== Roles tab (List page) ====================

        // 2026-10-30 19:32 modified: RolesOfUser -> RoleList, ALL roles + user name (LEFT JOIN USERS)
        public async Task<List<RoleRow>> RoleList()
        {
            using SqlConnection connection = Connect();

            string sql = """
                SELECT SYS_ROLES.ROLE_ID AS RoleId, SYS_ROLES.ROLE_DESC AS RoleDesc,
                       SYS_ROLES.USER_ID AS UserId, USERS.USERNAME AS UserName
                FROM SYS_ROLES
                LEFT JOIN USERS ON SYS_ROLES.USER_ID = USERS.USERID
                ORDER BY SYS_ROLES.ROLE_ID
                """;

            IEnumerable<RoleRow> rows = await connection.QueryAsync<RoleRow>(sql);
            return rows.ToList();
        }

        // Is this ROLE_ID used already?
        public async Task<bool> RoleExists(string roleId)
        {
            using SqlConnection connection = Connect();

            string sql = "SELECT COUNT(*) FROM SYS_ROLES WHERE ROLE_ID = @roleId";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { roleId = roleId });
            return count > 0;
        }

        // Insert one role
        // 2026-10-30 19:32 modified: only SYS_ROLES (the group is set in 角色群組設定 ZSYSRoleGroup)
        public async Task<bool> RoleInsert(RoleRow role)
        {
            using SqlConnection connection = Connect();

            string sql = """
                INSERT INTO SYS_ROLES (ROLE_ID, ROLE_DESC, USER_ID)
                VALUES (@RoleId, @RoleDesc, @UserId)
                """;

            int affected = await connection.ExecuteAsync(sql, role);
            return affected > 0;
        }

        // Update one role (ROLE_ID does not change)
        // 2026-10-30 19:32 modified: only SYS_ROLES (the group is set in 角色群組設定 ZSYSRoleGroup)
        public async Task<bool> RoleUpdate(RoleRow role)
        {
            using SqlConnection connection = Connect();

            string sql = """
                UPDATE SYS_ROLES SET
                    ROLE_DESC = @RoleDesc,
                    USER_ID = @UserId
                WHERE ROLE_ID = @RoleId
                """;

            int affected = await connection.ExecuteAsync(sql, role);
            return affected > 0;
        }

        // 2026-10-30 19:32 removed: SaveGroupOfRole (the group is set in 角色群組設定 ZSYSRoleGroup)

        // Delete one role, and remove it from all role groups
        public async Task<bool> RoleDelete(string roleId)
        {
            using SqlConnection connection = Connect();

            string sql = """
                DELETE FROM SYS_ROLEGROUPS_D WHERE ROLE_ID = @roleId;
                DELETE FROM SYS_ROLES WHERE ROLE_ID = @roleId;
                """;

            int affected = await connection.ExecuteAsync(sql, new { roleId = roleId });
            return affected > 0;
        }

        // 2026-10-29 15:44 removed: GroupsOfRole, SaveGroupsOfRole (群組 button is removed, group is saved with the role)

        // 2026-10-30 19:32 removed: RoleGroups section (GroupList, GroupExists, GroupInsert, GroupUpdate, GroupDelete)
        // moved to RoleGroupRepository (角色群組設定 ZSYSRoleGroup)
    }

    // One row of USERS (also used by the Vue form)
    public class UserRow
    {
        public string UserId { get; set; } = "";
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Description { get; set; }
        public string? Tel { get; set; }           // 2026-10-29 10:51 added
        public string? Mobile { get; set; }        // 2026-10-29 10:51 added
        public int? IsActive { get; set; }         // 2026-10-29 13:13 added: IS_ACTIVE, 0 = not active, 1 = active
        public string? Groups { get; set; }        // 2026-10-30 21:00 added: role groups of the user, list page only
        // 2026-10-29 13:13 removed: Password, AutoLogin (password is changed by the lock button)
    }

    // 2026-10-29 13:13 added: form of the change password modal
    public class PasswordForm
    {
        public string UserId { get; set; } = "";
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
    }

    // One row of SYS_ROLES (also used by the Vue form)
    public class RoleRow
    {
        public string RoleId { get; set; } = "";
        public string? RoleDesc { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }      // 2026-10-30 19:32 added: USERS.USERNAME, only for showing
        // 2026-10-30 19:32 removed: GroupId (the group is set in 角色群組設定 ZSYSRoleGroup)
    }

    // 2026-10-30 19:32 removed: GroupRow (moved to RoleGroupRepository)
}
