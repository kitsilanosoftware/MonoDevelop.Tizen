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

using MonoDevelop.Ide.Templates;
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using System.Text;

namespace MonoDevelop.Tizen
{
	public class NoBomFileDescriptionTemplate : TextFileDescriptionTemplate
	{
		public override Stream CreateFileContent (SolutionItem policyParent, Project project, string language,
							  string fileName, string identifier)
		{
			var s = base.CreateFileContent (policyParent, project, language,
							fileName,  identifier);
			var ms = s as MemoryStream;
			if (ms == null) {
				LoggingService.LogWarning ("Template {0} did not resolve to a MemoryStream.", fileName);
				return s;
			}

			// Skip BOM, if present.
			var bom = Encoding.UTF8.GetPreamble ();
			for (var i = 0; i < bom.Length; i++) {
				if (ms.ReadByte () != bom [i]) {
					ms.Seek (0, SeekOrigin.Begin);
					return ms;
				}
			}

			return ms;
		}
	}
}