using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin (
	"MonoDevelop.Tizen", 
	Namespace = "MonoDevelop.Tizen",
	Version = "1.0"
)]

[assembly:AddinName ("MonoDevelop.Tizen")]
[assembly:AddinCategory ("MonoDevelop.Tizen")]
[assembly:AddinDescription ("MonoDevelop.Tizen")]
[assembly:AddinAuthor ("Kitsilano Software Inc.")]

[assembly:AddinDependency ("::MonoDevelop.Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("::MonoDevelop.Ide", MonoDevelop.BuildInfo.Version)]
