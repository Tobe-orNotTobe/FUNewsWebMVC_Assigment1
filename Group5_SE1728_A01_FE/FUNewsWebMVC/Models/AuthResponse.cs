namespace FUNewsWebMVC.Models
{
	public class AuthResponse
	{
		public short AccountId { get; set; }
		public string AccountName { get; set; }
		public string AccountEmail { get; set; }
		public int AccountRole { get; set; }
		public string Token { get; set; }
		public string RoleName { get; set; }
	}

}
