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

namespace MonoDevelop.MeeGo
{

	class TizenNativeExecutionHandler : IExecutionHandler
	{

		public bool CanExecute (ExecutionCommand command)
		{
			return command is MeeGoExecutionCommand;
		}

		public IProcessAsyncOperation Execute (
			ExecutionCommand command,
			IConsole console)
		{
			var cmd = (MeeGoExecutionCommand) command;
			var targetDevice = MeeGoDevice.GetChosenDevice ();
			if (targetDevice == null) {
				return new NullProcessAsyncOperation (false);
			}

			var conf = cmd.Config;
			string[] extraFiles = GetExtraUploads (conf);
			MeeGoUtility.Upload (targetDevice, conf, extraFiles,
					     console.Out, console.Error)
				.WaitForCompleted ();

			var proc = CreateProcess (cmd, targetDevice,
						  console.Out.Write,
						  console.Error.Write);
			proc.Run ();
			return proc;
		}

		private static string[] GetExtraUploads (
			MeeGoProjectConfiguration conf)
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
			MeeGoExecutionCommand cmd,
			MeeGoDevice device,
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
			MeeGoExecutionCommand cmd)
		{
			var sb = new StringBuilder ();
			foreach (var arg in cmd.EnvironmentVariables)
				sb.AppendFormat ("export {0}='{1}'; ", arg.Key, arg.Value);

			sb.AppendFormat ("gcc -g -o {0}/{1} {0}/Main.c $(pkg-config --cflags --libs mono-2)", cmd.DeviceProjectPath, cmd.Name);
			sb.AppendFormat (" && cd {0} && ./{1}", cmd.DeviceProjectPath, cmd.Name);

			return sb.ToString ();
		}
	}
}
