using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("VersionControl.Mercurial", 
	Namespace = "MonoDevelop",
	Version = "1.0",
	Category = "Version Control")]

[assembly:AddinName ("Mercurial support")]
[assembly:AddinDescription ("Mercurial support for the Version Control Add-in")]
[assembly:AddinUrl("https://github.com/svedm/monodevelop-hg-addin")]
[assembly:AddinAuthor("Svetoslav Karasev")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("VersionControl", MonoDevelop.BuildInfo.Version)]