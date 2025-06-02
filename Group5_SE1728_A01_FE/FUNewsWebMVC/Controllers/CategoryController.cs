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

		private bool IsStaffOrAdmin()
		{
			var role = Request.Cookies["Role"];
			return role == "Staff" || role == "Admin";
		}

		public async Task<IActionResult> Index(string searchTerm = "")
		{
			if (!IsStaffOrAdmin())
			{
				TempData["Error"] = "Access denied. Only Staff and Admin can manage categories.";
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
			if (!IsStaffOrAdmin())
			{
				return Forbid();
			}

			try
			{
				ViewBag.Action = "Create";

				var categories = await _categoryService.GetCategoriesAsync();
				ViewBag.ParentCategories = new SelectList(
					categories.Where(c => c.IsActive == true),
					"CategoryId", "CategoryName");

				var model = new Category
				{
					IsActive = true 
				};

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
		public async Task<IActionResult> Create(Category category, bool IsActive = true)
		{
			if (!IsStaffOrAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			// Explicitly set IsActive from form parameter
			category.IsActive = IsActive;

			// Log the received data
			_logger.LogInformation($"Controller: Creating category {category.CategoryName}, IsActive: {category.IsActive}");

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
			ViewBag.ParentCategories = new SelectList(
				categories.Where(c => c.IsActive == true),
				"CategoryId", "CategoryName");
			return PartialView("_CategoryForm", category);
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			if (!IsStaffOrAdmin())
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

				// Get all categories except the current one for parent category dropdown
				var categories = await _categoryService.GetCategoriesAsync();
				var availableParents = categories.Where(c => c.CategoryId != id && c.IsActive == true);

				ViewBag.ParentCategories = new SelectList(
					availableParents,
					"CategoryId", "CategoryName", category.ParentCategoryId);

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
		public async Task<IActionResult> Edit(Category category, bool IsActive = true)
		{
			if (!IsStaffOrAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			// Explicitly set IsActive from form parameter
			category.IsActive = IsActive;

			// Log the received data
			_logger.LogInformation($"Controller: Updating category {category.CategoryId}, IsActive: {category.IsActive}");

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
			var availableParents = categories.Where(c => c.CategoryId != category.CategoryId && c.IsActive == true);

			ViewBag.ParentCategories = new SelectList(
				availableParents,
				"CategoryId", "CategoryName", category.ParentCategoryId);
			return PartialView("_CategoryForm", category);
		}

		[HttpPost]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			if (!IsStaffOrAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			try
			{
				_logger.LogInformation($"Attempting to delete category: {id}");

				// Get the category first to check if it exists
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