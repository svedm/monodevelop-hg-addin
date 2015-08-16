namespace Hg.Net.Models
{
	public enum Status
	{
		Default,
		Modified = 'M',
		Added = 'A',
		Removed = 'R',
		Clean = 'C',
		Missing = '!',
		Unknown = '?',
		Ignored = 'I',
		Origin = ' ',
		Conflicted = 'U',
		All
	}
}

