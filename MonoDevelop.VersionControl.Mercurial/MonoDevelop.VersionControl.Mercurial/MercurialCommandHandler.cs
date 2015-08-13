using System;
using MonoDevelop.Components.Commands;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.IO;
using MonoDevelop.VersionControl.Mercurial.GUI;
using MonoDevelop.Projects;
using Gtk;
using MonoDevelop.Ide;

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
			var vcitem = GetItems()[0];
			((MercurialRepository)vcitem.Repository).Ignore(new [] { vcitem.Path });
		}

		[CommandHandler(Commands.Publish)]
		[CommandHandler(MercurialCommands.Push)]
		protected void OnMercurialPublish()
		{
			var vcitem = GetItems()[0];
			var repo = ((MercurialRepository)vcitem.Repository);
			var branches = repo.GetKnownBranches(vcitem.Path);
			string defaultBranch = string.Empty,
			localPath = vcitem.IsDirectory ? (string)vcitem.Path.FullPath : Path.GetDirectoryName(vcitem.Path.FullPath);

			if (repo.IsModified(MercurialRepository.GetLocalBasePath(vcitem.Path.FullPath)))
			{
				MessageDialog md = new MessageDialog(MonoDevelop.Ide.IdeApp.Workbench.RootWindow, DialogFlags.Modal, 
					                   MessageType.Question, ButtonsType.YesNo, 
					                   GettextCatalog.GetString("You have uncommitted local changes. Push anyway?"));
				try
				{
					if ((int)ResponseType.Yes != md.Run())
					{
						return;
					}
				}
				finally
				{
					md.Destroy();
				}
			}

			foreach (var branch in branches)
			{
				if (BranchType.Parent == branch.Value)
				{
					defaultBranch = branch.Key;
					break;
				}
			}

			var bsd = new MainDialog(branches.Keys, defaultBranch, localPath, false, true, true, true);
			try
			{
				if ((int)Gtk.ResponseType.Ok == bsd.Run() && !string.IsNullOrEmpty(bsd.SelectedLocation))
				{
					var worker = new VersionControlTask();
					worker.Description = string.Format("Pushing to {0}", bsd.SelectedLocation);
					worker.Operation = delegate
					{
						repo.Push(bsd.SelectedLocation, vcitem.Path, bsd.SaveDefault, bsd.Overwrite, bsd.OmitHistory, worker.ProgressMonitor);
					};
					worker.Start();
				}
			}
			finally
			{
				bsd.Destroy();
			}
		}

		[CommandUpdateHandler(Commands.Publish)]
		[CommandUpdateHandler(MercurialCommands.Push)]
		protected void UpdateMercurialPublish(CommandInfo item)
		{
			CanPull(item);
		}

		[CommandHandler(MercurialCommands.Export)]
		protected void OnExport()
		{
			var vcitem = GetItems()[0];
			var repo = ((MercurialRepository)vcitem.Repository);

			var fsd = new FileChooserDialog(GettextCatalog.GetString("Choose export location"), 
				          null, FileChooserAction.Save, "Cancel", ResponseType.Cancel, 
				          "Save", ResponseType.Accept);
			fsd.SetCurrentFolder(vcitem.Path.FullPath.ParentDirectory);

			try
			{
				if ((int)Gtk.ResponseType.Accept == fsd.Run() && !string.IsNullOrEmpty(fsd.Filename))
				{
					var worker = new VersionControlTask();
					worker.Description = string.Format("Exporting to {0}", fsd.Filename);
					worker.Operation = delegate
					{
						repo.Export(vcitem.Path, fsd.Filename, worker.ProgressMonitor);
					};
					worker.Start();
				}
			}
			finally
			{
				fsd.Destroy();
			}
		}

		[CommandUpdateHandler(MercurialCommands.Export)]
		protected void UpdateExport(CommandInfo item)
		{
			CanPull(item);
		}

		[CommandUpdateHandler(MercurialCommands.Incoming)]
		protected void CanGetIncoming(CommandInfo item)
		{
			if (GetItems().Count == 1)
			{
				var vcitem = GetItems()[0];
				item.Visible = (vcitem.Repository is MercurialRepository);
			}
			else
			{
				item.Visible = false;
			}
		}

		[CommandHandler(MercurialCommands.Incoming)]
		protected void OnGetIncoming()
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
			}// check for parent branch

			var bsd = new MainDialog(branches.Keys, defaultBranch, localPath, false, false, false, false);
			try
			{
				if ((int)Gtk.ResponseType.Ok == bsd.Run())
				{
					var worker = new VersionControlTask();
					worker.Description = string.Format("Incoming from {0}", bsd.SelectedLocation);
					worker.Operation = delegate
					{
						repo.LocalBasePath = MercurialRepository.GetLocalBasePath(localPath);
						Revision[] history = repo.GetIncoming(bsd.SelectedLocation);
						DispatchService.GuiDispatch(() =>
							{
								var view = new MonoDevelop.VersionControl.Views.LogView(localPath, true, history, repo);
								//IdeApp.Workbench.OpenDocument (view, true);
							});
					};
					worker.Start();
				}
			}
			finally
			{
				bsd.Destroy();
			}

		}

		[CommandUpdateHandler(MercurialCommands.Outgoing)]
		protected void CanGetOutgoing(CommandInfo item)
		{
			if (GetItems().Count == 1)
			{
				var vcitem = GetItems()[0];
				item.Visible = (vcitem.Repository is MercurialRepository);
			}
			else
			{
				item.Visible = false;
			}
		}

		[CommandHandler(MercurialCommands.Outgoing)]
		protected void OnGetOutgoing()
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
			}// check for parent branch

			var bsd = new MainDialog(branches.Keys, defaultBranch, localPath, false, false, false, false);
			try
			{
				if ((int)Gtk.ResponseType.Ok == bsd.Run())
				{
					var worker = new VersionControlTask();
					worker.Description = string.Format("Outgoing to {0}", bsd.SelectedLocation);
					worker.Operation = delegate
					{
						repo.LocalBasePath = MercurialRepository.GetLocalBasePath(localPath);
						Revision[] history = repo.GetOutgoing(bsd.SelectedLocation);
						DispatchService.GuiDispatch(() =>
							{
								var view = new MonoDevelop.VersionControl.Views.LogView(localPath, true, history, repo);
								//IdeApp.Workbench.OpenDocument (view, true);
							});
					};
					worker.Start();
				}
			}
			finally
			{
				bsd.Destroy();
			}
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

