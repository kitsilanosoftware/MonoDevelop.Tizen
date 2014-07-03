// 
// MeeGoUtility.cs
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

using System;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core;
using MonoDevelop.Ide;
 
using System.Diagnostics;
using Tamir.SharpSsh;
using System.Threading;

namespace MonoDevelop.MeeGo
{
	static class MeeGoUtility
	{
		//FIXME: needs better file list and handling of subdirectories
		public static IAsyncOperation Upload (MeeGoDevice targetDevice, MeeGoProjectConfiguration conf,
						      string[] extraFiles,
						      TextWriter outWriter, TextWriter errorWriter)
		{
			var sftp = new Sftp (targetDevice.Address, targetDevice.Username, targetDevice.Password);
			
			var outDirFiles = Directory.GetFiles (conf.OutputDirectory, "*", SearchOption.TopDirectoryOnly);
			var op = conf.OutputDirectory.ParentDirectory;
			var files = new List<string> ();
			for (int i = 0; i < outDirFiles.Length; i++)
				files.Add (op.Combine (outDirFiles[i]));
			if (extraFiles != null)
				files.AddRange (extraFiles);
			
			var scop = new SshTransferOperation<Sftp> (sftp, targetDevice.Port, delegate (Sftp s) {
				var dir = conf.ParentItem.Name;
				try {
					s.Mkdir (dir);
				} catch {}
				s.Put (files.ToArray (), dir);
			});
			scop.Run ();
			return scop;
		}
		
		public static bool NeedsUploading (MeeGoProjectConfiguration conf)
		{
			var markerFile = conf.OutputDirectory.Combine (".meego_last_uploaded");
			return File.Exists (conf.CompiledOutputName) && (!File.Exists (markerFile) 
				|| File.GetLastWriteTime (markerFile) < File.GetLastWriteTime (conf.OutputAssembly));
		}
				
		public static void TouchUploadMarker (MeeGoProjectConfiguration conf)
		{
			var markerFile = conf.OutputDirectory.Combine (".meego_last_uploaded");
			if (File.Exists (markerFile))
				File.SetLastWriteTime (markerFile, DateTime.Now);
			else
				File.WriteAllText (markerFile, "This file is used to determine when the app was last uploaded to a device");
		}
	}
	
	class MeeGoDevice
	{
		public MeeGoDevice (string address, string username, string password)
		{
			ushort port;
			MaybeSplitHostPort (ref address, out port);

			this.Address = address;
			this.Port = port;
			this.Username = username;
			this.Password = password;
		}
		
		public string Address  { get; set; }
		public ushort Port     { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }

		private static void MaybeSplitHostPort (ref string address,
							out ushort port)
		{
			port = 0;

			int colonAt = address.IndexOf (':');
			if (colonAt <= 0)
				return;

			string portStr = address.Substring (colonAt + 1);
			if (! UInt16.TryParse (portStr, out port))
				return;

			address = address.Substring (0, colonAt);
		}

		static MeeGoDevice chosenDevice;
		
		public static MeeGoDevice GetChosenDevice ()
		{
			if (chosenDevice == null) {
				DispatchService.GuiSyncDispatch (delegate {
					chosenDevice = MeeGoDevicePicker.GetDevice (null);
				});
			}
			return chosenDevice;
		}
		
		public static void ResetChosenDevice ()
		{
			chosenDevice = null;
		}
	}
}

