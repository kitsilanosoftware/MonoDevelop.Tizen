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

using MonoDevelop.Ide;

namespace MonoDevelop.Tizen
{
	public class TizenSdkInfo
	{
		public TizenSdkInfo (string sdkPath, string monoRt, string authorKey, string authorPass, string deviceId)
		{
			this.SdkPath = sdkPath;
			this.MonoRuntimePath = monoRt;
			this.AuthorKey = authorKey;
			this.AuthorKeyPassword = authorPass;
			this.DeviceId = deviceId;
		}

		public string SdkPath           { get; set; }
		public string MonoRuntimePath   { get; set; }
		public string AuthorKey         { get; set; }
		public string AuthorKeyPassword { get; set; }
		public string DeviceId          { get; set; }

		static TizenSdkInfo sdkInfo;

		public static TizenSdkInfo GetSdkInfo ()
		{
			return EnsureSdkInfo (false);
		}

		public static TizenSdkInfo EnsureSdkInfo (bool forceDialog)
		{
			if (sdkInfo == null || forceDialog) {
				DispatchService.GuiSyncDispatch (delegate {
					sdkInfo = TizenSdkDialog.GetSdkInfo (null);
				});
			}
			return sdkInfo;
		}
	}
}
