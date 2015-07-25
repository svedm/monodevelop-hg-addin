﻿using System;
using System.Collections.Generic;
using System.Xml;
using Hg.Net.Models;
using System.Linq;

namespace Hg.Net
{
	public class MercurialClient
	{
		private readonly HgCommandServerClient _hgClient;

		public MercurialClient(string repoPath, string mercurialPath)
		{
			if (string.IsNullOrEmpty(repoPath))
			{
				throw new ArgumentException("repoPath cannot be empty");
			}

			_hgClient = new HgCommandServerClient(mercurialPath);
			_hgClient.Connect(repoPath);
		}

		public static void Init(string path, string hgPath)
		{
			HgCommandServerClient.Init(path, hgPath);
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

			return _hgClient.ExecuteCommand(new[] { "commit", "-m", message }).Response;
		}

		public IList<CommandServerRevision> Log(string revisionRange, List<string> files, bool followAcrossCopy = false, bool followFirstMergeParent = false,
			DateTime fromDate = default(DateTime), DateTime toDate = default(DateTime), bool showCopiedFiles = false,
			string searchText = null, bool showRemoves = false, bool onlyMerges = false, bool excludeMerges = false,
			string user = null, string branch = null, string pruneRevisions = null, int limit = 0,
			string includePattern = null, string excludePattern = null)
	{
			var argumentHelper = new ArgumentHelper();
			argumentHelper.Add("log", "--style", "xml");
			argumentHelper.AddIfNotNullOrEmpty(false, "--rev", revisionRange);
			argumentHelper.AddIf(followAcrossCopy, "--follow");
			argumentHelper.AddIf(followFirstMergeParent, "--follow-first");
			argumentHelper.AddIf(showCopiedFiles, "--copies");
			argumentHelper.AddIfNotNullOrEmpty(false, "--keyword", searchText);
			argumentHelper.AddIf(showRemoves, "--removed");
			argumentHelper.AddIf(onlyMerges, "--only-merges");
			argumentHelper.AddIf(excludeMerges, "--no-merges");
			argumentHelper.AddIfNotNullOrEmpty(false, "--user", user);
			argumentHelper.AddIfNotNullOrEmpty(false, "--branch", branch);
			argumentHelper.AddIfNotNullOrEmpty(false, "--prune", pruneRevisions);
			argumentHelper.AddIfNotNullOrEmpty(false, "--include", includePattern);
			argumentHelper.AddIfNotNullOrEmpty(false, "--exclude", excludePattern);
			argumentHelper.AddIf(limit > 0, "--limit", limit.ToString());
			argumentHelper.AddIf(fromDate != default(DateTime) && toDate != default(DateTime),
				string.Format("{0} to {1}", fromDate.ToString("yyyy-MM-dd HH:mm:ss"), toDate.ToString("yyyy-MM-dd HH:mm:ss")));
			argumentHelper.AddIf(files != null, files.ToArray());

			var resp = _hgClient.ExecuteCommand(argumentHelper.GetList());
			if (resp.ResultCode != 0)
			{
				throw new Exception("Failed to get log. Error: " + resp.Error);
			}

			try
			{
				return XmlHelper.GetRevisions(resp.Response);
			}
			catch (XmlException ex)
			{
				throw new Exception("Error getting log", ex);
			}
		}

		public IDictionary<string,Status> Status(IEnumerable<string> files, bool quiet = false,
			Status onlyFilesWithThisStatus = Hg.Net.Models.Status.Default, bool showCopiedSources = false, 
			string fromRevision = null, string onlyRevision = null, string includePattern = null,
			string excludePattern = null, bool recurseSubRepositories = false)
		{
			var argumentHelper = new ArgumentHelper();

			argumentHelper.Add("status");
			argumentHelper.AddIf(quiet, "--quiet");
			if (onlyFilesWithThisStatus != Hg.Net.Models.Status.Default)
			{
				argumentHelper.Add(ArgumentHelper.ArgumentForStatus(onlyFilesWithThisStatus));
			}
			argumentHelper.AddIf(showCopiedSources, "--copies");
			argumentHelper.AddIfNotNullOrEmpty(false, "--rev", fromRevision);
			argumentHelper.AddIfNotNullOrEmpty(false, "--change", onlyRevision);
			argumentHelper.AddIfNotNullOrEmpty(false, "--include", includePattern);
			argumentHelper.AddIfNotNullOrEmpty(false, "--exclude", excludePattern);
			argumentHelper.AddIf(recurseSubRepositories, "--subrepos");

			if (files != null)
				argumentHelper.Add(files.ToArray());

			var result = _hgClient.ExecuteCommand(argumentHelper.GetList());
			if (result.ResultCode != 0)
			{
				throw new Exception("Error retrieving status");
			}

			return result.Response.Split(new[]{ "\n" }, StringSplitOptions.RemoveEmptyEntries).Aggregate(new Dictionary<string,Status>(), (dict, line) =>
			{
				if (2 < line.Length)
				{
					dict[line.Substring(2)] = ParseStatus(line.Substring(0, 1));
				}
				return dict;
			});
		}

		private static Status ParseStatus(string input)
		{
			if (Enum.GetValues(typeof(Status)).Cast<Status>().Any(x => ((char)x) == input[0]))
				return (Status)(input[0]);
			return Hg.Net.Models.Status.Clean;
		}
	}
}
