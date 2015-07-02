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
			if (string.IsNullOrEmpty(repoPath))
			{
				throw new ArgumentException("repoPath cannot be empty");
			}

			_hgClient = new HgClient(mercurialPath);
			_hgClient.Connect(repoPath);
		}

		public static void Init(string path, string hgPath)
		{
			HgClient.Init(path, hgPath);
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
			var x = _hgClient.ExecuteCommand(args);
			return x.Response;
		}

		public string Add(string[] files)
		{
			if (files.Length == 0)
			{
				throw new ArgumentException("Please enter a file");
			}

			var args = new List<string> { "add" };
			args.AddRange(files);

			return _hgClient.ExecuteCommand(args).Response;
		}

		public string Commit(string message)
		{
			if (string.IsNullOrEmpty(message))
			{
				throw new ArgumentException("Please write commit message");
			}

			return _hgClient.ExecuteCommand(new string[] { "commit", "-m", message }).Response;
		}
	}
}

