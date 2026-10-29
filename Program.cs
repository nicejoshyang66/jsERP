var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// 2026-10-29 16:15 modified: page title = menu caption for every page (Filters/PageTitleFilter.cs)
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<jsERP.Filters.PageTitleFilter>();
});

// 2026-10-24 17:35 added: database repository (login + menu)
builder.Services.AddScoped<jsERP.Services.ErpRepository>();
builder.Services.AddScoped<jsERP.Services.RoleRepository>(); // 2026-10-28 15:00 added: 角色資料維護

// 2026-10-24 17:35 added: cookie login
builder.Services.AddAuthentication("ErpCookie").AddCookie("ErpCookie", option =>
{
    option.LoginPath = "/Login";
    option.Cookie.Name = "jsERP.Auth";
    option.ExpireTimeSpan = TimeSpan.FromDays(30);
    option.Events.OnRedirectToLogin = context =>
    {
        // API calls get 401, pages are redirected to the login page
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // 2026-10-24 17:35 added
app.UseAuthorization();

// 2026-10-24 17:35 modified: start page is Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();
