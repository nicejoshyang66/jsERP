// 2026-10-28 15:00 added: 角色資料維護 (one page, 3 tabs: Users / Roles / Role Groups)
// 2026-10-29 10:51 modified: 2 pages
//   List    : users (add / edit / delete), 設定群組 button goes to ListDet
//   ListDet : one user on top, tabs Roles / RoleGroups below
// 2026-10-30 19:32 modified: one page List, tabs Users / Roles (ListDet removed)
//   Role groups moved to ZSYSRoleGroupController (角色群組設定)
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

        // 2026-10-30 19:32 removed: ListDet (設定群組 button is removed)

        // ==================== Users tab (List page) ====================

        // GET /ZSYSRole/UserList (also the options of the USER_ID select in the role form)
        public async Task<IActionResult> UserList()
        {
            return Json(await _repository.UserList());
        }

        // 2026-10-30 19:32 removed: UserOne (ListDet page is removed)

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
                return BadRequest(new { message = "帳號 " + userId + " 還有 " + roleCount + " 個角色，請先到 Roles 分頁刪除角色。" }); // 2026-10-30 19:32 modified
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

        // ==================== Roles tab (List page) ====================

        // 2026-10-30 19:32 modified: UserRoles -> RoleList, GET /ZSYSRole/RoleList : all roles + user name
        public async Task<IActionResult> RoleList()
        {
            return Json(await _repository.RoleList());
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
            // 2026-10-30 19:32 removed: GroupId (the group is set in 角色群組設定)

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
            // 2026-10-30 19:32 removed: check of the group
            return "";
        }

        // 2026-10-30 19:32 removed: RoleGroups section (GroupList, GroupCreate, GroupUpdate, GroupDelete, CheckGroup)
        // moved to ZSYSRoleGroupController (角色群組設定)
    }
}
