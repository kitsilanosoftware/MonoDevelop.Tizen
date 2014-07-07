// 
// TizenUtility.cs
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
using System.Threading;

namespace MonoDevelop.Tizen
{
	static class TizenUtility
	{
		//FIXME: needs better file list and handling of subdirectories
		public static void Upload (TizenSdkInfo sdkInfo,
					   TizenProjectConfiguration conf,
					   string[] extraPaths,
					   TextWriter outWriter,
					   TextWriter errorWriter)
		{
			var sdb = new TizenSdkSdb (conf, sdkInfo);

			var outDirFiles = Directory.GetFiles (conf.OutputDirectory, "*", SearchOption.TopDirectoryOnly);
			var op = conf.OutputDirectory.ParentDirectory;
			var localPaths = new List<string> ();
			for (int i = 0; i < outDirFiles.Length; i++)
				localPaths.Add (op.Combine (outDirFiles[i]));
			if (extraPaths != null)
				localPaths.AddRange (extraPaths);

			var s = TizenSdkSdb.DevicePathSeparator;
			var remoteDir = TizenSdkSdb.DeviceHome + s + conf.ParentItem.Name;
			foreach (var localPath in localPaths) {
				var f = Path.GetFileName (localPath);
				var remotePath = remoteDir + s + f;

				sdb.Push (localPath, remotePath);
			}
		}

		public static bool NeedsUploading (TizenProjectConfiguration conf)
		{
			var markerFile = conf.OutputDirectory.Combine (".meego_last_uploaded");
			return File.Exists (conf.CompiledOutputName) && (!File.Exists (markerFile) 
				|| File.GetLastWriteTime (markerFile) < File.GetLastWriteTime (conf.OutputAssembly));
		}
				
		public static void TouchUploadMarker (TizenProjectConfiguration conf)
		{
			var markerFile = conf.OutputDirectory.Combine (".meego_last_uploaded");
			if (File.Exists (markerFile))
				File.SetLastWriteTime (markerFile, DateTime.Now);
			else
				File.WriteAllText (markerFile, "This file is used to determine when the app was last uploaded to a device");
		}

		public static string EscapeProcessArgument (string arg)
		{
			// FIXME: What's the right kind of escaping, as we want to be maximally
			// cross-platform?  We'll chicken out for now, because we're under a
			// deadline.
			return '"' + arg + '"';
		}

		public class Copier
		{
			TextReader source;
			Action<string> target;

			public static void Start (TextReader source,
						  Action<string> target)
			{
				var copier = new Copier (source, target);
				var ts = new ThreadStart (copier.Run);
				var thread = new Thread (ts);
				thread.Start ();
			}

			private Copier (TextReader source,  Action<string> target)
			{
				this.source = source;
				this.target = target;
			}

			private void Run ()
			{
				try {
					for (;;) {
						var line = source.ReadLine ();
						if (line == null)
							break;
						target (line);
					}
				} catch (IOException) {
					// Stop copying.
				}
			}
		}
	}
}
