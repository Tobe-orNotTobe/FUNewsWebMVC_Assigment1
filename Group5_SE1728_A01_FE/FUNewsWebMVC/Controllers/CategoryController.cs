using FUNewsWebMVC.Filter;
using FUNewsWebMVC.Models;
using FUNewsWebMVC.Services;
using FUNewsWebMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 3;
        public int PageIndex { get; set; } = 1;
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        public async Task<IActionResult> Index(int pageIndex = 1)
        {
            PageIndex = pageIndex;
            var categories = await _service.GetCategoriesAsync();
            TotalCount = categories.Count;

            var pagedCategories = categories
                                .Skip((pageIndex - 1) * PageSize)
                                .Take(PageSize)
                                .ToList();

            var model = new CategoryListViewModel
            {
                Categories = pagedCategories,
                PageIndex = pageIndex,
                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Action = "Create";
            var categories = await _service.GetCategoriesAsync();
            ViewBag.ParentCategories = new SelectList(categories, "CategoryId", "CategoryName");
            return PartialView("_PartialView", new Category());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                await _service.AddAsync(category);
                return Json(new { success = true });
            }
            return PartialView("_PartialView", category);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _service.GetByIdAsync(id);
            if (category == null) return NotFound();

            ViewBag.Action = "Edit";
            var categories = await _service.GetCategoriesAsync();

            ViewBag.ParentCategories = new SelectList(categories, "CategoryId", "CategoryName", category.ParentCategoryId);
            return PartialView("_PartialView", category);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                await _service.UpdateAsync(category);
                return Json(new { success = true });
            }
            return PartialView("_PartialView", category);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var category = await _service.GetByIdAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _service.GetByIdAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _service.DeleteAsync(id);
            return RedirectToAction("Index");
        }


    }

}