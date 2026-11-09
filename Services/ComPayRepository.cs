// 2026-11-09 14:30 added: 客戶 / 供應商基本資料 (_T_COMPAY + _T_CPYCLS)
// CLASS_ID 'A' = 客戶, 'B' = 供應商. One company can be both (two rows in _T_CPYCLS).
using Dapper;
using Microsoft.Data.SqlClient;

namespace jsERP.Services
{
    public class ComPayRepository
    {
        private readonly IConfiguration _configuration;

        public ComPayRepository(IConfiguration configuration)
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

        // Companies of one class. keyword searches id / name / nick / tax id ("" = all)
        public async Task<List<ComPayRow>> List(string classId, string keyword)
        {
            using SqlConnection connection = Connect();

            string sql = """
                SELECT C.DEBIT_ID AS DebitId, C.DEBIT_NAME AS DebitName, C.DEBIT_NICK AS DebitNick,
                       C.TAX_ID AS TaxId, C.CAREER_PIC AS CareerPic, C.CONTACT AS Contact,
                       C.ADDRESS AS Address, C.MAILING_ADDRESS AS MailingAddress,
                       C.BILLING_ADDRESS AS BillingAddress, C.TEL AS Tel, C.FAX AS Fax,
                       C.EMAIL AS Email, CAST(C.REMARK AS NVARCHAR(MAX)) AS Remark
                FROM _T_COMPAY AS C
                JOIN _T_CPYCLS AS L ON L.DEBIT_ID = C.DEBIT_ID
                WHERE L.CLASS_ID = @classId
                  AND (@keyword = ''
                       OR C.DEBIT_ID   LIKE '%' + @keyword + '%'
                       OR C.DEBIT_NAME LIKE '%' + @keyword + '%'
                       OR C.DEBIT_NICK LIKE '%' + @keyword + '%'
                       OR C.TAX_ID     LIKE '%' + @keyword + '%')
                ORDER BY C.DEBIT_ID
                """;

            IEnumerable<ComPayRow> rows = await connection.QueryAsync<ComPayRow>(sql,
                new { classId = classId, keyword = keyword });
            return rows.ToList();
        }

        // Is this DEBIT_ID in _T_COMPAY already? (any class)
        public async Task<bool> CompanyExists(string debitId)
        {
            using SqlConnection connection = Connect();

            string sql = "SELECT COUNT(*) FROM _T_COMPAY WHERE DEBIT_ID = @debitId";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { debitId = debitId });
            return count > 0;
        }

        // Does this company already have this class?
        public async Task<bool> ClassExists(string debitId, string classId)
        {
            using SqlConnection connection = Connect();

            string sql = "SELECT COUNT(*) FROM _T_CPYCLS WHERE DEBIT_ID = @debitId AND CLASS_ID = @classId";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { debitId = debitId, classId = classId });
            return count > 0;
        }

        // Insert a new company + its class
        public async Task Insert(ComPayRow row, string classId)
        {
            using SqlConnection connection = Connect();

            string sql = """
                INSERT INTO _T_COMPAY
                    (DEBIT_ID, DEBIT_NAME, DEBIT_NICK, TAX_ID, CAREER_PIC, CONTACT,
                     ADDRESS, MAILING_ADDRESS, BILLING_ADDRESS, TEL, FAX, EMAIL, REMARK)
                VALUES
                    (@DebitId, @DebitName, @DebitNick, @TaxId, @CareerPic, @Contact,
                     @Address, @MailingAddress, @BillingAddress, @Tel, @Fax, @Email, @Remark);

                INSERT INTO _T_CPYCLS (DEBIT_ID, CLASS_ID) VALUES (@DebitId, @ClassId);
                """;

            await connection.ExecuteAsync(sql, new
            {
                row.DebitId, row.DebitName, row.DebitNick, row.TaxId, row.CareerPic, row.Contact,
                row.Address, row.MailingAddress, row.BillingAddress, row.Tel, row.Fax, row.Email, row.Remark,
                ClassId = classId
            });
        }

        // The company exists already (other class): only add this class
        public async Task InsertClass(string debitId, string classId)
        {
            using SqlConnection connection = Connect();

            string sql = "INSERT INTO _T_CPYCLS (DEBIT_ID, CLASS_ID) VALUES (@debitId, @classId)";

            await connection.ExecuteAsync(sql, new { debitId = debitId, classId = classId });
        }

        // Update one company (DEBIT_ID does not change)
        public async Task<bool> Update(ComPayRow row)
        {
            using SqlConnection connection = Connect();

            string sql = """
                UPDATE _T_COMPAY SET
                    DEBIT_NAME = @DebitName,
                    DEBIT_NICK = @DebitNick,
                    TAX_ID = @TaxId,
                    CAREER_PIC = @CareerPic,
                    CONTACT = @Contact,
                    ADDRESS = @Address,
                    MAILING_ADDRESS = @MailingAddress,
                    BILLING_ADDRESS = @BillingAddress,
                    TEL = @Tel,
                    FAX = @Fax,
                    EMAIL = @Email,
                    REMARK = @Remark
                WHERE DEBIT_ID = @DebitId
                """;

            int affected = await connection.ExecuteAsync(sql, row);
            return affected > 0;
        }

        // Delete the company and ALL its classes (客戶 and 供應商)
        public async Task<bool> Delete(string debitId)
        {
            using SqlConnection connection = Connect();

            string sql = """
                DELETE FROM _T_CPYCLS WHERE DEBIT_ID = @debitId;
                DELETE FROM _T_COMPAY WHERE DEBIT_ID = @debitId;
                """;

            int affected = await connection.ExecuteAsync(sql, new { debitId = debitId });
            return affected > 0;
        }
    }

    // One row of _T_COMPAY (only the 13 columns of the page, also used by the Vue form)
    public class ComPayRow
    {
        public string DebitId { get; set; } = "";
        public string? DebitName { get; set; }
        public string? DebitNick { get; set; }
        public string? TaxId { get; set; }
        public string? CareerPic { get; set; }
        public string? Contact { get; set; }
        public string? Address { get; set; }
        public string? MailingAddress { get; set; }
        public string? BillingAddress { get; set; }
        public string? Tel { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? Remark { get; set; }
    }
}
