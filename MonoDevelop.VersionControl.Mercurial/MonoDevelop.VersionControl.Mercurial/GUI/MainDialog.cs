using System;
using System.Collections.Generic;
using Gtk;

namespace MonoDevelop.VersionControl.Mercurial.GUI
{
	public partial class MainDialog : Gtk.Dialog
	{
		Gtk.ListStore branchStore = new Gtk.ListStore(typeof(string));

		public string SelectedLocation
		{
			get
			{
				var loc = string.Empty;

				try
				{
					Gtk.TreeIter iter;
					branchTreeView.Selection.GetSelected(out iter);
					loc = (string)branchStore.GetValue(iter, 0);
				}
				catch
				{
					// ignored
				}

				return loc;
			}
		}

		public bool SaveDefault
		{
			get { return defaultCB.Active; }
		}

		public string LocalPath
		{
			get { return localPathButton.Filename; }
		}

		public bool Overwrite
		{
			get { return overwriteCB.Active; }
		}

		public bool OmitHistory
		{
			get { return omitCB.Active; }
		}

		protected virtual void OnOmitCBToggled(object sender, System.EventArgs e)
		{
			if (omitCB.Active)
			{
				overwriteCB.Active = false;
			}
			overwriteCB.Sensitive = omitCB.Active;
		}

		public MainDialog(ICollection<string> branchLocations, string defaultLocation, string localDirectory, bool enableLocalPathSelection, bool enableRemember, bool enableOverwrite, bool enableOmitHistory)
		{
			this.Build();

			Parent = MonoDevelop.Ide.IdeApp.Workbench.RootWindow;
			var textRenderer = new Gtk.CellRendererText();
			textRenderer.Editable = true;
			textRenderer.Edited += delegate(object o, EditedArgs args)
			{
				try
				{
					Gtk.TreeIter eiter;
					branchStore.GetIterFromString(out eiter, args.Path);
					branchStore.SetValue(eiter, 0, args.NewText);
				}
				catch
				{
					// ignored
				}
			};

			branchTreeView.Model = branchStore;
			branchTreeView.HeadersVisible = false;
			branchTreeView.AppendColumn("Branch", textRenderer, "text", 0);

			Gtk.TreeIter iter,
			defaultIter = default(Gtk.TreeIter);
			var found = false;

			foreach (var location in branchLocations)
			{
				iter = branchStore.AppendValues(location);
				if (location == defaultLocation)
				{
					defaultIter = iter;
					found = true;
				}
			}
			iter = branchStore.AppendValues(string.Empty);

			if (1 == branchLocations.Count)
			{
				branchStore.GetIterFirst(out iter);
			}

			branchTreeView.Selection.SelectIter(found ? defaultIter : iter);

			if (!string.IsNullOrEmpty(localDirectory))
				localPathButton.SetCurrentFolder(localDirectory);
			localPathButton.Sensitive = enableLocalPathSelection;
			omitCB.Visible = enableOmitHistory;
			defaultCB.Sensitive = enableRemember;
			overwriteCB.Sensitive = enableOverwrite;
		}
	}
}

