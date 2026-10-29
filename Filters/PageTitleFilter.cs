// 2026-10-29 16:15 added: page title = menu caption, for every page (registered once in Program.cs)
// Before a View is shown, read SYS_PROGRAM.FORM_CAPTION of this controller and put it in ViewData["Title"].
// The view can use @ViewData["Title"] for <h1>; _Layout uses it for <title>.
using jsERP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace jsERP.Filters
{
    public class PageTitleFilter : IAsyncResultFilter
    {
        private readonly ErpRepository _repository;

        public PageTitleFilter(ErpRepository repository)
        {
            _repository = repository;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            // Only for pages (View), not for Json / Ok / BadRequest
            if (context.Result is ViewResult viewResult)
            {
                string controllerName = context.RouteData.Values["controller"]?.ToString() ?? "";
                string actionName = context.RouteData.Values["action"]?.ToString() ?? "";

                string? title = await _repository.PageTitle(controllerName, actionName);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    viewResult.ViewData["Title"] = title;
                }
            }

            await next();
        }
    }
}
