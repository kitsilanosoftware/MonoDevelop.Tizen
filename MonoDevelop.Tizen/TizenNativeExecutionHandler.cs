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

using System;
using System.Linq;
using MonoDevelop.Projects;
using MonoDevelop.Core.Execution;
using System.Collections.Generic;
using Tamir.SharpSsh;
using Tamir.SharpSsh.jsch;
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
			var targetDevice = TizenDevice.GetChosenDevice ();
			if (targetDevice == null) {
				return new NullProcessAsyncOperation (false);
			}

			var conf = cmd.Config;
			string[] extraFiles = GetExtraUploads (conf);
			TizenUtility.Upload (targetDevice, conf, extraFiles,
					     console.Out, console.Error)
				.WaitForCompleted ();

			var proc = CreateProcess (cmd, targetDevice,
						  console.Out.Write,
						  console.Error.Write);
			proc.Run ();
			return proc;
		}

		private static string[] GetExtraUploads (
			TizenProjectConfiguration conf)
		{
			var dnp = conf.ParentItem as Project;
			var files = dnp.Files;
			var pf = files.GetFileWithVirtualPath ("Main.c");
			if (pf == null)
				return null;

			var s = pf.FilePath.ToString ();
			return new string[] { s };
		}

		private static SshRemoteProcess CreateProcess (
			TizenExecutionCommand cmd,
			TizenDevice device,
			Action<string> stdOut,
			Action<string> stdErr)
		{
			string shCommand = GetShCommand (cmd);
			var ssh = new LiveSshExec (device.Address,
						   device.Username,
						   device.Password);
			return new SshRemoteProcess (ssh, device.Port,
						     shCommand, stdOut, stdErr,
						     null);
		}

		private static string GetShCommand (
			TizenExecutionCommand cmd)
		{
			var sb = new StringBuilder ();
			foreach (var arg in cmd.EnvironmentVariables)
				sb.AppendFormat ("export {0}='{1}' && ",
						 arg.Key, arg.Value);

			// Experiment: link statically to the Mono
			// runtime, and dynamically to the rest of the
			// environment--but pkg-config does not seem
			// to let us do that directly.  TODO: Optional?
			bool staticMono = true;
			var mtf = "MonoTizen_gcc_flags";

			sb.AppendFormat ("{0}=\"$(pkg-config{1} --cflags --libs mono-2)\" && ",
					 mtf, staticMono ? " --static" : "");

			if (staticMono)
				sb.AppendFormat ("{0}=\"$(echo ${0} | sed 's@ -lmono[^ ]* @ -Wl,-static&-Wl,-Bdynamic @g')\" && ",
						 mtf);

			sb.AppendFormat ("gcc -g -o {0}/{1} {0}/Main.c ${2} && ",
					 cmd.DeviceProjectPath, cmd.Name, mtf);
			sb.AppendFormat ("cd {0} && ./{1}",
					 cmd.DeviceProjectPath, cmd.Name);

			return sb.ToString ();
		}
	}
}
