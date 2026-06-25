# SpotiPatch

SpotiPatch is a Windows installer for Spicetify CLI and Spicetify Marketplace. It is available as a self-contained WPF application and as a standalone PowerShell script.

## Requirements

- Windows 10 or later
- Spotify installed from spotify.com, not the Microsoft Store
- Spotify opened and logged in at least once
- Internet access during installation

Run SpotiPatch as a normal user. Do not use **Run as administrator**. Spicetify modifies files owned by the normal Windows account, and elevated execution can make those files inaccessible to Spotify.

## WPF application

Build and run:

```powershell
dotnet build .\SpotiPatch.sln -c Release
dotnet run --project .\SpotiPatch-Installer\SpotiPatch-Installer.csproj
```

Create a self-contained single-file executable:

```powershell
dotnet publish .\SpotiPatch-Installer\SpotiPatch-Installer.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true
```

The installer scripts in `SpotiPatch-Installer\Scripts` are embedded in the executable. Spicetify binaries and Marketplace assets are downloaded from the official Spicetify repositories while installation runs.

Logs are written to:

```text
%LOCALAPPDATA%\SpotiPatch\Logs
```

## PowerShell application

```powershell
.\SpotiPatch-Installer.ps1
.\SpotiPatch-Installer.ps1 -Silent
.\SpotiPatch-Installer.ps1 -Force
.\SpotiPatch-Installer.ps1 -Uninstall
```

The PowerShell version stores logs in a `Data` directory beside the script.

## After Spotify updates

Use the WPF application's re-apply action, or run:

```powershell
spicetify restore backup apply
```

## Uninstall

The uninstall action attempts to restore Spotify, removes known Spicetify directories, and removes Spicetify entries from the user PATH.

## License

MIT. See `LICENSE` in the repository root.
