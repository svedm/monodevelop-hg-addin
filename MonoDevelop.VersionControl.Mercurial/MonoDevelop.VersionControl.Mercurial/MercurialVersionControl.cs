using System;

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
			throw new NotImplementedException();
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
	}
}

