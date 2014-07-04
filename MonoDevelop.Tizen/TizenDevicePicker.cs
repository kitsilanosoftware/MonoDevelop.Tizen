// 
// TizenDevicePicker.cs
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

namespace MonoDevelop.Tizen
{
	class TizenDevicePicker : Dialog
	{
		Entry userEntry, passwordEntry, addressEntry, deviceEntry;
		Button okButton, cancelButton;
		
		public TizenDevicePicker (Window parentWindow)
		{
			TransientFor = parentWindow;
			Modal = true;
			Build ();
			
			userEntry.Text = PropertyService.Get<string> ("TizenDevice.User") ?? "";
			passwordEntry.Text = PropertyService.Get<string> ("TizenDevice.Password") ?? "";
			addressEntry.Text = PropertyService.Get<string> ("TizenDevice.Address") ?? "";
			deviceEntry.Text = PropertyService.Get<string> ("TizenDevice.Id") ?? "";
			
			deviceEntry.Changed += delegate {
				okButton.Sensitive = !string.IsNullOrEmpty (deviceEntry.Text);
			};
			okButton.Sensitive = !string.IsNullOrEmpty (deviceEntry.Text);
		}

		void Build ()
		{
			Title = "Tizen Device";
			BorderWidth = 6;
			
			deviceEntry = new Entry () { ActivatesDefault = true };
			addressEntry = new Entry () { ActivatesDefault = true };
			userEntry = new Entry () { ActivatesDefault = true };
			passwordEntry = new Entry () { Visibility = false, ActivatesDefault = true };
			
			var deviceLabel = new Label ("_Device:") {
				Xalign = 0,
				Justify = Justification.Left,
				UseUnderline = true,
				MnemonicWidget = deviceEntry
			};
			var addressLabel = new Label ("_Address:") {
				Xalign = 0,
				Justify = Justification.Left,
				UseUnderline = true,
				MnemonicWidget = addressEntry
			};
			var userLabel = new Label ("_Username:") {
				Xalign = 0,
				Justify = Justification.Left,
				UseUnderline = true,
				MnemonicWidget = userEntry
			};
			var passwordLabel = new Label ("_Password:") {
				Xalign = 0,
				Justify = Justification.Left,
				UseUnderline = true,
				MnemonicWidget = passwordEntry
			};
			
			var table = new Table (2, 4, false);
			var fill = AttachOptions.Expand | AttachOptions.Fill;
			var none = AttachOptions.Shrink;
			var expand = AttachOptions.Expand;
			
			table.Attach (deviceLabel,   0, 1, 0, 1, expand, none, 2, 2);
			table.Attach (deviceEntry,   1, 2, 0, 1, fill,    none, 2, 2);
			table.Attach (addressLabel,  0, 1, 0, 2, expand, none, 2, 2);
			table.Attach (addressEntry,  1, 2, 0, 2, fill,    none, 2, 2);
			table.Attach (userLabel,     0, 1, 1, 3, expand, none, 2, 2);
			table.Attach (userEntry,     1, 2, 1, 3, fill,    none, 2, 2);
			table.Attach (passwordLabel, 0, 1, 2, 4, expand, none, 2, 2);
			table.Attach (passwordEntry, 1, 2, 2, 4, fill,    none, 2, 2);
			
			VBox.PackStart (new Label ("Enter details of the Tizen device:"), true, false, 6);
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
		
		TizenDevice GetDevice ()
		{
			PropertyService.Set ("TizenDevice.User", userEntry.Text);
			PropertyService.Set ("TizenDevice.Password", passwordEntry.Text);
			PropertyService.Set ("TizenDevice.Address", addressEntry.Text);
			PropertyService.Set ("TizenDevice.Id", deviceEntry.Text);
			
			return new TizenDevice (deviceEntry.Text, addressEntry.Text, userEntry.Text, passwordEntry.Text);
		}
		
		public override void Dispose ()
		{
			Destroy ();
			base.Dispose ();
		}
		
		public static TizenDevice GetDevice (Window parentWindow)
		{
			using (var dialog = new TizenDevicePicker (parentWindow ?? MessageService.RootWindow)) {
				int result = dialog.Run ();
				if (result != (int)ResponseType.Ok)
					return null;
				return dialog.GetDevice ();
			}
		}
	}
}

