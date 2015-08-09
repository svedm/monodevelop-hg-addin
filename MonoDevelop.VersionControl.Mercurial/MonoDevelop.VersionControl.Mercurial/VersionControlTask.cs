using System;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using System.Threading;

namespace MonoDevelop.VersionControl.Mercurial
{
	public delegate void VersionControlOperation();

	public class VersionControlTask
	{
		public string Description{ get; set; }

		public VersionControlOperation Operation{ get; set; }

		public IProgressMonitor ProgressMonitor{ get; protected set; }

		public VersionControlTask() : this(string.Empty, null)	{ }

		public VersionControlTask(string description, VersionControlOperation operation)
		{
			ProgressMonitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor("Version Control", null, true, true);
			Description = description;
			Operation = operation;
		}

		public void Start()
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
				try
				{
					ProgressMonitor.BeginTask(Description, 0);
					Operation();
					ProgressMonitor.ReportSuccess(GettextCatalog.GetString("Done."));
				}
				catch (Exception e)
				{
					ProgressMonitor.ReportError(e.Message, e);
				}
				finally
				{
					ProgressMonitor.EndTask();
					ProgressMonitor.Dispose();
				}
			});
		}
	}
}

