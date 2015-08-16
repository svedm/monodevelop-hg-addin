using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin(
	"MonoDevelop.VersionControl.Mercurial", 
	Namespace = "MonoDevelop.VersionControl.Mercurial",
	Version = "1.0"
)]

[assembly:AddinName("Mercurial support")]
[assembly:AddinCategory("Version Control")]
[assembly:AddinDescription("MonoDevelop.VersionControl.Mercurial")]
[assembly:AddinAuthor("Svetoslav Karasev")]
[assembly:AddinDescription("Provides support for the Mercurial version control system")]
[assembly:AddinUrl("https://github.com/svedm/monodevelop-hg-addin")]