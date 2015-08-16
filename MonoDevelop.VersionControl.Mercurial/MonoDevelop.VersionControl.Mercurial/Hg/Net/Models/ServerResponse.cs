using System.Text;

namespace Hg.Net.Models
{
	public class ServerResponse
	{
		public Channel Channel { get; private set; }

		public byte[] Buffer { get; private set; }

		public string Messsage
		{
			get { return string.IsNullOrEmpty(_message) ? Encoding.UTF8.GetString(Buffer) : _message; }
		}

		private readonly string _message;

		public ServerResponse(Channel channel, byte[] buffer)
		{
			Channel = channel;
			Buffer = buffer;
		}

		public ServerResponse(Channel channel, string message)
		{
			Channel = channel;
			_message = message;
		}
	}
}