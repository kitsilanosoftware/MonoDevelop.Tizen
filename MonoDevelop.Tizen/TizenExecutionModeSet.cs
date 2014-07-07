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

using System;
using MonoDevelop.Core.Execution;
using System.Collections.Generic;

namespace MonoDevelop.Tizen
{
	public class TizenExecutionModeSet : IExecutionModeSet
	{
		IExecutionMode monoMode;
		IExecutionMode nativeMode;

		public string Name { get { return "Tizen";  } }

		public IEnumerable<IExecutionMode> ExecutionModes {
			get {
				yield return monoMode ?? (monoMode = new TizenMonoExecutionMode ());
				yield return nativeMode ?? (nativeMode = new TizenNativeExecutionMode ());
			}
		}
	}

	class TizenMonoExecutionMode : IExecutionMode
	{
		TizenExecutionHandler handler;

		public string Name { get { return "Tizen Device, Installed Mono"; } }

		public string Id { get { return "TizenMonoExecutionMode"; } }

		public IExecutionHandler ExecutionHandler {
			get {
				return handler ?? (handler = new TizenExecutionHandler ());
			}
		}
	}

	class TizenNativeExecutionMode : IExecutionMode
	{
		TizenNativeExecutionHandler handler;

		public string Name { get { return "Tizen Device, TPK-packaged"; } }

		public string Id { get { return "TizenNativeExecutionMode"; } }

		public IExecutionHandler ExecutionHandler {
			get {
				return handler ?? (handler = new TizenNativeExecutionHandler ());
			}
		}
	}
}
