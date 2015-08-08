using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin(
	"MonoDevelop.VersionControl.Mercurial", 
	Namespace = "MonoDevelop.VersionControl.Mercurial",
	Version = "1.0"
)]

[assembly:AddinName("MonoDevelop.VersionControl.Mercurial")]
[assembly:AddinCategory("IDE extensions")]
[assembly:AddinDescription("MonoDevelop.VersionControl.Mercurial")]
[assembly:AddinAuthor("Svetoslav Karasev")]

[assembly:AddinDependency("Core", "5.9")]
[assembly:AddinDependency("Ide", "5.9")]
[assembly:AddinDependency("VersionControl", "5.9")]