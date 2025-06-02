// Controllers/NewsArticleController.cs - Complete Fixed Version
using FUNewsWebMVC.Filter;
using FUNewsWebMVC.Models;
using FUNewsWebMVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsWebMVC.Controllers
{
	[AuthFilter]
	public class NewsArticleController : Controller
	{
		private readonly INewsArticleService _newsService;
		private readonly ICategoryService _categoryService;
		private readonly ITagService _tagService;
		private readonly ILogger<NewsArticleController> _logger;

		public NewsArticleController(
			INewsArticleService newsService,
			ICategoryService categoryService,
			ITagService tagService,
			ILogger<NewsArticleController> logger)
		{
			_newsService = newsService ?? throw new ArgumentNullException(nameof(newsService));
			_categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
			_tagService = tagService ?? throw new ArgumentNullException(nameof(tagService));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		private bool IsStaffOrAdmin()
		{
			var role = Request.Cookies["Role"];
			return role == "Staff" || role == "Admin";
		}

		private short? GetCurrentUserId()
		{
			var userIdCookie = Request.Cookies["UserId"];
			if (short.TryParse(userIdCookie, out short userId))
			{
				return userId;
			}
			return null;
		}

		private string GenerateArticleId()
		{
			return $"ART{DateTime.Now:yyyyMMdd}{DateTime.Now:HHmmss}{new Random().Next(10, 99)}";
		}

		// Index - Display all articles
		public async Task<IActionResult> Index(string searchTerm = "", string filter = "", short? categoryId = null)
		{
			try
			{
				_logger.LogInformation($"Loading news articles index with filter: {filter}, search: {searchTerm}");

				List<NewsArticle> articles = new List<NewsArticle>();

				// Handle different filters
				switch (filter.ToLower())
				{
					case "my-articles":
						if (IsStaffOrAdmin())
						{
							var userId = GetCurrentUserId();
							if (userId.HasValue)
							{
								articles = await _newsService.GetMyArticlesAsync(userId.Value);
							}
						}
						break;
					case "active":
						articles = await _newsService.GetActiveNewsArticlesAsync();
						break;
					default:
						if (!string.IsNullOrEmpty(searchTerm))
						{
							articles = await _newsService.SearchNewsArticlesAsync(searchTerm);
						}
						else if (categoryId.HasValue)
						{
							articles = await _newsService.GetNewsArticlesByCategoryAsync(categoryId.Value);
						}
						else
						{
							articles = await _newsService.GetNewsArticlesAsync();
						}
						break;
				}

				// Filter by status for non-authenticated users
				var role = Request.Cookies["Role"];
				if (string.IsNullOrEmpty(role))
				{
					articles = articles.Where(a => a.NewsStatus == true).ToList();
				}

				ViewBag.SearchTerm = searchTerm;
				ViewBag.Filter = filter;
				ViewBag.CategoryId = categoryId;
				ViewBag.Categories = await _categoryService.GetCategoriesAsync();
				ViewBag.Tags = await _tagService.GetTagsAsync();
				ViewBag.CanManage = IsStaffOrAdmin();
				ViewBag.IsMyArticles = filter.ToLower() == "my-articles";

				_logger.LogInformation($"Successfully loaded {articles.Count} articles");
				return View(articles);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading news articles index");
				TempData["Error"] = "Failed to load news articles. Please try again.";
				return View(new List<NewsArticle>());
			}
		}

		// Details - View single article
		public async Task<IActionResult> Details(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				TempData["Error"] = "Article ID is required.";
				return RedirectToAction(nameof(Index));
			}

			try
			{
				_logger.LogInformation($"Loading details for article: {id}");

				var article = await _newsService.GetNewsArticleByIdAsync(id);
				if (article == null)
				{
					TempData["Error"] = "Article not found.";
					return RedirectToAction(nameof(Index));
				}

				// Check if user can view inactive articles
				if (article.NewsStatus != true && !IsStaffOrAdmin())
				{
					TempData["Error"] = "Article not found.";
					return RedirectToAction(nameof(Index));
				}

				ViewBag.CanManage = IsStaffOrAdmin();
				return View(article);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error loading article details for ID: {id}");
				TempData["Error"] = "Failed to load article details.";
				return RedirectToAction(nameof(Index));
			}
		}

		// ✅ AJAX CREATE MODAL ACTION
		[HttpPost]
		public async Task<IActionResult> CreateModal([FromBody] CreateNewsArticleRequest request)
		{
			if (!IsStaffOrAdmin())
			{
				return Json(new { success = false, message = "Access denied. Only Staff can create news articles." });
			}

			try
			{
				_logger.LogInformation($"Received create request with {request.SelectedTags?.Count ?? 0} tags");

				// Validate required fields
				var validationErrors = new List<string>();

				if (string.IsNullOrWhiteSpace(request.NewsTitle))
					validationErrors.Add("Article title is required.");

				if (string.IsNullOrWhiteSpace(request.Headline))
					validationErrors.Add("Headline is required.");

				if (string.IsNullOrWhiteSpace(request.NewsContent))
					validationErrors.Add("Article content is required.");

				if (!request.CategoryId.HasValue || request.CategoryId <= 0)
					validationErrors.Add("Category is required.");

				if (validationErrors.Any())
				{
					return Json(new { success = false, message = "Validation failed.", errors = validationErrors });
				}

				// Get current user
				var userId = GetCurrentUserId();
				if (!userId.HasValue)
				{
					return Json(new { success = false, message = "Unable to identify current user. Please log in again." });
				}

				// Create article object
				var article = new NewsArticle
				{
					NewsArticleId = GenerateArticleId(),
					NewsTitle = request.NewsTitle.Trim(),
					Headline = request.Headline.Trim(),
					NewsContent = request.NewsContent.Trim(),
					NewsSource = string.IsNullOrWhiteSpace(request.NewsSource) ? null : request.NewsSource.Trim(),
					CategoryId = request.CategoryId.Value,
					NewsStatus = request.NewsStatus,
					CreatedById = userId.Value,
					CreatedDate = DateTime.Now
				};

				_logger.LogInformation($"Creating article via modal: {article.NewsTitle} by user {userId} with tags: [{string.Join(", ", request.SelectedTags ?? new List<int>())}]");

				// ✅ FIXED: Pass selected tags to service
				var success = await _newsService.CreateNewsArticleAsync(article, request.SelectedTags ?? new List<int>());

				if (success)
				{
					return Json(new { success = true, message = "News article created successfully!" });
				}
				else
				{
					return Json(new { success = false, message = "Failed to create news article. Please try again." });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating news article via modal");
				return Json(new { success = false, message = $"Error creating article: {ex.Message}" });
			}
		}

		[HttpPost]
		public async Task<IActionResult> EditModal([FromBody] EditNewsArticleRequest request)
		{
			if (!IsStaffOrAdmin())
			{
				return Json(new { success = false, message = "Access denied. Only Staff can edit news articles." });
			}

			try
			{
				_logger.LogInformation($"Received edit request for {request.NewsArticleId} with {request.SelectedTags?.Count ?? 0} tags");

				// Validate required fields
				var validationErrors = new List<string>();

				if (string.IsNullOrWhiteSpace(request.NewsArticleId))
					validationErrors.Add("Article ID is required.");

				if (string.IsNullOrWhiteSpace(request.NewsTitle))
					validationErrors.Add("Article title is required.");

				if (string.IsNullOrWhiteSpace(request.Headline))
					validationErrors.Add("Headline is required.");

				if (string.IsNullOrWhiteSpace(request.NewsContent))
					validationErrors.Add("Article content is required.");

				if (!request.CategoryId.HasValue || request.CategoryId <= 0)
					validationErrors.Add("Category is required.");

				if (validationErrors.Any())
				{
					return Json(new { success = false, message = "Validation failed.", errors = validationErrors });
				}

				// Get existing article
				var existingArticle = await _newsService.GetNewsArticleByIdAsync(request.NewsArticleId);
				if (existingArticle == null)
				{
					return Json(new { success = false, message = "Article not found." });
				}

				// Get current user
				var userId = GetCurrentUserId();

				// Update article properties
				existingArticle.NewsTitle = request.NewsTitle.Trim();
				existingArticle.Headline = request.Headline.Trim();
				existingArticle.NewsContent = request.NewsContent.Trim();
				existingArticle.NewsSource = string.IsNullOrWhiteSpace(request.NewsSource) ? null : request.NewsSource.Trim();
				existingArticle.CategoryId = request.CategoryId.Value;
				existingArticle.NewsStatus = request.NewsStatus;
				existingArticle.UpdatedById = userId;
				existingArticle.ModifiedDate = DateTime.Now;

				_logger.LogInformation($"Updating article via modal: {existingArticle.NewsArticleId} by user {userId} with tags: [{string.Join(", ", request.SelectedTags ?? new List<int>())}]");

				// ✅ FIXED: Pass selected tags to service
				var success = await _newsService.UpdateNewsArticleAsync(existingArticle, request.SelectedTags ?? new List<int>());

				if (success)
				{
					return Json(new { success = true, message = "News article updated successfully!" });
				}
				else
				{
					return Json(new { success = false, message = "Failed to update news article. Please try again." });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating news article via modal");
				return Json(new { success = false, message = $"Error updating article: {ex.Message}" });
			}
		}

		// ✅ GET ARTICLE DATA FOR MODAL EDIT
		[HttpGet]
		public async Task<IActionResult> GetArticleData(string id)
		{
			if (!IsStaffOrAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			try
			{
				var article = await _newsService.GetNewsArticleByIdAsync(id);
				if (article == null)
				{
					return Json(new { success = false, message = "Article not found." });
				}

				var data = new
				{
					newsArticleId = article.NewsArticleId,
					newsTitle = article.NewsTitle,
					headline = article.Headline,
					newsContent = article.NewsContent,
					newsSource = article.NewsSource,
					categoryId = article.CategoryId,
					newsStatus = article.NewsStatus ?? false,
					tagIds = article.Tags?.Select(t => t.TagId).ToList() ?? new List<int>()
				};

				return Json(new { success = true, data = data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting article data for {id}");
				return Json(new { success = false, message = "Failed to load article data." });
			}
		}

		// ✅ TOGGLE STATUS (AJAX)
		[HttpPost]
		public async Task<IActionResult> ToggleStatus([FromBody] string id)
		{
			if (!IsStaffOrAdmin())
			{
				return Json(new { success = false, message = "Access denied" });
			}

			try
			{
				var success = await _newsService.ToggleArticleStatusAsync(id);
				if (success)
				{
					return Json(new { success = true, message = "Article status updated successfully!" });
				}
				else
				{
					return Json(new { success = false, message = "Failed to update article status" });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error toggling status for article: {id}");
				return Json(new { success = false, message = ex.Message });
			}
		}

		// Standard MVC Actions for non-AJAX operations

		// Create GET
		[HttpGet]
		public async Task<IActionResult> Create()
		{
			if (!IsStaffOrAdmin())
			{
				TempData["Error"] = "Access denied. Only Staff can create news articles.";
				return RedirectToAction(nameof(Index));
			}

			try
			{
				ViewBag.Categories = await _categoryService.GetCategoriesAsync();
				ViewBag.Tags = await _tagService.GetTagsAsync();

				var model = new NewsArticle
				{
					NewsArticleId = GenerateArticleId(),
					CreatedDate = DateTime.Now,
					NewsStatus = true
				};

				return View(model);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading create form");
				TempData["Error"] = "Failed to load create form.";
				return RedirectToAction(nameof(Index));
			}
		}

		// Create POST
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(NewsArticle article, List<int> selectedTags)
		{
			if (!IsStaffOrAdmin())
			{
				TempData["Error"] = "Access denied. Only Staff can create news articles.";
				return RedirectToAction(nameof(Index));
			}

			if (!ModelState.IsValid)
			{
				ViewBag.Categories = await _categoryService.GetCategoriesAsync();
				ViewBag.Tags = await _tagService.GetTagsAsync();
				return View(article);
			}

			try
			{
				var userId = GetCurrentUserId();
				if (!userId.HasValue)
				{
					TempData["Error"] = "Unable to identify current user. Please log in again.";
					return RedirectToAction("Login", "Auth");
				}

				article.CreatedById = userId.Value;
				article.CreatedDate = DateTime.Now;
				article.NewsStatus = article.NewsStatus ?? true;

				if (string.IsNullOrEmpty(article.NewsArticleId))
				{
					article.NewsArticleId = GenerateArticleId();
				}

				var success = await _newsService.CreateNewsArticleAsync(article);
				if (success)
				{
					TempData["Success"] = "News article created successfully!";
					return RedirectToAction(nameof(Index));
				}
				else
				{
					ModelState.AddModelError("", "Failed to create news article. Please try again.");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating news article");
				ModelState.AddModelError("", $"Error creating article: {ex.Message}");
			}

			ViewBag.Categories = await _categoryService.GetCategoriesAsync();
			ViewBag.Tags = await _tagService.GetTagsAsync();
			return View(article);
		}

		// Delete GET
		[HttpGet]
		public async Task<IActionResult> Delete(string id)
		{
			if (!IsStaffOrAdmin())
			{
				TempData["Error"] = "Access denied. Only Staff can delete news articles.";
				return RedirectToAction(nameof(Index));
			}

			if (string.IsNullOrEmpty(id))
			{
				TempData["Error"] = "Article ID is required.";
				return RedirectToAction(nameof(Index));
			}

			try
			{
				var article = await _newsService.GetNewsArticleByIdAsync(id);
				if (article == null)
				{
					TempData["Error"] = "Article not found.";
					return RedirectToAction(nameof(Index));
				}

				return View(article);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error loading article for delete: {id}");
				TempData["Error"] = "Failed to load article for deletion.";
				return RedirectToAction(nameof(Index));
			}
		}

		// Delete POST
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(string id)
		{
			if (!IsStaffOrAdmin())
			{
				TempData["Error"] = "Access denied. Only Staff can delete news articles.";
				return RedirectToAction(nameof(Index));
			}

			try
			{
				var success = await _newsService.DeleteNewsArticleAsync(id);
				if (success)
				{
					TempData["Success"] = "News article deleted successfully!";
				}
				else
				{
					TempData["Error"] = "Failed to delete news article.";
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error deleting article: {id}");
				TempData["Error"] = $"Error deleting article: {ex.Message}";
			}

			return RedirectToAction(nameof(Index));
		}
	}

	// ✅ REQUEST MODELS FOR AJAX ACTIONS
	public class CreateNewsArticleRequest
	{
		public string NewsTitle { get; set; } = string.Empty;
		public string Headline { get; set; } = string.Empty;
		public string NewsContent { get; set; } = string.Empty;
		public string? NewsSource { get; set; }
		public short? CategoryId { get; set; }
		public bool NewsStatus { get; set; } = true;
		public List<int> SelectedTags { get; set; } = new List<int>();
	}

	public class EditNewsArticleRequest
	{
		public string NewsArticleId { get; set; } = string.Empty;
		public string NewsTitle { get; set; } = string.Empty;
		public string Headline { get; set; } = string.Empty;
		public string NewsContent { get; set; } = string.Empty;
		public string? NewsSource { get; set; }
		public short? CategoryId { get; set; }
		public bool NewsStatus { get; set; } = true;
		public List<int> SelectedTags { get; set; } = new List<int>();
	}
}