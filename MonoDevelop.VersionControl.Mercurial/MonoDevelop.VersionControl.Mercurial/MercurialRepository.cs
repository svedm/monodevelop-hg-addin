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

		public MercurialRepository(string rootPath)
		{
			this.RootPath = rootPath;
			_mercurialClient = new MercurialClient(rootPath, "/usr/local/bin/hg");
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
				if(success)
				{
					monitor.ReportSuccess("Successfuly pushed");
				}
				else
				{
					monitor.ReportError("Failed", new Exception("Failed"));
				}
			}
			catch(Exception ex)
			{
				monitor.ReportError(ex.Message, ex);
			}

			return new MercurialRepository(serverPath);
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
			throw new NotImplementedException();
		}

		protected override void OnCheckout(MonoDevelop.Core.FilePath targetLocalPath, Revision rev, bool recurse, MonoDevelop.Core.IProgressMonitor monitor)
		{
			throw new NotImplementedException();
		}

		protected override void OnRevert(MonoDevelop.Core.FilePath[] localPaths, bool recurse, MonoDevelop.Core.IProgressMonitor monitor)
		{
			throw new NotImplementedException();
		}

		protected override void OnRevertRevision(MonoDevelop.Core.FilePath localPath, Revision revision, MonoDevelop.Core.IProgressMonitor monitor)
		{
			throw new NotImplementedException();
		}

		protected override void OnRevertToRevision(MonoDevelop.Core.FilePath localPath, Revision revision, MonoDevelop.Core.IProgressMonitor monitor)
		{
			throw new NotImplementedException();
		}

		protected override void OnAdd(MonoDevelop.Core.FilePath[] localPaths, bool recurse, MonoDevelop.Core.IProgressMonitor monitor)
		{
			throw new NotImplementedException();
		}

		protected override void OnDeleteFiles(MonoDevelop.Core.FilePath[] localPaths, bool force, MonoDevelop.Core.IProgressMonitor monitor, bool keepLocal)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		protected override void OnIgnore(MonoDevelop.Core.FilePath[] localPath)
		{
			throw new NotImplementedException();
		}

		protected override void OnUnignore(MonoDevelop.Core.FilePath[] localPath)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region implemented abstract members of UrlBasedRepository

		public override string[] SupportedProtocols
		{
			get
			{
				throw new NotImplementedException();
			}
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

		private static VersionStatus ConvertStatus (Status status) 
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

		#endregion
	}
}

