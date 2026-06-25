# SpotiPatch 🎵

<p align="center">
  <img src="https://img.shields.io/badge/Version-1.9-green?style=for-the-badge" alt="Version 1.9">
  <img src="https://img.shields.io/badge/Windows-10%2F11-blue?style=for-the-badge&logo=windows" alt="Windows 10/11">
  <img src="https://img.shields.io/badge/.NET-8.0-purple?style=for-the-badge&logo=dotnet" alt=".NET 8.0">
</p>

<p align="center">
  <b>A modern, user-friendly installer for Spicetify on Windows</b>
</p>

<p align="center">
  <a href="#-installation">Installation</a> •
  <a href="#-features">Features</a> •
  <a href="#-screenshots">Screenshots</a> •
  <a href="#-troubleshooting">Troubleshooting</a> •
  <a href="#-credits">Credits</a>
</p>

---

## ✨ Features

- 🎯 **One-click installation** — Installs Spicetify CLI and Marketplace
- 🖥️ **Modern WPF GUI** — Spotify-inspired Windows interface
- ⌨️ **PowerShell alternative** — Console installer with silent, force, and uninstall options
- 🔍 **Automatic detection** — Finds supported Spotify and Spicetify locations
- ✅ **Installation verification** — Confirms that CLI and Marketplace files were installed correctly
- 🔄 **Reliable re-apply** — Restores, backs up, and reapplies patches after Spotify updates
- 🧹 **Clean uninstall** — Restores Spotify, removes Spicetify files, and cleans the user PATH
- 📝 **Detailed logs** — Records installation output for troubleshooting

## 📋 Requirements

Before using SpotiPatch, make sure you have:

- **Windows 10 or 11**
- **Spotify installed from [spotify.com](https://www.spotify.com/download)** — not the Microsoft Store version
- Spotify opened and logged in at least once
- An internet connection during installation
- PowerShell 5.1 or later when using the PowerShell installer

> [!IMPORTANT]
> Run SpotiPatch as a normal Windows user. **Do not use “Run as administrator.”** Spicetify must modify Spotify files using the same account that runs Spotify. Elevated execution can make those files inaccessible to the normal account.

## 📥 Installation

### Option 1: GUI installer (recommended)

1. Download `SpotiPatch-Installer-v1.9.zip` from the [Releases](https://github.com/ios12checker/SpotiPatch/releases) page.
2. Extract the ZIP file.
3. Double-click `SpotiPatch-Installer.exe` normally.
4. Click **Install Spicetify** and confirm the installation.
5. Restart Spotify when the installer finishes.
6. Open the new **Marketplace** tab in Spotify's sidebar.

The GUI is self-contained and does not require a separate .NET installation.

### Option 2: PowerShell installer

1. Download and extract `SpotiPatch-Installer-v1.9.zip`.
2. Right-click `SpotiPatch-Installer.ps1` and choose **Run with PowerShell**.
3. Press `I` to install.
4. Restart Spotify when the installer finishes.

Available parameters:

```powershell
# Silent installation
.\SpotiPatch-Installer.ps1 -Silent

# Force reinstall
.\SpotiPatch-Installer.ps1 -Force

# Uninstall
.\SpotiPatch-Installer.ps1 -Uninstall
```

For security, download the release and inspect the script instead of piping remote code directly into PowerShell.

## 📸 Screenshots

<p align="center">
  <img width="895" height="745" alt="SpotiPatch WPF interface" src="https://github.com/user-attachments/assets/4110a4d5-d0a6-4755-83ce-ff5c784a813e" />
  <br>
  <i>Modern WPF interface with a Spotify-inspired design</i>
</p>

<p align="center">
  <img width="1119" height="629" alt="SpotiPatch PowerShell interface" src="https://github.com/user-attachments/assets/7b4244e8-6d6c-4fb6-a722-8fd29250712a" />
  <br>
  <i>PowerShell console interface</i>
</p>

## 🎮 Usage

### After installation

1. Restart Spotify completely.
2. Look for the **Marketplace** tab in the left sidebar.
3. Browse and install themes, extensions, snippets, and custom apps.

### After Spotify updates

Spotify updates can overwrite Spicetify patches.

**GUI:** Open SpotiPatch, click **Install Spicetify**, and choose **Yes** when asked whether to re-apply the existing installation.

**PowerShell:**

```powershell
spicetify restore backup apply
```

### Uninstalling

**GUI:** Click **Uninstall** and confirm.

**PowerShell:**

```powershell
.\SpotiPatch-Installer.ps1 -Uninstall
```

The uninstall process attempts to restore Spotify, remove known Spicetify directories, and remove Spicetify entries from the user PATH.

## 📝 Logs

The GUI stores logs in:

```text
%LOCALAPPDATA%\SpotiPatch\Logs
```

The PowerShell installer stores logs in a `Data` folder beside the script.

Logs can contain your Windows username, computer name, and local paths. Review them before sharing publicly.

## 🏗️ Building from source

### Requirements

- .NET 8 SDK
- Windows 10 or 11

### Build instructions

```powershell
# Clone the repository
git clone https://github.com/ios12checker/SpotiPatch.git
cd SpotiPatch

# Build the solution
dotnet build .\Source\SpotiPatch.sln -c Release

# Publish a self-contained single-file executable
dotnet publish .\Source\SpotiPatch-Installer\SpotiPatch-Installer.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -o .\SpotiPatch-Release
```

The PowerShell installer is located at `Source\SpotiPatch-Installer.ps1`.

## 📝 Changelog

### v1.9

- Fixed false success messages after failed commands, timeouts, and non-zero exit codes
- Added protection against unsafe administrator-mode execution
- Added explicit CLI and Marketplace verification
- Fixed Marketplace detection in `%APPDATA%`
- Fixed partial PowerShell installations and timeout handling
- Fixed clean-checkout builds and GitHub Actions release paths
- Added reproducible embedded installer scripts
- Removed local logs from release archives
- Added release documentation and an expanded changelog

### v1.8

- Added the WPF graphical installer
- Added the PowerShell console installer
- Added uninstall functionality
- Added background job execution
- Improved compatibility with Spicetify 2.42.14

## 🐛 Troubleshooting

### “SpotiPatch must not be run as administrator”

Close SpotiPatch and start it normally by double-clicking the EXE or using **Run with PowerShell** without elevation.

### “Spotify not found”

Install Spotify from the official [spotify.com download page](https://www.spotify.com/download), open it, log in, and leave it running for at least 60 seconds before trying again.

### Marketplace tab not appearing

1. Quit Spotify completely, including its system tray process.
2. Open SpotiPatch normally.
3. Click **Install Spicetify** and re-apply the existing installation when prompted.
4. Restart Spotify.
5. Check the latest SpotiPatch log if the installer reports an error.

### Spotify shows a blank or black window

This can happen when Spicetify was previously run with administrator privileges. Restore or reinstall Spotify, then run SpotiPatch again as a normal user.

### Windows SmartScreen warning

The release executable is currently unsigned, so Windows may display a SmartScreen warning. Download releases only from this repository and verify the files before running them.

## 🤝 Credits

**Created by:** [iOS12Checker](https://github.com/ios12checker)

**Powered by:** [Spicetify](https://github.com/spicetify) — the project that makes Spotify customization possible.

Special thanks to:

- The [Spicetify CLI](https://github.com/spicetify/cli) team
- The [Spicetify Marketplace](https://github.com/spicetify/marketplace) team
- Theme and extension developers throughout the community

## 📄 License

SpotiPatch is licensed under the [MIT License](LICENSE).

SpotiPatch is an unofficial installer and is not affiliated with Spotify AB or the Spicetify project. Use it at your own risk.

## 💬 Support

- 🐛 [Report issues](../../issues)
- 💡 [Request features](../../discussions)
- ⭐ Star the repository if SpotiPatch helped you

---

<p align="center">
  Made with ❤️ by <a href="https://github.com/ios12checker">Lil_Batti/iOS12Checker</a>
</p>
