using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace SpotiPatch
{
    public partial class App : Application
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(int dpiFlag);

        private const int DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Set DPI awareness before anything else for crisp text
            try
            {
                SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            }
            catch
            {
                try
                {
                    SetProcessDPIAware();
                }
                catch { }
            }

            // Enable WPF per-monitor DPI support
            RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.Default;

            base.OnStartup(e);
        }
    }
}
