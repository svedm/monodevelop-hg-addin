using System;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;

namespace MonoDevelop.VersionControl.Mercurial
{
	public class MercurialNodeExtension : NodeBuilderExtension
	{
		public MercurialNodeExtension()
		{
		}

		#region implemented abstract members of NodeBuilderExtension

		public override bool CanBuildNode(Type dataType)
		{
			return typeof(ProjectFile).IsAssignableFrom(dataType)
				|| typeof(SystemFile).IsAssignableFrom(dataType)
				|| typeof(IFolderItem).IsAssignableFrom(dataType)
				|| typeof(IWorkspaceObject).IsAssignableFrom(dataType);
		}

		#endregion
	}
}

