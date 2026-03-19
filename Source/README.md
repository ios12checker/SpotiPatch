# 🎵 SpotiPatch - Spicetify GUI Installer

A user-friendly graphical installer for **Spicetify** on Windows. No coding required - just double-click and install!

---

## 📥 Quick Start

1. **Download** this folder
2. **Double-click** `Install-Spicetify.bat`
3. **Click** "Install Spicetify" in the GUI
4. **Restart** Spotify when done

---

## ✅ Prerequisites

Before installing, make sure you have:

- [x] **Spotify** installed from [spotify.com](https://www.spotify.com/download/)
- [x] **Logged into Spotify** for at least 60 seconds (this creates required config files)

---

## 📦 What's Included

| File | Description |
|------|-------------|
| `Install-Spicetify.bat` | Double-click this to start the installer |
| `SpotiPatch-Installer.ps1` | The actual installer (GUI) - don't run directly |
| `README.md` | This file |

---

## 🚀 Installation Steps

1. **Close Spotify** completely (check system tray)
2. Run `Install-Spicetify.bat`
3. The GUI will open:
   - Shows installation progress
   - Displays detailed logs
   - Handles everything automatically
4. Wait for "Installation Complete!" message
5. **Restart Spotify**
6. Look for the **Marketplace** tab in Spotify's sidebar

---

## 🎨 Using Spicetify

After installation:

1. Open Spotify
2. Click the **Marketplace** tab in the left sidebar
3. Browse and install:
   - **Themes** - Change Spotify's appearance
   - **Extensions** - Add new features
   - **Snippets** - Small UI tweaks

---

## 🔄 Updating

When Spotify updates, you may need to re-apply Spicetify:

1. Open PowerShell or Command Prompt
2. Run: `spicetify restore backup apply`

---

## ❓ Troubleshooting

### "Spicetify command not found"
- Restart your terminal/PowerShell
- Or restart your computer

### "Cannot find Spotify.exe"
- Make sure Spotify is installed from the official website
- Not the Microsoft Store version

### "Access denied" errors
- Right-click `Install-Spicetify.bat` → "Run as administrator"

### Spotify reverts after update
- This is normal after Spotify updates
- Run `spicetify backup apply` to restore

---

## 🛡️ Safety

This installer:
- ✅ Only downloads from official Spicetify repositories
- ✅ Does not modify system files
- ✅ Can be uninstalled via `spicetify restore`
- ✅ Is open source - you can inspect the code

---

## 📚 More Info

- **Spicetify Docs**: https://spicetify.app/docs
- **Marketplace**: https://github.com/spicetify/marketplace
- **Themes & Extensions**: Browse via the Marketplace tab in Spotify

---

Made with 💚 for the Spotify modding community
