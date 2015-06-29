namespace Hg.Net
{
	public enum Channel
	{
		/// <summary>
		/// Input channel: the length field here tells the client how many bytes to send.
		/// </summary>
		I = 'I',

		/// <summary>
		/// Line based input channel: the client should send a single line of input (no more than length bytes).
		/// </summary>
		/// <remarks>This channel is used when Mercurial interacts with the user or when iterating over stdin. Data sent should include the line separator (\n or \r\n).</remarks> 
		L = 'L',

		/// <summary>
		/// Output channel: most of the communication happens on this channel. When running commands, output Mercurial writes to stdout is written to this channel.
		/// </summary>
		O = 'o',

		/// <summary>
		/// Error channel: when running commands, this correlates to stderr.
		/// </summary>
		E = 'e',

		/// <summary>
		/// Result channel: the server uses this channel to tell the client that a command finished by writing its return value (command specific).
		/// </summary>
		R = 'r',

		/// <summary>
		/// Debug channel: used when the server is started with logging to '-'.
		/// </summary>
		D = 'd'
	}
}