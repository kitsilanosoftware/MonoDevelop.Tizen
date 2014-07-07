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

namespace MonoDevelop.Tizen
{
	public class TizenSdkBuild
	{
		public static void DoBuild (IProgressMonitor monitor,
					    TizenProjectConfiguration config,
					    BuildResult res)
		{
			DoNativeMake (monitor, config, res);
		}

		public static bool DoNativeMake (IProgressMonitor monitor,
						 TizenProjectConfiguration config,
						 BuildResult res)
		{
			var dnp = config.ParentItem as Project;
			var clb = dnp.BaseDirectory.Combine ("CommandLineBuild");
				 
			if (!Directory.Exists (clb)) {
				res.AddError (string.Format ("'{0}' is not a directory.", clb));
				return false;
			}

			var p = new Process ();
			var psi = p.StartInfo;

			psi.UseShellExecute = false;
			psi.FileName = "native-make";
			psi.WorkingDirectory = pfp.ToString ();

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
