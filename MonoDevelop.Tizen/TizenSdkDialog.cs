//
// TizenSdkDialog.cs
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
		FileEntry monoX86Entry;
		FileEntry monoArmEntry;
		FileEntry authorKeyEntry;
		Entry authorPassEntry;
		Entry deviceEntry;
		Button okButton, cancelButton;

		public TizenSdkDialog (Window parentWindow)
		{
			TransientFor = parentWindow;
			Modal = true;
			Build ();

			sdkFolderEntry.Path = PropertyService.Get<string> ("MonoTizen.SdkFolder") ?? "";
			monoX86Entry.Path = PropertyService.Get<string> ("MonoTizen.MonoX86Runtime") ?? "";
			monoArmEntry.Path = PropertyService.Get<string> ("MonoTizen.MonoArmRuntime") ?? "";
			authorKeyEntry.Path = PropertyService.Get<string> ("MonoTizen.AuthorKey") ?? "";
			deviceEntry.Text = PropertyService.Get<string> ("MonoTizen.DeviceId") ?? "";

			okButton.Sensitive = CheckValues ();
			sdkFolderEntry.PathChanged += OnChanged;
			monoX86Entry.PathChanged += OnChanged;
			monoArmEntry.PathChanged += OnChanged;
			authorKeyEntry.PathChanged += OnChanged;
			authorPassEntry.Changed += OnChanged;
		}

		private void OnChanged (object s, EventArgs a)
		{
			okButton.Sensitive = CheckValues ();
		}

		private bool CheckValues ()
		{
			return CheckSdb () && CheckAuthorKey () && CheckAuthorPass ();
		}

		private bool CheckSdb ()
		{
			var path = sdkFolderEntry.Path;

			return !string.IsNullOrEmpty (path) &&
				TizenSdkSdb.GetSdbPathFromSdkPath (path) != null;
		}

		private bool CheckAuthorKey ()
		{
			var path = authorKeyEntry.Path;
			if (string.IsNullOrEmpty (path) || !File.Exists (path))
				return false;

			return true;
		}

		private bool CheckAuthorPass ()
		{
			return !string.IsNullOrEmpty (authorPassEntry.Text);
		}

		void Build ()
		{
			Title = "Tizen SDK Info";
			BorderWidth = 6;

			deviceEntry = new Entry () { ActivatesDefault = true };
			sdkFolderEntry = new FolderEntry ();
			monoX86Entry = new FileEntry ();
			monoArmEntry = new FileEntry ();
			authorKeyEntry = new FileEntry ();
			authorPassEntry = new Entry () { Visibility = false, ActivatesDefault = true };

			var sdkFolderLabel = new Label ("_SDK Installation Folder:") {
				Xalign = 0,
				Justify = Justification.Left,
				UseUnderline = true,
				MnemonicWidget = sdkFolderEntry
			};
			var monoX86Label = new Label ("_Mono Runtime Bundle (x86):") {
				Xalign = 0,
				Justify = Justification.Left,
				UseUnderline = true,
				MnemonicWidget = monoX86Entry
			};
			var monoArmLabel = new Label ("_Mono Runtime Bundle (ARM):") {
				Xalign = 0,
				Justify = Justification.Left,
				UseUnderline = true,
				MnemonicWidget = monoArmEntry
			};
			var authorKeyLabel = new Label ("_Author Key:") {
				Xalign = 0,
				Justify = Justification.Left,
				UseUnderline = true,
				MnemonicWidget = authorKeyEntry
			};
			var authorPassLabel = new Label ("_Author Key Password:") {
				Xalign = 0,
				Justify = Justification.Left,
				UseUnderline = true,
				MnemonicWidget = authorPassEntry
			};
			var deviceLabel = new Label ("_Device ID:") {
				Xalign = 0,
				Justify = Justification.Left,
				UseUnderline = true,
				MnemonicWidget = deviceEntry
			};

			var table = new Table (2, 5, false);
			var fill = AttachOptions.Expand | AttachOptions.Fill;
			var none = AttachOptions.Shrink;
			var expand = AttachOptions.Expand;

			table.Attach (sdkFolderLabel,   0, 1, 0, 1, expand, none, 2, 2);
			table.Attach (sdkFolderEntry,   1, 2, 0, 1, fill,   none, 2, 2);
			table.Attach (monoX86Label,     0, 1, 1, 2, expand, none, 2, 2);
			table.Attach (monoX86Entry,     1, 2, 1, 2, fill,   none, 2, 2);
			table.Attach (monoArmLabel,     0, 1, 2, 3, expand, none, 2, 2);
			table.Attach (monoArmEntry,     1, 2, 2, 3, fill,   none, 2, 2);
			table.Attach (authorKeyLabel,   0, 1, 3, 4, expand, none, 2, 2);
			table.Attach (authorKeyEntry,   1, 2, 3, 4, fill,   none, 2, 2);
			table.Attach (authorPassLabel,  0, 1, 4, 5, expand, none, 2, 2);
			table.Attach (authorPassEntry,  1, 2, 4, 5, fill,   none, 2, 2);
			table.Attach (deviceLabel,      0, 1, 5, 6, expand, none, 2, 2);
			table.Attach (deviceEntry,      1, 2, 5, 6, fill,   none, 2, 2);

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
			var monoX86 = monoX86Entry.Path;
			var monoArm = monoArmEntry.Path;
			var authorKey = authorKeyEntry.Path;
			var deviceId = deviceEntry.Text;

			PropertyService.Set ("MonoTizen.SdkFolder", sdkFolder);
			PropertyService.Set ("MonoTizen.MonoX86Runtime", monoX86);
			PropertyService.Set ("MonoTizen.MonoArmRuntime", monoArm);
			PropertyService.Set ("MonoTizen.AuthorKey", authorKey);
			PropertyService.Set ("MonoTizen.DeviceId", deviceId);

			return new TizenSdkInfo (sdkFolder, monoX86, monoArm, authorKey, authorPassEntry.Text, deviceId);
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
