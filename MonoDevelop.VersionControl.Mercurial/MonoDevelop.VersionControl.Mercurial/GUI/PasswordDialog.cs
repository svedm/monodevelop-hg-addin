using System;

namespace MonoDevelop.VersionControl.Mercurial.GUI
{
	public partial class PasswordDialog : Gtk.Dialog
	{
		public PasswordDialog(string prompt)
		{
			this.Build();
			this.Parent = MonoDevelop.Ide.IdeApp.Workbench.RootWindow;
			this.promptLabel.Text = GLib.Markup.EscapeText(prompt);
		}
	}
}