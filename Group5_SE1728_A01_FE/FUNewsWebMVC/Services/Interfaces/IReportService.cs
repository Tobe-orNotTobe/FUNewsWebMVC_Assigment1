using FUNewsWebMVC.Models;

namespace FUNewsWebMVC.Services.Interfaces
{
	public interface IReportService
	{
		Task<ReportViewModel> GenerateReportAsync(DateTime startDate, DateTime endDate);
	}
}
