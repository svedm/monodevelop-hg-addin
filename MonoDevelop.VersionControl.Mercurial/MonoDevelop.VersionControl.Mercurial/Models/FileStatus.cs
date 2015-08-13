using Hg.Net.Models;

namespace MonoDevelop.VersionControl.Mercurial
{
	public class FileStatus
	{
		public readonly string Revision;
		public readonly string Filename;
		public Status Status;

		public FileStatus(string revision, string filename, Status status)
		{
			Revision = revision;
			Filename = filename;
			Status = status;
		}
	}
}

