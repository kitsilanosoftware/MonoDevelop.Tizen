// Copyright 2014 Kitsilano Software Inc.
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
			if (!EnsureMonoRuntime (monitor, res))
				return false;

			var unversionedSo = CreateUnversionedSo (monitor, res);
			if (unversionedSo == null)
				return false;

			if (!DoNativeMake (monitor, res))
				return false;

			File.Delete (unversionedSo);

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

		private bool EnsureMonoRuntime (IProgressMonitor monitor,
						BuildResult res)
		{
			var project = GetProject ();
			if (project == null)
				return false;

			var baseDir = project.BaseDirectory;
			var incDir = Path.Combine (baseDir, "mono");
			var incMonoDir = Path.Combine (incDir, "mono");
			if (Directory.Exists (incMonoDir)) {
				monitor.Log.WriteLine ("{0} exists; skipping Mono runtime setup.", incMonoDir);
				return true;
			}

			var rtPath = SdkInfo.MonoRuntimePath;
			monitor.BeginTask (string.Format ("Unpacking Mono runtime {0}...", rtPath), 1);

			var zis = new ZipInputStream (File.OpenRead (rtPath));
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

			return true;
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

		private bool DoNativeMake (IProgressMonitor monitor,
					   BuildResult res)
		{
			var buildDir = GetProjectSubdir ("CommandLineBuild");
			if (!Directory.Exists (buildDir)) {
				res.AddError (string.Format ("'{0}' is not a directory.", buildDir));
				return false;
			}

			var nativeMakePath = Path.Combine (
				new string[] { SdkInfo.SdkPath, "tools", "ide", "bin", "native-make" });

			monitor.BeginTask ("Invoking native-make...", 1);

			var p = new Process ();
			var psi = p.StartInfo;

			psi.UseShellExecute = false;
			psi.FileName = nativeMakePath;
			psi.WorkingDirectory = buildDir;

			p.Start ();
			p.WaitForExit ();
			monitor.EndTask ();

			if (p.ExitCode != 0) {
				res.AddError (string.Format ("native-make failed with code '{0}'", p.ExitCode));
				return false;
			}

			return true;
		}
	}
}
