// Copyright (c) 2014 Kitsilano Software Inc.
//
// This file is part of MonoTizen.
//
// MonoTizen is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// MonoTizen is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with MonoTizen.  If not, see <http://www.gnu.org/licenses/>.

using System.IO;
using System.Text;
using System.Diagnostics;
using System;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using ICSharpCode.SharpZipLib.Zip;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Tizen
{
	public class TizenSdkBuild
	{
		public TizenSdkBuild (TizenProjectConfiguration config,
				      TizenSdkInfo sdkInfo)
		{
			this.Config = config;
			this.SdkInfo = sdkInfo;
		}

		public TizenProjectConfiguration Config { get; set; }
		TizenSdkInfo SdkInfo { get; set; }

		public bool DoNativeBuild (IProgressMonitor monitor,
					   BuildResult res)
		{
			var arch = EnsureMonoRuntime (monitor, res);
			if (arch == null)
				return false;

			var unversionedSo = CreateUnversionedSo (monitor, res);
			if (unversionedSo == null)
				return false;

			if (!DoNativeMake (monitor, res, arch))
				return false;

			File.Delete (unversionedSo);

			if (!DoNativePackaging (monitor, res, arch))
				return false;

			return true;
		}

		private Project GetProject ()
		{
			return Config.ParentItem as Project;
		}

		private string GetProjectSubdir (string subdir)
		{
			var project = GetProject ();
			if (project == null)
				return null;

			return Path.Combine (project.BaseDirectory, subdir);
		}

		private string GetArch ()
		{
			var platform = Config.Platform;

			if (platform.EndsWith (TizenProject.X86))
				return TizenProject.X86;

			if (platform.EndsWith (TizenProject.ARM))
				return TizenProject.ARM;

			return null;
		}

		private string EnsureMonoRuntime (IProgressMonitor monitor,
						  BuildResult res)
		{
			var project = GetProject ();
			if (project == null)
				return null;

			var arch = GetArch ();
			string rtPath = null;
			if (arch == TizenProject.X86)
				rtPath = SdkInfo.MonoX86Path;
			else if (arch == TizenProject.ARM)
				rtPath = SdkInfo.MonoArmPath;
			if (string.IsNullOrEmpty (rtPath) || !File.Exists (rtPath)) {
				res.AddError (string.Format ("Cannot determine Mono runtime location for {0}",
							     Config.Platform));
				return null;
			}

			monitor.BeginTask (string.Format ("Unpacking Mono runtime {0}...", rtPath), 1);

			var zis = new ZipInputStream (File.OpenRead (rtPath));
			var baseDir = project.BaseDirectory;
			var buffer = new byte[4096];
			for (var ze = zis.GetNextEntry (); ze != null; ze = zis.GetNextEntry ()) {
				var target = Path.Combine (baseDir, ze.Name);
				if (ze.IsDirectory) {
					if (!Directory.Exists (target)) {
						Directory.CreateDirectory (target);
					}
				} else if (ze.IsFile) {
					using (var f = File.Create (target)) {
						while (true) {
							var n = zis.Read (buffer, 0, buffer.Length);
							if (n <= 0)
								break;
							f.Write (buffer, 0, n);
						}
					}
				}
			}

			monitor.EndTask ();

			return arch;
		}

		private string CreateUnversionedSo (IProgressMonitor monitor,
						    BuildResult res)
		{
			var libDir = GetProjectSubdir ("lib");
			if (!Directory.Exists (libDir)) {
				res.AddError (string.Format ("'{0}' is not a directory.", libDir));
				return null;
			}

			var libNameAt = libDir.Length + 1;
			foreach (var de in Directory.EnumerateFiles (libDir, "libmono*.so.*")) {
				var extAt = de.LastIndexOf (".so.");
				if (extAt > libNameAt) {
					var unversionedSo = de.Substring (0, extAt) + ".so";
					if (File.Exists (unversionedSo))
						return unversionedSo;

					monitor.BeginTask ("Installing unversioned .so...", 1);
					File.Copy (de, unversionedSo);
					monitor.EndTask ();
					return unversionedSo;
				}
			}

			return null;
		}

		interface INativeToolReporter
		{
			void BeginTask (string task);
			void EndTask ();
			void AddError (string message);
			void WriteLine (string line);
		}

		public class BuildReporter : INativeToolReporter
		{
			private IProgressMonitor monitor;
			private BuildResult res;

			public BuildReporter (IProgressMonitor monitor,
					      BuildResult res)
			{
				this.monitor = monitor;
				this.res = res;
			}

			public void BeginTask (string task)
			{
				monitor.BeginTask (task, 1);
			}

			public void EndTask ()
			{
				monitor.EndTask ();
			}

			public void AddError (string message)
			{
				res.AddError (message);
			}

			public void WriteLine (string line)
			{
				monitor.Log.WriteLine (line);
			}
		}

		public class BasicReporter : INativeToolReporter
		{
			private TextWriter writer;
			private string task;

			public BasicReporter (TextWriter writer)
			{
				this.writer = writer;
			}

			public void BeginTask (string task)
			{
				this.task = task;
				writer.WriteLine ("*** Begin task: {0}", task);
			}

			public void EndTask ()
			{
				writer.WriteLine ("*** End task: {0}", task);
			}

			public void AddError (string message)
			{
				writer.WriteLine ("*** Error: {0}", message);
			}

			public void WriteLine (string line)
			{
				writer.WriteLine (line);
			}
		}

		private string GetNativeBuildDir (INativeToolReporter reporter)
		{
			var buildDir = GetProjectSubdir ("CommandLineBuild");
			if (!Directory.Exists (buildDir)) {
				reporter.AddError (string.Format ("'{0}' is not a directory.", buildDir));
				return null;
			}
			return buildDir;
		}

		private string GetNativeTool (string tool)
		{
			return Path.Combine (
				new string[] { SdkInfo.SdkPath, "tools", "ide", "bin", tool });
		}

		private bool InvokeNativeTool (INativeToolReporter reporter,
					       string tool,
					       string arguments)
		{
			var buildDir = GetNativeBuildDir (reporter);
			if (buildDir == null)
				return false;

			var p = new Process ();
			var psi = p.StartInfo;

			psi.UseShellExecute = false;
			psi.FileName = GetNativeTool (tool);
			psi.WorkingDirectory = buildDir;
			psi.Arguments = arguments;
			psi.RedirectStandardOutput = true;
			psi.RedirectStandardError = true;

			reporter.BeginTask (string.Format ("Invoking {0}...", tool));
			reporter.WriteLine ("Command: " + psi.FileName + " " + arguments);
			p.Start ();

			TizenUtility.Copier.Start (p.StandardOutput, reporter.WriteLine);
			TizenUtility.Copier.Start (p.StandardError, reporter.WriteLine);

			p.WaitForExit ();
			reporter.EndTask ();

			if (p.ExitCode != 0) {
				reporter.AddError (string.Format ("{0} failed with code '{1}'", tool, p.ExitCode));
				return false;
			}

			return true;
		}

		private bool DoNativeMake (IProgressMonitor monitor,
					   BuildResult res,
					   string arch)
		{
			return InvokeNativeTool (new BuildReporter (monitor, res), "native-make",
						 "--arch " + arch);
		}

		private string Escape (string arg)
		{
			return TizenUtility.EscapeProcessArgument (arg);
		}

		private string GetNativePackagingArguments (string arch)
		{
			var sb = new StringBuilder ();

			sb.AppendFormat ("--arch {0} ", arch);
			sb.AppendFormat ("-ak {0} ", Escape (SdkInfo.AuthorKey));
			// Tizen passwords are not sensitive, are they?
			sb.AppendFormat ("-ap {0} ", Escape (SdkInfo.AuthorKeyPassword));

			return sb.ToString ();
		}

		private bool DoNativePackaging (IProgressMonitor monitor,
						BuildResult res,
						string arch)
		{
			var arguments = GetNativePackagingArguments (arch);

			return InvokeNativeTool (new BuildReporter (monitor, res), "native-packaging", arguments);
		}

		private string GetPackageBasedArguments (string tpkPath)
		{
			var sb = new StringBuilder ();

			if (!string.IsNullOrEmpty (SdkInfo.DeviceId))
				sb.AppendFormat ("--serial {0} ", Escape (SdkInfo.DeviceId));
			sb.AppendFormat ("-p {0}", Escape (tpkPath));

			return sb.ToString ();
		}

		public bool DoNativeInstall (string tpkPath, IConsole console)
		{
			var arguments = GetPackageBasedArguments (tpkPath);

			return InvokeNativeTool (new BasicReporter (console.Out), "native-install", arguments);
		}

		public bool DoNativeRun (string tpkId, IConsole console)
		{
			var arguments = GetPackageBasedArguments (tpkId);

			return InvokeNativeTool (new BasicReporter (console.Out), "native-run", arguments);
		}
	}
}
