// 2026-10-30 19:32 added: 角色群組設定 (moved out of ZSYSRole/ListDet RoleGroups tab)
//   Left  : SYS_ROLEGROUPS add / edit / delete
//   Right : click a group -> roles in this group (SYS_ROLEGROUPS_D) add / edit / delete
using jsERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace jsERP.Controllers
{
    [Authorize]
    public class ZSYSRoleGroupController : Controller
    {
        private readonly RoleGroupRepository _repository;

        public ZSYSRoleGroupController(RoleGroupRepository repository)
        {
            _repository = repository;
        }

        // GET /ZSYSRoleGroup/List : the page (data is loaded by Vue)
        public IActionResult List()
        {
            return View();
        }

        // ==================== Left: role groups ====================

        // GET /ZSYSRoleGroup/GroupList
        public async Task<IActionResult> GroupList()
        {
            return Json(await _repository.GroupList());
        }

        // POST /ZSYSRoleGroup/GroupCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GroupCreate([FromBody] GroupRow group)
        {
            string message = CheckGroup(group);
            if (message != "")
            {
                return BadRequest(new { message = message });
            }

            if (await _repository.GroupExists(group.GroupId))
            {
                return BadRequest(new { message = "群組 " + group.GroupId + " 已經存在。" });
            }

            await _repository.GroupInsert(group);
            return Ok(new { message = "新增成功。" });
        }

        // POST /ZSYSRoleGroup/GroupUpdate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GroupUpdate([FromBody] GroupRow group)
        {
            string message = CheckGroup(group);
            if (message != "")
            {
                return BadRequest(new { message = message });
            }

            bool ok = await _repository.GroupUpdate(group);
            if (!ok)
            {
                return BadRequest(new { message = "修改失敗，找不到這筆資料。" });
            }
            return Ok(new { message = "修改成功。" });
        }

        // POST /ZSYSRoleGroup/GroupDelete?groupId=QC
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GroupDelete(string groupId)
        {
            bool ok = await _repository.GroupDelete(groupId ?? "");
            if (!ok)
            {
                return BadRequest(new { message = "刪除失敗，找不到這筆資料。" });
            }
            return Ok(new { message = "刪除成功。" });
        }

        // Trim text and check required fields. Return "" when OK.
        private string CheckGroup(GroupRow? group)
        {
            if (group == null)
            {
                return "資料格式錯誤。";
            }

            group.GroupId = (group.GroupId ?? "").Trim().ToUpper();
            group.GroupDesc = (group.GroupDesc ?? "").Trim();

            if (group.GroupId == "")
            {
                return "請輸入群組代號。";
            }
            return "";
        }

        // ==================== Right: roles in one group ====================

        // GET /ZSYSRoleGroup/GroupRoleList?groupId=ACC
        public async Task<IActionResult> GroupRoleList(string groupId)
        {
            return Json(await _repository.GroupRoleList(groupId ?? ""));
        }

        // GET /ZSYSRoleGroup/FreeRoleList?keepRoleId=ACC01 : options of the ROLE_ID select
        public async Task<IActionResult> FreeRoleList(string keepRoleId)
        {
            return Json(await _repository.FreeRoleList(keepRoleId ?? ""));
        }

        // POST /ZSYSRoleGroup/GroupRoleCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GroupRoleCreate([FromBody] GroupRoleForm form)
        {
            string message = await CheckGroupRole(form);
            if (message != "")
            {
                return BadRequest(new { message = message });
            }

            await _repository.GroupRoleInsert(form);
            return Ok(new { message = "新增成功。" });
        }

        // POST /ZSYSRoleGroup/GroupRoleUpdate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GroupRoleUpdate([FromBody] GroupRoleForm form)
        {
            string message = await CheckGroupRole(form);
            if (message != "")
            {
                return BadRequest(new { message = message });
            }

            bool ok = await _repository.GroupRoleUpdate(form);
            if (!ok)
            {
                return BadRequest(new { message = "修改失敗，找不到這筆資料。" });
            }
            return Ok(new { message = "修改成功。" });
        }

        // POST /ZSYSRoleGroup/GroupRoleDelete?groupId=ACC&roleId=ACC01
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GroupRoleDelete(string groupId, string roleId)
        {
            bool ok = await _repository.GroupRoleDelete(groupId ?? "", roleId ?? "");
            if (!ok)
            {
                return BadRequest(new { message = "刪除失敗，找不到這筆資料。" });
            }
            return Ok(new { message = "刪除成功。" });
        }

        // Check the form of the right side. Return "" when OK.
        private async Task<string> CheckGroupRole(GroupRoleForm? form)
        {
            if (form == null)
            {
                return "資料格式錯誤。";
            }

            form.GroupId = (form.GroupId ?? "").Trim();
            form.OldRoleId = (form.OldRoleId ?? "").Trim();
            form.RoleId = (form.RoleId ?? "").Trim();

            if (!await _repository.GroupExists(form.GroupId))
            {
                return "找不到群組 " + form.GroupId + "。";
            }
            if (form.RoleId == "")
            {
                return "請選擇角色。";
            }
            if (!await _repository.RoleExists(form.RoleId))
            {
                return "找不到角色 " + form.RoleId + "。";
            }

            // One role can only be in one group (editing and not changing the role is OK)
            string? groupOfRole = await _repository.GroupOfRole(form.RoleId);
            if (groupOfRole != null && form.RoleId != form.OldRoleId)
            {
                return "角色 " + form.RoleId + " 已經在群組 " + groupOfRole + " 裡了。";
            }
            return "";
        }
    }
}
