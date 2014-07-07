//
// TizenSdkDialog.cs
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
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using System.IO;

namespace MonoDevelop.Tizen
{
	class TizenSdkDialog : Dialog
	{
		FolderEntry sdkFolderEntry;
		FileEntry monoRtEntry;
		Entry deviceEntry;
		Button okButton, cancelButton;

		public TizenSdkDialog (Window parentWindow)
		{
			TransientFor = parentWindow;
			Modal = true;
			Build ();

			sdkFolderEntry.Path = PropertyService.Get<string> ("MonoTizen.SdkFolder") ?? "";
			monoRtEntry.Path = PropertyService.Get<string> ("MonoTizen.MonoRuntime") ?? "";
			deviceEntry.Text = PropertyService.Get<string> ("MonoTizen.DeviceId") ?? "";

			okButton.Sensitive = CheckValues ();
			sdkFolderEntry.PathChanged += OnPathChanged;
			monoRtEntry.PathChanged += OnPathChanged;
		}

		private void OnPathChanged (object s, EventArgs a)
		{
			okButton.Sensitive = CheckValues ();
		}

		private bool CheckValues ()
		{
			return CheckSdb () && CheckMonoRt ();
		}

		private bool CheckSdb ()
		{
			var path = sdkFolderEntry.Path;

			return !string.IsNullOrEmpty (path) &&
				TizenSdkSdb.GetSdbPathFromSdkPath (path) != null;
		}

		private bool CheckMonoRt ()
		{
			var path = monoRtEntry.Path;
			if (string.IsNullOrEmpty (path) || !File.Exists (path))
				return false;

			var fileName = System.IO.Path.GetFileName (path);
			if (!fileName.StartsWith ("mono-tizen", StringComparison.OrdinalIgnoreCase))
				return false;

			if (! (fileName.EndsWith (".armv7l.zip", StringComparison.OrdinalIgnoreCase) ||
			       fileName.EndsWith (".i586.zip", StringComparison.OrdinalIgnoreCase)))
				return false;

			return true;
		}

		void Build ()
		{
			Title = "Tizen SDK Info";
			BorderWidth = 6;

			deviceEntry = new Entry () { ActivatesDefault = true };
			sdkFolderEntry = new FolderEntry ();
			monoRtEntry = new FileEntry ();

			var sdkFolderLabel = new Label ("_SDK Installation Folder:") {
				Xalign = 0,
				Justify = Justification.Left,
				UseUnderline = true,
				MnemonicWidget = sdkFolderEntry
			};
			var monoRtLabel = new Label ("_Mono Runtime Bundle:") {
				Xalign = 0,
				Justify = Justification.Left,
				UseUnderline = true,
				MnemonicWidget = monoRtEntry
			};
			var deviceLabel = new Label ("_Device ID:") {
				Xalign = 0,
				Justify = Justification.Left,
				UseUnderline = true,
				MnemonicWidget = deviceEntry
			};

			var table = new Table (2, 1, false);
			var fill = AttachOptions.Expand | AttachOptions.Fill;
			var none = AttachOptions.Shrink;
			var expand = AttachOptions.Expand;

			table.Attach (sdkFolderLabel,   0, 1, 0, 1, expand, none, 2, 2);
			table.Attach (sdkFolderEntry,   1, 2, 0, 1, fill,   none, 2, 2);
			table.Attach (monoRtLabel,      0, 1, 1, 2, expand, none, 2, 2);
			table.Attach (monoRtEntry,      1, 2, 1, 2, fill,   none, 2, 2);
			table.Attach (deviceLabel,      0, 1, 2, 3, expand, none, 2, 2);
			table.Attach (deviceEntry,      1, 2, 2, 3, fill,   none, 2, 2);

			VBox.PackStart (new Label ("Tizen Project Setup:"), true, false, 6);
			VBox.PackStart (table, true, false, 6);

			cancelButton = new Button (Gtk.Stock.Cancel);
			this.AddActionWidget (cancelButton, ResponseType.Cancel);
			okButton = new Button (Gtk.Stock.Ok) { CanDefault = true };
			this.AddActionWidget (okButton, ResponseType.Ok);
			okButton.HasDefault = true;

			ShowAll ();

			Resize (400, 80);
			Resizable = false;
		}

		TizenSdkInfo GetSdkInfo ()
		{
			var sdkFolder = sdkFolderEntry.Path;
			var monoRt = monoRtEntry.Path;
			var deviceId = deviceEntry.Text;

			PropertyService.Set ("MonoTizen.SdkFolder", sdkFolder);
			PropertyService.Set ("MonoTizen.MonoRuntime", monoRt);
			PropertyService.Set ("MonoTizen.DeviceId", deviceId);

			return new TizenSdkInfo (sdkFolder, monoRt, deviceId);
		}

		public override void Dispose ()
		{
			Destroy ();
			base.Dispose ();
		}

		public static TizenSdkInfo GetSdkInfo (Window parentWindow)
		{
			using (var dialog = new TizenSdkDialog (parentWindow ?? MessageService.RootWindow)) {
				int result = dialog.Run ();
				if (result != (int)ResponseType.Ok)
					return null;
				return dialog.GetSdkInfo ();
			}
		}
	}
}
