# Changelog

All notable changes to SpotiPatch are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

## [1.9.0] - 2026-06-25

### Added

- Administrator-mode protection in both the WPF and PowerShell installers.
- Explicit verification of both Spicetify CLI and Marketplace installations.
- Validation of required Marketplace files (`manifest.json` and `index.js`).
- Per-user WPF logs in `%LOCALAPPDATA%\SpotiPatch\Logs`.
- A tracked MIT license and dedicated release documentation.

### Changed

- Split the WPF installation into separate CLI and Marketplace stages.
- Use the embedded CLI and Marketplace installer snapshots for reproducible builds.
- Re-apply Spicetify with `spicetify restore backup apply`.
- Close Spotify before applying or restoring modifications.
- Run subprocess output collection concurrently to avoid blocked output streams.
- Remove unused Material Design and Windows Forms dependencies.
- Include the installer scripts in the repository instead of downloading them during CI.
- Update application metadata to version 1.9.0 and copyright 2026.

### Fixed

- Prevent successful completion messages after failed commands, non-zero exit codes, or timeouts.
- Fix Marketplace detection by checking `%APPDATA%\spicetify\CustomApps\marketplace`.
- Fix PowerShell partial installations where Marketplace was missing but CLI was already installed.
- Fix PowerShell jobs returning success after installation or apply failures.
- Fix timeout handling so stalled child processes are terminated correctly.
- Fix possible deadlocks caused by reading standard output and standard error sequentially.
- Fix the Marketplace theme prompt using assignment instead of equality comparison.
- Fix script patching for PowerShell files containing a script-level `param` block.
- Fix GitHub Actions paths and the broken Marketplace installer URL.
- Fix clean-checkout builds failing because embedded scripts were ignored.
- Fix stale installation instructions referring to a missing batch file or administrator mode.

### Security

- Stop recommending or permitting elevated Spicetify execution.
- Exclude local logs containing usernames, machine names, and paths from release archives.
- Package only release files and documentation in the ZIP, never local logs.

## [1.8.0]

### Added

- WPF graphical installer.
- PowerShell console installer.
- Spicetify CLI and Marketplace installation.
- Uninstall support.
- Background job execution.
- Project credits and release documentation.

### Fixed

- Apply command compatibility with Spicetify 2.42.14.
- General installation error handling.
