# SpotiPatch Installer

A self-contained .NET 8 WPF installer for Spicetify CLI and Spicetify Marketplace.

## Runtime behavior

1. Refuses to run installation, apply, or uninstall operations with administrator privileges.
2. Runs the embedded Spicetify CLI installer snapshot.
3. Verifies `spicetify.exe` and adds its directory to the current process PATH.
4. Runs the embedded Marketplace installer snapshot.
5. Verifies that Marketplace contains its required application files.
6. Closes Spotify and runs `spicetify apply`.
7. Reports success only when every required step succeeds.

Re-apply uses:

```powershell
spicetify restore backup apply
```

Installer logs are stored in `%LOCALAPPDATA%\SpotiPatch\Logs`.

## Build

```powershell
dotnet build -c Release
```

Publish:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Project structure

```text
SpotiPatch-Installer/
├── App.xaml
├── App.xaml.cs
├── AssemblyInfo.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── Scripts/
│   ├── spicetify-cli-install.ps1
│   └── spicetify-marketplace-install.ps1
└── SpotiPatch-Installer.csproj
```

The installer scripts are embedded for reproducible builds. They still download Spicetify release binaries and Marketplace assets from the official repositories at runtime.
