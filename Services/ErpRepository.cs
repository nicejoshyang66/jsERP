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


            // 2026-10-25 18:00 modified: use stored procedure SP_MENU (Database/04_sp_menu.sql)
            // USERS -> SYS_ROLES -> SYS_ROLEGROUPS_D -> SYS_PROGRAM_D -> SYS_PROGRAM
            var parameters = new { USER_ID = userId };

            IEnumerable<MenuRow> rows = await connection.QueryAsync<MenuRow>(
                "dbo.SP_MENU",
                parameters,
                commandType: System.Data.CommandType.StoredProcedure);
            return rows.ToList();
        }

        // 2026-10-28 09:40 added: SYS_PROGRAM master list (top menus, no parent)
        // 2026-10-28 14:00 modified: remove CODE_NO, also show disabled masters (so they can be edited)
        public async Task<List<ProgramRow>> ProgramMasterList()
        {
            using SqlConnection connection = Connect();

            string sql = """
                SELECT ROW_ID, FORM_ID, FORM_CAPTION, SORT_NO, REMARK, IS_ENABLED
                FROM SYS_PROGRAM
                WHERE ISNULL(PARENT_ID, '') = ''
                ORDER BY SORT_NO, FORM_ID
                """;

            IEnumerable<ProgramRow> rows = await connection.QueryAsync<ProgramRow>(sql);
            return rows.ToList();
        }

        // 2026-10-28 14:00 added: one master by FORM_ID (for the ListDet page)
        public async Task<ProgramRow?> ProgramMasterOne(string formId)
        {
            using SqlConnection connection = Connect();

            string sql = """
                SELECT ROW_ID, FORM_ID, FORM_CAPTION, SORT_NO, REMARK, IS_ENABLED
                FROM SYS_PROGRAM
                WHERE FORM_ID = @formId AND ISNULL(PARENT_ID, '') = ''
                """;

            var parameters = new { formId = formId };

            return await connection.QueryFirstOrDefaultAsync<ProgramRow>(sql, parameters);
        }

        // 2026-10-28 09:40 added: SYS_PROGRAM detail list (children of one master)
        public async Task<List<ProgramRow>> ProgramDetailList(string parentId)
        {
            using SqlConnection connection = Connect();

            string sql = """
                SELECT ROW_ID, FORM_ID, FORM_CAPTION, SORT_NO,
                       CONTROLLER_NAME, ACTION_NAME, ROUTE_NAME, REMARK, IS_ENABLED
                FROM SYS_PROGRAM
                WHERE PARENT_ID = @parentId
                ORDER BY SORT_NO, FORM_ID
                """;

            var parameters = new { parentId = parentId };

            IEnumerable<ProgramRow> rows = await connection.QueryAsync<ProgramRow>(sql, parameters);
            return rows.ToList();
        }

        // 2026-10-28 10:30 added: is this FORM_ID used already?
        public async Task<bool> ProgramExists(string formId)
        {
            using SqlConnection connection = Connect();

            string sql = "SELECT COUNT(*) FROM SYS_PROGRAM WHERE FORM_ID = @formId";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { formId = formId });
            return count > 0;
        }

        // 2026-10-28 10:30 added: insert one program (master: PARENT_ID = '')
        // TR_SYS_PROGRAM adds the SYS_PROGRAM_D rows.
        public async Task<bool> ProgramInsert(ProgramForm form)
        {
            using SqlConnection connection = Connect();

            string sql = """
                INSERT INTO SYS_PROGRAM
                    (FORM_ID, FORM_CAPTION, PARENT_ID, SORT_NO, CONTROLLER_NAME, ACTION_NAME,
                     ROUTE_NAME, IS_ENABLED, REMARK)
                VALUES
                    (@FormId, @FormCaption, @ParentId, @SortNo, @ControllerName, @ActionName,
                     @RouteName, @IsEnabled, @Remark)
                """; // 2026-10-28 14:00 modified: remove CODE_NO

            int affected = await connection.ExecuteAsync(sql, form);
            return affected > 0;
        }

        // 2026-10-28 10:30 added: update one program (FORM_ID and PARENT_ID do not change)
        public async Task<bool> ProgramUpdate(ProgramForm form)
        {
            using SqlConnection connection = Connect();

            string sql = """
                UPDATE SYS_PROGRAM SET
                    FORM_CAPTION = @FormCaption,
                    SORT_NO = @SortNo,
                    CONTROLLER_NAME = @ControllerName,
                    ACTION_NAME = @ActionName,
                    ROUTE_NAME = @RouteName,
                    IS_ENABLED = @IsEnabled,
                    REMARK = @Remark
                WHERE ROW_ID = @RowId
                """; // 2026-10-28 14:00 modified: remove CODE_NO

            int affected = await connection.ExecuteAsync(sql, form);
            return affected > 0;
        }

        // 2026-10-28 10:30 added: delete one program and its children
        // TR_SYS_PROGRAM deletes the SYS_PROGRAM_D rows.
        public async Task<bool> ProgramDelete(int rowId)
        {
            using SqlConnection connection = Connect();

            string sql = """
                DELETE FROM SYS_PROGRAM
                WHERE PARENT_ID = (SELECT FORM_ID FROM SYS_PROGRAM WHERE ROW_ID = @rowId);

                DELETE FROM SYS_PROGRAM
                WHERE ROW_ID = @rowId;
                """;

            int affected = await connection.ExecuteAsync(sql, new { rowId = rowId });
            return affected > 0;
        }
    }

    // 2026-10-28 10:30 added: data sent from the Vue form (create / edit)
    // 2026-10-28 14:00 modified: remove CodeNo
    public class ProgramForm
    {
        public int RowId { get; set; }
        public string FormId { get; set; } = "";
        public string FormCaption { get; set; } = "";
        public string ParentId { get; set; } = "";
        public string SortNo { get; set; } = "";
        public string ControllerName { get; set; } = "";
        public string ActionName { get; set; } = "";
        public string RouteName { get; set; } = "";
        public int IsEnabled { get; set; } = 1;
        public string Remark { get; set; } = "";
    }

    // 2026-10-28 09:40 added: one row of SYS_PROGRAM for the maintain page
    // 2026-10-28 14:00 modified: remove CODE_NO
    public class ProgramRow
    {
        public int ROW_ID { get; set; }
        public string? FORM_ID { get; set; }
        public string? FORM_CAPTION { get; set; }
        public string? SORT_NO { get; set; }
        public string? CONTROLLER_NAME { get; set; }
        public string? ACTION_NAME { get; set; }
        public string? ROUTE_NAME { get; set; }
        public string? REMARK { get; set; }
        public int IS_ENABLED { get; set; } // 2026-10-28 10:30 added
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
        public string? PARENT_ID { get; set; } // 2026-10-28 09:24 renamed from PARENT_NAME
        public string? SORT_NO { get; set; }
        public string? CONTROLLER_NAME { get; set; }
        public string? ACTION_NAME { get; set; }
        public string? ROUTE_NAME { get; set; }
    }
}
