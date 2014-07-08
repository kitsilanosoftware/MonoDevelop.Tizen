# MonoDevelop.Tizen

Support for developing and deploying Mono/Tizen applications in MonoDevelop

This is a fork of the now obsolete MonoDevelop.MeeGo add-in which used
to live in the main MonoDevelop repository; cf. this pull request for
details:

<https://github.com/mono/monodevelop/pull/610>

## Usage

Create a new solution of type C# → Tizen → Tizen Native Project.
Before the first build, the Tizen add-in will prompt you for the
following:

  * **SDK Installation Folder** (mandatory): The root of your Tizen
    SDK installation, the SDB tool should be located at `tools/sdb` or
    `tools/sdb.exe` relative to the chosen value;

  * **Mono Runtime Bundle** (mandatory): The path to a ZIP
    re-packaging of the MonoTizen RPMs.  You can download ready-to-use
    bundles here:

    <http://phio.crosstwine.com/kitsilano/mono-tizen/tarballs/add-in/runtime/>

    or build your own from RPMs using the `Makefile` in:

    <https://github.com/kitsilanosoftware/MonoTizen.BuildScripts/>

  * **Author Key** (mandatory): The `*.p12` key to use for generating
    native (`*.tpk`) packages;

  * **Author Key Password** (mandatory): Password of the `*.p12` key;

  * **Device ID** (optional): The ID of the target device, as provided
    by `sdb devices`; if left empty, the native tools will use the
    default device.
