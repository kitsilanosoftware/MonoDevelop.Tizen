// 
// TizenProject.cs
//  
// Authors:
//       Michael Hutchinson <mhutchinson@novell.com>
//       Kitsilano Software Inc.
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
// Copyright (c) 2014 Kitsilano Software Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using System;
using System.Xml;
using System.Collections.Generic;

namespace MonoDevelop.Tizen
{
	
	public class TizenProject : DotNetAssemblyProject
	{
		// TODO: Enum with mapping to Tizen SDK names.
		public static readonly string X86 = "i386";
		public static readonly string ARM = "armel";

		#region Constructors
		
		public TizenProject ()
		{
			Init ();
		}
		
		public TizenProject (string language) : base (language)
		{
			Init ();
		}
		
		public TizenProject (string languageName, ProjectCreateInformation info, XmlElement projectOptions)
			: base (languageName, info, projectOptions)
		{
			Init ();
		}

		void Init ()
		{
			// KLUDGE: DotNetProject is hardcoded to create two configurations at
			// startup, Debug and Release, for a single platform.  Let's fix that up.
			var configs = new List<SolutionItemConfiguration>(Configurations);
			var archs = new string[] { X86, ARM };

			foreach (var config in configs) {
				Configurations.Remove (config);

				foreach (var arch in archs) {
					var newConfig = config.Clone () as TizenProjectConfiguration;
					newConfig.Platform = "Tizen_" + arch;
					Configurations.Add (newConfig);
				}
			}
		}

		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			var conf = new TizenProjectConfiguration (name);
			conf.CopyFrom (base.CreateConfiguration (name));
			return conf;
		}
		
		#endregion
		
		#region Build

		protected override BuildResult DoBuild (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			// Phase 1: Perform normal .NET project build.
			var res = base.DoBuild (monitor, configuration);
			if (res.ErrorCount > 0)
				return res;

			// Phase 2: Tizen SDK wrapping, linking, and packaging.
			var config = (TizenProjectConfiguration) GetConfiguration (configuration);
			var sdkInfo = TizenSdkInfo.GetSdkInfo ();
			if (sdkInfo == null) {
				res.AddError ("SDK information not provided.");
				return res;
			}

			var sdkBuild = new TizenSdkBuild (config, sdkInfo);
			sdkBuild.DoNativeBuild (monitor, res);

			return res;
		}

		#endregion

		#region Execution
		
		protected override ExecutionCommand CreateExecutionCommand (ConfigurationSelector configSel,
		                                                            DotNetProjectConfiguration configuration)
		{
			var conf = (TizenProjectConfiguration) configuration;
			return new TizenExecutionCommand (conf) {
				UserAssemblyPaths = GetUserAssemblyPaths (configSel),
			};
		}
		
		/*
		protected override void OnExecute (IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configSel)
		{
			var conf = (MeeGoProjectConfiguration) GetConfiguration (configSel);
			var cmd = (MeeGoExecutionCommand)CreateExecutionCommand (configSel, conf);
			
			using (var opMon = new AggregatedOperationMonitor (monitor)) {
				if (MeeGoUtility.NeedsUploading (conf)) {
					using (var op = MeeGoUtility.Upload (cmd.Device, cmd.AppExe)) {
						opMon.AddOperation (op);
						op.WaitForOutput ();
						if (op.ExitCode != 0)
							return;
					}
					MeeGoUtility.TouchUploadMarker (conf);
				}
				
				IConsole console = null;
				try {
						
					console = conf.ExternalConsole
						? context.ExternalConsoleFactory.CreateConsole (!conf.PauseConsoleOutput)
						: context.ConsoleFactory.CreateConsole (!conf.PauseConsoleOutput);
					
					var ex = context.ExecutionHandler.Execute (cmd, console);
					opMon.AddOperation (ex);
					ex.WaitForCompleted ();
				} finally {
					if (console != null)
						console.Dispose ();
				}
			}
		}*/
		
		#endregion
	}
}
