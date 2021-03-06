﻿using System;

namespace MonoDevelop.VersionControl.Mercurial
{
	public class MercurialRevision : Revision
	{
		public readonly string RevisionNumber;

		public const string Head = "tip";
		public const string None = "NONE";

		public MercurialRevision(Repository repository, string revisionNumber)
			: base(repository)
		{
			if (string.IsNullOrEmpty(revisionNumber))
			{
				throw new ArgumentException("revision number cannot be emty");
			}
			this.RevisionNumber = revisionNumber;
		}

		public MercurialRevision(Repository repository, string revision, DateTime time, string author, string email, string message)
			: base(repository, time, author, message)
		{
			RevisionNumber = revision;
			Email = email;
		}

		#region implemented abstract members of Revision

		public override Revision GetPrevious()
		{
			int revisionNumber;
			return int.TryParse(RevisionNumber, out revisionNumber) 
				? new MercurialRevision(this.Repository, (revisionNumber - 1).ToString()) 
				: new MercurialRevision(Repository, string.Format("p1({0})", RevisionNumber));
		}

		#endregion
	}
}

