using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.VersionControl.Mercurial.GUI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl.Mercurial
{
	internal class BranchCommandHandler : CommandHandler
	{
		protected override void Update(CommandInfo info)
		{
			MercurialVersionControl bvc = null;

			foreach (var vcs in VersionControlService.GetVersionControlSystems().OfType<MercurialVersionControl>())
			{
				bvc = vcs;
			}

			info.Visible = (null != bvc && bvc.IsInstalled);
		}

		protected override void Run()
		{
			var bsd = new MainDialog(new List<string>(), string.Empty, Environment.GetFolderPath(Environment.SpecialFolder.Personal), true, false, false, false);
			try
			{
				if ((int)Gtk.ResponseType.Ok != bsd.Run() || string.IsNullOrEmpty(bsd.SelectedLocation)) return;

				string branchLocation = bsd.SelectedLocation,
					branchName = GetLastChunk(branchLocation),
					localPath = Path.Combine(bsd.LocalPath, (string.Empty == branchName) ? "branch" : branchName);
				var worker = new VersionControlTask
				{
					Description = string.Format("Branching from {0} to {1}", branchLocation, localPath)
				};
				worker.Operation = delegate
				{
					DoBranch(branchLocation, localPath, worker.ProgressMonitor);
				};
				worker.Start();
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

			foreach (var vcs in VersionControlService.GetVersionControlSystems().OfType<MercurialVersionControl>())
			{ 
				bvc = vcs;
			}

			if (null == bvc || !bvc.IsInstalled)
				throw new Exception("Mercurial is not installed");

			bvc.Branch(location, localPath, monitor);

			var list = Directory.GetFiles(localPath);

			ProjectCheck[] checks =
				{
					path => path.EndsWith(".mds"),
					path => path.EndsWith(".mdp"),
					MonoDevelop.Projects.Services.ProjectService.IsWorkspaceItemFile
				};

			foreach (var file in from check in checks from file in list where check(file) select file)
			{
				var file1 = file;
				Gtk.Application.Invoke(delegate(object o, EventArgs ea)
				{
					IdeApp.Workspace.OpenWorkspaceItem(file1);
				});
				return;
			}
		}

		private static string GetLastChunk(string branchLocation)
		{
			string[] separators = { "/", Path.DirectorySeparatorChar.ToString() };
			var chunk = string.Empty;

			foreach (var chunks in from separator in separators where branchLocation.Contains(separator) select branchLocation.Split('/'))
			{
				for (var i = chunks.Length - 1; i >= 0; --i)
				{
					if (string.Empty != (chunk = chunks[i].Trim()))
						return chunk;
				}
			}

			return string.Empty;
		}
	}
}

