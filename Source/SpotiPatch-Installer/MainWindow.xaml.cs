using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace SpotiPatch
{
    public partial class MainWindow : Window
    {
        private bool _isInstalling = false;
        private StringBuilder _logBuilder = new StringBuilder();
        private string _logFilePath;

        private void InitializeLogFile()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dataDir = Path.Combine(localAppData, "SpotiPatch", "Logs");
            Directory.CreateDirectory(dataDir);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            _logFilePath = Path.Combine(dataDir, $"SpotiPatch_Log_{timestamp}.txt");

            var header = $"SpotiPatch Installer Log{Environment.NewLine}" +
                        $"========================{Environment.NewLine}" +
                        $"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
                        $"User: {Environment.UserName}{Environment.NewLine}" +
                        $"Computer: {Environment.MachineName}{Environment.NewLine}" +
                        $"OS: {Environment.OSVersion}{Environment.NewLine}" +
                        $"========================{Environment.NewLine}{Environment.NewLine}";

            File.WriteAllText(_logFilePath, header);
        }

        private void WriteToLogFile(string message)
        {
            try
            {
                if (!string.IsNullOrEmpty(_logFilePath))
                {
                    File.AppendAllText(_logFilePath, message + Environment.NewLine);
                }
            }
            catch { /* Ignore file write errors */ }
        }

        public MainWindow()
        {
            InitializeComponent();
            InitializeLogFile();

            MouseLeftButtonDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                    DragMove();
            };

            Log("Ready to install Spicetify");
            Log("Make sure Spotify is installed and logged in for 60+ seconds");
            Log($"Log file: {_logFilePath}");
        }

        private bool EnsureNormalUser(string operation)
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                return true;

            var message =
                $"SpotiPatch must not be run as administrator when {operation}.\n\n" +
                "Spicetify modifies files for your normal Windows account. Running elevated can leave Spotify " +
                "with files that the normal account cannot access.\n\n" +
                "Close SpotiPatch and start it normally.";

            Log($"ERROR: Administrator privileges detected. Cannot continue with {operation}.");
            MessageBox.Show(message, "Administrator Mode Not Supported", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling)
            {
                var result = MessageBox.Show(
                    "Installation is in progress. Are you sure you want to exit?",
                    "Confirm Exit",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                    return;
            }
            Close();
        }

        private async void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling) return;
            if (!EnsureNormalUser("uninstalling Spicetify")) return;

            var result = MessageBox.Show(
                "This will completely remove Spicetify from your system.\n\n" +
                "Steps:\n1. Restore Spotify to original state\n2. Remove Spicetify files\n3. Remove configuration\n\n" +
                "Are you sure you want to uninstall?",
                "Confirm Uninstallation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            _isInstalling = true;
            SetUIState(false);
            ClearLog();

            try
            {
                var uninstallClean = false;
                await RunStepAsync("Uninstalling Spicetify", 0, 100, async () =>
                {
                    uninstallClean = await UninstallSpicetifyAsync();
                });

                Log("============================");
                Log(uninstallClean ? "Uninstallation completed successfully!" : "Uninstallation completed with warnings. Review the log.");
                Log("============================");
                MessageBox.Show(
                    uninstallClean
                        ? "Spicetify has been uninstalled.\n\nSpotify should now be back to its original state."
                        : "Spicetify cleanup finished, but one or more steps reported a warning.\n\nReview the log for details.",
                    uninstallClean ? "Uninstallation Complete" : "Uninstallation Completed with Warnings",
                    MessageBoxButton.OK,
                    uninstallClean ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
                MessageBox.Show($"Uninstallation failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isInstalling = false;
                SetUIState(true);
            }
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling) return;
            if (!EnsureNormalUser("installing or applying Spicetify")) return;

            var (spicetifyInstalled, marketplaceInstalled) = CheckExistingInstallation();
            if (spicetifyInstalled && marketplaceInstalled)
            {
                var result = MessageBox.Show(
                    "Spicetify and Marketplace are already installed!\n\nWould you like to:\n- Click 'Yes' to re-apply Spicetify\n- Click 'No' to cancel\n\nTip: Use 'spicetify restore backup apply' if Spotify was updated.",
                    "Already Installed", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                    await ReapplySpicetifyAsync();
                else
                    Log("Installation cancelled - already installed");
                return;
            }
            else if (spicetifyInstalled)
            {
                var result = MessageBox.Show(
                    "Spicetify CLI is already installed, but Marketplace might be missing.\n\nWould you like to:\n- Click 'Yes' to install Marketplace\n- Click 'No' to cancel",
                    "Partial Installation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    Log("Installation cancelled by user");
                    return;
                }
            }

            var confirm = MessageBox.Show(
                "This will install Spicetify CLI and Marketplace.\n\nRequirements:\n- Spotify installed from spotify.com (not Microsoft Store)\n- Logged into Spotify for at least 60 seconds\n\nDo you want to continue?",
                "Start Installation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                Log("Installation cancelled by user");
                return;
            }

            _isInstalling = true;
            SetUIState(false);
            ClearLog();
            var installationSucceeded = false;

            try
            {
                await RunStepAsync("Installing Spicetify CLI", 0, 45, async () =>
                {
                    var script = GetEmbeddedScript("SpotiPatch.Scripts.spicetify-cli-install.ps1");
                    script = PatchScript(script, 1);
                    await ExecutePowerShellAsync(script, "CLI");
                });
                await RunStepAsync("Verifying CLI", 45, 55, VerifyCliInstallationAsync);
                await RunStepAsync("Installing Marketplace", 55, 75, async () =>
                {
                    var script = GetEmbeddedScript("SpotiPatch.Scripts.spicetify-marketplace-install.ps1");
                    script = PatchScript(script, 0);
                    await ExecutePowerShellAsync(script, "Marketplace");
                });
                await RunStepAsync("Verifying Marketplace", 75, 82, VerifyMarketplaceInstallationAsync);
                await RunStepAsync("Applying Spicetify", 82, 95, () => ApplySpicetifyAsync());
                await RunStepAsync("Detecting Spotify installation", 95, 100, () =>
                {
                    DetectSpotify();
                    return Task.CompletedTask;
                });

                Log("============================");
                Log("Installation completed successfully!");
                Log("Please restart Spotify to see the changes");
                Log("Look for the Marketplace tab in the sidebar");
                Log($"Log saved to: {_logFilePath}");
                Log("============================");
                installationSucceeded = true;
                MessageBox.Show(
                    "Spicetify has been installed successfully!\n\nPlease restart Spotify to see the changes.\n\nYou can now browse themes and extensions from the Marketplace tab.\n\n" +
                    $"Log file:\n{_logFilePath}",
                    "Installation Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
                if (ex.InnerException != null) Log($"Details: {ex.InnerException.Message}");
                Log($"Log file: {_logFilePath}");
                MessageBox.Show($"Installation failed:\n{ex.Message}\n\nLog file:\n{_logFilePath}", "Installation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isInstalling = false;
                SetUIState(true);
                UpdateStatus(installationSucceeded ? "Installation complete" : "Installation failed");
            }
        }

        private async Task RunStepAsync(string status, int startProgress, int endProgress, Func<Task> action)
        {
            UpdateStatus(status);
            SetProgress(startProgress);
            Log("");
            Log($">>> {status}...");
            await action();
            SetProgress(endProgress);
            Log($"✓ {status} completed");
        }

        private string GetEmbeddedScript(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) throw new FileNotFoundException($"Embedded resource not found: {resourceName}");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private string PatchScript(string script, int promptChoice)
        {
            var header = $@"
# SpotiPatch Auto-Confirm Header
function Read-Host {{ return 'Y' }}
$Host.UI.RawUI | Add-Member -MemberType ScriptMethod -Name Flushinputbuffer -Value {{}} -Force
$Host.UI | Add-Member -MemberType ScriptMethod -Name PromptForChoice -Value {{ return {promptChoice} }} -Force
# End Header

";
            const string marker = "$ErrorActionPreference = 'Stop'";
            var markerIndex = script.IndexOf(marker, StringComparison.Ordinal);
            return markerIndex >= 0 ? script.Insert(markerIndex, header) : header + script;
        }

        private (bool spicetifyInstalled, bool marketplaceInstalled) CheckExistingInstallation()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var spicetifyPaths = new[]
            {
                Path.Combine(userProfile, ".spicetify", "spicetify.exe"),
                Path.Combine(localAppData, "spicetify", "spicetify.exe")
            };
            var marketplacePaths = new[]
            {
                Path.Combine(appData, "spicetify", "CustomApps", "marketplace"),
                Path.Combine(userProfile, ".spicetify", "CustomApps", "marketplace"),
                Path.Combine(localAppData, "spicetify", "CustomApps", "marketplace"),
                Path.Combine(userProfile, "spicetify-cli", "CustomApps", "marketplace")
            };
            var spicetifyInstalled = spicetifyPaths.Any(File.Exists);
            var marketplaceInstalled = marketplacePaths.Any(IsMarketplaceInstallationValid);
            if (spicetifyInstalled) Log($"Found existing Spicetify installation\nLocation: {spicetifyPaths.First(File.Exists)}");
            if (marketplaceInstalled) Log($"Found existing Marketplace installation\nLocation: {marketplacePaths.First(IsMarketplaceInstallationValid)}");
            return (spicetifyInstalled, marketplaceInstalled);
        }

        private static bool IsMarketplaceInstallationValid(string path) =>
            Directory.Exists(path) && File.Exists(Path.Combine(path, "manifest.json")) && File.Exists(Path.Combine(path, "index.js"));

        private async Task ReapplySpicetifyAsync()
        {
            if (!EnsureNormalUser("re-applying Spicetify")) return;
            _isInstalling = true;
            SetUIState(false);
            try
            {
                Log("Re-applying Spicetify...");
                await ApplySpicetifyAsync(true);
                Log("============================\nSpicetify re-applied successfully!\nRestart Spotify to see changes\n============================");
                MessageBox.Show("Spicetify has been re-applied!\n\nPlease restart Spotify to see the changes.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
                MessageBox.Show($"Failed to re-apply Spicetify:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isInstalling = false;
                SetUIState(true);
            }
        }

        private string StripAnsiCodes(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var result = Regex.Replace(input, @"\x1B\[[0-9;]*[A-Za-z]", "");
            result = Regex.Replace(result, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "");
            result = Regex.Replace(result, @"\bes\b", "");
            result = Regex.Replace(result, @"^\s*>\s*$", "");
            return result.Trim();
        }

        private async Task ExecutePowerShellAsync(string script, string stepName)
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"spicetify_install_{stepName}_{Guid.NewGuid()}.ps1");
            try
            {
                await File.WriteAllTextAsync(tempFile, script);
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -NoProfile -File \"{tempFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetTempPath()
                };
                using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start PowerShell process");
                var outputTask = Task.Run(async () =>
                {
                    string line;
                    while ((line = await process.StandardOutput.ReadLineAsync()) != null)
                        if (!string.IsNullOrWhiteSpace(line)) Dispatcher.Invoke(() => Log(StripAnsiCodes(line)));
                });
                var errorTask = Task.Run(async () =>
                {
                    string line;
                    while ((line = await process.StandardError.ReadLineAsync()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var cleanLine = StripAnsiCodes(line);
                        if (!cleanLine.Contains("Patching files") && !cleanLine.Contains("Extracting")) Dispatcher.Invoke(() => Log(cleanLine));
                    }
                });
                using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                try { await process.WaitForExitAsync(timeout.Token); }
                catch (OperationCanceledException)
                {
                    try { process.Kill(true); } catch { }
                    await process.WaitForExitAsync();
                    throw new TimeoutException("The installation script exceeded the 10 minute timeout and was stopped.");
                }
                await Task.WhenAll(outputTask, errorTask);
                if (process.ExitCode != 0) throw new InvalidOperationException($"Installation script exited with code {process.ExitCode}");
            }
            finally
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            }
        }

        private void DetectSpotify()
        {
            var paths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Spotify"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "apps", "spotify", "current"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Spotify"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Spotify")
            };
            foreach (var path in paths)
            {
                if (!File.Exists(Path.Combine(path, "Spotify.exe"))) continue;
                Log($"Found Spotify at: {path}");
                return;
            }
            Log("Warning: Could not find Spotify in common locations");
            Log("Make sure Spotify is installed from spotify.com");
        }

        private async Task VerifyCliInstallationAsync()
        {
            Log("Verifying Spicetify CLI is properly installed...");
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var possiblePaths = new[] { Path.Combine(userProfile, ".spicetify"), Path.Combine(localAppData, "spicetify") };
            string spicetifyPath = null;
            for (var attempt = 1; attempt <= 10 && spicetifyPath == null; attempt++)
            {
                spicetifyPath = possiblePaths.Select(path => Path.Combine(path, "spicetify.exe")).FirstOrDefault(File.Exists);
                if (spicetifyPath == null && attempt < 10)
                {
                    Log($"Waiting for CLI installation... (attempt {attempt}/10)");
                    await Task.Delay(1000);
                }
            }
            if (spicetifyPath == null) throw new FileNotFoundException("Spicetify CLI was not found after installation.");
            var directory = Path.GetDirectoryName(spicetifyPath);
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + directory);
            Log($"Verified: spicetify.exe found at {spicetifyPath}");
            var code = await RunSpicetifyCommandAsync(spicetifyPath, TimeSpan.FromSeconds(10), "-v");
            if (code != 0) throw new InvalidOperationException($"Spicetify version check failed with exit code {code}.");
            Log("CLI verification complete");
        }

        private async Task VerifyMarketplaceInstallationAsync()
        {
            Log("Verifying Marketplace installation...");
            var paths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "spicetify", "CustomApps", "marketplace"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "spicetify", "CustomApps", "marketplace"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".spicetify", "CustomApps", "marketplace"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "spicetify-cli", "CustomApps", "marketplace")
            };
            for (var attempt = 1; attempt <= 10; attempt++)
            {
                var path = paths.FirstOrDefault(IsMarketplaceInstallationValid);
                if (path != null) { Log($"Verified: Marketplace found at {path}"); return; }
                if (attempt < 10) await Task.Delay(500);
            }
            throw new DirectoryNotFoundException("Marketplace was not found after installation. The Marketplace installer may have failed.");
        }

        private async Task<int> RunSpicetifyCommandAsync(string spicetifyPath, TimeSpan timeout, params string[] arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = spicetifyPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(spicetifyPath)
            };
            foreach (var argument in arguments) psi.ArgumentList.Add(argument);
            using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start: spicetify {string.Join(" ", arguments)}");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            using var cancellation = new CancellationTokenSource(timeout);
            try { await process.WaitForExitAsync(cancellation.Token); }
            catch (OperationCanceledException)
            {
                try { process.Kill(true); } catch { }
                await process.WaitForExitAsync();
                throw new TimeoutException($"The command 'spicetify {string.Join(" ", arguments)}' timed out.");
            }
            LogProcessOutput(await outputTask, false);
            LogProcessOutput(await errorTask, true);
            return process.ExitCode;
        }

        private void LogProcessOutput(string output, bool isError)
        {
            if (string.IsNullOrWhiteSpace(output)) return;
            foreach (var rawLine in output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = StripAnsiCodes(rawLine);
                if (!string.IsNullOrWhiteSpace(line)) Log(isError ? $"[WARN] {line}" : line);
            }
        }

        private void StopSpotify()
        {
            foreach (var process in Process.GetProcessesByName("Spotify"))
            {
                using (process)
                {
                    try { process.Kill(true); process.WaitForExit(5000); }
                    catch (Exception ex) { Log($"[WARN] Could not close Spotify process {process.Id}: {ex.Message}"); }
                }
            }
        }

        private async Task ApplySpicetifyAsync(bool rebuildBackup = false)
        {
            var paths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".spicetify", "spicetify.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "spicetify", "spicetify.exe")
            };
            var spicetifyPath = paths.FirstOrDefault(File.Exists) ?? throw new FileNotFoundException("Could not find spicetify.exe.");
            Log($"Applying Spicetify using: {spicetifyPath}");
            StopSpotify();
            var arguments = rebuildBackup ? new[] { "restore", "backup", "apply" } : new[] { "apply" };
            Log($"Running: spicetify {string.Join(" ", arguments)}");
            var exitCode = await RunSpicetifyCommandAsync(spicetifyPath, TimeSpan.FromMinutes(5), arguments);
            if (exitCode != 0) throw new InvalidOperationException($"Spicetify failed with exit code {exitCode}.");
            Log("Spicetify applied successfully!");
        }

        private async Task<bool> UninstallSpicetifyAsync()
        {
            var success = true;
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var executables = new[] { Path.Combine(userProfile, ".spicetify", "spicetify.exe"), Path.Combine(localAppData, "spicetify", "spicetify.exe") };
            var executable = executables.FirstOrDefault(File.Exists);
            if (executable != null)
            {
                try
                {
                    StopSpotify();
                    success = await RunSpicetifyCommandAsync(executable, TimeSpan.FromMinutes(2), "restore") == 0;
                }
                catch (Exception ex) { Log($"Warning: Could not restore Spotify: {ex.Message}"); success = false; }
            }
            foreach (var dir in new[] { Path.Combine(appData, "spicetify"), Path.Combine(localAppData, "spicetify"), Path.Combine(userProfile, ".spicetify"), Path.Combine(userProfile, "spicetify-cli") })
            {
                if (!Directory.Exists(dir)) continue;
                try { Directory.Delete(dir, true); Log($"Removed: {dir}"); }
                catch (Exception ex) { Log($"Warning: Could not remove {dir}: {ex.Message}"); success = false; }
            }
            try
            {
                var userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
                if (userPath != null)
                {
                    var cleaned = string.Join(";", userPath.Split(';').Where(path => !path.Contains("spicetify", StringComparison.OrdinalIgnoreCase)));
                    Environment.SetEnvironmentVariable("PATH", cleaned, EnvironmentVariableTarget.User);
                }
            }
            catch (Exception ex) { Log($"Warning: Could not clean PATH: {ex.Message}"); success = false; }
            return success;
        }

        private void Log(string message)
        {
            var logLine = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _logBuilder.AppendLine(logLine);
            WriteToLogFile(logLine);
            Dispatcher.Invoke(() => { LogText.Text = _logBuilder.ToString(); LogScrollViewer.ScrollToEnd(); });
        }

        private void ClearLog() { _logBuilder.Clear(); LogText.Text = ""; }
        private void UpdateStatus(string status) => Dispatcher.Invoke(() => StatusText.Text = status);
        private void SetProgress(int value) => Dispatcher.Invoke(() => InstallProgress.Value = Math.Min(100, Math.Max(0, value)));
        private void SetUIState(bool isIdle) => Dispatcher.Invoke(() =>
        {
            InstallButton.IsEnabled = isIdle;
            InstallButton.Content = isIdle ? "Install Spicetify" : "Installing...";
            UninstallButton.IsEnabled = isIdle;
            CancelButton.IsEnabled = isIdle;
        });
    }
}
