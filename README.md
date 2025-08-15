# Systray Doom

## Download

```pwsh
# 1. Clone the repo
git clone https://github.com/citelao/systray-doom

# 2. Put DOOM1.WAD in the systray_doom/ directory (you can find the shareware free & legal online)
# cp somethingsomething
```

## Usage

For usage of the systray library, see [the Systray README](./Systray/README.md).

> dotnet add package citelao.SystrayIcon

## Dev

```pwsh
# Build the Rust bindings
cd rust_bindings/
cargo build

# Build & run the Dotnet!
cd ..
dotnet run --project .\systray_doom\systray_doom.csproj
```

### Publishing versions

```pwsh
# Bump the version
# In systray.csproj, bump `<Version>`.

# Create the nupkg
# https://learn.microsoft.com/en-us/nuget/quickstart/create-and-publish-a-package-using-the-dotnet-cli
dotnet build -c release
# => Systray\bin\Release\citelao.SystrayIcon.0.1.0.nupkg

ls Systray\bin\Release\*.nupkg

# Get an API key
# (for initial push, use `*` glob pattern)
# https://www.nuget.org/account/apikeys
# https://int.nugettest.org/account/apikeys
$apiKey = # paste from the website

# Push to Nuget
dotnet nuget push .\Systray\bin\Release\citelao.SystrayIcon.0.1.0.1.nupkg --api-key $apiKey --source https://api.nuget.org/v3/index.json
# Dummy Nuget:
# dotnet nuget push .\Systray\bin\Release\citelao.SystrayIcon.0.1.0.nupkg --api-key $apiKey --source https://int.nugettest.org
```

## TODO

### Blockers

* [x] Split off systray code into a library
* [x] Support AnyCPU target
* [x] Support `<DisableRuntimeMarshalling>True</DisableRuntimeMarshalling>`
* [x] ^ Fix `WindowSubclassHandler` to support non-marshalled delegates.
* [x] License (ensure we cite WinForms)
* [x] Add README to package
* [x] Publish to NuGet
* [x] Add unit tests
* [x] Cleaner public types

### Nice-to-have

* [x] Reorganize internal types (`NoReleaseHwnd`, `NoReleaseSafeHandle`, etc.)
* [ ] Upgrade to a newer commit of Doomgeneric (specifically, one after the [sound commit](https://github.com/ozkl/doomgeneric/commit/d0946b46cf617467f014a25e264fd952698a13f9))
* [x] Automatically build the Rust bindings before building & launching the C#.
* [x] Deploy the Rust DLL alongside the C# so that you can run `dotnet run` anywhere.
* [ ] Better name in Taskbar personalization menu
* [ ] Better logging

### TODO Features

* [ ] Double-click to show full window
* [x] Right-click menu
* [ ] Click to play/pause(?) (invisible input window to play?)