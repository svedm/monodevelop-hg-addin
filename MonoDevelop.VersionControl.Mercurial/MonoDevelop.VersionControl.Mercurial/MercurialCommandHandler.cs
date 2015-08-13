using System;
using MonoDevelop.Components.Commands;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.IO;
using MonoDevelop.VersionControl.Mercurial.GUI;
using MonoDevelop.Projects;
using Gtk;

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

		[CommandUpdateHandler(MercurialCommands.Pull)]
		protected void CanPull(CommandInfo item)
		{
			if (GetItems().Count == 1)
			{
				var vcitem = GetItems()[0];
				item.Visible = (vcitem.Repository is MercurialRepository &&
				((MercurialRepository)vcitem.Repository).CanPull(vcitem.Path));
			}
			else
			{
				item.Visible = false;
			}
		}

		[CommandHandler(MercurialCommands.Pull)]
		protected void OnPull()
		{
			var vcitem = GetItems()[0];
			var repo = ((MercurialRepository)vcitem.Repository);
			var branches = repo.GetKnownBranches(vcitem.Path);
			string defaultBranch = string.Empty,
			localPath = vcitem.IsDirectory ? (string)vcitem.Path.FullPath : Path.GetDirectoryName(vcitem.Path.FullPath);

			foreach (var branch in branches)
			{
				if (BranchType.Parent == branch.Value)
				{
					defaultBranch = branch.Key;
					break;
				}
			}

			var bsd = new MainDialog(branches.Keys, defaultBranch, localPath, false, true, true, false);
			try
			{
				if ((int)Gtk.ResponseType.Ok == bsd.Run() && !string.IsNullOrEmpty(bsd.SelectedLocation))
				{
					var worker = new VersionControlTask();
					worker.Description = string.Format("Pulling from {0}", bsd.SelectedLocation);
					worker.Operation = delegate
					{
						repo.Pull(bsd.SelectedLocation, vcitem.Path, bsd.SaveDefault, bsd.Overwrite, worker.ProgressMonitor);
					};
					worker.Start();
				}
			}
			finally
			{
				bsd.Destroy();
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

		[CommandUpdateHandler(MercurialCommands.Init)]
		protected void CanInit(CommandInfo item)
		{
			if (1 == GetItems().Count)
			{
				var vcitem = GetItems()[0];
				if (vcitem.WorkspaceObject is Solution && null == vcitem.Repository)
				{
					item.Visible = true;
					return;
				}
			} 
			item.Visible = false;
		}

		[CommandHandler(MercurialCommands.Init)]
		protected void OnInit()
		{
			MercurialVersionControl bvc = null;
			MercurialRepository repo = null;
			var vcitem = GetItems()[0];
			var path = vcitem.Path;
			List<FilePath> addFiles = null;
			var solution = (Solution)vcitem.WorkspaceObject;

			foreach (var vcs in VersionControlService.GetVersionControlSystems ())
				if (vcs is MercurialVersionControl)
					bvc = (MercurialVersionControl)vcs;

			if (null == bvc || !bvc.IsInstalled)
				throw new Exception("Can't use mercurial");

			bvc.Init(path);

			repo = new MercurialRepository(bvc, string.Format("file://{0}", path));
			addFiles = GetAllFiles(solution);

			repo.Add(addFiles.ToArray(), false, null);
			solution.NeedsReload = true;
		}

		[CommandUpdateHandler(MercurialCommands.Ignore)]
		protected void CanIgnore(CommandInfo item)
		{
			if (GetItems().Count == 1)
			{
				var vcitem = GetItems()[0];
				if (vcitem.Repository is MercurialRepository)
				{
					item.Visible = !((MercurialRepository)vcitem.Repository).IsVersioned(vcitem.Path);
					return;
				}
			} 
			item.Visible = false;
		}

		[CommandHandler(MercurialCommands.Ignore)]
		protected void OnIgnore()
		{
			VersionControlItem vcitem = GetItems()[0];
			((MercurialRepository)vcitem.Repository).Ignore(new [] { vcitem.Path });
		}

		private static List<FilePath> GetAllFiles(Solution s)
		{
			var files = new List<FilePath> { s.FileName };

			foreach (var child in s.GetAllSolutions())
			{
				if (s != child)
					files.AddRange(GetAllFiles(child));
			}

			foreach (var project in s.GetAllProjects ())
			{
				files.Add(project.FileName);
				foreach (var pfile in project.Files)
				{
					files.Add(pfile.FilePath);
				}
			}

			return files;
		}
	}
}

