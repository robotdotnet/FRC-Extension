using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using RoboRIO_Tool;

namespace RobotDotNet.FRC_Extension
{
    internal class OutputWriter : IConsoleWriter
    {
        private Window m_window = null;
        private IVsOutputWindowPane m_outputPane = null;

        private bool m_initialized = false;

        private static OutputWriter s_instance;

        public static OutputWriter Instance
        {
            get { return s_instance ?? (s_instance = new OutputWriter()); }
        }

        private OutputWriter()
        {
            
        }

        private void Initialize()
        {
            if (m_initialized)
                return;
            DTE dte = Package.GetGlobalService(typeof (SDTE)) as DTE;
            m_window = dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);

            if (m_window == null)
                return;

            IVsOutputWindow outputWindow = Package.GetGlobalService(typeof (SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindow == null)
                return;
            Guid paneGuid = VSConstants.OutputWindowPaneGuid.GeneralPane_guid;
            outputWindow.CreatePane(ref paneGuid, "FRC", 1, 0);
            outputWindow.GetPane(ref paneGuid, out m_outputPane);

            if (m_outputPane == null)
                return;

            m_initialized = true;

        }

        public void Write(string value)
        {
            if (!m_initialized)
                Initialize();
            if (!m_initialized) return;
            m_window.Visible = true;
            m_outputPane.OutputStringThreadSafe(value);
            m_outputPane.Activate();
        }

        public void Write(int value)
        {
            Write(value.ToString());
        }

        public void Write(double value)
        {
            Write(value.ToString());
        }

        public void WriteLine(string value)
        {
            if (!m_initialized)
                Initialize();
            if (!m_initialized) return;
            m_window.Visible = true;
            m_outputPane.OutputStringThreadSafe(value + "\n");
            m_outputPane.Activate();
        }

        public void WriteLine(int value)
        {
            WriteLine(value.ToString());
        }

        public void WriteLine(double value)
        {
            WriteLine(value.ToString());
        }
    }
}
