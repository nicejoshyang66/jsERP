// 2026-10-24 17:35 added: menu API for the left sidebar
using System.Security.Claims;
using jsERP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace jsERP.Controllers
{
    [Authorize]
    [Route("api/menu")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class MenuController : Controller
    {
        private readonly ErpRepository _repository;
        private readonly ILogger<MenuController> _logger;

        public MenuController(ErpRepository repository, ILogger<MenuController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // GET /api/menu
        public async Task<IActionResult> Index()
        {
            try
            {
                // 1. Read the menu rows of the login user
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                List<MenuRow> rows = await _repository.Menu(userId);

                // 2. Collect all FORM_ID, used to check if a parent exists   2026-10-25 16:30 modified
                List<string> formNames = new List<string>();
                foreach (MenuRow row in rows)
                {
                    formNames.Add(row.FORM_ID ?? ""); // 2026-10-25 16:30 modified
                }

                // 3. Build top menus and their children
                List<object> menu = new List<object>();

                foreach (MenuRow root in rows)
                {
                    // A row is a top menu when it has no parent, or its parent is not in the list
                    bool hasParent = !string.IsNullOrWhiteSpace(root.PARENT_ID); // 2026-10-28 09:24 modified
                    if (hasParent && formNames.Contains(root.PARENT_ID!)) // 2026-10-28 09:24 modified
                    {
                        continue;
                    }

                    List<object> children = new List<object>();
                    foreach (MenuRow child in rows)
                    {
                        if (child.PARENT_ID == root.FORM_ID) // 2026-10-28 09:24 modified: PARENT_NAME -> PARENT_ID
                        {
                            children.Add(new
                            {
                                id = child.ROW_ID, // 2026-10-25 16:00 modified
                                code = child.FORM_ID, // 2026-10-25 16:30 modified
                                name = child.FORM_CAPTION ?? "",
                                sortNo = child.SORT_NO ?? "", // 2026-10-24 18:10 added
                                url = MenuUrl(child)
                            });
                        }
                    }

                    menu.Add(new
                    {
                        id = root.ROW_ID, // 2026-10-25 16:00 modified
                        code = root.FORM_ID, // 2026-10-25 16:30 modified
                        name = root.FORM_CAPTION ?? "",
                        sortNo = root.SORT_NO ?? "", // 2026-10-24 18:10 added
                        url = MenuUrl(root),
                        children = children
                    });
                }

                return Ok(new
                {
                    userId = userId,
                    userName = User.Identity?.Name,
                    menu = menu
                });
            }
            catch (Exception ex) when (ex is SqlException or InvalidOperationException)
            {
                _logger.LogError(ex, "Menu database unavailable");
                return StatusCode(503, new { message = "選單載入失敗，請稍後重試" });
            }
        }

        // Make "/Controller/Action?route" ; no controller or action -> "#"
        private string MenuUrl(MenuRow row)
        {
            if (string.IsNullOrWhiteSpace(row.CONTROLLER_NAME) || string.IsNullOrWhiteSpace(row.ACTION_NAME))
            {
                return "#";
            }

            string path = Url.Content("~/") + Uri.EscapeDataString(row.CONTROLLER_NAME) + "/" + Uri.EscapeDataString(row.ACTION_NAME);

            if (!string.IsNullOrWhiteSpace(row.ROUTE_NAME))
            {
                path = path + "?" + row.ROUTE_NAME.TrimStart('?');
            }

            return path;
        }
    }
}
