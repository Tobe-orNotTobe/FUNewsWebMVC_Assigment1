using FUNewsWebMVC.Filter;
using FUNewsWebMVC.Models;
using FUNewsWebMVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FUNewsWebMVC.Controllers
{
	[AuthFilter]
	public class CategoryController : Controller
	{
		private readonly ICategoryService _categoryService;
		private readonly ILogger<CategoryController> _logger;

		public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger)
		{
			_categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		private bool IsStaff()
		{
			var role = Request.Cookies["Role"];
			return role == "Staff";
		}

		public async Task<IActionResult> Index(string searchTerm = "")
		{
			if (!IsStaff())
			{
				TempData["Error"] = "Access denied. Only Staff can manage categories.";
				return RedirectToAction("Index", "Home");
			}

			try
			{
				_logger.LogInformation($"Loading categories index with search term: {searchTerm}");

				List<Category> categories;

				if (!string.IsNullOrEmpty(searchTerm))
				{
					categories = await _categoryService.SearchCategoriesAsync(searchTerm);
				}
				else
				{
					categories = await _categoryService.GetCategoriesAsync();
				}

				ViewBag.SearchTerm = searchTerm;
				_logger.LogInformation($"Successfully loaded {categories.Count} categories");
				return View(categories);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading categories index");
				TempData["Error"] = "Failed to load categories. Please try again.";
				return View(new List<Category>());
			}
		}

		[HttpGet]
		public async Task<IActionResult> Create()
		{
			if (!IsStaff())
			{
				return Forbid();
			}

			try
			{
				ViewBag.Action = "Create";

				var categories = await _categoryService.GetCategoriesAsync(); // show all
				ViewBag.ParentCategories = new SelectList(categories, "CategoryId", "CategoryName");

				var model = new Category { IsActive = true };
				return PartialView("_CategoryForm", model);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading create form");
				return PartialView("_CategoryForm", new Category());
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Category category)
		{
			if (!IsStaff())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			_logger.LogInformation($"Creating category: {category.CategoryName}, IsActive: {category.IsActive}");

			if (ModelState.IsValid)
			{
				try
				{
					var success = await _categoryService.CreateCategoryAsync(category);
					if (success)
					{
						return Json(new { success = true });
					}
					else
					{
						return Json(new { success = false, message = "Failed to create category." });
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error creating category");
					return Json(new { success = false, message = ex.Message });
				}
			}

			ViewBag.Action = "Create";
			var categories = await _categoryService.GetCategoriesAsync();
			ViewBag.ParentCategories = new SelectList(categories, "CategoryId", "CategoryName");
			return PartialView("_CategoryForm", category);
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			if (!IsStaff())
			{
				return Forbid();
			}

			try
			{
				var category = await _categoryService.GetCategoryByIdAsync(id);
				if (category == null)
				{
					return NotFound("Category not found.");
				}

				ViewBag.Action = "Edit";
				var categories = await _categoryService.GetCategoriesAsync();
				var availableParents = categories.Where(c => c.CategoryId != id);
				ViewBag.ParentCategories = new SelectList(availableParents, "CategoryId", "CategoryName", category.ParentCategoryId);

				return PartialView("_CategoryForm", category);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error loading category for edit: {id}");
				return BadRequest("Failed to load category for editing.");
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Category category)
		{
			if (!IsStaff())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			_logger.LogInformation($"Updating category: {category.CategoryId}, IsActive: {category.IsActive}");

			if (ModelState.IsValid)
			{
				try
				{
					var success = await _categoryService.UpdateCategoryAsync(category);
					if (success)
					{
						return Json(new { success = true });
					}
					else
					{
						return Json(new { success = false, message = "Failed to update category." });
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error updating category");
					return Json(new { success = false, message = ex.Message });
				}
			}

			ViewBag.Action = "Edit";
			var categories = await _categoryService.GetCategoriesAsync();
			var availableParents = categories.Where(c => c.CategoryId != category.CategoryId);
			ViewBag.ParentCategories = new SelectList(availableParents, "CategoryId", "CategoryName", category.ParentCategoryId);
			return PartialView("_CategoryForm", category);
		}

		[HttpPost]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			if (!IsStaff())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			try
			{
				_logger.LogInformation($"Attempting to delete category: {id}");

				var category = await _categoryService.GetCategoryByIdAsync(id);
				if (category == null)
				{
					return Json(new { success = false, message = "Category not found." });
				}

				var success = await _categoryService.DeleteCategoryAsync(id);
				if (success)
				{
					_logger.LogInformation($"Successfully deleted category: {id}");
					return Json(new { success = true, message = "Category deleted successfully." });
				}
				else
				{
					_logger.LogWarning($"Failed to delete category: {id}");
					return Json(new { success = false, message = "Failed to delete category. It may be in use by news articles." });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error deleting category: {id}");
				return Json(new { success = false, message = $"Error deleting category: {ex.Message}" });
			}
		}
	}
}
