using FUNewsWebMVC.Models;

namespace FUNewsWebMVC.ViewModels
{
	public class SystemAccountListViewModel
	{
		public List<SystemAccount> Accounts { get; set; } = new();
		public int PageIndex { get; set; } = 1;
		public int TotalPages { get; set; }
		public string? SearchTerm { get; set; }
		public int TotalCount => Accounts.Count;
		public bool HasPreviousPage => PageIndex > 1;
		public bool HasNextPage => PageIndex < TotalPages;

		// Statistics properties
		public int TotalAccounts { get; set; }
		public int StaffCount { get; set; }
		public int LecturerCount { get; set; }
		public int AdminCount { get; set; }
	}

	public class SystemAccountCreateViewModel
	{
		public SystemAccount Account { get; set; } = new();
		public List<RoleOption> AvailableRoles { get; set; } = new();
	}

	public class SystemAccountEditViewModel
	{
		public SystemAccount Account { get; set; } = new();
		public List<RoleOption> AvailableRoles { get; set; } = new();
		public bool CanChangeRole { get; set; } = true;
		public bool IsCurrentUser { get; set; }
	}

	public class RoleOption
	{
		public int Value { get; set; }
		public string Text { get; set; } = "";
		public string Description { get; set; } = "";
		public string BadgeClass { get; set; } = "";
	}

	public class SystemAccountStatisticsViewModel
	{
		public int TotalAccounts { get; set; }
		public int AdminAccounts { get; set; }
		public int StaffAccounts { get; set; }
		public int LecturerAccounts { get; set; }
		public decimal AdminPercentage => TotalAccounts > 0 ? (decimal)AdminAccounts / TotalAccounts * 100 : 0;
		public decimal StaffPercentage => TotalAccounts > 0 ? (decimal)StaffAccounts / TotalAccounts * 100 : 0;
		public decimal LecturerPercentage => TotalAccounts > 0 ? (decimal)LecturerAccounts / TotalAccounts * 100 : 0;
	}
}