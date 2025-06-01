using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsWebMVC.Filter
{
	public class AuthFilter : ActionFilterAttribute
	{
		public override void OnActionExecuting(ActionExecutingContext context)
		{
			var request = context.HttpContext.Request;
			var token = request.Cookies["Token"];

			// Bỏ qua filter cho AuthController
			var controller = context.RouteData.Values["controller"]?.ToString()?.ToLower();
			if (controller == "auth")
			{
				return;
			}

			if (string.IsNullOrEmpty(token))
			{
				context.Result = new RedirectToRouteResult(new RouteValueDictionary
				{
					{ "Controller", "Auth" },
					{ "Action", "Login" }
				});
			}
		}
	}
}
