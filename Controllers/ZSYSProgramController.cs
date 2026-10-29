// 2026-10-28 09:40 added: 系統程式資料維護 (SYS_PROGRAM master / detail)
using jsERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace jsERP.Controllers
{
    [Authorize]
    public class ZSYSProgramController : Controller
    {
        private readonly ErpRepository _repository;

        public ZSYSProgramController(ErpRepository repository)
        {
            _repository = repository;
        }

        // GET /ZSYSProgram/List : the page (data is loaded by Vue)
        public IActionResult List()
        {
            return View();
        }

        // GET /ZSYSProgram/MasterList : top menus (PARENT_ID is empty)
        public async Task<IActionResult> MasterList()
        {
            List<ProgramRow> rows = await _repository.ProgramMasterList();
            return Json(ToJsonList(rows));
        }

        // 2026-10-28 14:00 added: GET /ZSYSProgram/ListDet?parentId=S01 : detail page of one master (data is loaded by Vue)
        public IActionResult ListDet(string parentId)
        {
            ViewBag.ParentId = parentId ?? "";
            return View();
        }

        // 2026-10-28 14:00 added: GET /ZSYSProgram/MasterOne?formId=S01 : one master
        public async Task<IActionResult> MasterOne(string formId)
        {
            ProgramRow? row = await _repository.ProgramMasterOne(formId ?? "");
            if (row == null)
            {
                return NotFound(new { message = "找不到主選單 " + formId + "。" });
            }

            List<ProgramRow> rows = new List<ProgramRow>();
            rows.Add(row);
            return Json(ToJsonList(rows)[0]);
        }

        // GET /ZSYSProgram/DetailList?parentId=S01 : children of one master
        public async Task<IActionResult> DetailList(string parentId)
        {
            List<ProgramRow> rows = await _repository.ProgramDetailList(parentId ?? "");
            return Json(ToJsonList(rows));
        }

        // 2026-10-28 10:30 added: POST /ZSYSProgram/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] ProgramForm form)
        {
            string message = CheckForm(form);
            if (message != "")
            {
                return BadRequest(new { message = message });
            }

            // 2026-10-28 14:00 added: detail must belong to an existing master
            if (form.ParentId != "" && await _repository.ProgramMasterOne(form.ParentId) == null)
            {
                return BadRequest(new { message = "找不到主選單 " + form.ParentId + "。" });
            }

            if (await _repository.ProgramExists(form.FormId))
            {
                return BadRequest(new { message = "代號 " + form.FormId + " 已經存在。" });
            }

            bool ok = await _repository.ProgramInsert(form);
            if (!ok)
            {
                return BadRequest(new { message = "新增失敗。" });
            }
            return Ok(new { message = "新增成功。" });
        }

        // 2026-10-28 10:30 added: POST /ZSYSProgram/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromBody] ProgramForm form)
        {
            string message = CheckForm(form);
            if (message != "")
            {
                return BadRequest(new { message = message });
            }

            bool ok = await _repository.ProgramUpdate(form);
            if (!ok)
            {
                return BadRequest(new { message = "修改失敗，找不到這筆資料。" });
            }
            return Ok(new { message = "修改成功。" });
        }

        // 2026-10-28 10:30 added: POST /ZSYSProgram/Delete?rowId=5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int rowId)
        {
            bool ok = await _repository.ProgramDelete(rowId);
            if (!ok)
            {
                return BadRequest(new { message = "刪除失敗，找不到這筆資料。" });
            }
            return Ok(new { message = "刪除成功。" });
        }

        // 2026-10-28 10:30 added: trim text and check required fields. Return "" when OK.
        // 2026-10-28 14:00 modified: remove CodeNo
        private string CheckForm(ProgramForm? form)
        {
            if (form == null)
            {
                return "資料格式錯誤。";
            }

            form.FormId = (form.FormId ?? "").Trim().ToUpper();
            form.FormCaption = (form.FormCaption ?? "").Trim();
            form.ParentId = (form.ParentId ?? "").Trim();
            form.SortNo = (form.SortNo ?? "").Trim();
            form.ControllerName = (form.ControllerName ?? "").Trim();
            form.ActionName = (form.ActionName ?? "").Trim();
            form.RouteName = (form.RouteName ?? "").Trim();
            form.Remark = (form.Remark ?? "").Trim();

            if (form.FormId == "")
            {
                return "請輸入代號。";
            }
            if (form.FormCaption == "")
            {
                return "請輸入名稱。";
            }
            return "";
        }

        // Use simple names for Vue (rowId, formId ...)
        // 2026-10-28 14:00 modified: remove codeNo
        private List<object> ToJsonList(List<ProgramRow> rows)
        {
            List<object> list = new List<object>();
            foreach (ProgramRow row in rows)
            {
                list.Add(new
                {
                    rowId = row.ROW_ID,
                    formId = row.FORM_ID ?? "",
                    formCaption = row.FORM_CAPTION ?? "",
                    sortNo = row.SORT_NO ?? "",
                    controllerName = row.CONTROLLER_NAME ?? "",
                    actionName = row.ACTION_NAME ?? "",
                    routeName = row.ROUTE_NAME ?? "",
                    remark = row.REMARK ?? "",
                    isEnabled = row.IS_ENABLED // 2026-10-28 10:30 added
                });
            }
            return list;
        }
    }
}
