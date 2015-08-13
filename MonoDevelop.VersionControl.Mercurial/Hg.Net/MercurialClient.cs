using System;
using System.Collections.Generic;
using System.Xml;
using Hg.Net.Models;
using System.Linq;
using System.IO;

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
			if (string.IsNullOrEmpty(mercurialPath))
			{
				mercurialPath = DefaultHgPath;
			}

			_hgClient = new HgCommandServerClient(mercurialPath);
			_hgClient.Connect(repoPath);
		}

		public static string DefaultHgPath 
		{
			get
			{
				var os = Environment.OSVersion.VersionString.ToLower();
				return (os.Contains("win")) ? "hg" : (MacDetector.IsRunningOnMac()) ? "/usr/local/bin/hg" : "/usr/bin/hg";
			}
		}

		public static void Init(string path, string hgPath)
		{
			if (string.IsNullOrEmpty(hgPath))
			{
				hgPath = DefaultHgPath;
			}

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

			return Commit(message, null);
		}

		public string Commit(string message, IEnumerable<string> files, bool addAndRemoveUnknowns = false, bool closeBranch = false,
			string includePattern = null, string excludePattern = null, string messageLog = null, DateTime date = default(DateTime), string user = null)
		{
			if (string.IsNullOrEmpty(message))
			{
				throw new ArgumentException("Please write commit message");
			}

			var argumentHelper = new ArgumentHelper();
			argumentHelper.Add("commit");

			argumentHelper.AddIfNotNullOrEmpty(false, "--message", message);
			argumentHelper.AddIf(addAndRemoveUnknowns, "--addremove");
			argumentHelper.AddIf(closeBranch, "--close-branch");
			argumentHelper.AddIfNotNullOrEmpty(false, "--include", includePattern);
			argumentHelper.AddIfNotNullOrEmpty(false, "--exclude", excludePattern);
			argumentHelper.AddIfNotNullOrEmpty(false, "--logfile", messageLog);
			argumentHelper.AddIfNotNullOrEmpty(false, "--user", user);
			argumentHelper.AddFormattedDateArgument("--date", date);

			if (files != null)
			{
				argumentHelper.Add(files.ToArray());
			}

			var result = _hgClient.ExecuteCommand(argumentHelper.GetList());
			if (result.ResultCode != 1 && result.ResultCode != 0)
			{
				throw new Exception("Error committing");
			}

			return result.Response;
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

		public IDictionary<string, Status> Status(IEnumerable<string> files, bool quiet = false,
			Status onlyFilesWithThisStatus = Models.Status.Default, bool showCopiedSources = false,
			string fromRevision = null, string onlyRevision = null, string includePattern = null,
			string excludePattern = null, bool recurseSubRepositories = false)
		{
			var argumentHelper = new ArgumentHelper();

			argumentHelper.Add("status");
			argumentHelper.AddIf(quiet, "--quiet");
			if (onlyFilesWithThisStatus != Models.Status.Default)
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

			return result.Response.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).Aggregate(new Dictionary<string, Status>(), (dict, line) =>
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
			return Models.Status.Clean;
		}

		public bool Push(string destination, string toRevision = null, bool force = false, string branch = null, bool allowNewBranch = false)
		{
			var argumentHelper = new ArgumentHelper();
			argumentHelper.Add("push");

			argumentHelper.AddIfNotNullOrEmpty(false, "--rev", toRevision);
			argumentHelper.AddIf(force, "--force");
			argumentHelper.AddIfNotNullOrEmpty(false, "--branch", branch);
			argumentHelper.AddIf(allowNewBranch, "--new-branch");
			argumentHelper.AddIf(!string.IsNullOrEmpty(destination), destination);

			var result = _hgClient.ExecuteCommand(argumentHelper.GetList());
			if (result.ResultCode != 1 && result.ResultCode != 0)
			{
				throw new Exception("Error pushing: " + result.Error);
			}

			return result.ResultCode == 0;
		}

		public bool Update(string revision, bool discardUncommittedChanges = false, bool updateAcrossBranches = false, DateTime toDate = default(DateTime))
		{
			var argumentHelper = new ArgumentHelper();
			argumentHelper.Add("update");

			argumentHelper.AddIf(discardUncommittedChanges, "--clean");
			argumentHelper.AddIf(updateAcrossBranches, "--check");
			argumentHelper.AddFormattedDateArgument("--date", toDate);
			argumentHelper.AddIf(!string.IsNullOrEmpty(revision), revision);

			var result = _hgClient.ExecuteCommand(argumentHelper.GetList());
			if (result.ResultCode != 1 && result.ResultCode != 0)
			{
				throw new Exception("Error updating");
			}

			return result.ResultCode == 0;
		}

		public void Revert(string revision, IList<string> files, DateTime date = default(DateTime), bool saveBackups = true, string includePattern = null, string excludePattern = null, bool dryRun = false)
		{
			var argumentHelper = new ArgumentHelper();
			argumentHelper.Add("revert");

			argumentHelper.AddIfNotNullOrEmpty(false, "--rev", revision);
			argumentHelper.AddFormattedDateArgument("--date", date);
			argumentHelper.AddIf(!saveBackups, "--no-backup");
			argumentHelper.AddIfNotNullOrEmpty(false, "--include", includePattern);
			argumentHelper.AddIfNotNullOrEmpty(false, "--exclude", excludePattern);
			argumentHelper.AddIf(dryRun, "--dry-run");

			if (files == null || !files.Any())
			{
				argumentHelper.Add("--all");
			}
			else
			{
				argumentHelper.Add(files.ToArray());
			}

			var result = _hgClient.ExecuteCommand(argumentHelper.GetList());

			if (result.ResultCode != 0)
			{
				throw new Exception("Error reverting");
			}
		}

		public void Remove(IList<string> files, bool after = false, bool force = false, string includePattern = null, string excludePattern = null)
		{
			if (files == null || !files.Any())
			{
				throw new ArgumentException("File list cannot be empty", "files");
			}

			var argumentHelper = new ArgumentHelper();
			argumentHelper.Add("remove");
			argumentHelper.AddIf(after, "--after");
			argumentHelper.AddIf(force, "--force");
			argumentHelper.AddIfNotNullOrEmpty(false, "--include", includePattern);
			argumentHelper.AddIfNotNullOrEmpty(false, "--exclude", excludePattern);
			argumentHelper.Add(files.ToArray());

			var result = _hgClient.ExecuteCommand(argumentHelper.GetList());

			if (result.ResultCode != 0)
			{
				throw new Exception(string.Format("Error removing {0}", string.Join(" , ", files.ToArray())));
			}
		}

		public void Ignore(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentException("Path cannot be empty");
			}

			File.AppendAllLines(Path.Combine(_hgClient.RepoPath, ".hgignore"), new []{ path });
		}

		public void Unignore(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentException("Path cannot be empty");
			}

			var hgignorePath = Path.Combine(_hgClient.RepoPath, ".hgignore");
			var lines = File.ReadAllLines(path).ToList();
			var element = lines.FirstOrDefault(x => x == path);
			if (element != null)
			{
				lines.Remove(element);
			}

			File.WriteAllLines(hgignorePath, lines);
		}

		public IDictionary<string,bool> Resolve(IEnumerable<string> files, bool all = false, bool list = false, bool mark = false,
			bool unmark = false, string mergeTool = null, string includePattern = null, string excludePattern = null)
		{
			var argumentHelper = new ArgumentHelper();
			argumentHelper.Add("resolve");

			argumentHelper.AddIf(all, "--all");
			argumentHelper.AddIf(list, "--list");
			argumentHelper.AddIf(mark, "--mark");
			argumentHelper.AddIf(unmark, "--unmark");
			argumentHelper.AddIfNotNullOrEmpty(false, "--tool", mergeTool);
			argumentHelper.AddIfNotNullOrEmpty(false, "--include", includePattern);
			argumentHelper.AddIfNotNullOrEmpty(false, "--exclude", excludePattern);
			if (files != null)
				argumentHelper.Add(files.ToArray());

			var result = _hgClient.ExecuteCommand(argumentHelper.GetList());
			var statuses = result.Response.Split(new[]{ '\n' }, StringSplitOptions.RemoveEmptyEntries)
				.Aggregate(new Dictionary<string,bool>(),
                (dict, line) =>
				{
					dict[line.Substring(2).Trim()] = (line[0] == 'R');
					return dict;
				});
			return statuses;
		}

		public IEnumerable<CommandServerRevision> Heads(IEnumerable<string> revisions, string startRevision=null, bool onlyTopologicalHeads=false, bool showClosed=false)
		{
			var argumentHelper = new ArgumentHelper();
			argumentHelper.Add("heads", "--style", "xml");

			argumentHelper.AddIfNotNullOrEmpty(false, "--rev", startRevision);
			argumentHelper.AddIf(onlyTopologicalHeads, "--topo");
			argumentHelper.AddIf(showClosed, "--closed");
			if (revisions != null)
			{
				argumentHelper.Add(revisions.ToArray());
			}

			var result = _hgClient.ExecuteCommand(argumentHelper.GetList());
			if (result.ResultCode != 1 && result.ResultCode != 0)
			{
				throw new Exception("Error getting heads");
			}

			try 
			{
				return XmlHelper.GetRevisions(result.Response);
			}
			catch (XmlException ex) 
			{
				throw new Exception("Error parsing heads: " + ex.Message);
			}
		}

		public bool Merge(string revision, bool force=false, string mergeTool=null, bool dryRun=false)
		{
			var argumentHelper = new ArgumentHelper();
			argumentHelper.Add("merge");

			argumentHelper.AddIf(force, "--force");
			argumentHelper.AddIfNotNullOrEmpty(false, mergeTool, "--tool");
			argumentHelper.AddIf(dryRun, "--preview");
			argumentHelper.AddIf(!string.IsNullOrEmpty(revision), revision);

			var result = _hgClient.ExecuteCommand(argumentHelper.GetList());
			if (result.ResultCode != 1 && result.ResultCode != 0)
			{
				throw new Exception("Error merging");
			}

			return result.ResultCode == 0;
		}

		public IDictionary<string,string> Paths(string name=null)
		{
			var argumentHelper = new ArgumentHelper();
			argumentHelper.Add("paths");

			argumentHelper.AddIf(!string.IsNullOrEmpty (name), name);

			var result = _hgClient.ExecuteCommand(argumentHelper.GetList());
			if (result.ResultCode != 0)
			{
				throw new Exception("Error getting paths");
			}
				
			return result.Response
				.Split (new[]{"\n"}, StringSplitOptions.RemoveEmptyEntries)
				.Aggregate (new Dictionary<string,string>(), (dict,line) =>
				{
					var tokens = line.Split (new[]{'='}, 2);
					dict[tokens[0].Trim ()] = tokens[1].Trim ();
					return dict;
				});
		}

		public bool Pull(string source, string toRevision=null, bool update=false, bool force=false, string branch=null)
		{
			var argumentHelper = new ArgumentHelper();
			argumentHelper.Add("pull");

			argumentHelper.AddIfNotNullOrEmpty(false, "--rev", toRevision);
			argumentHelper.AddIf(update, "--update");
			argumentHelper.AddIf(force, "--force");
			argumentHelper.AddIfNotNullOrEmpty(false, "--branch", branch);
			argumentHelper.AddIf(!string.IsNullOrEmpty (source), source);


			var result = _hgClient.ExecuteCommand(argumentHelper.GetList());
			if (result.ResultCode != 1 && result.ResultCode != 0)
			{
				throw new Exception("Error pulling");
			}

			return result.ResultCode == 0;
		}

		public void Archive(string destination, string revision = null, string prefix = null, bool decode = true, bool recurseSubRepositories = false, string includePattern = null, string excludePattern = null)
		{
			if (string.IsNullOrEmpty(destination))
			{
				throw new ArgumentException("Destination cannot be empty", "destination");
			}

			var argumentHelper = new ArgumentHelper();
			argumentHelper.Add("archive");

			argumentHelper.AddIfNotNullOrEmpty(false, "--rev", revision);
			argumentHelper.AddIfNotNullOrEmpty(false, "--prefix", prefix);
			argumentHelper.AddIf(!decode, "--no-decode");
			argumentHelper.AddIf(recurseSubRepositories, "--subrepos");
			argumentHelper.AddIfNotNullOrEmpty(false, "--include", includePattern);
			argumentHelper.AddIfNotNullOrEmpty(false, "--exclude", excludePattern);
			argumentHelper.Add(destination);

			var result = _hgClient.ExecuteCommand(argumentHelper.GetList());
			if (result.ResultCode != 0)
			{
				throw new Exception(string.Format("Error archiving to {0}", destination));
			}
		}

		public IList<CommandServerRevision> Incoming(string source, string toRevision, bool force = false, bool showNewestFirst = false, string bundleFile = null, string branch = null, int limit = 0, bool showMerges = true, bool recurseSubRepos = false)
		{
			var argumentHelper = new ArgumentHelper();
			argumentHelper.Add("incoming", "--style", "xml");

			argumentHelper.AddIfNotNullOrEmpty(false, "--rev", toRevision);
			argumentHelper.AddIf(force, "--force");
			argumentHelper.AddIf(showNewestFirst, "--newest-first");
			argumentHelper.AddIfNotNullOrEmpty(false, "--bundle", bundleFile);
			argumentHelper.AddIfNotNullOrEmpty(false, "--branch", branch);
			argumentHelper.AddIf(!showMerges, "--no-merges");
			argumentHelper.AddIf(recurseSubRepos, "--subrepos");
			if (limit > 0)
			{
				argumentHelper.Add("--limit");
				argumentHelper.Add(limit.ToString());
			}
			argumentHelper.AddIf(!string.IsNullOrEmpty(source), source);

			var result = _hgClient.ExecuteCommand(argumentHelper.GetList());
			if (result.ResultCode != 1 && result.ResultCode != 0)
			{
				throw new Exception("Error getting incoming");
			}
				
			try
			{
				var index = result.Response.IndexOf("<?xml");
				if (index < 0)
					return new List<CommandServerRevision>();
				return XmlHelper.GetRevisions(result.Response.Substring(index));
			}
			catch (XmlException ex)
			{
				throw new Exception("Error parsing incoming " + ex.Message);
			}
		}

		public IList<CommandServerRevision> Outgoing(string source, string toRevision, bool force = false, bool showNewestFirst = false, string branch = null, int limit = 0, bool showMerges = true, bool recurseSubRepos = false)
		{
			var argumentHelper = new ArgumentHelper();
			argumentHelper.Add("outgoing", "--style", "xml");

			argumentHelper.AddIfNotNullOrEmpty(false, "--rev", toRevision);
			argumentHelper.AddIf(force, "--force");
			argumentHelper.AddIf(showNewestFirst, "--newest-first");
			argumentHelper.AddIfNotNullOrEmpty(false, "--branch", branch);
			argumentHelper.AddIf(!showMerges, "--no-merges");
			argumentHelper.AddIf(recurseSubRepos, "--subrepos");
			if (limit > 0)
			{
				argumentHelper.Add("--limit");
				argumentHelper.Add(limit.ToString());
			}
			argumentHelper.AddIf(!string.IsNullOrEmpty(source), source);

			var result = _hgClient.ExecuteCommand(argumentHelper.GetList());
			if (result.ResultCode != 1 && result.ResultCode != 0)
			{
				throw new Exception("Error getting outgoing");
			}

			try
			{
				int index = result.Response.IndexOf("<?xml");
				if (0 > index)
					return new List<CommandServerRevision>();
				return XmlHelper.GetRevisions(result.Response.Substring(index));
			}
			catch (XmlException ex)
			{
				throw new Exception("Error parsing outgoing" + ex.Message);
			}
		}

		public static void Clone(string source, string destination, bool updateWorkingCopy=true, string updateToRevision=null, string cloneToRevision=null, string onlyCloneBranch=null, bool forcePullProtocol=false, bool compressData=true, string mercurialPath=null)
		{
			if (string.IsNullOrEmpty(source))
			{
				throw new ArgumentException("Source must not be empty.", "source");
			}
			if (string.IsNullOrEmpty(mercurialPath))
			{
				mercurialPath = DefaultHgPath;
			}

			var argumentHelper = new ArgumentHelper();
			argumentHelper.Add("clone");

			argumentHelper.AddIf(!updateWorkingCopy, "--noupdate");
			argumentHelper.AddIf(forcePullProtocol, "--pull");
			argumentHelper.AddIf(!compressData, "--uncompressed");
			argumentHelper.AddIfNotNullOrEmpty(false, "--updaterev", updateToRevision);
			argumentHelper.AddIfNotNullOrEmpty(false, "--rev", cloneToRevision);
			argumentHelper.AddIfNotNullOrEmpty(false, "--branch", onlyCloneBranch);
			argumentHelper.Add (source);
			argumentHelper.AddIf(!string.IsNullOrEmpty (destination), destination);

			HgCommandServerClient.Execute(mercurialPath, argumentHelper.ToString());
		}
	}
}

