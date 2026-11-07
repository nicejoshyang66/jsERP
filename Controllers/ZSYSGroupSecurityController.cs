// 2026-10-30 11:13 added: 群組權限資料維護 (rewritten from xxxBackup GroupSecurityController)
//   List : role group select on top, rights of the programs below (data is loaded by Vue)
//   存檔 : save all rows of the group,  複製群組程式 : copy the rights of another group
using jsERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace jsERP.Controllers
{
    [Authorize]
    public class ZSYSGroupSecurityController : Controller
    {
        private readonly GroupSecurityRepository _repository;

        public ZSYSGroupSecurityController(GroupSecurityRepository repository)
        {
            _repository = repository;
        }

        // GET /ZSYSGroupSecurity/List : the page (data is loaded by Vue)
        public IActionResult List()
        {
            return View();
        }

        // GET /ZSYSGroupSecurity/GroupList : all role groups
        public async Task<IActionResult> GroupList()
        {
            return Json(await _repository.GroupList());
        }

        // GET /ZSYSGroupSecurity/ProgramList?groupId=ACC : rights of one group
        public async Task<IActionResult> ProgramList(string groupId)
        {
            return Json(await _repository.ProgramList(groupId ?? ""));
        }

        // POST /ZSYSGroupSecurity/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save([FromBody] SecuritySaveForm form)
        {
            if (form == null)
            {
                return BadRequest(new { message = "資料格式錯誤。" });
            }

            form.GroupId = (form.GroupId ?? "").Trim();
            if (form.GroupId == "")
            {
                return BadRequest(new { message = "沒有選擇群組，無法存檔。" });
            }
            if (!await _repository.GroupExists(form.GroupId))
            {
                return BadRequest(new { message = "找不到群組 " + form.GroupId + "。" });
            }

            if (form.Rows == null)
            {
                form.Rows = new List<SecurityRow>();
            }

            foreach (SecurityRow row in form.Rows)
            {
                // Rights can only be 0 or 1
                row.IsView = row.IsView == 1 ? 1 : 0;
                row.IsAdd = row.IsAdd == 1 ? 1 : 0;
                row.IsEdit = row.IsEdit == 1 ? 1 : 0;
                row.IsDelete = row.IsDelete == 1 ? 1 : 0;
                row.IsPrint = row.IsPrint == 1 ? 1 : 0;
                row.IsSign = row.IsSign == 1 ? 1 : 0;
                row.Remark = (row.Remark ?? "").Trim();

                // REMARK is varchar(30): 1 Chinese word = 2 bytes
                if (TextBytes(row.Remark) > 30)
                {
                    return BadRequest(new { message = "程式 " + row.FormId + " 的備註太長（最多 15 個中文字或 30 個英數字）。" });
                }
            }

            await _repository.Save(form.GroupId, form.Rows);
            return Ok(new { message = "權限資料已存檔。" });
        }

        // POST /ZSYSGroupSecurity/Copy : copy the rights of SourceId to TargetId
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Copy([FromBody] SecurityCopyForm form)
        {
            if (form == null)
            {
                return BadRequest(new { message = "資料格式錯誤。" });
            }

            string targetId = (form.TargetId ?? "").Trim();
            string sourceId = (form.SourceId ?? "").Trim();

            if (targetId == "")
            {
                return BadRequest(new { message = "沒有目前群組，無法複製。" });
            }
            if (sourceId == "")
            {
                return BadRequest(new { message = "請選擇來源群組。" });
            }
            if (targetId == sourceId)
            {
                return BadRequest(new { message = "來源群組不可與目前群組相同。" });
            }
            if (!await _repository.GroupExists(targetId) || !await _repository.GroupExists(sourceId))
            {
                return BadRequest(new { message = "找不到群組。" });
            }

            int count = await _repository.Copy(targetId, sourceId);
            return Ok(new { message = "已將 " + sourceId + " 的權限複製到 " + targetId + "（" + count + " 筆）。" });
        }

        // Bytes of the text in varchar: English / number = 1, Chinese = 2
        private int TextBytes(string text)
        {
            int bytes = 0;
            foreach (char c in text)
            {
                if (c > 127)
                {
                    bytes += 2;
                }
                else
                {
                    bytes += 1;
                }
            }
            return bytes;
        }
    }
}
