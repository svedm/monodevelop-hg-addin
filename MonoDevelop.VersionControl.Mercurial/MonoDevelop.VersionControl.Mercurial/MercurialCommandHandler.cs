using System;
using MonoDevelop.Components.Commands;
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Mercurial
{
	public class MercurialCommandHandler : VersionControlCommandHandler
	{
		public MercurialCommandHandler()
		{
		}

		[CommandUpdateHandler(MercurialCommands.Resolve)]
		protected void CanResolve(CommandInfo item)
		{
			bool visible = true;

			foreach (var vcitem in GetItems ())
			{
				if (!(visible = (vcitem.Repository is MercurialRepository &&
				    ((MercurialRepository)vcitem.Repository).CanResolve(vcitem.Path))))
				{
					break;
				}
			}

			item.Visible = visible;
		}

		[CommandHandler(MercurialCommands.Resolve)]
		protected void OnResolve()
		{
			List<FilePath> files = null;
			MercurialRepository repo = null;

			foreach (VersionControlItemList repolist in GetItems().SplitByRepository())
			{
				repo = (MercurialRepository)repolist[0].Repository;
				files = new List<FilePath>(repolist.Count);
				foreach (var item in repolist)
				{
					files.Add(new FilePath(item.Path));
				}

				var worker = new VersionControlTask();
				worker.Description = string.Format("Resolving {0}", files[0]);
				worker.Operation = delegate
				{
					repo.Resolve(files.ToArray(), true, worker.ProgressMonitor);
				};
				worker.Start();
			}
		}

		[CommandUpdateHandler (MercurialCommands.Merge)]
		protected void CanMerge (CommandInfo item)
		{
			if (1 == GetItems ().Count) {
				VersionControlItem vcitem = GetItems ()[0];
				item.Visible = (vcitem.Repository is MercurialRepository &&
					((MercurialRepository)vcitem.Repository).CanMerge(vcitem.Path));
			} else { item.Visible = false; }
		}

		[CommandHandler (MercurialCommands.Merge)]
		protected void OnMerge()
		{
			VersionControlItem vcitem = GetItems ()[0];
			MercurialRepository repo = ((MercurialRepository)vcitem.Repository);
			repo.Merge ();
		}
	}
}

