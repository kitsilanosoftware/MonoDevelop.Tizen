// 
// TizenExecutionHandler.cs
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

using System;
using System.Linq;
using MonoDevelop.Core.Execution;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;

namespace MonoDevelop.Tizen
{

	class TizenExecutionHandler : IExecutionHandler
	{

		public bool CanExecute (ExecutionCommand command)
		{
			return command is TizenExecutionCommand;
		}
		
		public IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
		{
			var cmd = (TizenExecutionCommand) command;
			var sdkInfo = TizenSdkInfo.GetSdkInfo ();
			if (sdkInfo == null) {
				return new NullProcessAsyncOperation (false);
			}
			
			//if (MeeGoUtility.NeedsUploading (cmd.Config)) {
				TizenUtility.Upload (sdkInfo, cmd.Config, null, console.Out, console.Error);
			//}

			var proc = CreateProcess (cmd, null, sdkInfo, console.Out.WriteLine, console.Error.WriteLine);
			proc.Start ();
			return proc;
		}
		
		public static SdbShellCommand CreateProcess (
			TizenExecutionCommand cmd,
			string sdbOptions,
			TizenSdkInfo sdkInfo,
			Action<string> stdOut, Action<string> stdErr)
		{
			string exec = GetCommandString (cmd, sdbOptions);

			var sdb = new TizenSdkSdb (cmd.Config, sdkInfo);

			return new SdbShellCommand (sdb, exec, stdOut, stdErr);
		}
		
		public static string GetCommandString (
			TizenExecutionCommand cmd,
			string sdbOptions)
		{
			string runtimeArgs = string.IsNullOrEmpty (cmd.RuntimeArguments)
				? (string.IsNullOrEmpty (sdbOptions)? "--debug" : "")
				: cmd.RuntimeArguments;
			
			var sb = new StringBuilder ();
			foreach (var arg in cmd.EnvironmentVariables)
				sb.AppendFormat ("{0}='{1}' ", arg.Key, arg.Value);
			sb.Append ("mono");
			if (!string.IsNullOrEmpty (sdbOptions))
				sb.AppendFormat (" --debug --debugger-agent={0}", sdbOptions);
			
			sb.AppendFormat (" {0} '{1}' {2}", runtimeArgs, cmd.DeviceExePath, cmd.Arguments);
			
			return sb.ToString ();
		}
	}

	class SdbShellCommand : IProcessAsyncOperation, IAsyncOperation
	{
		TizenSdkSdb sdb;
		string command;
		Action<string> stdOut, stdErr;
		Process process;
		ManualResetEvent wait = new ManualResetEvent (false);

		public SdbShellCommand (
			TizenSdkSdb sdb,
			string command,
			Action<string> stdOut,
			Action<string> stdErr)
		{
			this.sdb = sdb;
			this.command = command;
			this.stdErr = stdErr;
			this.stdOut = stdOut;
		}

		public void Start ()
		{
			// Oh.  The RunOperations thingy was expected
			// by the SSH lib, not by MonoDevelop.  Let's
			// do this for now.  TODO: Cleanup.
			var ts = new ThreadStart (this.RunOperations);
			var thread = new Thread (ts);
			thread.Start ();
		}

		private void RunOperations ()
		{
			var p = sdb.ShellNoWait (command);
			this.process = p;

			TizenUtility.Copier.Start (p.StandardOutput, stdOut);
			TizenUtility.Copier.Start (p.StandardError, stdErr);
			p.WaitForExit ();

			ExitCode = p.ExitCode;
			Success = ExitCode == 0;
			IsCompleted = true;
			wait.Set ();
			if (Completed != null)
				Completed (this);
		}

		public int ExitCode { get; private set; }
		public int ProcessId { get; private set; }

		public void Cancel ()
		{
			var p = this.process;
			if (p != null) {
				this.process = null;
				try {
					p.Kill ();
				} catch (Win32Exception x) {
					LoggingService.LogError (
						"Could not kill sdb", x);
				}
			}
		}

		public void Dispose ()
		{
			Cancel ();
		}

		public void WaitForCompleted ()
		{
			WaitHandle.WaitAll (new WaitHandle [] { wait });
		}

		public event OperationHandler Completed;

		public bool IsCompleted { get; private set; }
		public bool Success { get; private set; }
		public bool SuccessWithWarnings { get; private set; }
	}
}
