// 2026-11-09 14:30 added: 客戶 / 供應商基本資料 (one controller for both)
// The menu sends CLASSID in the query string (SYS_PROGRAM.ROUTE_NAME):
//   /BASComPay/List?CLASSID=A  -> 客戶
//   /BASComPay/List?CLASSID=B  -> 供應商
using jsERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace jsERP.Controllers
{
    [Authorize]
    public class BASComPayController : Controller
    {
        private readonly ComPayRepository _repository;

        public BASComPayController(ComPayRepository repository)
        {
            _repository = repository;
        }

        // GET /BASComPay/List?CLASSID=A : the page (data is loaded by Vue)
        public IActionResult List(string classId)
        {
            classId = CheckClassId(classId);
            if (classId == "")
            {
                return BadRequest("CLASSID must be A or B.");
            }

            ViewData["ClassId"] = classId;
            ViewData["ClassName"] = classId == "A" ? "客戶" : "供應商";
            return View();
        }

        // GET /BASComPay/Rows?classId=A&keyword=xxx
        public async Task<IActionResult> Rows(string classId, string keyword)
        {
            classId = CheckClassId(classId);
            if (classId == "")
            {
                return BadRequest(new { message = "CLASSID must be A or B." });
            }

            return Json(await _repository.List(classId, (keyword ?? "").Trim()));
        }

        // POST /BASComPay/Create?classId=A
        // If DEBIT_ID already exists (it is the other class), only add this class.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string classId, [FromBody] ComPayRow row)
        {
            classId = CheckClassId(classId);
            if (classId == "")
            {
                return BadRequest(new { message = "CLASSID must be A or B." });
            }

            string message = CheckRow(row);
            if (message != "")
            {
                return BadRequest(new { message = message });
            }

            if (await _repository.ClassExists(row.DebitId, classId))
            {
                return BadRequest(new { message = "代號 " + row.DebitId + " 已經存在。" });
            }

            if (await _repository.CompanyExists(row.DebitId))
            {
                // Company is there already: keep its data, only add the class
                await _repository.InsertClass(row.DebitId, classId);
                return Ok(new { message = "代號 " + row.DebitId + " 已是其他類別的公司，已加入此類別 (公司資料不變)。" });
            }

            await _repository.Insert(row, classId);
            return Ok(new { message = "新增成功。" });
        }

        // POST /BASComPay/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromBody] ComPayRow row)
        {
            string message = CheckRow(row);
            if (message != "")
            {
                return BadRequest(new { message = message });
            }

            bool ok = await _repository.Update(row);
            if (!ok)
            {
                return BadRequest(new { message = "修改失敗，找不到這筆資料。" });
            }
            return Ok(new { message = "修改成功。" });
        }

        // POST /BASComPay/Delete?debitId=02371687
        // Deletes _T_COMPAY and all its rows in _T_CPYCLS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string debitId)
        {
            bool ok = await _repository.Delete((debitId ?? "").Trim());
            if (!ok)
            {
                return BadRequest(new { message = "刪除失敗，找不到這筆資料。" });
            }
            return Ok(new { message = "刪除成功。" });
        }

        // Only 'A' or 'B'. Quotes are removed, so CLASSID='A' also works. Return "" when wrong.
        private string CheckClassId(string? classId)
        {
            classId = (classId ?? "").Replace("'", "").Trim().ToUpper();
            if (classId == "A" || classId == "B")
            {
                return classId;
            }
            return "";
        }

        // Trim text and check required fields. Return "" when OK.
        private string CheckRow(ComPayRow? row)
        {
            if (row == null)
            {
                return "資料格式錯誤。";
            }

            row.DebitId = (row.DebitId ?? "").Trim();
            row.DebitName = (row.DebitName ?? "").Trim();
            row.DebitNick = (row.DebitNick ?? "").Trim();
            row.TaxId = (row.TaxId ?? "").Trim();
            row.CareerPic = (row.CareerPic ?? "").Trim();
            row.Contact = (row.Contact ?? "").Trim();
            row.Address = (row.Address ?? "").Trim();
            row.MailingAddress = (row.MailingAddress ?? "").Trim();
            row.BillingAddress = (row.BillingAddress ?? "").Trim();
            row.Tel = (row.Tel ?? "").Trim();
            row.Fax = (row.Fax ?? "").Trim();
            row.Email = (row.Email ?? "").Trim();
            row.Remark = (row.Remark ?? "").Trim();

            if (row.DebitId == "")
            {
                return "請輸入代號。";
            }
            if (row.DebitNick == "")
            {
                return "請輸入簡稱。";
            }
            return "";
        }
    }
}
