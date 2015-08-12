using System;
using MonoDevelop.Components.Commands;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.IO;
using MonoDevelop.VersionControl.Mercurial.GUI;

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

		[CommandUpdateHandler(MercurialCommands.Merge)]
		protected void CanMerge(CommandInfo item)
		{
			if (1 == GetItems().Count)
			{
				VersionControlItem vcitem = GetItems()[0];
				item.Visible = (vcitem.Repository is MercurialRepository &&
				((MercurialRepository)vcitem.Repository).CanMerge(vcitem.Path));
			}
			else
			{
				item.Visible = false;
			}
		}

		[CommandHandler(MercurialCommands.Merge)]
		protected void OnMerge()
		{
			VersionControlItem vcitem = GetItems()[0];
			MercurialRepository repo = ((MercurialRepository)vcitem.Repository);
			repo.Merge();
		}

		[CommandUpdateHandler(MercurialCommands.Rebase)]
		protected void CanRebase(CommandInfo item)
		{
			if (1 == GetItems().Count)
			{
				var vcitem = GetItems()[0];
				if (vcitem.Repository is MercurialRepository)
				{
					var repo = vcitem.Repository as MercurialRepository;
					item.Visible = (repo.CanPull(vcitem.Path) &&
					repo.CanRebase());
				}
			} 
			item.Visible = false;
		}

		[CommandHandler(MercurialCommands.Rebase)]
		protected void OnRebase()
		{
			var vcitem = GetItems()[0];
			var repo = ((MercurialRepository)vcitem.Repository);
			var branches = repo.GetKnownBranches(vcitem.Path);
			string defaultBranch = string.Empty,
			localPath = vcitem.IsDirectory ? (string)vcitem.Path.FullPath : Path.GetDirectoryName(vcitem.Path.FullPath);

			foreach (KeyValuePair<string, BranchType> branch in branches)
			{
				if (BranchType.Parent == branch.Value)
				{
					defaultBranch = branch.Key;
					break;
				}
			}

			var dialog = new MainDialog(branches.Keys, defaultBranch, localPath, false, true, true, false);
			try
			{
				if ((int)Gtk.ResponseType.Ok == dialog.Run() && !string.IsNullOrEmpty(dialog.SelectedLocation))
				{
					var worker = new VersionControlTask();
					worker.Description = string.Format("Rebasing on {0}", dialog.SelectedLocation);
					worker.Operation = delegate
					{
						repo.Rebase(dialog.SelectedLocation, vcitem.Path, dialog.SaveDefault, dialog.Overwrite, worker.ProgressMonitor);
					};
					worker.Start();
				}
			}
			finally
			{
				dialog.Destroy();
			}
		}
	}
}

