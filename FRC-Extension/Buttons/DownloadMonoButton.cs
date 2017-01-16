using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using RobotDotNet.FRC_Extension.MonoCode;
using RobotDotNet.FRC_Extension.RoboRIOCode;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class DownloadMonoButton : ButtonBase
    {
        private readonly MonoFile m_monoFile;
        private bool m_downloading;
        private readonly InstallMonoButton m_installButton;

        public DownloadMonoButton(Frc_ExtensionPackage package, MonoFile monoFile, InstallMonoButton installButton)
            : base(package, false, GuidList.guidFRC_ExtensionCmdSet, (int)PkgCmdIDList.cmdidDownloadMono)
        {
            m_monoFile = monoFile;
            m_installButton = installButton;
        }


        public override void ButtonCallback(object sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
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
                        // We're just going right back to doing something on the UI thread, so just resume back on the UI thread.
                        if (!await CheckForInternetConnectionAsync().ConfigureAwait(true))
                        {
                            m_downloading = false;
                            menuCommand.Visible = true;
                            return;
                        }

                        m_monoFile.ResetToDefaultDirectory();

                        bool downloadNew = !m_monoFile.CheckFileValid();

                        if (downloadNew)
                        {
                            Output.ProgressBarLabel = "Downloading Mono";
                            await m_monoFile.DownloadMonoAsync(Output).ConfigureAwait(false);

                            //Verify Download
                            bool verified = m_monoFile.CheckFileValid();

                            if (verified)
                            {
                                // Show a Message Box to prove we were here
                                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                                IVsUIShell uiShell = Package.PublicGetService<IVsUIShell, SVsUIShell>();
                                Guid clsid = Guid.Empty;
                                int result = await ShowMessageAsync("Mono Successfully Downloaded. Would you like to install it to the RoboRIO?",
                                    string.Empty).ConfigureAwait(false);
                                if (result == 6)
                                {
                                    //Install Mono.
                                    await m_installButton.DeployMonoAsync(m_installButton.GetMenuCommand()).ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                // Show a Message Box to prove we were here
                                await ShowMessageAsync("Mono Download Failed. Please Try Again", string.Empty).ConfigureAwait(false);
                            }

                        }
                        else
                        {
                            // Show a Message Box to prove we were here
                            IVsUIShell uiShell = Package.PublicGetService<IVsUIShell, SVsUIShell>();
                            await ShowMessageAsync("Mono Already Downloaded", string.Empty).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        await Output.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        m_downloading = false;
                        menuCommand.Visible = true;
                    }
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    m_downloading = false;
                    menuCommand.Visible = true;
                }
            });
        }

        private async Task<int> ShowMessageAsync(string title, string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            IVsUIShell uiShell = Package.PublicGetService<IVsUIShell, SVsUIShell>();
            int result;
            ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                                0,
                                Guid.Empty,
                                title,
                                message,
                                string.Empty,
                                0,
                                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                                OLEMSGICON.OLEMSGICON_INFO,
                                0, // false
                                out result));
            return result;
        }

        private class TimeoutWebClient : WebClient
        {
            private readonly int m_timeout;

            public TimeoutWebClient(int timeout)
            {
                m_timeout = timeout;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var result = base.GetWebRequest(address);
                result.Timeout = m_timeout;
                return result;
            }
        }

        private static async Task<bool> CheckForInternetConnectionAsync()
        {
            bool haveInternet;
            try
            {
                using (var client = new TimeoutWebClient(1000))
                {
                    using (var stream = await client.OpenReadTaskAsync(DeployProperties.MonoUrl).ConfigureAwait(false))
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

        protected override System.Threading.Tasks.Task ButtonCallbackAsync(object sender, EventArgs e)
        {
            return System.Threading.Tasks.Task.FromResult(false);
        }
    }
}
