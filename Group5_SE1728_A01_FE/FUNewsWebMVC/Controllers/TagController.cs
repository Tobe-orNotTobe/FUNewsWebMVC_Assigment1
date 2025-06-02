using FUNewsWebMVC.Filter;
using FUNewsWebMVC.Models;
using FUNewsWebMVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsWebMVC.Controllers
{
	[AuthFilter]
	public class TagController : Controller
	{
		private readonly ITagService _tagService;
		private readonly ILogger<TagController> _logger;

		public TagController(ITagService tagService, ILogger<TagController> logger)
		{
			_tagService = tagService ?? throw new ArgumentNullException(nameof(tagService));
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
				TempData["Error"] = "Access denied. Only Staff and Admin can manage tags.";
				return RedirectToAction("Index", "Home");
			}

			try
			{
				_logger.LogInformation($"Loading tags index with search term: {searchTerm}");

				List<Tag> tags;

				if (!string.IsNullOrEmpty(searchTerm))
				{
					tags = await _tagService.SearchTagsAsync(searchTerm);
				}
				else
				{
					tags = await _tagService.GetTagsAsync();
				}

				ViewBag.SearchTerm = searchTerm;
				_logger.LogInformation($"Successfully loaded {tags.Count} tags");
				return View(tags);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading tags index");
				TempData["Error"] = "Failed to load tags. Please try again.";
				return View(new List<Tag>());
			}
		}

		// Updated TagController methods - fix create tag error

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
				var model = new Tag();
				return PartialView("_TagForm", model);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading create form");
				return PartialView("_TagForm", new Tag());
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Tag tag)
		{
			if (!IsStaffOrAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			try
			{
				var existingTags = await _tagService.GetTagsAsync();
				var maxId = existingTags.Any() ? existingTags.Max(t => t.TagId) : 0;
				tag.TagId = maxId + 1;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting max tag ID");
				return Json(new { success = false, message = "Failed to generate tag ID." });
			}

			if (ModelState.IsValid)
			{
				try
				{
					var success = await _tagService.CreateTagAsync(tag);
					if (success)
					{
						return Json(new { success = true });
					}
					else
					{
						return Json(new { success = false, message = "Failed to create tag." });
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error creating tag");
					return Json(new { success = false, message = ex.Message });
				}
			}

			ViewBag.Action = "Create";
			return PartialView("_TagForm", tag);
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
				var tag = await _tagService.GetTagByIdAsync(id);
				if (tag == null)
				{
					return NotFound("Tag not found.");
				}

				ViewBag.Action = "Edit";
				return PartialView("_TagForm", tag);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error loading tag for edit: {id}");
				return BadRequest("Failed to load tag for editing.");
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Tag tag)
		{
			if (!IsStaffOrAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			if (ModelState.IsValid)
			{
				try
				{
					var success = await _tagService.UpdateTagAsync(tag);
					if (success)
					{
						return Json(new { success = true });
					}
					else
					{
						return Json(new { success = false, message = "Failed to update tag." });
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error updating tag");
					return Json(new { success = false, message = ex.Message });
				}
			}

			ViewBag.Action = "Edit";
			return PartialView("_TagForm", tag);
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
				_logger.LogInformation($"Attempting to delete tag: {id}");

				// Get the tag first to check if it exists
				var tag = await _tagService.GetTagByIdAsync(id);
				if (tag == null)
				{
					return Json(new { success = false, message = "Tag not found." });
				}

				var success = await _tagService.DeleteTagAsync(id);
				if (success)
				{
					_logger.LogInformation($"Successfully deleted tag: {id}");
					return Json(new { success = true, message = "Tag deleted successfully. All article associations have been removed." });
				}
				else
				{
					_logger.LogWarning($"Failed to delete tag: {id}");
					return Json(new { success = false, message = "Failed to delete tag." });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error deleting tag: {id}");
				return Json(new { success = false, message = $"Error deleting tag: {ex.Message}" });
			}
		}
	}
}
