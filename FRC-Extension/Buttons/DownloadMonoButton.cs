using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using RobotDotNet.FRC_Extension.MonoCode;
using RobotDotNet.FRC_Extension.WPILibFolder;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class DownloadMonoButton : ButtonBase
    {
        private readonly MonoFile m_monoFile;
        private bool m_downloading = false;

        public DownloadMonoButton(Frc_ExtensionPackage package, MonoFile monoFile)
            : base(package, false, GuidList.guidFRC_ExtensionCmdSet, (int) PkgCmdIDList.cmdidDownloadMono)
        {
            m_monoFile = monoFile;
        }


        public override async void ButtonCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
            {
                return;
            }

            if (!m_downloading)
            {
                try
                {

                    m_downloading = true;
                    menuCommand.Visible = false;
                    if (!(await CheckForInternetConnection()))
                    {
                        m_downloading = false;
                        menuCommand.Visible = true;
                        return;
                    }

                    m_monoFile.ResetToDefaultDirectory();

                    bool downloadNew = !m_monoFile.CheckFileValid();

                    if (downloadNew)
                    {
                        m_output.ProgressBarLabel = "Downloading Mono";
                        await m_monoFile.DownloadMono(m_output);

                        //Verify Download
                        bool verified = m_monoFile.CheckFileValid();

                        if (verified)
                        {
                            // Show a Message Box to prove we were here
                            IVsUIShell uiShell = (IVsUIShell)m_package.PublicGetService(typeof(SVsUIShell));
                            Guid clsid = Guid.Empty;
                            int result;
                            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                                0,
                                ref clsid,
                                "Mono Successfully Downloaded",
                                string.Format(CultureInfo.CurrentCulture, "", this.ToString()),
                                string.Empty,
                                0,
                                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                                OLEMSGICON.OLEMSGICON_INFO,
                                0, // false
                                out result));
                        }
                        else
                        {
                            // Show a Message Box to prove we were here
                            IVsUIShell uiShell = (IVsUIShell)m_package.PublicGetService(typeof(SVsUIShell));
                            Guid clsid = Guid.Empty;
                            int result;
                            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                                0,
                                ref clsid,
                                "Mono Download Failed. Please Try Again",
                                string.Format(CultureInfo.CurrentCulture, "", this.ToString()),
                                string.Empty,
                                0,
                                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                                OLEMSGICON.OLEMSGICON_INFO,
                                0, // false
                                out result));
                        }

                    }
                    else
                    {
                        // Show a Message Box to prove we were here
                        IVsUIShell uiShell = (IVsUIShell)m_package.PublicGetService(typeof(SVsUIShell));
                        Guid clsid = Guid.Empty;
                        int result;
                        Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                            0,
                            ref clsid,
                            "Mono Already Downloaded",
                            string.Format(CultureInfo.CurrentCulture, "", this.ToString()),
                            string.Empty,
                            0,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                            OLEMSGICON.OLEMSGICON_INFO,
                            0, // false
                            out result));
                    }
                }
                catch (Exception ex)
                {
                    m_output.WriteLine(ex.ToString());
                    m_downloading = false;
                    menuCommand.Visible = true;
                }
                m_downloading = false;
                menuCommand.Visible = true;
            }
        }

        private class TimeoutWebClient : WebClient
        {
            private readonly int m_timeout;

            public TimeoutWebClient(int timeout)
            {
                this.m_timeout = timeout;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var result = base.GetWebRequest(address);
                result.Timeout = this.m_timeout;
                return result;
            }
        }

        private static async Task<bool> CheckForInternetConnection()
        {
            bool haveInternet;
            try
            {
                using (var client = new TimeoutWebClient(1000))
                {
                    using (var stream = await client.OpenReadTaskAsync(DeployProperties.MonoUrl))
                    {
                        haveInternet = true;
                    }
                }
            }
            catch
            {
                haveInternet = false;
            }

            return haveInternet;
        }
    }
}
