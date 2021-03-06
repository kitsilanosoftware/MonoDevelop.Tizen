// 
// TizenSoftDebuggerSession.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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

using System.Net;
using MonoDevelop.Debugger.Soft;
using Mono.Debugging.Soft;
using Mono.Debugging.Client;
using MonoDevelop.Core.Execution;
using System.IO;

namespace MonoDevelop.Tizen
{
	public class TizenSoftDebuggerSession : Mono.Debugging.Soft.SoftDebuggerSession
	{
		SdbShellCommand process;
		
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			var dsi = (TizenSoftDebuggerStartInfo) startInfo;
			StartProcess (dsi);
			StartListening (dsi);
		}
		
		protected override string GetConnectingMessage (DebuggerStartInfo startInfo)
		{
			var dsi = (TizenSoftDebuggerStartInfo) startInfo;
			var dra = (SoftDebuggerRemoteArgs) dsi.StartArgs;

			return string.Format ("Waiting for debugger to connect on {0}:{1}...", dra.Address, dra.DebugPort);
		}
		
		protected override void EndSession ()
		{
			base.EndSession ();
			EndProcess ();
		}
		
		void StartProcess (TizenSoftDebuggerStartInfo dsi)
		{
			TizenUtility.Upload (dsi.SdkInfo, dsi.ExecutionCommand.Config, null, null, null);
			var dra = (SoftDebuggerRemoteArgs) dsi.StartArgs;
			string debugOptions = string.Format ("transport=dt_socket,address={0}:{1}", dra.Address, dra.DebugPort);
			
			process = TizenExecutionHandler.CreateProcess (dsi.ExecutionCommand, debugOptions, dsi.SdkInfo,
			                                               x => OnTargetOutput (false, x),
			                                               x => OnTargetOutput (true, x));
			
			process.Completed += delegate {
				process = null;
			};
			
			TargetExited += delegate {
				EndProcess ();
			};
			
			process.Start();
		}
		
		void EndProcess ()
		{
			if (process == null)
				return;
			if (!process.IsCompleted) {
				try {
					process.Cancel ();
				} catch {}
			}
		}
		
		protected override void OnExit ()
		{
			base.OnExit ();
			EndProcess ();
		}
	}
	
	class TizenSoftDebuggerStartInfo : SoftDebuggerStartInfo
	{
		public TizenExecutionCommand ExecutionCommand { get; private set; }
		public TizenSdkInfo SdkInfo { get; private set; }
		
		public TizenSoftDebuggerStartInfo (IPAddress address,
						   int debugPort,
						   TizenExecutionCommand cmd,
						   TizenSdkInfo sdkInfo)
			: base (new SoftDebuggerListenArgs (cmd.Name, address, debugPort))
		{
			ExecutionCommand = cmd;
			SdkInfo = sdkInfo;
		}
	}
}
