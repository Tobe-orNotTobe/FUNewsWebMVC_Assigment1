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

		public async Task<IActionResult> Details(int id)
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
					TempData["Error"] = "Tag not found.";
					return RedirectToAction(nameof(Index));
				}
				return View(tag);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error loading tag details for ID: {id}");
				TempData["Error"] = "Failed to load tag details.";
				return RedirectToAction(nameof(Index));
			}
		}

		[HttpGet]
		public IActionResult Create()
		{
			if (!IsStaffOrAdmin())
			{
				return Forbid();
			}

			return View(new Tag());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Tag tag)
		{
			if (!IsStaffOrAdmin())
			{
				return Forbid();
			}

			if (!ModelState.IsValid)
			{
				return View(tag);
			}

			try
			{
				_logger.LogInformation($"Creating tag: {tag.TagName}");

				var success = await _tagService.CreateTagAsync(tag);
				if (success)
				{
					TempData["Success"] = "Tag created successfully!";
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ModelState.AddModelError("", "Failed to create tag. Please try again.");
					return View(tag);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating tag");
				ModelState.AddModelError("", $"Error creating tag: {ex.Message}");
				return View(tag);
			}
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
					TempData["Error"] = "Tag not found.";
					return RedirectToAction(nameof(Index));
				}
				return View(tag);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error loading tag for edit: {id}");
				TempData["Error"] = "Failed to load tag for editing.";
				return RedirectToAction(nameof(Index));
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, Tag tag)
		{
			if (!IsStaffOrAdmin())
			{
				return Forbid();
			}

			if (id != tag.TagId)
			{
				TempData["Error"] = "Tag ID mismatch.";
				return RedirectToAction(nameof(Index));
			}

			if (!ModelState.IsValid)
			{
				return View(tag);
			}

			try
			{
				_logger.LogInformation($"Updating tag: {tag.TagId}");

				var success = await _tagService.UpdateTagAsync(tag);
				if (success)
				{
					TempData["Success"] = "Tag updated successfully!";
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ModelState.AddModelError("", "Failed to update tag. Please try again.");
					return View(tag);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error updating tag: {tag.TagId}");
				ModelState.AddModelError("", $"Error updating tag: {ex.Message}");
				return View(tag);
			}
		}

		[HttpGet]
		public async Task<IActionResult> Delete(int id)
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
					TempData["Error"] = "Tag not found.";
					return RedirectToAction(nameof(Index));
				}
				return View(tag);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error loading tag for delete: {id}");
				TempData["Error"] = "Failed to load tag for deletion.";
				return RedirectToAction(nameof(Index));
			}
		}

		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			if (!IsStaffOrAdmin())
			{
				return Forbid();
			}

			try
			{
				_logger.LogInformation($"Deleting tag: {id}");

				var success = await _tagService.DeleteTagAsync(id);
				if (success)
				{
					TempData["Success"] = "Tag deleted successfully!";
				}
				else
				{
					TempData["Error"] = "Failed to delete tag. It may be associated with news articles.";
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error deleting tag: {id}");
				TempData["Error"] = $"Error deleting tag: {ex.Message}";
			}

			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		public async Task<IActionResult> CreateModal([FromBody] Tag tag)
		{
			if (!IsStaffOrAdmin())
			{
				return Json(new { success = false, message = "Access denied" });
			}

			if (!ModelState.IsValid)
			{
				return Json(new
				{
					success = false,
					message = "Invalid data",
					errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
				});
			}

			try
			{
				var success = await _tagService.CreateTagAsync(tag);
				if (success)
				{
					return Json(new { success = true, message = "Tag created successfully!" });
				}
				else
				{
					return Json(new { success = false, message = "Failed to create tag" });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating tag via modal");
				return Json(new { success = false, message = ex.Message });
			}
		}

		[HttpPost]
		public async Task<IActionResult> EditModal([FromBody] Tag tag)
		{
			if (!IsStaffOrAdmin())
			{
				return Json(new { success = false, message = "Access denied" });
			}

			if (!ModelState.IsValid)
			{
				return Json(new
				{
					success = false,
					message = "Invalid data",
					errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
				});
			}

			try
			{
				var success = await _tagService.UpdateTagAsync(tag);
				if (success)
				{
					return Json(new { success = true, message = "Tag updated successfully!" });
				}
				else
				{
					return Json(new { success = false, message = "Failed to update tag" });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating tag via modal");
				return Json(new { success = false, message = ex.Message });
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetTagData(int id)
		{
			if (!IsStaffOrAdmin())
			{
				return Json(new { success = false, message = "Access denied" });
			}

			try
			{
				var tag = await _tagService.GetTagByIdAsync(id);
				if (tag == null)
				{
					return Json(new { success = false, message = "Tag not found" });
				}

				return Json(new
				{
					success = true,
					data = new
					{
						tagId = tag.TagId,
						tagName = tag.TagName,
						note = tag.Note
					}
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting tag data for ID: {id}");
				return Json(new { success = false, message = "Failed to load tag data" });
			}
		}
	}
}
