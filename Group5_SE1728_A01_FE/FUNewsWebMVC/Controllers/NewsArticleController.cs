using FUNewsWebMVC.Filter;
using FUNewsWebMVC.Models;
using FUNewsWebMVC.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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

		// Index - Display all articles with search and filter
		public async Task<IActionResult> Index(string searchTerm = "", string filter = "", short? categoryId = null, string articleId = "")
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
				ViewBag.ArticleId = articleId; // Pass articleId to view for auto-opening modal
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

		// Details - Return partial view for modal popup only
		public async Task<IActionResult> Details(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return Json(new { success = false, message = "Article ID is required." });
			}

			try
			{
				_logger.LogInformation($"Loading details for article: {id}");

				var article = await _newsService.GetNewsArticleByIdAsync(id);
				if (article == null)
				{
					return Json(new { success = false, message = "Article not found." });
				}

				// Check if user can view inactive articles
				if (article.NewsStatus != true && !IsStaffOrAdmin())
				{
					return Json(new { success = false, message = "Article not found." });
				}

				ViewBag.CanManage = IsStaffOrAdmin();

				// Always return partial view for modal popup
				return PartialView("_ArticleDetails", article);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error loading article details for ID: {id}");
				return Json(new { success = false, message = "Failed to load article details." });
			}
		}

		// Create GET - Return partial view for modal
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
				ViewBag.Categories = new SelectList(
					(await _categoryService.GetCategoriesAsync()).Where(c => c.IsActive == true),
					"CategoryId", "CategoryName");
				ViewBag.Tags = await _tagService.GetTagsAsync();

				var model = new NewsArticle
				{
					NewsArticleId = GenerateArticleId(),
					CreatedDate = DateTime.Now,
					NewsStatus = true
				};

				return PartialView("_ArticleForm", model);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading create form");
				return PartialView("_ArticleForm", new NewsArticle());
			}
		}

		// Create POST - Handle form submission
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(NewsArticle article, List<int> selectedTags, bool NewsStatus = false)
		{
			if (!IsStaffOrAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			if (ModelState.IsValid)
			{
				try
				{
					var userId = GetCurrentUserId();
					if (!userId.HasValue)
					{
						return Json(new { success = false, message = "Unable to identify current user." });
					}

					article.CreatedById = userId.Value;
					article.CreatedDate = DateTime.Now;
					article.NewsStatus = NewsStatus; // Explicitly set from form parameter

					if (string.IsNullOrEmpty(article.NewsArticleId))
					{
						article.NewsArticleId = GenerateArticleId();
					}

					var success = await _newsService.CreateNewsArticleAsync(article, selectedTags ?? new List<int>());
					if (success)
					{
						return Json(new { success = true });
					}
					else
					{
						return Json(new { success = false, message = "Failed to create article." });
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error creating news article");
					return Json(new { success = false, message = ex.Message });
				}
			}

			ViewBag.Action = "Create";
			ViewBag.Categories = new SelectList(
				(await _categoryService.GetCategoriesAsync()).Where(c => c.IsActive == true),
				"CategoryId", "CategoryName");
			ViewBag.Tags = await _tagService.GetTagsAsync();
			return PartialView("_ArticleForm", article);
		}

		// Edit GET - Return partial view for modal
		[HttpGet]
		public async Task<IActionResult> Edit(string id)
		{
			if (!IsStaffOrAdmin())
			{
				return Forbid();
			}

			if (string.IsNullOrEmpty(id))
			{
				return BadRequest("Article ID is required.");
			}

			try
			{
				var article = await _newsService.GetNewsArticleByIdAsync(id);
				if (article == null)
				{
					return NotFound("Article not found.");
				}

				ViewBag.Action = "Edit";

				// Get categories for dropdown
				var categories = await _categoryService.GetCategoriesAsync();
				ViewBag.Categories = new SelectList(
					categories.Where(c => c.IsActive == true),
					"CategoryId", "CategoryName", article.CategoryId);

				// Get all tags
				ViewBag.Tags = await _tagService.GetTagsAsync();

				return PartialView("_ArticleForm", article);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error loading article for edit: {id}");
				return BadRequest("Failed to load article for editing.");
			}
		}

		// Edit POST - Handle form submission
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(NewsArticle article, List<int> selectedTags, bool NewsStatus = false)
		{
			if (!IsStaffOrAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			// Remove validation for fields that should not be validated in edit
			ModelState.Remove("CreatedDate");
			ModelState.Remove("CreatedById");

			if (ModelState.IsValid)
			{
				try
				{
					var userId = GetCurrentUserId();

					// Get the existing article to preserve certain fields
					var existingArticle = await _newsService.GetNewsArticleByIdAsync(article.NewsArticleId);
					if (existingArticle == null)
					{
						return Json(new { success = false, message = "Article not found." });
					}

					// Update only the editable fields
					existingArticle.NewsTitle = article.NewsTitle;
					existingArticle.Headline = article.Headline;
					existingArticle.NewsContent = article.NewsContent;
					existingArticle.NewsSource = article.NewsSource;
					existingArticle.CategoryId = article.CategoryId;
					existingArticle.NewsStatus = NewsStatus; // Explicitly set from form parameter
					existingArticle.UpdatedById = userId;
					existingArticle.ModifiedDate = DateTime.Now;

					_logger.LogInformation($"Updating article {article.NewsArticleId} with status: {NewsStatus}, tags: {selectedTags?.Count ?? 0}");

					var success = await _newsService.UpdateNewsArticleAsync(existingArticle, selectedTags ?? new List<int>());
					if (success)
					{
						return Json(new { success = true });
					}
					else
					{
						return Json(new { success = false, message = "Failed to update article." });
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error updating news article");
					return Json(new { success = false, message = ex.Message });
				}
			}

			// If validation failed, return the form with errors
			ViewBag.Action = "Edit";
			var categories = await _categoryService.GetCategoriesAsync();
			ViewBag.Categories = new SelectList(
				categories.Where(c => c.IsActive == true),
				"CategoryId", "CategoryName", article.CategoryId);
			ViewBag.Tags = await _tagService.GetTagsAsync();
			return PartialView("_ArticleForm", article);
		}

		// Delete POST - Handle deletion with confirmation
		[HttpPost]
		public async Task<IActionResult> DeleteConfirmed(string id)
		{
			if (!IsStaffOrAdmin())
			{
				return Json(new { success = false, message = "Access denied." });
			}

			try
			{
				var success = await _newsService.DeleteNewsArticleAsync(id);
				if (success)
				{
					return Json(new { success = true });
				}
				else
				{
					return Json(new { success = false, message = "Failed to delete article." });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error deleting article: {id}");
				return Json(new { success = false, message = ex.Message });
			}
		}

		// Get Article Data - For AJAX operations
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
	}
}