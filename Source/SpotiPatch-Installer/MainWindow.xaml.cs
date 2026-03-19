using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
            // Get the directory where the executable is located
            var exeDir = AppContext.BaseDirectory;
            var dataDir = Path.Combine(exeDir, "Data");
            
            // Create Data folder if it doesn't exist
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
            
            // Enable window dragging
            MouseLeftButtonDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                    DragMove();
            };
            
            Log("Ready to install Spicetify");
            Log("Make sure Spotify is installed and logged in for 60+ seconds");
            Log($"Log file: {_logFilePath}");
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
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

            var result = MessageBox.Show(
                "This will completely remove Spicetify from your system.\n\n" +
                "Steps:\n" +
                "1. Restore Spotify to original state\n" +
                "2. Remove Spicetify files\n" +
                "3. Remove configuration\n\n" +
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
                await RunStepAsync("Uninstalling Spicetify", 0, 100, async () =>
                {
                    await UninstallSpicetifyAsync();
                });

                Log("============================");
                Log("Uninstallation completed!");
                Log("Spicetify has been removed");
                Log("============================");

                MessageBox.Show(
                    "Spicetify has been uninstalled.\n\n" +
                    "Spotify should now be back to its original state.",
                    "Uninstallation Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
                MessageBox.Show(
                    $"Uninstallation failed:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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

            // Check if already installed
            var (spicetifyInstalled, marketplaceInstalled) = CheckExistingInstallation();
            
            if (spicetifyInstalled && marketplaceInstalled)
            {
                var result = MessageBox.Show(
                    "Spicetify and Marketplace are already installed!\n\n" +
                    "Would you like to:\n" +
                    "- Click 'Yes' to re-apply Spicetify\n" +
                    "- Click 'No' to cancel\n\n" +
                    "Tip: Use 'spicetify restore backup apply' if Spotify was updated.",
                    "Already Installed",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);
                
                if (result == MessageBoxResult.Yes)
                {
                    await ReapplySpicetifyAsync();
                }
                else
                {
                    Log("Installation cancelled - already installed");
                }
                return;
            }
            else if (spicetifyInstalled)
            {
                var result = MessageBox.Show(
                    "Spicetify CLI is already installed, but Marketplace might be missing.\n\n" +
                    "Would you like to:\n" +
                    "- Click 'Yes' to install Marketplace\n" +
                    "- Click 'No' to cancel",
                    "Partial Installation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result != MessageBoxResult.Yes)
                {
                    Log("Installation cancelled by user");
                    return;
                }
            }

            // Confirmation dialog
            var confirm = MessageBox.Show(
                "This will install Spicetify CLI and Marketplace.\n\n" +
                "Requirements:\n" +
                "- Spotify installed from spotify.com (not Microsoft Store)\n" +
                "- Logged into Spotify for at least 60 seconds\n\n" +
                "Do you want to continue?",
                "Start Installation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                Log("Installation cancelled by user");
                return;
            }

            _isInstalling = true;
            SetUIState(false);
            ClearLog();

            try
            {
                // Step 1: Install CLI (includes Marketplace automatically)
                await RunStepAsync("Installing Spicetify CLI & Marketplace", 0, 60, async () =>
                {
                    var script = GetEmbeddedScript("SpotiPatch.Scripts.spicetify-cli-install.ps1");
                    script = PatchScript(script);
                    await ExecutePowerShellAsync(script, "CLI");
                });

                // Step 2: Verify Installation
                await RunStepAsync("Verifying installation", 60, 80, async () =>
                {
                    await VerifyCliInstallationAsync();
                });

                // Step 3: Apply Spicetify
                await RunStepAsync("Applying Spicetify", 80, 95, async () =>
                {
                    await ApplySpicetifyAsync();
                });

                // Step 4: Detect Spotify
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

                MessageBox.Show(
                    "Spicetify has been installed successfully!\n\n" +
                    "Please restart Spotify to see the changes.\n\n" +
                    "You can now browse themes and extensions from the Marketplace tab.\n\n" +
                    $"Log file saved to Desktop:\n{_logFilePath}",
                    "Installation Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
                if (ex.InnerException != null)
                    Log($"Details: {ex.InnerException.Message}");
                Log($"Log file: {_logFilePath}");

                MessageBox.Show(
                    $"Installation failed:\n{ex.Message}\n\n" +
                    $"Log file saved to Desktop:\n{_logFilePath}",
                    "Installation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isInstalling = false;
                SetUIState(true);
                UpdateStatus("Installation complete");
            }
        }

        private async Task RunStepAsync(string status, int startProgress, int endProgress, Func<Task> action)
        {
            UpdateStatus(status);
            SetProgress(startProgress);
            
            Log($"");
            Log($">>> {status}...");
            
            await action();
            
            SetProgress(endProgress);
            Log($"✓ {status} completed");
        }

        private string GetEmbeddedScript(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new FileNotFoundException($"Embedded resource not found: {resourceName}");
            
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private string PatchScript(string script)
        {
            // Inject auto-confirm header at the start of the script
            var header = @"
# SpotiPatch Auto-Confirm Header
function Read-Host { return 'Y' }
$Host.UI.RawUI | Add-Member -MemberType ScriptMethod -Name Flushinputbuffer -Value {} -Force
$Host.UI | Add-Member -MemberType ScriptMethod -Name PromptForChoice -Value { return 0 } -Force
# End Header

";
            
            return header + script;
        }

        private (bool spicetifyInstalled, bool marketplaceInstalled) CheckExistingInstallation()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            
            // Check for spicetify.exe
            var spicetifyPaths = new[]
            {
                Path.Combine(userProfile, ".spicetify", "spicetify.exe"),
                Path.Combine(localAppData, "spicetify", "spicetify.exe")
            };
            
            bool spicetifyInstalled = spicetifyPaths.Any(File.Exists);
            
            // Check for Marketplace
            var marketplacePaths = new[]
            {
                Path.Combine(userProfile, ".spicetify", "CustomApps", "marketplace"),
                Path.Combine(localAppData, "spicetify", "CustomApps", "marketplace"),
                Path.Combine(userProfile, "spicetify-cli", "CustomApps", "marketplace")
            };
            
            bool marketplaceInstalled = marketplacePaths.Any(Directory.Exists);
            
            if (spicetifyInstalled)
            {
                Log($"Found existing Spicetify installation");
                var path = spicetifyPaths.First(File.Exists);
                Log($"Location: {path}");
            }
            
            if (marketplaceInstalled)
            {
                Log($"Found existing Marketplace installation");
                var path = marketplacePaths.First(Directory.Exists);
                Log($"Location: {path}");
            }
            
            return (spicetifyInstalled, marketplaceInstalled);
        }
        
        private async Task ReapplySpicetifyAsync()
        {
            _isInstalling = true;
            SetUIState(false);
            
            try
            {
                Log("Re-applying Spicetify...");
                await ApplySpicetifyAsync();
                
                Log("============================");
                Log("Spicetify re-applied successfully!");
                Log("Restart Spotify to see changes");
                Log("============================");
                
                MessageBox.Show(
                    "Spicetify has been re-applied!\n\n" +
                    "Please restart Spotify to see the changes.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
                MessageBox.Show(
                    $"Failed to re-apply Spicetify:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
            
            // Remove ANSI escape sequences like [90m, [0m, [38;2;208;46;18m, etc.
            var result = Regex.Replace(input, @"\x1B\[[0-9;]*[A-Za-z]", "");
            
            // Remove other common control characters
            result = Regex.Replace(result, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "");
            
            // Remove PowerShell progress bar artifacts like 'es', ' OK', '>' progress indicators
            result = Regex.Replace(result, @"\bes\b", "");
            result = Regex.Replace(result, @"^\s*>\s*$", "");
            
            return result.Trim();
        }

        private async Task ExecutePowerShellAsync(string script, string stepName)
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"spicetify_install_{stepName}_{Guid.NewGuid()}.ps1");
            
            try
            {
                // Write script to temp file
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

                using var process = Process.Start(psi);
                if (process == null)
                    throw new InvalidOperationException("Failed to start PowerShell process");

                // Read output asynchronously
                var outputTask = Task.Run(async () =>
                {
                    string line;
                    while ((line = await process.StandardOutput.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var cleanLine = StripAnsiCodes(line);
                            Dispatcher.Invoke(() => Log(cleanLine));
                        }
                    }
                });

                var errorTask = Task.Run(async () =>
                {
                    string line;
                    while ((line = await process.StandardError.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var cleanLine = StripAnsiCodes(line);
                            // Only log if it's not a progress bar line
                            if (!cleanLine.Contains("Patching files") && !cleanLine.Contains("Extracting"))
                            {
                                Dispatcher.Invoke(() => Log(cleanLine));
                            }
                        }
                    }
                });

                // Wait for completion with timeout (10 minutes)
                var processTask = Task.WhenAll(outputTask, errorTask, process.WaitForExitAsync());
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(10));
                
                var completed = await Task.WhenAny(processTask, timeoutTask);

                if (completed == timeoutTask)
                {
                    try { process.Kill(); } catch { }
                    Log("WARNING: Installation is taking longer than expected, but may have completed.");
                    // Don't throw - sometimes the process completes but we miss the exit signal
                }
                else
                {
                    // Process completed normally
                    await processTask; // Ensure we await to get any exceptions
                }

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Installation script exited with code {process.ExitCode}");
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
                catch { }
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
                if (File.Exists(Path.Combine(path, "Spotify.exe")))
                {
                    Log($"Found Spotify at: {path}");
                    return;
                }
            }

            Log("Warning: Could not find Spotify in common locations");
            Log("Make sure Spotify is installed from spotify.com");
        }

        private async Task VerifyCliInstallationAsync()
        {
            Log("Verifying Spicetify CLI is properly installed...");
            
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            
            var possiblePaths = new[]
            {
                Path.Combine(userProfile, ".spicetify"),
                Path.Combine(localAppData, "spicetify")
            };

            string spicetifyPath = null;
            int attempts = 0;
            int maxAttempts = 10;
            
            // Retry loop - wait for installation to complete
            while (spicetifyPath == null && attempts < maxAttempts)
            {
                foreach (var path in possiblePaths)
                {
                    var exe = Path.Combine(path, "spicetify.exe");
                    if (File.Exists(exe))
                    {
                        spicetifyPath = exe;
                        // Add to PATH
                        Environment.SetEnvironmentVariable("PATH", 
                            Environment.GetEnvironmentVariable("PATH") + ";" + path);
                        Log($"Verified: spicetify.exe found at {exe}");
                        break;
                    }
                }
                
                if (spicetifyPath == null)
                {
                    attempts++;
                    if (attempts < maxAttempts)
                    {
                        Log($"Waiting for CLI installation... (attempt {attempts}/{maxAttempts})");
                        await Task.Delay(1000); // Wait 1 second between checks
                    }
                }
            }
            
            if (spicetifyPath == null)
            {
                throw new FileNotFoundException(
                    "Spicetify CLI was not found after installation. " +
                    "The installation may have failed or the PATH was not updated correctly.");
            }
            
            // Test that spicetify actually works
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = spicetifyPath,
                    Arguments = "-v",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    await process.WaitForExitAsync(new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                    if (process.ExitCode == 0)
                    {
                        var version = await process.StandardOutput.ReadToEndAsync();
                        Log($"CLI verified: Spicetify {version.Trim()}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Warning: Could not verify CLI version: {ex.Message}");
                // Don't throw - the exe exists, that's the main thing
            }
            
            Log("CLI verification complete. Proceeding to Marketplace installation...");
        }

        private async Task ApplySpicetifyAsync()
        {
            // Add spicetify to PATH for this session
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            
            var possiblePaths = new[]
            {
                Path.Combine(userProfile, ".spicetify"),
                Path.Combine(localAppData, "spicetify")
            };

            string spicetifyPath = null;
            foreach (var path in possiblePaths)
            {
                var exe = Path.Combine(path, "spicetify.exe");
                if (File.Exists(exe))
                {
                    spicetifyPath = exe;
                    Environment.SetEnvironmentVariable("PATH", 
                        Environment.GetEnvironmentVariable("PATH") + ";" + path);
                    break;
                }
            }

            if (spicetifyPath == null)
            {
                Log("Warning: Could not find spicetify.exe");
                Log("You may need to restart and run 'spicetify backup' then 'spicetify apply' manually");
                return;
            }

            Log($"Applying Spicetify using: {spicetifyPath}");
            
            var spicetifyDir = Path.GetDirectoryName(spicetifyPath);
            
            // Step 1: Create backup
            Log("Creating backup...");
            var backupPsi = new ProcessStartInfo
            {
                FileName = spicetifyPath,
                Arguments = "backup",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = spicetifyDir
            };

            using (var backupProcess = Process.Start(backupPsi))
            {
                if (backupProcess != null)
                {
                    string line;
                    while ((line = await backupProcess.StandardOutput.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            Log(line);
                    }
                    while ((line = await backupProcess.StandardError.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            Log($"[WARN] {line}");
                    }
                    await backupProcess.WaitForExitAsync();
                    Log($"Backup completed (exit code: {backupProcess.ExitCode})");
                }
            }

            // Step 2: Apply
            Log("Applying configuration...");
            var applyPsi = new ProcessStartInfo
            {
                FileName = spicetifyPath,
                Arguments = "apply",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = spicetifyDir
            };

            using var applyProcess = Process.Start(applyPsi);
            if (applyProcess == null)
            {
                Log("Warning: Failed to start spicetify apply");
                return;
            }

            string applyLine;
            while ((applyLine = await applyProcess.StandardOutput.ReadLineAsync()) != null)
            {
                if (!string.IsNullOrWhiteSpace(applyLine))
                    Log(applyLine);
            }

            while ((applyLine = await applyProcess.StandardError.ReadLineAsync()) != null)
            {
                if (!string.IsNullOrWhiteSpace(applyLine))
                    Log($"[WARN] {applyLine}");
            }

            await applyProcess.WaitForExitAsync();

            if (applyProcess.ExitCode == 0)
                Log("Spicetify applied successfully!");
            else
                Log($"Spicetify exited with code {applyProcess.ExitCode}");
        }

        private async Task UninstallSpicetifyAsync()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            
            // Find spicetify.exe
            var possiblePaths = new[]
            {
                Path.Combine(userProfile, ".spicetify"),
                Path.Combine(localAppData, "spicetify")
            };

            string spicetifyPath = null;
            foreach (var path in possiblePaths)
            {
                var exe = Path.Combine(path, "spicetify.exe");
                if (File.Exists(exe))
                {
                    spicetifyPath = exe;
                    break;
                }
            }

            // Step 1: Restore Spotify
            if (spicetifyPath != null)
            {
                Log("Restoring Spotify to original state...");
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = spicetifyPath,
                        Arguments = "restore",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        Log("Spotify restored successfully");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Warning: Could not restore Spotify: {ex.Message}");
                }
            }
            else
            {
                Log("spicetify.exe not found, skipping restore step");
            }

            // Step 2: Remove directories
            var dirsToRemove = new[]
            {
                Path.Combine(appData, "spicetify"),
                Path.Combine(localAppData, "spicetify"),
                Path.Combine(userProfile, ".spicetify"),
                Path.Combine(userProfile, "spicetify-cli")
            };

            foreach (var dir in dirsToRemove)
            {
                if (Directory.Exists(dir))
                {
                    Log($"Removing: {dir}");
                    try
                    {
                        Directory.Delete(dir, true);
                        Log("Removed successfully");
                    }
                    catch (Exception ex)
                    {
                        Log($"Warning: Could not remove {dir}: {ex.Message}");
                    }
                }
            }

            // Step 3: Remove from PATH
            Log("Cleaning up PATH...");
            try
            {
                var userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
                if (userPath != null)
                {
                    var paths = userPath.Split(';').ToList();
                    var originalCount = paths.Count;
                    paths.RemoveAll(p => p.Contains("spicetify", StringComparison.OrdinalIgnoreCase));
                    
                    if (paths.Count != originalCount)
                    {
                        var newPath = string.Join(";", paths);
                        Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
                        Log("PATH cleaned");
                    }
                    else
                    {
                        Log("PATH already clean");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Warning: Could not clean PATH: {ex.Message}");
            }
            
            Log("Uninstallation complete");
        }

        private void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logLine = $"[{timestamp}] {message}";
            _logBuilder.AppendLine(logLine);
            
            // Write to file
            WriteToLogFile(logLine);
            
            Dispatcher.Invoke(() =>
            {
                LogText.Text = _logBuilder.ToString();
                LogScrollViewer.ScrollToEnd();
            });
        }

        private void ClearLog()
        {
            _logBuilder.Clear();
            LogText.Text = "";
        }

        private void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = status;
            });
        }

        private void SetProgress(int value)
        {
            Dispatcher.Invoke(() =>
            {
                InstallProgress.Value = Math.Min(100, Math.Max(0, value));
            });
        }

        private void SetUIState(bool isIdle)
        {
            Dispatcher.Invoke(() =>
            {
                InstallButton.IsEnabled = isIdle;
                InstallButton.Content = isIdle ? "Install Spicetify" : "Installing...";
                CancelButton.IsEnabled = isIdle;
            });
        }
    }
}
