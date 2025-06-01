using FUNewsWebMVC.Filter;
using FUNewsWebMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsWebMVC.Controllers
{
	[AuthFilter]
	public class CategoryController : Controller
	{
		private readonly CategoryService _service;

		public CategoryController(CategoryService service)
		{
			_service = service;
		}

		public async Task<IActionResult> Index()
		{
			var categories = await _service.GetCategoriesAsync();
			return View(categories);
		}
	}

}
