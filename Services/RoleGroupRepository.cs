// 2026-10-30 19:32 added: 角色群組設定 (SYS_ROLEGROUPS / SYS_ROLEGROUPS_D)
//   Left  : role groups (SYS_ROLEGROUPS)
//   Right : roles in the selected group (SYS_ROLEGROUPS_D + SYS_ROLES + USERS)
// Note: these tables have no primary key, so we check "already exists" in code.
using Dapper;
using Microsoft.Data.SqlClient;

namespace jsERP.Services
{
    public class RoleGroupRepository
    {
        private readonly IConfiguration _configuration;

        public RoleGroupRepository(IConfiguration configuration)
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

        // ==================== Left: role groups ====================

        // All role groups
        public async Task<List<GroupRow>> GroupList()
        {
            using SqlConnection connection = Connect();

            string sql = """
                SELECT ROLEGROUP_ID AS GroupId, ROLEGROUP_DESC AS GroupDesc
                FROM SYS_ROLEGROUPS
                ORDER BY ROLEGROUP_ID
                """;

            IEnumerable<GroupRow> rows = await connection.QueryAsync<GroupRow>(sql);
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

        // ==================== Right: roles in one group ====================

        // Roles in one group + user name
        public async Task<List<GroupRoleRow>> GroupRoleList(string groupId)
        {
            using SqlConnection connection = Connect();

            string sql = """
                SELECT SYS_ROLES.ROLE_ID AS RoleId, SYS_ROLES.ROLE_DESC AS RoleDesc,
                       SYS_ROLES.USER_ID AS UserId, USERS.USERNAME AS UserName
                FROM SYS_ROLEGROUPS_D
                JOIN SYS_ROLES ON SYS_ROLES.ROLE_ID = SYS_ROLEGROUPS_D.ROLE_ID
                LEFT JOIN USERS ON SYS_ROLES.USER_ID = USERS.USERID
                WHERE SYS_ROLEGROUPS_D.ROLEGROUP_ID = @groupId
                ORDER BY SYS_ROLES.ROLE_ID
                """;

            IEnumerable<GroupRoleRow> rows = await connection.QueryAsync<GroupRoleRow>(sql, new { groupId = groupId });
            return rows.ToList();
        }

        // Options of the ROLE_ID select: roles that have NO group yet.
        // keepRoleId = the role of the row being edited, it is also in the list ("" when creating).
        public async Task<List<GroupRoleRow>> FreeRoleList(string keepRoleId)
        {
            using SqlConnection connection = Connect();

            string sql = """
                SELECT SYS_ROLES.ROLE_ID AS RoleId, SYS_ROLES.ROLE_DESC AS RoleDesc,
                       SYS_ROLES.USER_ID AS UserId, USERS.USERNAME AS UserName
                FROM SYS_ROLES
                LEFT JOIN USERS ON SYS_ROLES.USER_ID = USERS.USERID
                WHERE SYS_ROLES.ROLE_ID NOT IN (SELECT ROLE_ID FROM SYS_ROLEGROUPS_D)
                   OR SYS_ROLES.ROLE_ID = @keepRoleId
                ORDER BY SYS_ROLES.ROLE_ID
                """;

            IEnumerable<GroupRoleRow> rows = await connection.QueryAsync<GroupRoleRow>(sql, new { keepRoleId = keepRoleId });
            return rows.ToList();
        }

        // Is this ROLE_ID in SYS_ROLES?
        public async Task<bool> RoleExists(string roleId)
        {
            using SqlConnection connection = Connect();

            string sql = "SELECT COUNT(*) FROM SYS_ROLES WHERE ROLE_ID = @roleId";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { roleId = roleId });
            return count > 0;
        }

        // Which group is this role in? Return null when it has no group.
        public async Task<string?> GroupOfRole(string roleId)
        {
            using SqlConnection connection = Connect();

            string sql = "SELECT TOP 1 ROLEGROUP_ID FROM SYS_ROLEGROUPS_D WHERE ROLE_ID = @roleId";

            return await connection.QueryFirstOrDefaultAsync<string?>(sql, new { roleId = roleId });
        }

        // Put one role into one group
        public async Task<bool> GroupRoleInsert(GroupRoleForm form)
        {
            using SqlConnection connection = Connect();

            string sql = """
                INSERT INTO SYS_ROLEGROUPS_D (ROLEGROUP_ID, ROLE_ID)
                VALUES (@GroupId, @RoleId)
                """;

            int affected = await connection.ExecuteAsync(sql, form);
            return affected > 0;
        }

        // Change the role of one row: OldRoleId -> RoleId
        public async Task<bool> GroupRoleUpdate(GroupRoleForm form)
        {
            using SqlConnection connection = Connect();

            string sql = """
                UPDATE SYS_ROLEGROUPS_D SET ROLE_ID = @RoleId
                WHERE ROLEGROUP_ID = @GroupId AND ROLE_ID = @OldRoleId
                """;

            int affected = await connection.ExecuteAsync(sql, form);
            return affected > 0;
        }

        // Remove one role from one group (the role in SYS_ROLES is NOT deleted)
        public async Task<bool> GroupRoleDelete(string groupId, string roleId)
        {
            using SqlConnection connection = Connect();

            string sql = "DELETE FROM SYS_ROLEGROUPS_D WHERE ROLEGROUP_ID = @groupId AND ROLE_ID = @roleId";

            int affected = await connection.ExecuteAsync(sql, new { groupId = groupId, roleId = roleId });
            return affected > 0;
        }
    }

    // One row of SYS_ROLEGROUPS (also used by the Vue form)
    public class GroupRow
    {
        public string GroupId { get; set; } = "";
        public string? GroupDesc { get; set; }
    }

    // One role in a group (right table, and options of the ROLE_ID select)
    public class GroupRoleRow
    {
        public string RoleId { get; set; } = "";
        public string? RoleDesc { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
    }

    // Form of the right side (create / edit)
    public class GroupRoleForm
    {
        public string GroupId { get; set; } = "";
        public string? OldRoleId { get; set; }     // only when editing: the role before the change
        public string RoleId { get; set; } = "";
    }
}
