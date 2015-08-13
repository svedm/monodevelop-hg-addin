using System;
using MonoDevelop.Core;
using System.Linq;
using System.Collections.Generic;
using Hg.Net;
using System.IO;
using Hg.Net.Models;

namespace MonoDevelop.VersionControl.Mercurial
{
	public class MercurialRepository : UrlBasedRepository
	{
		private readonly MercurialClient _mercurialClient;
		public string LocalBasePath { get; set; }

		public MercurialRepository()
		{
		}

		public MercurialRepository(MercurialVersionControl vcs, string url)
			: base(vcs)
		{
			Url = url;
			RootPath = new Uri(url).AbsolutePath;
			_mercurialClient = new MercurialClient(new Uri(url).AbsolutePath, "/usr/local/bin/hg");
		}

		#region implemented abstract members of Repository

		public override string GetBaseText(FilePath localFile)
		{
			return OnGetTextAtRevision(localFile, new MercurialRevision(this, MercurialRevision.Head));
		}

		protected override Revision[] OnGetHistory(FilePath localFile, Revision since)
		{
			return _mercurialClient.Log(((MercurialRevision)since).RevisionNumber,
				new List<string> { localFile.FullPath })
					.Select(r => new MercurialRevision(this, r.RevisionId, r.Date, r.Author, r.Email, r.Message)).ToArray();
		}

		protected override System.Collections.Generic.IEnumerable<VersionInfo> OnGetVersionInfo(System.Collections.Generic.IEnumerable<MonoDevelop.Core.FilePath> paths, bool getRemoteStatus)
		{
			if (paths != null)
			{
				return paths.Select(p => CheckStatus(this, p));
			}

			return new List<VersionInfo>();
		}

		protected override VersionInfo[] OnGetDirectoryVersionInfo(MonoDevelop.Core.FilePath localDirectory, bool getRemoteStatus, bool recursive)
		{
			localDirectory = ((FilePath)localDirectory).CanonicalPath;
			return CheckStatuses(this, localDirectory);
		}

		protected override Repository OnPublish(string serverPath, MonoDevelop.Core.FilePath localPath, MonoDevelop.Core.FilePath[] files, string message, MonoDevelop.Core.IProgressMonitor monitor)
		{
			try
			{
				var success = _mercurialClient.Push(serverPath);
				if (success)
				{
					monitor.ReportSuccess("Successfuly pushed");
				}
				else
				{
					monitor.ReportError("Failed", new Exception("Failed"));
				}
			}
			catch (Exception ex)
			{
				monitor.ReportError(ex.Message, ex);
			}

			return new MercurialRepository((MercurialVersionControl)this.VersionControlSystem, serverPath);
		}

		protected override void OnUpdate(MonoDevelop.Core.FilePath[] localPaths, bool recurse, MonoDevelop.Core.IProgressMonitor monitor)
		{
			foreach (var localPath in localPaths)
			{
				try
				{
					var success = _mercurialClient.Update(null);

					if (success)
					{
						monitor.ReportSuccess("Successfuly updated " + localPath.FileName);
					}
					else
					{
						var errStr = "Update failed on " + localPath;
						monitor.ReportError(errStr, new Exception(errStr));
					}
				}
				catch (Exception ex)
				{
					monitor.ReportError(ex.Message, ex);
				}
			}
		}

		protected override void OnCommit(ChangeSet changeSet, MonoDevelop.Core.IProgressMonitor monitor)
		{
			try
			{
				_mercurialClient.Commit(changeSet.GlobalComment, changeSet.Items.Select(i => Path.Combine(changeSet.BaseLocalPath, i.LocalPath)).ToArray());
			}
			catch (Exception ex)
			{
				monitor.ReportError(ex.Message, ex);
			}

			monitor.ReportSuccess(string.Empty);
		}

		protected override void OnCheckout(MonoDevelop.Core.FilePath targetLocalPath, Revision rev, bool recurse, MonoDevelop.Core.IProgressMonitor monitor)
		{
			// Hm... Checkout in mercurial 
			throw new NotImplementedException();
		}

		protected override void OnRevert(MonoDevelop.Core.FilePath[] localPaths, bool recurse, MonoDevelop.Core.IProgressMonitor monitor)
		{
			try
			{
				_mercurialClient.Revert(null, localPaths.Select(p => p.FullPath.ToString()).ToList());
			}
			catch (Exception ex)
			{
				monitor.ReportError(ex.Message, ex);
			}

			monitor.ReportSuccess(string.Empty);
		}

		protected override void OnRevertRevision(MonoDevelop.Core.FilePath localPath, Revision revision, MonoDevelop.Core.IProgressMonitor monitor)
		{
			try
			{
				var rev = revision == null ? new MercurialRevision(this, MercurialRevision.Head) : (MercurialRevision)revision;

				_mercurialClient.Revert(rev.ToString(), new List<string> { localPath });
			}
			catch (Exception ex)
			{
				monitor.ReportError(ex.Message, ex);
			}

			monitor.ReportSuccess(string.Empty);
		}

		protected override void OnRevertToRevision(MonoDevelop.Core.FilePath localPath, Revision revision, MonoDevelop.Core.IProgressMonitor monitor)
		{
			OnRevertRevision(localPath, revision, monitor);
		}

		protected override void OnAdd(MonoDevelop.Core.FilePath[] localPaths, bool recurse, MonoDevelop.Core.IProgressMonitor monitor)
		{
			try
			{
				_mercurialClient.Add(localPaths.Select(x => x.FullPath.ToString()).ToArray());
				monitor.ReportSuccess("Files successfuly added");
			}
			catch (Exception ex)
			{
				monitor.ReportError(ex.Message, ex);
			}
		}

		protected override void OnDeleteFiles(MonoDevelop.Core.FilePath[] localPaths, bool force, MonoDevelop.Core.IProgressMonitor monitor, bool keepLocal)
		{
			try
			{
				_mercurialClient.Remove(localPaths.Select(p => p.FullPath.ToString()).ToList(), force: force);
				monitor.ReportSuccess("Files successfuly removed");
			}
			catch (Exception ex)
			{
				monitor.ReportError(ex.Message, ex);
			}
		}

		protected override void OnDeleteDirectories(MonoDevelop.Core.FilePath[] localPaths, bool force, MonoDevelop.Core.IProgressMonitor monitor, bool keepLocal)
		{
			throw new NotImplementedException();
		}

		protected override string OnGetTextAtRevision(FilePath repositoryPath, Revision revision)
		{
			var rev = new MercurialRevision(this, revision == null ? MercurialRevision.Head : revision.Name);
			return _mercurialClient.Cat(repositoryPath.FullPath, rev.RevisionNumber);
		}

		protected override RevisionPath[] OnGetRevisionChanges(Revision revision)
		{
			List<RevisionPath> paths = new List<RevisionPath>();
			foreach (var status in GetStatus(this.RootPath, (MercurialRevision)revision)
				.Where (s => s.Status != Status.Clean && s.Status != Status.Ignored))
			{
				paths.Add(new RevisionPath(Path.Combine(RootPath, status.Filename), ConvertAction(status.Status), status.Status.ToString()));
			}

			return paths.ToArray();
		}

		protected override void OnIgnore(MonoDevelop.Core.FilePath[] localPath)
		{
			foreach (var filePath in localPath)
			{
				_mercurialClient.Ignore(filePath);
			}
		}

		protected override void OnUnignore(MonoDevelop.Core.FilePath[] localPath)
		{
			foreach (var filePath in localPath)
			{
				_mercurialClient.Ignore(filePath);
			}
		}

		#endregion

		#region implemented abstract members of UrlBasedRepository

		public override string[] SupportedProtocols
		{
			get { return MercurialVersionControl.protocols; }
		}

		#endregion

		#region support methods

		private VersionInfo[] CheckStatuses(Repository repo, string path)
		{
			var statuses = _mercurialClient.Status(new[]{ path });
			var result = new List<VersionInfo>();

			foreach (var status in statuses)
			{
				result.Add(GetInfoFromStatus(status.Value, status.Key, repo));
			}

			return result.ToArray();
		}

		private VersionInfo CheckStatus(Repository repo, string path)
		{
			var shortPath = path.Split(Path.DirectorySeparatorChar).Last();
			var statuses = _mercurialClient.Status(new[]{ path });

			if (!statuses.ContainsKey(shortPath))
			{
				statuses[path] = Status.Clean;
			}

			return GetInfoFromStatus(statuses[path], path, repo);
		}

		private VersionInfo GetInfoFromStatus(Status status, string path, Repository repo)
		{
			return new VersionInfo(path, repo.RootPath, Directory.Exists(path), ConvertStatus(status), null, VersionStatus.Unversioned, null);
		}

		private static VersionStatus ConvertStatus(Status status)
		{
			switch (status)
			{
				case Status.Added:
					return VersionStatus.Versioned | VersionStatus.ScheduledAdd;
				case Status.Conflicted:
					return VersionStatus.Versioned | VersionStatus.Conflicted;
				case Status.Removed:
					return VersionStatus.Versioned | VersionStatus.ScheduledDelete;
				case Status.Ignored:
					return VersionStatus.Versioned | VersionStatus.Ignored;
				case Status.Modified:
					return VersionStatus.Versioned | VersionStatus.Modified;
				case Status.Clean:
					return VersionStatus.Versioned;
			}

			return VersionStatus.Unversioned;
		}

		private IEnumerable<FileStatus> GetStatus(string path, MercurialRevision revision)
		{
			string rootRelativePath = ((FilePath)path).ToRelative(RootPath);
			string revString = null;
			if (null != revision && MercurialRevision.Head != revision.RevisionNumber && MercurialRevision.None != revision.RevisionNumber)
			{
				revString = revision.RevisionNumber;
			}
						
			IDictionary<string, Status> statuses = _mercurialClient.Status(new[]{ path }, onlyRevision: revString);
			if (!statuses.ContainsKey(path))
			{
				if (statuses.ContainsKey(rootRelativePath))
				{
					statuses[path] = statuses[rootRelativePath];
					statuses.Remove(rootRelativePath);
				}
				else if (statuses.ContainsKey(path))
				{
					statuses[path] = statuses[path];
					statuses.Remove(path);
				}
				else
				{
					statuses[path] = Status.Clean;
				}
			}
				
			return statuses.Select(pair => new FileStatus(MercurialRevision.None,
					Path.IsPathRooted(pair.Key) ? pair.Key : (string)((FilePath)Path.Combine(this.RootPath, pair.Key)),	pair.Value));
		}

		private static RevisionAction ConvertAction(Status status)
		{
			switch (status)
			{
				case Status.Added:
					return RevisionAction.Add;
				case Status.Modified:
					return RevisionAction.Modify;
				case Status.Removed:
					return RevisionAction.Delete;
			}

			return RevisionAction.Other;
		}

		private static bool IsProjectOrDirectory (FilePath localPath)
		{
			return Directory.Exists (localPath) ||
				MonoDevelop.Projects.Services.ProjectService.IsSolutionItemFile (localPath) ||
				MonoDevelop.Projects.Services.ProjectService.IsWorkspaceItemFile (localPath);
		}

		internal bool IsVersioned (FilePath localPath) 
		{
			if (string.IsNullOrEmpty (GetLocalBasePath (localPath.FullPath)))
			{
				return false;
			}

			var info = GetVersionInfo (localPath, VersionInfoQueryFlags.None);
			return (null != info && info.IsVersioned);
		}

		#endregion

		public bool CanResolve(FilePath path)
		{
			return IsConflicted(path);
		}

		public virtual bool IsConflicted(FilePath localFile)
		{
			if (string.IsNullOrEmpty(GetLocalBasePath(localFile.FullPath)))
			{
				return false;
			}

			var info = GetVersionInfo(localFile, VersionInfoQueryFlags.None);
			return (null != info && info.IsVersioned && (0 != (info.Status & VersionStatus.Conflicted)));
		}


		public static string GetLocalBasePath(string localPath)
		{
			if (null == localPath)
			{
				return string.Empty;
			}
			if (Directory.Exists(Path.Combine(localPath, ".hg")))
			{
				return localPath;
			}

			return GetLocalBasePath(Path.GetDirectoryName(localPath));
		}

		public virtual void Resolve(FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			foreach (var localPath in localPaths)
			{
				try
				{
					_mercurialClient.Resolve(new[]{ localPath.FullPath.ToString() }, mark: true);
				}
				catch (Exception ce)
				{
					monitor.ReportError(ce.Message, ce);
				}

				monitor.ReportSuccess(string.Empty);
			}
		}

		public virtual bool CanMerge (FilePath localPath)
		{
			return _mercurialClient.Heads(null)
				.Select(r => new MercurialRevision(this, r.RevisionId, r.Date, r.Author, r.Message, null)).Count() > 1;
		}

		public virtual void Merge ()
		{
			_mercurialClient.Merge(null);
		}

		public virtual bool CanPull (FilePath localPath)
		{
			return IsProjectOrDirectory (localPath.FullPath) && IsVersioned (localPath);
		}

		public virtual bool CanRebase ()
		{
			//TODO Implement hg rebase
			return false;
		}

		public virtual Dictionary<string, BranchType> GetKnownBranches(FilePath path)
		{
			try 
			{
				return _mercurialClient.Paths().Aggregate (new Dictionary<string, BranchType> (), (dict, pair) => 
				{
					dict[pair.Value] = BranchType.Parent;
					return dict;
				});
			} catch (Exception ex) 
			{
				LoggingService.LogWarning ("Error getting known branches", ex);
			}

			return new Dictionary<string, BranchType>();
		}

		public virtual void Rebase (string pullLocation, FilePath localPath, bool remember, bool overwrite, IProgressMonitor monitor) 
		{
			//TODO Implement hg rebase
			throw new NotImplementedException ();
		}

		internal bool IsModified (FilePath localFile)
		{
			if (string.IsNullOrEmpty(GetLocalBasePath (localFile.FullPath)))
			{
				return false;
			}

			var info = GetVersionInfo(localFile, VersionInfoQueryFlags.None);

			return (info != null && info.IsVersioned && info.HasLocalChanges);
		}

		public virtual void Push (string pushLocation, FilePath localPath, bool remember, bool overwrite, bool omitHistory, IProgressMonitor monitor) 
		{
			try 
			{
				_mercurialClient.Push (pushLocation, force: overwrite, allowNewBranch: overwrite);
			} 
			catch (Exception ex) 
			{
				monitor.ReportError (ex.Message, ex);
			}

			monitor.ReportSuccess (string.Empty);
		}

		public virtual void Pull(string selectedLocation, FilePath path, bool saveDefault, bool overwrite, IProgressMonitor progressMonitor)
		{
			try 
			{
				_mercurialClient.Pull (selectedLocation, update: true, force: overwrite);
			} 
			catch (Exception ce)
			{
				progressMonitor.ReportError (ce.Message, ce);
			}
			progressMonitor.ReportSuccess (string.Empty);
		}

		public virtual void Export(FilePath localPath, FilePath exportLocation, IProgressMonitor monitor)
		{
			try 
			{
				_mercurialClient.Archive(exportLocation);
				monitor.ReportSuccess(string.Empty);
			} 
			catch(Exception ex) 
			{
				monitor.ReportError(ex.Message, ex);
			}
		}

		public Revision[] GetIncoming(string remote)
		{
			return _mercurialClient.Incoming(remote, null)
				.Select (r => new MercurialRevision(this, r.RevisionId, r.Date, r.Author, r.Email, r.Message)).ToArray();
		}

		public Revision[] GetOutgoing(string remote)
		{
			return _mercurialClient.Outgoing(remote, null)
				.Select (r => new MercurialRevision(this, r.RevisionId, r.Date, r.Author, r.Email, r.Message)).ToArray();
		}
	}
}

