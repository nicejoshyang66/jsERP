// var builder = WebApplication.CreateBuilder(args);
using jsERP.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// 2026-10-29 16:15 modified: page title = menu caption for every page (Filters/PageTitleFilter.cs)
// builder.Services.AddControllersWithViews(options =>
// {
//     options.Filters.Add<jsERP.Filters.PageTitleFilter>();
// });

builder.Services.AddControllersWithViews(options =>
        {
            options.Filters.Add<jsERP.Filters.PageTitleFilter>();

        });



// 2026-10-24 17:35 added: database repository (login + menu)
// builder.Services.AddScoped<jsERP.Services.ErpRepository>();
// builder.Services.AddScoped<jsERP.Services.RoleRepository>(); // 2026-10-28 15:00 added: 角色資料維護
// builder.Services.AddScoped<jsERP.Services.GroupSecurityRepository>(); // 2026-10-30 11:13 added: 群組權限資料維護
// builder.Services.AddScoped<jsERP.Services.RoleGroupRepository>(); // 2026-10-30 19:32 added: 角色群組設定

builder.Services.AddScoped<jsERP.Services.ErpRepository>();
builder.Services.AddScoped<jsERP.Services.RoleRepository>();
builder.Services.AddScoped<jsERP.Services.GroupSecurityRepository>();
builder.Services.AddScoped<jsERP.Services.RoleGroupRepository>();
builder.Services.AddScoped<jsERP.Services.ComPayRepository>(); // 2026-11-09 14:30 added: 客戶 / 供應商基本資料

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

