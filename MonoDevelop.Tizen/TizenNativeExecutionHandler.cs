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

using System;
using System.Linq;
using MonoDevelop.Projects;
using MonoDevelop.Core.Execution;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core;
using System.Text;
using System.Threading;

namespace MonoDevelop.Tizen
{

	class TizenNativeExecutionHandler : IExecutionHandler
	{

		public bool CanExecute (ExecutionCommand command)
		{
			return command is TizenExecutionCommand;
		}

		public IProcessAsyncOperation Execute (
			ExecutionCommand command,
			IConsole console)
		{
			var cmd = (TizenExecutionCommand) command;
			var config = cmd.Config;
			var sdkInfo = TizenSdkInfo.GetSdkInfo ();
			if (sdkInfo == null)
				return Finish (false);

			var project = config.ParentItem as Project;
			var tpkPath = FindTpkPath (project);
			if (tpkPath == null)
				return Finish (false);

			var sdkBuild = new TizenSdkBuild (config, sdkInfo);
			if (!sdkBuild.DoNativeInstall (tpkPath, console))
				return Finish (false);

			var tpkId = ExtractTpkId (tpkPath);
			if (tpkId == null)
				return Finish (false);

			var success = sdkBuild.DoNativeRun (tpkId, console);
			return Finish (success);
		}

		private IProcessAsyncOperation Finish (bool success)
		{
			return new NullProcessAsyncOperation (success);
		}

		private string FindTpkPath (Project project)
		{
			var buildDir = Path.Combine (project.BaseDirectory, "CommandLineBuild");
			var tpks = new List<string> (Directory.EnumerateFiles (buildDir, "*.tpk"));

			if (tpks.Count == 0)
				return null;

			tpks.Sort (delegate (string a, string b) {
				DateTime ta = File.GetLastWriteTime (a);
				DateTime tb = File.GetLastWriteTime (b);

				return tb.CompareTo (ta);
			});

			return tpks [0];
		}

		private string ExtractTpkId (string tpkPath)
		{
			var fileName = Path.GetFileName (tpkPath);
			var dashAt = fileName.IndexOf ('-');
			if (dashAt <= 0)
				return null;

			return fileName.Substring (0, dashAt);
		}
	}
}
