// 2026-10-28 15:00 added: 角色資料維護 (one page, 3 tabs: Users / Roles / Role Groups)
// 2026-10-29 10:51 modified: 2 pages
//   List    : users (add / edit / delete), 設定群組 button goes to ListDet
//   ListDet : one user on top, tabs Roles / RoleGroups below
using jsERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace jsERP.Controllers
{
    [Authorize]
    public class ZSYSRoleController : Controller
    {
        private readonly RoleRepository _repository;

        public ZSYSRoleController(RoleRepository repository)
        {
            _repository = repository;
        }

        // GET /ZSYSRole/List : the page (data is loaded by Vue)
        public IActionResult List()
        {
            return View();
        }

        // 2026-10-29 10:51 added: GET /ZSYSRole/ListDet?userId=JOSH : roles / role groups of one user (data is loaded by Vue)
        public IActionResult ListDet(string userId)
        {
            ViewBag.UserId = userId ?? "";
            return View();
        }

        // ==================== Users (List page) ====================

        // GET /ZSYSRole/UserList
        public async Task<IActionResult> UserList()
        {
            return Json(await _repository.UserList());
        }

        // 2026-10-29 10:51 added: GET /ZSYSRole/UserOne?userId=JOSH : one user (top of ListDet)
        public async Task<IActionResult> UserOne(string userId)
        {
            UserRow? user = await _repository.UserOne(userId ?? "");
            if (user == null)
            {
                return NotFound(new { message = "找不到使用者 " + userId + "。" });
            }
            return Json(user);
        }

        // POST /ZSYSRole/UserCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserCreate([FromBody] UserRow user)
        {
            string message = CheckUser(user);
            if (message != "")
            {
                return BadRequest(new { message = message });
            }

            if (await _repository.UserExists(user.UserId))
            {
                return BadRequest(new { message = "帳號 " + user.UserId + " 已經存在。" });
            }

            await _repository.UserInsert(user);
            return Ok(new { message = "新增成功。" });
        }

        // POST /ZSYSRole/UserUpdate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserUpdate([FromBody] UserRow user)
        {
            string message = CheckUser(user);
            if (message != "")
            {
                return BadRequest(new { message = message });
            }

            bool ok = await _repository.UserUpdate(user);
            if (!ok)
            {
                return BadRequest(new { message = "修改失敗，找不到這筆資料。" });
            }
            return Ok(new { message = "修改成功。" });
        }

        // 2026-10-29 13:13 added: POST /ZSYSRole/UserPassword : change password (lock button)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserPassword([FromBody] PasswordForm form)
        {
            if (form == null)
            {
                return BadRequest(new { message = "資料格式錯誤。" });
            }

            string newPassword = (form.NewPassword ?? "").Trim();
            string confirmPassword = (form.ConfirmPassword ?? "").Trim();

            if (newPassword == "")
            {
                return BadRequest(new { message = "請輸入新的密碼。" });
            }
            if (newPassword.Length > 50)
            {
                return BadRequest(new { message = "密碼最多 50 個字。" });
            }
            if (newPassword != confirmPassword)
            {
                return BadRequest(new { message = "兩次輸入的密碼不一樣。" });
            }

            bool ok = await _repository.UserChangePassword(form.UserId ?? "", newPassword);
            if (!ok)
            {
                return BadRequest(new { message = "修改失敗，找不到這筆資料。" });
            }
            return Ok(new { message = "密碼已變更。" });
        }

        // POST /ZSYSRole/UserDelete?userId=JOSH
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserDelete(string userId)
        {
            userId = userId ?? "";

            // A user who still has roles can not be deleted
            int roleCount = await _repository.UserRoleCount(userId);
            if (roleCount > 0)
            {
                return BadRequest(new { message = "帳號 " + userId + " 還有 " + roleCount + " 個角色，請先到「設定群組」刪除角色。" });
            }

            bool ok = await _repository.UserDelete(userId);
            if (!ok)
            {
                return BadRequest(new { message = "刪除失敗，找不到這筆資料。" });
            }
            return Ok(new { message = "刪除成功。" });
        }

        // Trim text and check required fields. Return "" when OK.
        private string CheckUser(UserRow? user)
        {
            if (user == null)
            {
                return "資料格式錯誤。";
            }

            user.UserId = (user.UserId ?? "").Trim().ToUpper();
            user.UserName = (user.UserName ?? "").Trim();
            // 2026-10-29 13:13 removed: Password, AutoLogin
            user.Email = (user.Email ?? "").Trim();
            user.IsActive = user.IsActive == 0 ? 0 : 1;  // 2026-10-29 13:13 added: only 0 or 1
            user.Description = (user.Description ?? "").Trim();
            user.Tel = (user.Tel ?? "").Trim();         // 2026-10-29 10:51 added
            user.Mobile = (user.Mobile ?? "").Trim();   // 2026-10-29 10:51 added

            if (user.UserId == "")
            {
                return "請輸入帳號。";
            }
            if (user.UserName == "")
            {
                return "請輸入姓名。";
            }
            return "";
        }

        // ==================== Roles tab (ListDet page) ====================

        // GET /ZSYSRole/UserRoles?userId=JOSH : roles of one user
        public async Task<IActionResult> UserRoles(string userId)
        {
            return Json(await _repository.RolesOfUser(userId ?? ""));
        }

        // POST /ZSYSRole/RoleCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RoleCreate([FromBody] RoleRow role)
        {
            string message = await CheckRole(role);
            if (message != "")
            {
                return BadRequest(new { message = message });
            }

            if (await _repository.RoleExists(role.RoleId))
            {
                return BadRequest(new { message = "角色 " + role.RoleId + " 已經存在。" });
            }

            await _repository.RoleInsert(role);
            return Ok(new { message = "新增成功。" });
        }

        // POST /ZSYSRole/RoleUpdate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RoleUpdate([FromBody] RoleRow role)
        {
            string message = await CheckRole(role);
            if (message != "")
            {
                return BadRequest(new { message = message });
            }

            bool ok = await _repository.RoleUpdate(role);
            if (!ok)
            {
                return BadRequest(new { message = "修改失敗，找不到這筆資料。" });
            }
            return Ok(new { message = "修改成功。" });
        }

        // POST /ZSYSRole/RoleDelete?roleId=MIS00 (also removes it from all role groups)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RoleDelete(string roleId)
        {
            bool ok = await _repository.RoleDelete(roleId ?? "");
            if (!ok)
            {
                return BadRequest(new { message = "刪除失敗，找不到這筆資料。" });
            }
            return Ok(new { message = "刪除成功。" });
        }

        // 2026-10-29 15:44 removed: RoleGroups, RoleGroupsSave (群組 button is removed, group is saved with the role)

        // Trim text and check required fields. Return "" when OK.
        private async Task<string> CheckRole(RoleRow? role)
        {
            if (role == null)
            {
                return "資料格式錯誤。";
            }

            role.RoleId = (role.RoleId ?? "").Trim().ToUpper();
            role.RoleDesc = (role.RoleDesc ?? "").Trim();
            role.UserId = (role.UserId ?? "").Trim();
            role.GroupId = (role.GroupId ?? "").Trim();  // 2026-10-29 15:44 added: "" = no group

            if (role.RoleId == "")
            {
                return "請輸入角色代號。";
            }
            if (role.UserId == "")
            {
                return "請選擇使用者。";
            }
            if (!await _repository.UserExists(role.UserId))
            {
                return "找不到使用者 " + role.UserId + "。";
            }
            // 2026-10-29 15:44 added: the group must exist
            if (role.GroupId != "" && !await _repository.GroupExists(role.GroupId))
            {
                return "找不到群組 " + role.GroupId + "。";
            }
            return "";
        }

        // ==================== RoleGroups tab (ListDet page) ====================

        // 2026-10-29 10:51 modified: GET /ZSYSRole/GroupList?userId=JOSH : all groups + roles of this user in each group
        public async Task<IActionResult> GroupList(string userId)
        {
            return Json(await _repository.GroupList(userId ?? ""));
        }

        // POST /ZSYSRole/GroupCreate
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

        // POST /ZSYSRole/GroupUpdate
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

        // POST /ZSYSRole/GroupDelete?groupId=QC
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

        // 2026-10-29 10:51 removed: RoleList, GroupRoles, GroupRoleAdd, GroupRoleDelete
        // (role <-> group is now set by the 群組 button of the Roles tab)

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
    }
}
