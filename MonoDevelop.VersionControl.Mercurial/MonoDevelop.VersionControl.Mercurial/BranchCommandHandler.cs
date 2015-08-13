using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.VersionControl.Mercurial.GUI;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl.Mercurial
{
	internal class BranchCommandHandler : CommandHandler
	{
		protected override void Update(CommandInfo info)
		{
			MercurialVersionControl bvc = null;

			foreach (var vcs in VersionControlService.GetVersionControlSystems ())
				if (vcs is MercurialVersionControl)
					bvc = (MercurialVersionControl)vcs;

			info.Visible = (null != bvc && bvc.IsInstalled);
		}

		protected override void Run()
		{
			var bsd = new MainDialog(new List<string>(), string.Empty, Environment.GetFolderPath(Environment.SpecialFolder.Personal), true, false, false, false);
			try
			{
				if ((int)Gtk.ResponseType.Ok == bsd.Run() && !string.IsNullOrEmpty(bsd.SelectedLocation))
				{
					string branchLocation = bsd.SelectedLocation,
					branchName = GetLastChunk(branchLocation),
					localPath = Path.Combine(bsd.LocalPath, (string.Empty == branchName) ? "branch" : branchName);
					var worker = new VersionControlTask();
					worker.Description = string.Format("Branching from {0} to {1}", branchLocation, localPath);
					worker.Operation = delegate
					{
						DoBranch(branchLocation, localPath, worker.ProgressMonitor);
					};
					worker.Start();
				}
			}
			finally
			{
				bsd.Destroy();
			}

		}

		delegate bool ProjectCheck(string path);

		private static void DoBranch(string location, string localPath, IProgressMonitor monitor)
		{
			MercurialVersionControl bvc = null;

			foreach (VersionControlSystem vcs in VersionControlService.GetVersionControlSystems ())
				if (vcs is MercurialVersionControl)
					bvc = (MercurialVersionControl)vcs;

			if (null == bvc || !bvc.IsInstalled)
				throw new Exception("Mercurial is not installed");

			// Branch
			bvc.Branch(location, localPath, monitor);

			// Search for solution/project file in local branch;
			// open if found
			string[] list = System.IO.Directory.GetFiles(localPath);

			ProjectCheck[] checks =
			{
				delegate (string path)
				{
					return path.EndsWith(".mds");
				},
				delegate (string path)
				{
					return path.EndsWith(".mdp");
				},
				MonoDevelop.Projects.Services.ProjectService.IsWorkspaceItemFile
			};

			foreach (ProjectCheck check in checks)
			{
				foreach (string file in list)
				{
					if (check(file))
					{
						Gtk.Application.Invoke(delegate (object o, EventArgs ea)
							{
								IdeApp.Workspace.OpenWorkspaceItem(file);
							});
						return;
					}
				}
			}
		}

		private static string GetLastChunk(string branchLocation)
		{
			string[] chunks = null,
			separators = { "/", Path.DirectorySeparatorChar.ToString() };
			string chunk = string.Empty;

			foreach (string separator in separators)
			{
				if (branchLocation.Contains(separator))
				{
					chunks = branchLocation.Split('/');
					for (int i = chunks.Length - 1; i >= 0; --i)
					{
						if (string.Empty != (chunk = chunks[i].Trim()))
							return chunk;
					}
				}
			}

			return string.Empty;
		}
	}
}

