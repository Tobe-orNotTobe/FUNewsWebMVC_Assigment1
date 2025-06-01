namespace FUNewsWebMVC.Models;

public class Category
{
	public short CategoryId { get; set; }

	public string CategoryName { get; set; } = null!;

	public string CategoryDesciption { get; set; } = null!;

	public short? ParentCategoryId { get; set; }

	public bool? IsActive { get; set; }

	public virtual Category? ParentCategory { get; set; }
}
