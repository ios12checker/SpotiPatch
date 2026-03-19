# SpotiPatch 🎵

<p align="center">
  <img src="https://img.shields.io/badge/Version-1.8-green?style=for-the-badge" alt="Version 1.8">
  <img src="https://img.shields.io/badge/Windows-10%2F11-blue?style=for-the-badge&logo=windows" alt="Windows 10/11">
  <img src="https://img.shields.io/badge/PowerShell-5.1%2B-purple?style=for-the-badge&logo=powershell" alt="PowerShell 5.1+">
</p>

<p align="center">
  <b>A modern, user-friendly installer for Spicetify on Windows</b>
</p>

<p align="center">
  <a href="#installation">Installation</a> •
  <a href="#features">Features</a> •
  <a href="#screenshots">Screenshots</a> •
  <a href="#credits">Credits</a>
</p>

---

## ✨ Features

- 🎯 **One-Click Installation** - Install Spicetify CLI & Marketplace in seconds
- 🖥️ **Modern WPF GUI** - Beautiful, Spotify-inspired interface
- ⌨️ **PowerShell Console** - Alternative command-line installer
- 🔍 **Auto-Detection** - Automatically finds your Spotify installation
- 🎨 **Marketplace Ready** - Browse and install themes & extensions instantly
- 🔄 **Easy Re-apply** - Re-apply patches after Spotify updates
- 🧹 **Clean Uninstall** - Complete removal with one click

## 📋 Prerequisites

Before installing SpotiPatch, make sure you have:

- ✅ **Spotify** installed from [spotify.com](https://www.spotify.com/download) (NOT Microsoft Store version)
- ✅ **Windows 10 or 11**
- ✅ **PowerShell 5.1** or higher (PowerShell 7.x recommended)
- ✅ Logged into Spotify for at least **60 seconds** before installing

## 📥 Installation

### Option 1: GUI Installer (Recommended)

1. Download `SpotiPatch-Installer.exe` from the [Releases](https://github.com/iOS12Checker/SpotiPatch/releases) page
2. Double-click to run (no installation required)
3. Click **"Install Spicetify"**
4. Restart Spotify and enjoy your new Marketplace tab!

### Option 2: PowerShell Script

1. Download `SpotiPatch-Installer.ps1` from the [Releases](https://github.com/iOS12Checker/SpotiPatch/releases) page
2. Right-click → **"Run with PowerShell"**
3. Press `I` to install
4. Restart Spotify

### Option 3: Command Line

```powershell
# Run directly from GitHub (not recommended for security)
irm https://raw.githubusercontent.com/iOS12Checker/SpotiPatch/main/SpotiPatch-Installer.ps1 | iex
```

## 📸 Screenshots

<p align="center">
  <img width="895" height="745" alt="image" src="https://github.com/user-attachments/assets/4110a4d5-d0a6-4755-83ce-ff5c784a813e" />
  <br>
  <i>Modern WPF Interface with Spotify-inspired design</i>
</p>

<p align="center">
  <img width="1119" height="629" alt="d" src="https://github.com/user-attachments/assets/7b4244e8-6d6c-4fb6-a722-8fd29250712a" />

  <br>
  <i>Clean PowerShell console interface</i>
</p>

## 🎮 Usage

### After Installation

1. **Restart Spotify** completely
2. Look for the **Marketplace** tab in the left sidebar
3. Browse and install themes, extensions, and custom apps

### After Spotify Updates

When Spotify updates, you'll need to re-apply Spicetify:

**GUI:** Click **"Re-apply Spicetify"** button

**PowerShell:**
```powershell
spicetify backup
spicetify apply
```

### Uninstalling

**GUI:** Click **"Uninstall"** button

**PowerShell:**
```powershell
spicetify restore
```

Or run the installer and select **Uninstall** option.

## 🏗️ Building from Source

### Requirements
- .NET 8.0 SDK
- Windows 10/11

### Build Instructions

```bash
# Clone the repository
git clone https://github.com/LilBatti/iOS12Checker/SpotiPatch.git
cd SpotiPatch

# Build the WPF application
cd Source/SpotiPatch-Installer
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Output will be in SpotiPatch-Release/
```

## 📝 Changelog

### v1.8
- ✅ Fixed apply command for Spicetify v2.42.14
- ✅ Added proper credits
- ✅ Improved error handling
- ✅ Background job execution for reliability
- ✅ Added uninstall functionality

## 🤝 Credits

**Created by:** [iOS12Checker](https://github.com/iOS12Checker)

**Powered by:** [Spicetify](https://github.com/spicetify) - The amazing tool that makes Spotify customization possible

Special thanks to:
- The [Spicetify CLI](https://github.com/spicetify/cli) team
- The [Spicetify Marketplace](https://github.com/spicetify/marketplace) team
- All the theme and extension developers in the community

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

**Note:** SpotiPatch is an unofficial installer and is not affiliated with Spotify AB or Spicetify. Use at your own risk.

## 🐛 Troubleshooting

### "Spotify not found"
Make sure you installed Spotify from the official website, not the Microsoft Store.

### "Access denied" errors
Run PowerShell as Administrator if you encounter permission issues.

### Marketplace tab not appearing
1. Restart Spotify completely (quit from system tray)
2. Run `spicetify apply` in PowerShell
3. Check that Marketplace is enabled in config

### After Spotify update
Spotify updates overwrite Spicetify patches. Simply re-run SpotiPatch and click "Re-apply".

## 💬 Support

- 🐛 [Report Issues](../../issues)
- 💡 [Request Features](../../discussions)
- ⭐ Star this repo if you found it helpful!

---

<p align="center">
  Made with ❤️ by <a href="https://github.com/LilBatti/iOS12Checker">Lil_Batti/iOS12Checker</a>
</p>
