// 2026-10-28 15:00 added: 角色資料維護 (USERS / SYS_ROLES / SYS_ROLEGROUPS / SYS_ROLEGROUPS_D)
// 2026-10-29 10:51 modified: 2 pages - List (users) and ListDet (roles / role groups of one user)
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

            string sql = """
                SELECT USERID AS UserId, USERNAME AS UserName,
                       EMAIL AS Email, DESCRIPTION AS Description,
                       TEL AS Tel, MOBILE AS Mobile, IS_ACTIVE AS IsActive
                FROM USERS
                ORDER BY USERID
                """;

            IEnumerable<UserRow> rows = await connection.QueryAsync<UserRow>(sql);
            return rows.ToList();
        }

        // 2026-10-29 10:51 added: one user (top of the ListDet page). Return null when not found.
        public async Task<UserRow?> UserOne(string userId)
        {
            using SqlConnection connection = Connect();

            string sql = """
                SELECT USERID AS UserId, USERNAME AS UserName, DESCRIPTION AS Description,
                       TEL AS Tel, MOBILE AS Mobile
                FROM USERS
                WHERE USERID = @userId
                """;

            return await connection.QueryFirstOrDefaultAsync<UserRow>(sql, new { userId = userId });
        }

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

        // ==================== Roles tab (ListDet page) ====================

        // Roles of one user
        // 2026-10-29 10:51 modified: also return the groups of each role, e.g. "MAK, STK"
        // 2026-10-29 15:44 modified: one role has one group, so return GroupId
        public async Task<List<RoleRow>> RolesOfUser(string userId)
        {
            using SqlConnection connection = Connect();

            string sql = """
                SELECT R.ROLE_ID AS RoleId, R.ROLE_DESC AS RoleDesc, R.USER_ID AS UserId,
                       (SELECT TOP 1 D.ROLEGROUP_ID
                        FROM SYS_ROLEGROUPS_D D
                        WHERE D.ROLE_ID = R.ROLE_ID) AS GroupId
                FROM SYS_ROLES R
                WHERE R.USER_ID = @userId
                ORDER BY R.ROLE_ID
                """;

            IEnumerable<RoleRow> rows = await connection.QueryAsync<RoleRow>(sql, new { userId = userId });
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
        // 2026-10-29 15:44 modified: also save its group (one transaction)
        public async Task<bool> RoleInsert(RoleRow role)
        {
            using SqlConnection connection = Connect();
            connection.Open();
            using SqlTransaction transaction = connection.BeginTransaction();

            string sql = """
                INSERT INTO SYS_ROLES (ROLE_ID, ROLE_DESC, USER_ID)
                VALUES (@RoleId, @RoleDesc, @UserId)
                """;

            int affected = await connection.ExecuteAsync(sql, role, transaction);
            await SaveGroupOfRole(connection, transaction, role);

            transaction.Commit();
            return affected > 0;
        }

        // Update one role (ROLE_ID does not change)
        // 2026-10-29 15:44 modified: also save its group (one transaction)
        public async Task<bool> RoleUpdate(RoleRow role)
        {
            using SqlConnection connection = Connect();
            connection.Open();
            using SqlTransaction transaction = connection.BeginTransaction();

            string sql = """
                UPDATE SYS_ROLES SET
                    ROLE_DESC = @RoleDesc,
                    USER_ID = @UserId
                WHERE ROLE_ID = @RoleId
                """;

            int affected = await connection.ExecuteAsync(sql, role, transaction);
            if (affected == 0)
            {
                // role not found, nothing to save
                transaction.Rollback();
                return false;
            }

            await SaveGroupOfRole(connection, transaction, role);

            transaction.Commit();
            return true;
        }

        // 2026-10-29 15:44 added: save the group of one role
        // GroupId = ""  -> DELETE the row
        // has a row     -> UPDATE it
        // no row        -> INSERT one
        private async Task SaveGroupOfRole(SqlConnection connection, SqlTransaction transaction, RoleRow role)
        {
            if (role.GroupId == "")
            {
                string deleteSql = "DELETE FROM SYS_ROLEGROUPS_D WHERE ROLE_ID = @RoleId";
                await connection.ExecuteAsync(deleteSql, role, transaction);
                return;
            }

            string updateSql = "UPDATE SYS_ROLEGROUPS_D SET ROLEGROUP_ID = @GroupId WHERE ROLE_ID = @RoleId";
            int affected = await connection.ExecuteAsync(updateSql, role, transaction);

            if (affected == 0)
            {
                string insertSql = "INSERT INTO SYS_ROLEGROUPS_D (ROLEGROUP_ID, ROLE_ID) VALUES (@GroupId, @RoleId)";
                await connection.ExecuteAsync(insertSql, role, transaction);
            }
        }

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

        // ==================== RoleGroups tab (ListDet page) ====================

        // 2026-10-29 10:51 modified: all role groups, and which roles of this user are in each group
        // UserRoleIds is empty when the user is not in the group.
        public async Task<List<GroupRow>> GroupList(string userId)
        {
            using SqlConnection connection = Connect();

            string sql = """
                SELECT G.ROLEGROUP_ID AS GroupId, G.ROLEGROUP_DESC AS GroupDesc,
                       (SELECT STRING_AGG(D.ROLE_ID, ', ')
                        FROM SYS_ROLEGROUPS_D D
                        INNER JOIN SYS_ROLES R ON R.ROLE_ID = D.ROLE_ID
                        WHERE D.ROLEGROUP_ID = G.ROLEGROUP_ID AND R.USER_ID = @userId) AS UserRoleIds
                FROM SYS_ROLEGROUPS G
                ORDER BY G.ROLEGROUP_ID
                """;

            IEnumerable<GroupRow> rows = await connection.QueryAsync<GroupRow>(sql, new { userId = userId });
            return rows.ToList();
        }

        // Is this ROLEGROUP_ID used already?
        public async Task<bool> GroupExists(string groupId)
        {
            using SqlConnection connection = Connect();

            string sql = "SELECT COUNT(*) FROM SYS_ROLEGROUPS WHERE ROLEGROUP_ID = @groupId";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { groupId = groupId });
            return count > 0;
        }

        // Insert one role group. TR_SYS_ROLEGROUPS adds the SYS_PROGRAM_D rows.
        public async Task<bool> GroupInsert(GroupRow group)
        {
            using SqlConnection connection = Connect();

            string sql = """
                INSERT INTO SYS_ROLEGROUPS (ROLEGROUP_ID, ROLEGROUP_DESC)
                VALUES (@GroupId, @GroupDesc)
                """;

            int affected = await connection.ExecuteAsync(sql, group);
            return affected > 0;
        }

        // Update one role group (ROLEGROUP_ID does not change)
        public async Task<bool> GroupUpdate(GroupRow group)
        {
            using SqlConnection connection = Connect();

            string sql = """
                UPDATE SYS_ROLEGROUPS SET ROLEGROUP_DESC = @GroupDesc
                WHERE ROLEGROUP_ID = @GroupId
                """;

            int affected = await connection.ExecuteAsync(sql, group);
            return affected > 0;
        }

        // Delete one role group and its roles. TR_SYS_ROLEGROUPS deletes the SYS_PROGRAM_D rows.
        public async Task<bool> GroupDelete(string groupId)
        {
            using SqlConnection connection = Connect();

            string sql = """
                DELETE FROM SYS_ROLEGROUPS_D WHERE ROLEGROUP_ID = @groupId;
                DELETE FROM SYS_ROLEGROUPS WHERE ROLEGROUP_ID = @groupId;
                """;

            int affected = await connection.ExecuteAsync(sql, new { groupId = groupId });
            return affected > 0;
        }

        // 2026-10-29 10:51 removed: RoleList, RolesOfGroup, GroupRoleExists, GroupRoleInsert, GroupRoleDelete
        // (role <-> group is now set by the 群組 button of the Roles tab)
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
        public string? GroupId { get; set; }       // 2026-10-29 15:44 modified: GroupIds -> GroupId, one role has one group ("" = none)
    }

    // One row of SYS_ROLEGROUPS (also used by the Vue form)
    public class GroupRow
    {
        public string GroupId { get; set; } = "";
        public string? GroupDesc { get; set; }
        public string? UserRoleIds { get; set; }   // 2026-10-29 10:51 added: roles of this user in the group, only for showing
        // 2026-10-29 15:44 removed: IsChecked (群組 button is removed)
    }

    // 2026-10-29 15:44 removed: RoleGroupsForm (群組 button is removed)
}
