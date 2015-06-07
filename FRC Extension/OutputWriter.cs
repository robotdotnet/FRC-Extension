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

namespace RobotDotNet.FRC_Extension
{
    internal static class OutputWriter
    {
        private static Window s_window = null;
        private static IVsOutputWindowPane s_outputPane = null;

        private static bool s_initialized = false;

        public static void Initialize()
        {
            if (s_initialized)
                return;
            DTE dte = Package.GetGlobalService(typeof (SDTE)) as DTE;
            s_window = dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);

            if (s_window == null)
                return;

            IVsOutputWindow outputWindow = Package.GetGlobalService(typeof (SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindow == null)
                return;
            Guid paneGuid = VSConstants.OutputWindowPaneGuid.GeneralPane_guid;
            outputWindow.CreatePane(ref paneGuid, "FRC", 1, 0);
            outputWindow.GetPane(ref paneGuid, out s_outputPane);

            if (s_outputPane == null)
                return;

            s_initialized = true;

        }

        public static void WriteToPane(string output)
        {
            if (!s_initialized)
                Initialize();
            if (!s_initialized) return;
            s_window.Visible = true;
            s_outputPane.OutputStringThreadSafe(output + "\n");
            s_outputPane.Activate();
        }

    }
}
