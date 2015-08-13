namespace MonoDevelop.VersionControl.Mercurial.GUI
{
	public partial class PasswordDialog : Gtk.Dialog
	{
		public PasswordDialog(string prompt)
		{
			Build();
			Parent = MonoDevelop.Ide.IdeApp.Workbench.RootWindow;
			promptLabel.Text = GLib.Markup.EscapeText(prompt);
		}
	}
}