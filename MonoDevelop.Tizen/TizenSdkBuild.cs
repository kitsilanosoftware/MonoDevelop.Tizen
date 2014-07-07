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
		public static bool DoBuild (IProgressMonitor monitor,
					    TizenProjectConfiguration config,
					    BuildResult res)
		{
			if (!EnsureMonoRuntime (monitor, config, res))
				return false;

			if (!DoNativeMake (monitor, config, res))
				return false;

			return true;
		}

		private static string GetBuildDir (TizenProjectConfiguration config)
		{
			var project = config.ParentItem as Project;
			if (project == null)
				return null;

			return Path.Combine (project.BaseDirectory, "CommandLineBuild");
		}

		private static bool EnsureMonoRuntime (IProgressMonitor monitor,
						       TizenProjectConfiguration config,
						       BuildResult res)
		{
			var project = config.ParentItem as Project;
			if (project == null)
				return false;

			var baseDir = project.BaseDirectory;
			var incMono = Path.Combine (baseDir, "inc", "mono");
			if (Directory.Exists (incMono))
				return true;

			var zip = "/tmp/mono-tizen-3.6.0-0.i586.zip";
			var zis = new ZipInputStream (File.OpenRead (zip));
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

			return true;
		}

		private static bool DoNativeMake (IProgressMonitor monitor,
						  TizenProjectConfiguration config,
						  BuildResult res)
		{
			var buildDir = GetBuildDir (config);
			if (!Directory.Exists (buildDir)) {
				res.AddError (string.Format ("'{0}' is not a directory.", buildDir));
				return false;
			}

			var p = new Process ();
			var psi = p.StartInfo;

			psi.UseShellExecute = false;
			psi.FileName = "native-make";
			psi.WorkingDirectory = buildDir;

			p.Start ();
			p.WaitForExit ();

			if (p.ExitCode != 0) {
				res.AddError (string.Format ("native-make failed with code '{0}'", p.ExitCode));
				return false;
			}

			return true;
		}
	}
}
