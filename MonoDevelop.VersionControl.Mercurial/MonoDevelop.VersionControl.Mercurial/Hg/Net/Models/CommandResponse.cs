namespace Hg.Net.Models
{
	public class CommandResponse
	{
		public string Response { get; set; }

		public string Error { get; set; }

		public int ResultCode { get; set; }

		public CommandResponse(int resultCode, string response, string error)
		{
			ResultCode = resultCode;
			Response = response;
			Error = error;
		}
	}
}