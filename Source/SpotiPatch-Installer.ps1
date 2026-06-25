#!/usr/bin/env pwsh
# SpotiPatch - Spicetify Installer (PowerShell Edition)

param(
    [switch]$Silent,
    [switch]$Force,
    [switch]$Uninstall
)

# Configuration
$script:Version = "1.9"
$script:DataDir = Join-Path $PSScriptRoot "Data"
$script:LogFile = Join-Path $script:DataDir "SpotiPatch_Log_$(Get-Date -Format 'yyyy-MM-dd_HH-mm-ss').txt"

function Initialize-Environment {
    if (!(Test-Path $script:DataDir)) {
        New-Item -ItemType Directory -Path $script:DataDir -Force | Out-Null
    }

    $header = @"
SpotiPatch Installer Log v$script:Version
================================
Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
User: $env:USERNAME
Computer: $env:COMPUTERNAME
================================

"@
    $header | Out-File -FilePath $script:LogFile -Encoding UTF8
    $host.UI.RawUI.WindowTitle = "SpotiPatch Installer v$script:Version"
}

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "HH:mm:ss"
    $logLine = "[$timestamp] [$Level] $Message"
    Add-Content -Path $script:LogFile -Value $logLine -Encoding UTF8

    switch ($Level) {
        "ERROR" { Write-Host $logLine -ForegroundColor Red }
        "WARN"  { Write-Host $logLine -ForegroundColor Yellow }
        "SUCCESS" { Write-Host $logLine -ForegroundColor Green }
        "STEP"  { Write-Host $logLine -ForegroundColor Cyan }
        default { Write-Host $logLine -ForegroundColor Gray }
    }
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Show-Header {
    Clear-Host
    Write-Host ""
    Write-Host "    ===============================================" -ForegroundColor Green
    Write-Host "                   SpotiPatch Installer             " -ForegroundColor Green
    Write-Host "              Spicetify for Windows               " -ForegroundColor Gray
    Write-Host "    ===============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "    Version: $script:Version" -ForegroundColor DarkGray
    Write-Host "    Log: $script:LogFile" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "    Created by: Lil_Batti/iOS12Checker" -ForegroundColor DarkGray
    Write-Host "    Powered by: github.com/spicetify" -ForegroundColor DarkGray
    Write-Host ""
}

function Show-Features {
    Write-Host ""
    Write-Host "    What will be installed:" -ForegroundColor White
    Write-Host ""
    Write-Host "    [CLI]  Spicetify CLI    - Core customization engine" -ForegroundColor Green
    Write-Host "    [MKT]  Marketplace      - Themes and extensions browser" -ForegroundColor Green
    Write-Host "    [AUTO] Auto-Config      - Detects Spotify automatically" -ForegroundColor Green
    Write-Host "    [1CLK] One-Click Apply  - Instant customization" -ForegroundColor Green
}

function Show-Prerequisites {
    Write-Host ""
    Write-Host "    Prerequisites:" -ForegroundColor Yellow
    Write-Host "       - Spotify must be installed from spotify.com (not Microsoft Store)" -ForegroundColor Gray
    Write-Host "       - Log into Spotify for at least 60 seconds before installing" -ForegroundColor Gray
    Write-Host ""
}

function Test-ExistingInstallation {
    $localAppData = $env:LOCALAPPDATA
    $userProfile = $env:USERPROFILE

    $spicetifyPaths = @(
        Join-Path $userProfile ".spicetify\spicetify.exe"
        Join-Path $localAppData "spicetify\spicetify.exe"
    )

    $marketplacePaths = @(
        Join-Path $env:APPDATA "spicetify\CustomApps\marketplace"
        Join-Path $userProfile ".spicetify\CustomApps\marketplace"
        Join-Path $localAppData "spicetify\CustomApps\marketplace"
    )

    $spicetifyInstalled = $spicetifyPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
    $marketplaceInstalled = $marketplacePaths | Where-Object {
        (Test-Path $_) -and
        (Test-Path (Join-Path $_ "manifest.json")) -and
        (Test-Path (Join-Path $_ "index.js"))
    } | Select-Object -First 1

    return @{
        Spicetify = [bool]$spicetifyInstalled
        Marketplace = [bool]$marketplaceInstalled
        SpicetifyPath = $spicetifyInstalled
        MarketplacePath = $marketplaceInstalled
    }
}

function Show-ProgressBar {
    param([int]$Percent, [string]$Status)
    Write-Host "`r    [$Percent%] $Status" -ForegroundColor Green -NoNewline
}

function Complete-ProgressBar {
    Write-Host ""
    Write-Host ""
}

function Install-SpicetifyCLI {
    Write-Log "Starting installation..." -Level "STEP"
    $url = "https://raw.githubusercontent.com/spicetify/cli/main/install.ps1"

    try {
        Show-ProgressBar -Percent 10 -Status "Downloading..."
        $script = Invoke-WebRequest -Uri $url -UseBasicParsing -ErrorAction Stop

        Show-ProgressBar -Percent 30 -Status "Installing..."
        $patchedScript = @"
function Read-Host { return 'Y' }`n`$Host.UI.RawUI | Add-Member -MemberType ScriptMethod -Name Flushinputbuffer -Value {} -Force`n`$Host.UI | Add-Member -MemberType ScriptMethod -Name PromptForChoice -Value { return 0 } -Force`n
"@ + $script.Content

        $job = Start-Job -ScriptBlock {
            param($code)
            Invoke-Expression $code 2>&1 | ForEach-Object { $_.ToString() }
        } -ArgumentList $patchedScript

        $completed = $job | Wait-Job -Timeout 600
        if (!$completed) {
            $job | Stop-Job -ErrorAction SilentlyContinue
            $job | Remove-Job -Force
            Write-Log "Installation timed out after 10 minutes" -Level "ERROR"
            return $false
        }

        $output = $job | Receive-Job 2>$null
        $job | Remove-Job -Force

        $output | ForEach-Object {
            if ($_ -and $_ -notmatch '^NotSpecified:|RemoteException|NativeCommandError') {
                Write-Log ($_ | Out-String).Trim()
            }
        }

        $installed = Test-ExistingInstallation
        if (!$installed.Spicetify) {
            Write-Log "spicetify.exe was not found after installation" -Level "ERROR"
            return $false
        }
        if (!$installed.Marketplace) {
            Write-Log "Marketplace was not found after installation" -Level "ERROR"
            return $false
        }

        Show-ProgressBar -Percent 60 -Status "CLI and Marketplace installed"
        Write-Log "Installation verified" -Level "SUCCESS"
        return $true
    }
    catch {
        Write-Log "Failed: $($_.Exception.Message)" -Level "ERROR"
        return $false
    }
}

function Invoke-SpicetifyApply {
    param([switch]$RebuildBackup)

    Write-Log "Applying Spicetify..." -Level "STEP"
    Show-ProgressBar -Percent 70 -Status "Preparing..."

    $localAppData = $env:LOCALAPPDATA
    $userProfile = $env:USERPROFILE

    $paths = @(
        Join-Path $userProfile ".spicetify\spicetify.exe"
        Join-Path $localAppData "spicetify\spicetify.exe"
    )

    $spicetifyPath = $paths | Where-Object { Test-Path $_ } | Select-Object -First 1

    if (!$spicetifyPath) {
        Write-Log "spicetify.exe not found" -Level "ERROR"
        return $false
    }

    Write-Log "Found: $spicetifyPath" -Level "INFO"

    $spicetifyDir = Split-Path $spicetifyPath -Parent
    $env:PATH = "$env:PATH;$spicetifyDir"

    Show-ProgressBar -Percent 75 -Status "Closing Spotify..."
    Get-Process "spotify" -ErrorAction SilentlyContinue | ForEach-Object {
        try { $_.Kill(); Start-Sleep -Seconds 2 } catch {}
    }

    Start-Sleep -Seconds 3
    Show-ProgressBar -Percent 80 -Status "Applying patches..."

    try {
        $arguments = if ($RebuildBackup) {
            @("restore", "backup", "apply")
        }
        else {
            @("apply")
        }

        Show-ProgressBar -Percent 90 -Status "Applying..."
        Write-Log "Running: spicetify $($arguments -join ' ')" -Level "INFO"
        $job = Start-Job -ScriptBlock {
            param($exe, $dir, $commandArguments)
            Set-Location $dir
            & $exe $commandArguments 2>&1
            $LASTEXITCODE
        } -ArgumentList $spicetifyPath, $spicetifyDir, (,$arguments)

        $completed = $job | Wait-Job -Timeout 300

        if ($completed) {
            $result = Receive-Job $job
            Remove-Job $job

            for ($i = 0; $i -lt $result.Count - 1; $i++) {
                if ($result[$i]) { Write-Log ($result[$i] | Out-String).Trim() }
            }

            $exitCode = $result[-1]

            if ($exitCode -eq 0) {
                Show-ProgressBar -Percent 100 -Status "Complete!"
                Write-Log "Applied successfully" -Level "SUCCESS"
                return $true
            }
            else {
                Show-ProgressBar -Percent 100 -Status "Failed"
                Write-Log "Spicetify failed with exit code $exitCode" -Level "ERROR"
                return $false
            }
        }
        else {
            $job | Stop-Job -ErrorAction SilentlyContinue
            Remove-Job $job -Force
            Show-ProgressBar -Percent 100 -Status "Timeout"
            Write-Log "Apply timed out after 5 minutes" -Level "ERROR"
            return $false
        }
    }
    catch {
        Write-Log "Apply error: $($_.Exception.Message)" -Level "ERROR"
        Show-ProgressBar -Percent 100 -Status "Failed"
        return $false
    }
}

function Invoke-Uninstall {
    $success = $true
    Show-Header
    Write-Host ""
    Write-Host "    Uninstalling Spicetify..." -ForegroundColor Yellow
    Write-Host ""

    $localAppData = $env:LOCALAPPDATA
    $appData = $env:APPDATA
    $userProfile = $env:USERPROFILE

    Write-Log "Stopping Spicetify processes..." -Level "STEP"
    Get-Process | Where-Object { $_.Name -like "*spicetify*" -or $_.Name -like "*spotify*" } | ForEach-Object {
        try {
            $_.Kill()
            Start-Sleep -Seconds 1
        }
        catch { }
    }

    $possiblePaths = @(
        Join-Path $userProfile ".spicetify"
        Join-Path $localAppData "spicetify"
    )

    $spicetifyPath = $possiblePaths | ForEach-Object { Join-Path $_ "spicetify.exe" } | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($spicetifyPath) {
        Write-Log "Restoring Spotify..." -Level "STEP"
        try {
            & $spicetifyPath restore 2>&1 | ForEach-Object { Write-Log ($_ | Out-String).Trim() }
            if ($LASTEXITCODE -eq 0) {
                Write-Log "Spotify restored" -Level "SUCCESS"
            }
            else {
                Write-Log "Restore failed with exit code $LASTEXITCODE" -Level "WARN"
                $success = $false
            }
        }
        catch {
            Write-Log "Warning: Could not restore" -Level "WARN"
            $success = $false
        }
    }

    Start-Sleep -Seconds 2

    $dirsToRemove = @(
        Join-Path $appData "spicetify"
        Join-Path $localAppData "spicetify"
        Join-Path $userProfile ".spicetify"
        Join-Path $userProfile "spicetify-cli"
    )

    foreach ($dir in $dirsToRemove) {
        if (Test-Path $dir) {
            Write-Log "Removing: $dir" -Level "STEP"
            try {
                Remove-Item -Path $dir -Recurse -Force -ErrorAction SilentlyContinue
                if (!(Test-Path $dir)) {
                    Write-Log "Removed" -Level "SUCCESS"
                }
                else {
                    Write-Log "Partially removed (some files in use)" -Level "WARN"
                    $success = $false
                }
            }
            catch {
                Write-Log "Warning: Could not remove" -Level "WARN"
                $success = $false
            }
        }
    }

    Write-Log "Cleaning PATH..." -Level "STEP"
    try {
        $userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
        if ($userPath) {
            $paths = $userPath -split ';' | Where-Object { $_.ToString() -notmatch 'spicetify' }
            $newPath = $paths -join ';'
            [Environment]::SetEnvironmentVariable("PATH", $newPath, "User")
            Write-Log "PATH cleaned" -Level "SUCCESS"
        }
    }
    catch {
        Write-Log "Warning: Could not clean PATH" -Level "WARN"
        $success = $false
    }

    Write-Host ""
    Write-Host "    Uninstallation complete!" -ForegroundColor Green
    Write-Host "    Spicetify has been removed from your system." -ForegroundColor Gray
    Write-Host ""
    if (!$Silent) {
        Write-Host "    Press any key to exit..." -ForegroundColor DarkGray
        $null = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    }

    return $success
}

function Show-Completion {
    param([bool]$Success)

    Complete-ProgressBar
    Write-Host ""
    Write-Host "    ===============================================" -ForegroundColor Green
    Write-Host ""

    if ($Success) {
        Write-Host "    Installation completed successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "    Next steps:" -ForegroundColor White
        Write-Host "      1. Restart Spotify" -ForegroundColor Gray
        Write-Host "      2. Look for the Marketplace tab" -ForegroundColor Gray
        Write-Host ""
        Write-Host "    After Spotify updates, run:" -ForegroundColor Gray
        Write-Host "      spicetify restore backup" -ForegroundColor Yellow
        Write-Host "      spicetify apply" -ForegroundColor Yellow
    }
    else {
        Write-Host "    Installation failed" -ForegroundColor Red
        Write-Host ""
        Write-Host "    Check the log:" -ForegroundColor Gray
        Write-Host "      $script:LogFile" -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "    ===============================================" -ForegroundColor Green
    Write-Host ""

    if (!$Silent) {
        Write-Host "    Press any key to exit..." -ForegroundColor DarkGray
        $null = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    }
}

Initialize-Environment

if (Test-IsAdministrator) {
    Write-Log "SpotiPatch must not be run as administrator. Start it normally and try again." -Level "ERROR"
    Write-Host ""
    Write-Host "    Running Spicetify as administrator can make Spotify files inaccessible" -ForegroundColor Yellow
    Write-Host "    to your normal Windows account." -ForegroundColor Yellow
    exit 1
}

if ($Uninstall) {
    $result = Invoke-Uninstall
    if ($result) { exit 0 } else { exit 1 }
}

Show-Header
Show-Features
Show-Prerequisites

$existing = Test-ExistingInstallation

if ($existing.Spicetify -and $existing.Marketplace -and !$Force) {
    Write-Host ""
    Write-Host "    Spicetify and Marketplace are already installed!" -ForegroundColor Green
    Write-Host "    Location: $($existing.SpicetifyPath)" -ForegroundColor DarkGray
    Write-Host ""

    if (!$Silent) {
        $response = Read-Host "    Would you like to re-apply? (Y/N)"
        if ($response -eq 'Y' -or $response -eq 'y') {
            $result = Invoke-SpicetifyApply -RebuildBackup
            Show-Completion -Success $result
            if (!$result) { exit 1 }
        }
    }
    exit 0
}

if (!$Silent) {
    Write-Host ""
    Write-Host "    Options:" -ForegroundColor White
    Write-Host "      [I] Install" -ForegroundColor Green
    Write-Host "      [U] Uninstall" -ForegroundColor Yellow
    Write-Host "      [C] Cancel" -ForegroundColor Gray
    Write-Host ""
    $response = Read-Host "    Select option (I/U/C)"

    switch ($response.ToUpper()) {
        'U' {
            $result = Invoke-Uninstall
            if ($result) { exit 0 } else { exit 1 }
        }
        'C' {
            exit 0
        }
    }
}

Write-Host ""
Write-Host "    Installing..." -ForegroundColor Cyan
Write-Host ""

$success = $true

if (!$existing.Spicetify -or !$existing.Marketplace -or $Force) {
    $cliResult = Install-SpicetifyCLI
    $success = $success -and $cliResult
}

if ($success) {
    $applyResult = Invoke-SpicetifyApply
    $success = $success -and $applyResult
}

Show-Completion -Success $success
if ($success) { exit 0 } else { exit 1 }
