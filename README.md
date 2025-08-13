# Systray Doom

## Download

```pwsh
# 1. Clone the repo
git clone https://github.com/citelao/systray-doom

# 2. Put DOOM1.WAD in the systray_doom/ directory (you can find the shareware free & legal online)
# cp somethingsomething
```

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
# Create the nupkg
# https://learn.microsoft.com/en-us/nuget/quickstart/create-and-publish-a-package-using-the-dotnet-cli
dontnet build -c release
dotnet pack
# => Systray\bin\Release\Citelao.Systray.1.0.0.nupkg

# Get an API key...

# Push to Nuget
dotnet nuget push .\Systray\bin\Release\Citelao.Systray.1.0.0.nupkg --api-key $apiKey
```

## TODO

### Blockers

* [x] Split off systray code into a library
* [x] Support AnyCPU target
* [ ] Support `<DisableRuntimeMarshalling>True</DisableRuntimeMarshalling>`
* [ ] ^ Fix `WindowSubclassHandler` to support non-marshalled delegates.
* [x] License (ensure we cite WinForms)
* [ ] Add README to package
* [ ] Publish to NuGet
* [ ] Add unit tests

### Nice-to-have

* [ ] Reorganize internal types (`NoReleaseHwnd`, `NoReleaseSafeHandle`, etc.)
* [ ] Upgrade to a newer commit of Doomgeneric (specifically, one after the [sound commit](https://github.com/ozkl/doomgeneric/commit/d0946b46cf617467f014a25e264fd952698a13f9))
* [x] Automatically build the Rust bindings before building & launching the C#.
* [x] Deploy the Rust DLL alongside the C# so that you can run `dotnet run` anywhere.
* [ ] Better name in Taskbar personalization menu
* [ ] Better logging

### TODO Features

* [ ] Double-click to show full window
* [x] Right-click menu
* [ ] Click to play/pause(?) (invisible input window to play?)