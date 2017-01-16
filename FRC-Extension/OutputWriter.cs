using System;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace RobotDotNet.FRC_Extension
{
    /// <summary>
    /// This class allows us to create an output windows that the RoboRIO connector can interface with.
    /// </summary>
    public class OutputWriter : IProgress<int>
    {
        private Window m_window;
        private IVsOutputWindowPane m_outputPane;
        private IVsStatusbar m_statusBar;

        private bool m_initialized;

        private static OutputWriter s_instance;

        public static OutputWriter Instance => s_instance ?? (s_instance = new OutputWriter());

        private OutputWriter()
        {
        }

        private void Initialize()
        {
            if (m_initialized)
                return;
            DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            m_window = dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);

            if (m_window == null)
                return;

            IVsOutputWindow outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindow == null)
                return;
            Guid paneGuid = VSConstants.OutputWindowPaneGuid.GeneralPane_guid;
            outputWindow.CreatePane(ref paneGuid, "FRC", 1, 0);
            outputWindow.GetPane(ref paneGuid, out m_outputPane);

            if (m_outputPane == null)
                return;

            m_statusBar = ServiceProvider.GlobalProvider.GetService(typeof(SVsStatusbar)) as IVsStatusbar;

            if (m_statusBar == null)
                return;

            m_initialized = true;
        }

        public async Task ClearAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!m_initialized)
                Initialize();
            if (!m_initialized) return;
            m_outputPane.Clear();
        }

        public async Task WriteAsync(string value)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!m_initialized)
                Initialize();
            if (!m_initialized) return;
            m_window.Visible = true;
            m_outputPane.OutputStringThreadSafe(value);
            m_outputPane.Activate();
        }

        public Task WriteAsync(int value)
        {
            return WriteAsync(value.ToString());
        }

        public Task WriteAsync(double value)
        {
            return WriteAsync(value.ToString());
        }

        public async Task WriteLineAsync(string value)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!m_initialized)
                Initialize();
            if (!m_initialized) return;
            m_window.Visible = true;
            m_outputPane.OutputStringThreadSafe(value + "\n");
            m_outputPane.Activate();
        }

        public Task WriteLineAsync(int value)
        {
            return WriteLineAsync(value.ToString());
        }

        public Task WriteLineAsync(double value)
        {
            return WriteLineAsync(value.ToString());
        }

        private uint m_cookie;
        private string m_progressLabel;

        public string ProgressBarLabel
        {
            get { return m_progressLabel; }
            set
            {
                m_progressLabel = value;
                if (!m_initialized)
                    Initialize();
                if (!m_initialized) return;
                m_statusBar.SetText(value);
            }
        }

        public void Report(int value)
        {
            if (!m_initialized)
                Initialize();
            if (!m_initialized) return;

            m_statusBar.Progress(ref m_cookie, 1, ProgressBarLabel, (uint)value, 100);
        }
    }
}
