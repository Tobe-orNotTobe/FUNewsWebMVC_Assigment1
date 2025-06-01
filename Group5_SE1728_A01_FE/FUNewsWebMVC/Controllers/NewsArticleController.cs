using Microsoft.AspNetCore.Mvc;

namespace FUNewsWebMVC.Controllers
{
	public class NewsArticleController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
