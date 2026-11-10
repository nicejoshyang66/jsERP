
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

        //get/bascompay/list?classid=a: the page (data is loaded by vue)

        public IActionResult List(string classId)
        {
            classId = CheckClassId(classId);
            if (classId == "")
            {
                return BadRequest("classid must be A or B");
            }
            ViewData["ClassId"] = classId;
            ViewData["ClassName"] = classId == "A" ? "客戶" : "供應商";
            return View();

        }

        //get /bascompay/rows?classid=a&keyword=xxx
        public async Task<IActionResult> Rows(string classId, string keyword)
        {
            classId = CheckClassId(classId);
            if (classId == "")
            {
                return BadRequest(new { message = "CLASSID must A or B" });
            }
            return Json(await _repository.List(classId, (keyword ?? "").Trim()));
        }

        //post /bascompay/create?classid=a
        //if debit_is already exists (its the other class),  only ass this class

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string classId, [FromBody] ComPayRow row)
        {
            classId = CheckClassId(classId);
            if (classId == "")
            {
                return BadRequest(new { message = "CLASSID must be A or B" });
            }

            string message = CheckRow(row);
            if (message != "")
            {
                return BadRequest(new { message = message });
            }

            if (await _repository.CompanyExists(row.DebitId))
            {
                await _repository.InsertClass(row.DebitId, classId);
                return Ok(new { message = "代號" + row.DebitId + "已是其他類別的公司,已加入此類別 (公司資料不變)。" });
            }

            await _repository.Insert(row, classId);
            return Ok(new { message = "新增成功" });

        }
        //update
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

        //post/bascompay/delete?debitid
        //delete _t_compay and all its rows un _t_cpycls

        public async Task<IActionResult> Delete(string debitid)
        {
            bool ok = await _repository.Delete((debitid ?? "").Trim());
            return Ok(new { message = "刪除成功。" });
        }

        // ONLY A OR B, quotes are removed, so classid='a' and also works, return "" when wrong
        private string CheckClassId(string? classId)
        {
            classId = (classId ?? "").Replace("'", "").Trim().ToUpper();
            if (classId == "A" || classId == "B")
            {
                return classId;
            }
            return "";
        }

        private string CheckRow(ComPayRow? row)
        {
            if (row == null)
            {
                return "資料格式錯誤";
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
                return "請輸入代號";
            }
            if (row.DebitNick == "")
            {
                return "請輸入簡稱";
            }

            return "";

        }




    }
}