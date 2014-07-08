# Copyright 2014 Kitsilano Software Inc.
#
# This file is part of MonoTizen.
#
# MonoTizen is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# MonoTizen is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with MonoTizen.  If not, see <http://www.gnu.org/licenses/>.

MDTOOL = '/Applications/Xamarin Studio.app/Contents/MacOS/mdtool'

.PHONY: all clean

MPACK = MonoDevelop.Tizen.MonoDevelop.Tizen_1.0.mpack

all: build/$(MPACK) build/index.html

build/index.html: build/$(MPACK)
	@mkdir -p $(dir $@)
	$(MDTOOL) setup rep-build $(dir $@)

# Depends on a successful Release MD build.
build/$(MPACK): MonoDevelop.Tizen/bin/Release/MonoDevelop.Tizen.dll
	@mkdir -p $(dir $@)
	$(MDTOOL) setup pack $< -d:$(dir $@)

clean:
	rm -rf build/
