using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin (
	"MonoDevelop.Tizen", 
	Namespace = "MonoDevelop.Tizen",
	Version = "1.0.0"
)]

[assembly:AddinName ("MonoDevelop.Tizen")]
[assembly:AddinCategory ("Mobile Development")]
[assembly:AddinDescription ("Support for developing and deploying LGPLv2-compliant applications using Mono for Tizen. If your application is not LGPLv2-compliant then you will need to purchase a commercial license from Xamarin. See http://xamarin.com/licensing.")]
[assembly:AddinAuthor ("Kitsilano Software Inc.")]

[assembly:AddinDependency ("::MonoDevelop.Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("::MonoDevelop.Ide", MonoDevelop.BuildInfo.Version)]
