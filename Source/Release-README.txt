SpotiPatch 1.9
===============

Recommended:
  Double-click SpotiPatch-Installer.exe

Alternative PowerShell version:
  Right-click SpotiPatch-Installer.ps1 and choose "Run with PowerShell"

IMPORTANT
---------
Run SpotiPatch as a normal user. Do not choose "Run as administrator".
Spicetify must modify Spotify files using the same Windows account that runs
Spotify.

Requirements
------------
- Windows 10 or later
- Spotify installed from spotify.com, not Microsoft Store
- Spotify opened and logged in at least once
- Internet connection during installation

PowerShell parameters
---------------------
.\SpotiPatch-Installer.ps1 -Silent
.\SpotiPatch-Installer.ps1 -Force
.\SpotiPatch-Installer.ps1 -Uninstall

Logs
----
The EXE stores logs in:
  %LOCALAPPDATA%\SpotiPatch\Logs

The PowerShell version stores logs in a Data folder beside the script.

After a Spotify update
----------------------
Use the EXE's re-apply action, or run:
  spicetify restore backup apply
