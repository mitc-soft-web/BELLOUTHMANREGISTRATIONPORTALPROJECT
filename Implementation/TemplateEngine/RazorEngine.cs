using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Mitc_Registration_Portal.Exceptions.TemplateEngine;
using Mitc_Registration_Portal.Interface.TemplateEngine;

namespace Mitc_Registration_Portal.Implementation.TemplateEngine
{
    public class RazorEngine : IRazorEngine
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IHttpContextAccessor _accessor;
        private readonly ILogger<RazorEngine> _logger;
        private readonly IServiceProvider _serviceProvider;

        public RazorEngine(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IHttpContextAccessor accessor,
            IServiceProvider serviceProvider, // Add this to constructor
            ILogger<RazorEngine> logger)
        {
            _viewEngine = viewEngine ?? throw new ArgumentNullException(nameof(viewEngine));
            _tempDataProvider = tempDataProvider ?? throw new ArgumentNullException(nameof(tempDataProvider));
            _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<string> ParseAsync<TModel>(string viewName, TModel model)
        {
            try
            {
                var actionContext = GetActionContext();
                var view = FindView(actionContext, viewName);

                await using var writer = new StringWriter();
                var viewContext = new ViewContext(
                    actionContext,
                    view,
                    new ViewDataDictionary<TModel>(
                        new EmptyModelMetadataProvider(),
                        new ModelStateDictionary())
                    { Model = model },
                    new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                    writer,
                    new HtmlHelperOptions())
                //{ RouteData = _context.GetRouteData() };
                { RouteData = actionContext.RouteData };

                await view.RenderAsync(viewContext);

                return writer.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw new RazorEngineException(ex.Message, ex);
            }
        }

        private IView FindView(ActionContext actionContext, string viewName)
        {
            var getViewResult = _viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: true);
            if (getViewResult.Success)
            {
                return getViewResult.View;
            }

            var findViewResult = _viewEngine.FindView(actionContext, viewName, isMainPage: true);
            if (findViewResult.Success)
            {
                return findViewResult.View;
            }

            var searchedLocations = getViewResult.SearchedLocations.Concat(findViewResult.SearchedLocations);
            var errorMessage = string.Join(
                Environment.NewLine,
                new[] { $"Unable to find view '{viewName}'. The following locations were searched:" }.Concat(searchedLocations)); ;

            throw new InvalidOperationException(errorMessage);
        }

        private ActionContext GetActionContext()
        {
            var httpContext = _accessor.HttpContext ?? new DefaultHttpContext
            {
                RequestServices = _serviceProvider
            };

            return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        }
    }
}
