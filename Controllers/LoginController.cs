// 2026-10-24 17:35 added: login page + login / logout API
using System.Security.Claims;
using jsERP.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace jsERP.Controllers
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class LoginController : Controller
    {
        private readonly ErpRepository _repository;
        private readonly ILogger<LoginController> _logger;

        public LoginController(ErpRepository repository, ILogger<LoginController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // GET /Login
        [HttpGet]
        public IActionResult Index()
        {
            // Already logged in -> go to home page
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST /api/auth/login
        [HttpPost("api/auth/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // 1. Check input
            if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "請輸入帳號與密碼" });
            }

            // 2. Check USERID + PWD2 in database
            LoginUser? user;
            try
            {
                user = await _repository.Login(request.UserId.Trim(), request.Password);
            }
            catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException or InvalidOperationException)
            {
                _logger.LogError(ex, "Login database unavailable");
                return StatusCode(503, new { message = "登入服務暫時無法使用，請確認資料庫連線設定" });
            }

            if (user == null)
            {
                return Unauthorized(new { message = "帳號或密碼錯誤" });
            }

            // 3. Save user info in the login cookie
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Name, user.UserName ?? "")
            };
            var identity = new ClaimsIdentity(claims, "ErpCookie");
            var principal = new ClaimsPrincipal(identity);
            var properties = new AuthenticationProperties
            {
                IsPersistent = request.RememberMe
            };

            await HttpContext.SignInAsync("ErpCookie", principal, properties);

            return Ok(new { redirectUrl = Url.Action("Index", "Home") });
        }

        // POST /api/auth/logout
        [Authorize]
        [HttpPost("api/auth/logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("ErpCookie");
            return Ok(new { redirectUrl = Url.Action("Index", "Login") });
        }
    }

    // Data sent from the login form
    public record LoginRequest(string? UserId, string? Password, bool RememberMe);
}
