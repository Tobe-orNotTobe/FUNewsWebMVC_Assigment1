using Newtonsoft.Json;

namespace FUNewsWebMVC.Models
{
	public class ODataResponse<T>
	{
		[JsonProperty("value")]
		public List<T> Value { get; set; }
	}
}
