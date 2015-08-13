using System;
using Hg.Net;
using MonoDevelop.Core;
using MonoDevelop.VersionControl;

namespace MonoDevelop.VersionControl.Mercurial
{
	public class MercurialVersionControl : VersionControlSystem
	{
		public MercurialVersionControl()
		{
		}

		#region implemented abstract members of VersionControlSystem

		protected override Repository OnCreateRepositoryInstance()
		{
			return new MercurialRepository();
		}

		public override IRepositoryEditor CreateRepositoryEditor(Repository repo)
		{
			//TODO check hg instalation
			return new UrlBasedRepositoryEditor((MercurialRepository)repo);
		}

		protected override MonoDevelop.Core.FilePath OnGetRepositoryPath(MonoDevelop.Core.FilePath path, string id)
		{
			
			return path;
		}

		public override string Name
		{
			get
			{
				return "Mercurial";
			}
		}

		public static readonly string[] protocols = { "http", "https", "ssh", "file" };

		#endregion

		public void Init(string newRepoPath)
		{
			MercurialClient.Init(newRepoPath, string.Empty);
		}

		public void Branch(string location, string localPath, IProgressMonitor monitor)
		{
			try
			{
				MercurialClient.Clone(source: location, destination: localPath);
			}
			catch (Exception ex)
			{
				monitor.ReportError(ex.Message, ex);
			}

			monitor.ReportSuccess(string.Empty);
		}

		public override bool IsInstalled
		{
			get
			{
				return true;
			}
		}

		public override Repository GetRepositoryReference(FilePath path, string id)
		{
			try
			{
				string url = MercurialRepository.GetLocalBasePath(path.FullPath);
				if (string.IsNullOrEmpty(url))
				{
					return null;
				}
				return new MercurialRepository(this, string.Format("file://{0}", url));
			}
			catch (Exception ex)
			{
				// No bzr
				LoggingService.LogError(ex.ToString());
				return null;
			}
		}
	}
}

