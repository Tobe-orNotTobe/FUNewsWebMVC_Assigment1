using FUNewsWebMVC.Models;
using FUNewsWebMVC.Services.Interfaces;
using Newtonsoft.Json;
using System.Text;

namespace FUNewsWebMVC.Services
{
	public class TagService : BaseService, ITagService
	{
		private readonly ILogger<TagService> _logger;

		public TagService(
			IHttpClientFactory clientFactory,
			IHttpContextAccessor contextAccessor,
			ILogger<TagService> logger)
			: base(clientFactory, contextAccessor)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task<List<Tag>> GetTagsAsync()
		{
			try
			{
				_logger.LogInformation("Fetching all tags");

				var client = CreateAuthorizedClient();
				var response = await client.GetAsync("Tags");
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<ODataResponse<Tag>>(content);

				_logger.LogInformation($"Successfully fetched {result?.Value?.Count ?? 0} tags");
				return result?.Value ?? new List<Tag>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching tags");
				throw new Exception("Failed to retrieve tags", ex);
			}
		}

		public async Task<Tag?> GetTagByIdAsync(int id)
		{
			try
			{
				_logger.LogInformation($"Fetching tag with ID: {id}");

				var client = CreateAuthorizedClient();
				var response = await client.GetAsync($"Tags({id})");

				if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					_logger.LogWarning($"Tag with ID {id} not found");
					return null;
				}

				response.EnsureSuccessStatusCode();
				var content = await response.Content.ReadAsStringAsync();
				var tag = JsonConvert.DeserializeObject<Tag>(content);

				_logger.LogInformation($"Successfully fetched tag: {tag?.TagName}");
				return tag;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error fetching tag {id}");
				throw new Exception($"Failed to retrieve tag with ID {id}", ex);
			}
		}

		public async Task<bool> CreateTagAsync(Tag tag)
		{
			if (tag == null)
			{
				throw new ArgumentNullException(nameof(tag));
			}

			try
			{
				_logger.LogInformation($"Creating tag: {tag.TagName}");

				var client = CreateAuthorizedClient();
				var json = JsonConvert.SerializeObject(tag, new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var content = new StringContent(json, Encoding.UTF8, "application/json");
				var response = await client.PostAsync("Tags", content);

				var success = response.IsSuccessStatusCode;

				if (success)
				{
					_logger.LogInformation($"Successfully created tag: {tag.TagName}");
				}
				else
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogWarning($"Failed to create tag. Status: {response.StatusCode}, Error: {errorContent}");
				}

				return success;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating tag");
				throw new Exception("Failed to create tag", ex);
			}
		}

		public async Task<bool> UpdateTagAsync(Tag tag)
		{
			if (tag == null)
			{
				throw new ArgumentNullException(nameof(tag));
			}

			try
			{
				_logger.LogInformation($"Updating tag: {tag.TagId}");

				var client = CreateAuthorizedClient();
				var json = JsonConvert.SerializeObject(tag, new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var content = new StringContent(json, Encoding.UTF8, "application/json");
				var response = await client.PutAsync($"Tags({tag.TagId})", content);

				var success = response.IsSuccessStatusCode;

				if (success)
				{
					_logger.LogInformation($"Successfully updated tag: {tag.TagId}");
				}
				else
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogWarning($"Failed to update tag. Status: {response.StatusCode}, Error: {errorContent}");
				}

				return success;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error updating tag {tag.TagId}");
				throw new Exception($"Failed to update tag with ID {tag.TagId}", ex);
			}
		}

		public async Task<bool> DeleteTagAsync(int id)
		{
			try
			{
				_logger.LogInformation($"Deleting tag: {id}");

				var client = CreateAuthorizedClient();
				var response = await client.DeleteAsync($"Tags({id})");

				var success = response.IsSuccessStatusCode;

				if (success)
				{
					_logger.LogInformation($"Successfully deleted tag: {id}");
				}
				else
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogWarning($"Failed to delete tag. Status: {response.StatusCode}, Error: {errorContent}");
				}

				return success;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error deleting tag {id}");
				throw new Exception($"Failed to delete tag with ID {id}", ex);
			}
		}

		public async Task<List<Tag>> SearchTagsAsync(string searchTerm)
		{
			if (string.IsNullOrWhiteSpace(searchTerm))
			{
				return await GetTagsAsync();
			}

			try
			{
				_logger.LogInformation($"Searching tags with term: {searchTerm}");

				var client = CreateAuthorizedClient();
				var encodedSearchTerm = Uri.EscapeDataString(searchTerm);
				var response = await client.GetAsync($"Tags?$filter=contains(TagName,'{encodedSearchTerm}') or contains(Note,'{encodedSearchTerm}')");
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<ODataResponse<Tag>>(content);

				_logger.LogInformation($"Search returned {result?.Value?.Count ?? 0} tags for term: {searchTerm}");
				return result?.Value ?? new List<Tag>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error searching tags with term: {searchTerm}");
				throw new Exception("Failed to search tags", ex);
			}
		}
	}
}
