// 2026-10-24 17:35 added: read login user and menu from JSERP
using Dapper;
using Microsoft.Data.SqlClient;

namespace jsERP.Services
{
    public class ErpRepository
    {
        private readonly IConfiguration _configuration;

        public ErpRepository(IConfiguration configuration)
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

        // Find the user by USERID + PWD2. Return null when not found.
        // (PWD is encrypted by the old system, so we do not use it.)
        public async Task<LoginUser?> Login(string userId, string password)
        {
            using SqlConnection connection = Connect();

            string sql = """
                SELECT USERID AS UserId, USERNAME AS UserName
                FROM USERS
                WHERE USERID = @userId AND PWD2 = @password
                """;

            var parameters = new
            {
                userId = userId,
                password = password
            };

            LoginUser? user = await connection.QueryFirstOrDefaultAsync<LoginUser>(sql, parameters);
            return user;
        }

        // Menu of one user:
        // USERS -> SYS_ROLES -> SYS_ROLEGROUPS_D -> SYS_PROGRAM_D -> SYS_PROGRAM   2026-10-25 18:00 modified
        public async Task<List<MenuRow>> Menu(string userId)
        {
            using SqlConnection connection = Connect();

            // DISTINCT: one user may belong to many role groups
            // string sql = """
            //     SELECT DISTINCT p.ROW_ID, p.FORM_ID, p.FORM_CAPTION, p.PARENT_NAME, p.SORT_NO,
            //            p.CONTROLLER_NAME, p.ACTION_NAME, p.ROUTE_NAME
            //     FROM USERS u
            //     JOIN SYS_ROLES r         ON r.USER_ID = u.USERID
            //     JOIN SYS_ROLEGROUPS_D gd ON gd.ROLE_ID = r.ROLE_ID
            //     JOIN SYS_SECURITY s      ON s.ROLEGROUP_ID = gd.ROLEGROUP_ID AND s.ACTIVE = 'Y'
            //     JOIN SYS_PROGRAM p       ON p.FORM_ID = s.FORM_NAME
            //     WHERE u.USERID = @userId AND p.IS_ENABLED = 1
            //     ORDER BY p.SORT_NO, p.FORM_ID
            //     """;

            // 2026-10-25 16:00 modified: PROGRAM_ID -> ROW_ID
            // 2026-10-25 16:30 modified: FORM_NAME -> FORM_ID
            // string sql = """
            //        SELECT DISTINCT p.ROW_ID, p.FORM_ID, p.FORM_CAPTION, p.PARENT_NAME, p.SORT_NO,
            //        p.CONTROLLER_NAME, p.ACTION_NAME, p.ROUTE_NAME
            //        FROM SYS_PROGRAM p
            //        WHERE p.IS_ENABLED=1
            //        ORDER BY p.SORT_NO
            // """;

            // 2026-10-25 18:00 modified: use stored procedure SP_MENU (Database/04_sp_menu.sql)
            // USERS -> SYS_ROLES -> SYS_ROLEGROUPS_D -> SYS_PROGRAM_D -> SYS_PROGRAM
            var parameters = new { USER_ID = userId };

            IEnumerable<MenuRow> rows = await connection.QueryAsync<MenuRow>(
                "dbo.SP_MENU",
                parameters,
                commandType: System.Data.CommandType.StoredProcedure);
            return rows.ToList();
        }
    }

    // Login result
    public class LoginUser
    {
        public string UserId { get; set; } = "";
        public string? UserName { get; set; }
    }

    // One row of SYS_PROGRAM (property names = column names)
    public class MenuRow
    {
        public int ROW_ID { get; set; } // 2026-10-25 16:00 renamed from PROGRAM_ID
        public string? FORM_ID { get; set; } // 2026-10-25 16:30 renamed from FORM_NAME
        public string? FORM_CAPTION { get; set; }
        public string? PARENT_NAME { get; set; }
        public string? SORT_NO { get; set; }
        public string? CONTROLLER_NAME { get; set; }
        public string? ACTION_NAME { get; set; }
        public string? ROUTE_NAME { get; set; }
    }
}
