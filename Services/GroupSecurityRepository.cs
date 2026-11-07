// 2026-10-30 11:13 added: 群組權限資料維護 (SYS_ROLEGROUPS / SYS_PROGRAM_D / SYS_PROGRAM)
// SYS_PROGRAM_D always has one row for every role group x program (TR_SYS_ROLEGROUPS / TR_SYS_PROGRAM),
// so this page only UPDATEs the rows. It never inserts or deletes them.
using Dapper;
using Microsoft.Data.SqlClient;

namespace jsERP.Services
{
    public class GroupSecurityRepository
    {
        private readonly IConfiguration _configuration;

        public GroupSecurityRepository(IConfiguration configuration)
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

        // All role groups (the 群組代號 select)
        public async Task<List<SecurityGroupRow>> GroupList()
        {
            using SqlConnection connection = Connect();

            string sql = """
                SELECT ROLEGROUP_ID AS GroupId, ROLEGROUP_DESC AS GroupDesc
                FROM SYS_ROLEGROUPS
                ORDER BY ROLEGROUP_ID
                """;

            IEnumerable<SecurityGroupRow> rows = await connection.QueryAsync<SecurityGroupRow>(sql);
            return rows.ToList();
        }

        // Is this ROLEGROUP_ID in SYS_ROLEGROUPS?
        public async Task<bool> GroupExists(string groupId)
        {
            using SqlConnection connection = Connect();

            string sql = "SELECT COUNT(*) FROM SYS_ROLEGROUPS WHERE ROLEGROUP_ID = @groupId";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { groupId = groupId });
            return count > 0;
        }

        // Rights of one role group.
        // Order: a master menu first, then its programs (by SORT_NO).
        // NULL rights are shown as 0 (not checked).
        public async Task<List<SecurityRow>> ProgramList(string groupId)
        {
            using SqlConnection connection = Connect();

            string sql = """
                SELECT D.ROW_ID AS RowId, D.FORM_ID AS FormId, P.FORM_CAPTION AS FormCaption,
                       CASE WHEN ISNULL(P.PARENT_ID, '') = '' THEN 1 ELSE 0 END AS IsMaster,
                       ISNULL(P.IS_ENABLED, 0) AS IsEnabled,
                       ISNULL(D.IS_VIEW, 0) AS IsView,
                       ISNULL(D.IS_ADD, 0) AS IsAdd,
                       ISNULL(D.IS_EDIT, 0) AS IsEdit,
                       ISNULL(D.IS_DELETE, 0) AS IsDelete,
                       ISNULL(D.IS_PRINT, 0) AS IsPrint,
                       ISNULL(D.IS_SIGN, 0) AS IsSign,
                       D.REMARK AS Remark
                FROM SYS_PROGRAM_D D
                LEFT JOIN SYS_PROGRAM P ON P.FORM_ID = D.FORM_ID
                LEFT JOIN SYS_PROGRAM M ON M.FORM_ID = P.PARENT_ID
                WHERE D.ROLEGROUP_ID = @groupId
                ORDER BY ISNULL(M.SORT_NO, P.SORT_NO), ISNULL(M.FORM_ID, D.FORM_ID),
                         CASE WHEN ISNULL(P.PARENT_ID, '') = '' THEN 0 ELSE 1 END,
                         P.SORT_NO, D.FORM_ID
                """;

            IEnumerable<SecurityRow> rows = await connection.QueryAsync<SecurityRow>(sql, new { groupId = groupId });
            return rows.ToList();
        }

        // Save the rights of one role group (one transaction).
        // ROLEGROUP_ID is in the WHERE, so rows of other groups can not be changed.
        public async Task Save(string groupId, List<SecurityRow> rows)
        {
            using SqlConnection connection = Connect();
            connection.Open();
            using SqlTransaction transaction = connection.BeginTransaction();

            string sql = """
                UPDATE SYS_PROGRAM_D SET
                    IS_VIEW = @IsView,
                    IS_ADD = @IsAdd,
                    IS_EDIT = @IsEdit,
                    IS_DELETE = @IsDelete,
                    IS_PRINT = @IsPrint,
                    IS_SIGN = @IsSign,
                    REMARK = @Remark
                WHERE ROW_ID = @RowId AND ROLEGROUP_ID = @GroupId
                """;

            foreach (SecurityRow row in rows)
            {
                await connection.ExecuteAsync(sql, new
                {
                    IsView = row.IsView,
                    IsAdd = row.IsAdd,
                    IsEdit = row.IsEdit,
                    IsDelete = row.IsDelete,
                    IsPrint = row.IsPrint,
                    IsSign = row.IsSign,
                    Remark = row.Remark,
                    RowId = row.RowId,
                    GroupId = groupId
                }, transaction);
            }

            transaction.Commit();
        }

        // Copy the rights of the source group to the target group.
        // Both groups have the same programs, so we UPDATE by FORM_ID (ROW_ID does not change).
        public async Task<int> Copy(string targetId, string sourceId)
        {
            using SqlConnection connection = Connect();

            string sql = """
                UPDATE T SET
                    IS_VIEW = S.IS_VIEW,
                    IS_ADD = S.IS_ADD,
                    IS_EDIT = S.IS_EDIT,
                    IS_DELETE = S.IS_DELETE,
                    IS_PRINT = S.IS_PRINT,
                    IS_SIGN = S.IS_SIGN,
                    REMARK = S.REMARK
                FROM SYS_PROGRAM_D T
                INNER JOIN SYS_PROGRAM_D S ON S.FORM_ID = T.FORM_ID AND S.ROLEGROUP_ID = @sourceId
                WHERE T.ROLEGROUP_ID = @targetId
                """;

            return await connection.ExecuteAsync(sql, new { targetId = targetId, sourceId = sourceId });
        }
    }

    // One row of SYS_ROLEGROUPS
    public class SecurityGroupRow
    {
        public string GroupId { get; set; } = "";
        public string? GroupDesc { get; set; }
    }

    // One row of SYS_PROGRAM_D (+ caption of SYS_PROGRAM). Rights: 0 = no, 1 = yes.
    public class SecurityRow
    {
        public int RowId { get; set; }
        public string? FormId { get; set; }
        public string? FormCaption { get; set; }   // only for showing
        public int IsMaster { get; set; }          // only for showing: 1 = master menu (no parent)
        public int IsEnabled { get; set; }         // only for showing: SYS_PROGRAM.IS_ENABLED
        public int IsView { get; set; }            // 檢視 (show in the menu)
        public int IsAdd { get; set; }             // 新增
        public int IsEdit { get; set; }            // 修改
        public int IsDelete { get; set; }          // 刪除
        public int IsPrint { get; set; }           // 列印
        public int IsSign { get; set; }            // 簽核
        public string? Remark { get; set; }        // 備註 varchar(30)
    }

    // Data of the 存檔 button
    public class SecuritySaveForm
    {
        public string GroupId { get; set; } = "";
        public List<SecurityRow> Rows { get; set; } = new List<SecurityRow>();
    }

    // Data of the 複製群組程式 modal
    public class SecurityCopyForm
    {
        public string TargetId { get; set; } = "";
        public string SourceId { get; set; } = "";
    }
}
