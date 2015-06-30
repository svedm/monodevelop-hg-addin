using System;
using Hg.Net;
using System.Collections.Generic;

namespace MonoDevelop.VersionControl.Mercurial
{
	public class MercurialClient
	{
		private readonly Hg.Net.HgClient _hgClient;

		public MercurialClient(string repoPath, string mercurialPath)
		{
			_hgClient = new HgClient(mercurialPath);
			_hgClient.Connect(repoPath);
		}

		public string Cat(string path, string revision)
		{
			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentException("Path cannot be empty");
			}

			var args = new List<string> { "cat", path };
			if (!string.IsNullOrEmpty(revision))
			{
				args.Add("--rev");
				args.Add(revision);
			}
			return _hgClient.ExecuteCommand(args).Response;
		}
	}
}

