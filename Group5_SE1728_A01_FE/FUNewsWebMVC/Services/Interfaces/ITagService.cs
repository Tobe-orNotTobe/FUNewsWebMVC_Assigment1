using FUNewsWebMVC.Models;

namespace FUNewsWebMVC.Services.Interfaces
{
	public interface ITagService
	{
		Task<List<Tag>> GetTagsAsync();
		Task<Tag?> GetTagByIdAsync(int id);
		Task<bool> CreateTagAsync(Tag tag);
		Task<bool> UpdateTagAsync(Tag tag);
		Task<bool> DeleteTagAsync(int id);
		Task<List<Tag>> SearchTagsAsync(string searchTerm);
	}
}
