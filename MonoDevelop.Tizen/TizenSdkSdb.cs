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

namespace MonoDevelop.Tizen
{
	public class TizenSdkSdb
	{
		public TizenSdkSdb (TizenProjectConfiguration config,
				    TizenDevice device)
		{
			this.Config = config;
			this.Device = device;
		}

		public static readonly string DeviceHome = "/home/developer/";
		public static readonly string DevicePathSeparator = "/";
		public TizenProjectConfiguration Config { get; set; }
		TizenDevice Device { get; set; }

		private string GetSdbPath ()
		{
			var path = Config.SdkPath;

			return Path.Combine(path, "tools", "sdb");
		}

		private string Escape (string arg)
		{
			// FIXME: What's the right kind of escaping,
			// as we want to be maximally cross-platform?
			// We'll chicken out for now, because we're
			// under a deadline.
			return '"' + arg + '"';
		}

		private string CombineArguments (string[] args)
		{
			var id = Device.Id;
			var sb = new StringBuilder ();

			if (!string.IsNullOrEmpty (id))
				sb.AppendFormat ("--serial {0} ", Escape (id));

			foreach (var arg in args)
				sb.AppendFormat ("{0} ", Escape (arg));

			return sb.ToString ();
		}

		public void Push (string localPath,
				  string remotePath) {
			var p = new Process ();
			var psi = p.StartInfo;

			psi.UseShellExecute = false;
			psi.FileName = GetSdbPath ();
			psi.Arguments = CombineArguments (
				new string[] { "push", localPath, remotePath });

			p.Start ();
			p.WaitForExit ();
			// OTOH, sdb is perfectly happy exiting with 0
			// when everything went wrong.  That's why we
			// can't have nice things.
			if (p.ExitCode != 0)
				throw new Exception ("sdb push failed.");
		}
	}
}