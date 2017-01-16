using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Renci.SshNet.Common;
using RobotDotNet.FRC_Extension.MonoCode;
using RobotDotNet.FRC_Extension.RoboRIOCode;
using Task = System.Threading.Tasks.Task;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class InstallMonoButton : ButtonBase
    {
        private readonly MonoFile m_monoFile;
        private bool m_installing;

        public InstallMonoButton(Frc_ExtensionPackage package, MonoFile monoFile)
            : base(package, false, GuidList.guidFRC_ExtensionCmdSet, (int)PkgCmdIDList.cmdidInstallMono)
        {
            m_monoFile = monoFile;
        }

        internal OleMenuCommand GetMenuCommand()
        {
            return OleMenuItem;
        }

        protected override async Task ButtonCallbackAsync(object sender, EventArgs e)
        {
            await ThreadHelperExtensions.SwitchToUiThread();
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
            {
                return;
            }

            if (!m_installing)
            {
                try
                {
                    m_monoFile.ResetToDefaultDirectory();

                    var properFileExists = m_monoFile.CheckFileValid();

                    if (properFileExists)
                    {
                        //We can deploy
                        await ThreadHelperExtensions.SwitchToUiThread();
                        m_installing = true;
                        menuCommand.Visible = false;
                        await DeployMonoAsync(menuCommand).ConfigureAwait(true);
                        m_installing = false;
                        menuCommand.Visible = true;
                    }
                    else
                    {
                        //Ask to see if we want to load the file or download it
                        string retVal = LoadMonoPopup();

                        if (!string.IsNullOrEmpty(retVal))
                        {
                            m_monoFile.FileName = retVal;
                            //Check for valid file.
                            properFileExists = m_monoFile.CheckFileValid();

                            if (properFileExists)
                            {
                                //We can deploy
                                await DeployMonoAsync(menuCommand).ConfigureAwait(false);
                            }
                            else
                            {
                                InvalidMonoPopup();
                            }

                        }
                        else
                        {
                            DownloadMonoPopup();
                        }
                    }
                }
                catch (SshConnectionException)
                {
                    await Output.WriteLineAsync("Connection to RoboRIO lost. Install aborted.").ConfigureAwait(false);
                    await ThreadHelperExtensions.SwitchToUiThread();
                    m_installing = false;
                    menuCommand.Visible = true;
                    Output.ProgressBarLabel = "Mono Install Failed";
                }
                catch (Exception ex)
                {
                    await Output.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
                    await ThreadHelperExtensions.SwitchToUiThread();
                    m_installing = false;
                    menuCommand.Visible = true;
                    Output.ProgressBarLabel = "Mono Install Failed";
                }
            }
        }
        internal async Task DeployMonoAsync(OleMenuCommand menuCommand)
        {
            try
            {

                string teamNumber = await Package.GetTeamNumberAsync().ConfigureAwait(false);

                if (teamNumber == null) return;

                //Disable Install Button
                await ThreadHelperExtensions.SwitchToUiThread();
                m_installing = true;
                menuCommand.Visible = false;

                MonoDeploy deploy = new MonoDeploy(teamNumber, m_monoFile);

                await deploy.DeployMonoAsync().ConfigureAwait(true);

                m_installing = false;
                menuCommand.Visible = true;

            }
            catch (Exception ex)
            {
                await Output.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
                await ThreadHelperExtensions.SwitchToUiThread();
                m_installing = false;
                menuCommand.Visible = true;
            }
        }

        public void DownloadMonoPopup()
        {
            // Show a Message Box to prove we were here
            IVsUIShell uiShell = Package.PublicGetService<IVsUIShell, SVsUIShell>();
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "Please Download Mono. This can be done by clicking the \nDownload Mono button.",
                       string.Empty,
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_INFO,
                       0,        // false
                       out result));
        }

        public void InvalidMonoPopup()
        {
            // Show a Message Box to prove we were here
            IVsUIShell uiShell = Package.PublicGetService<IVsUIShell, SVsUIShell>();
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "Mono file is Invalid. Please try again.",
                       string.Empty,
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_INFO,
                       0,        // false
                       out result));
        }

        public string LoadMonoPopup()
        {
            IVsUIShell uiShell = Package.PublicGetService<IVsUIShell, SVsUIShell>();
            Guid clsid = Guid.Empty;
            int result;

            uiShell.ShowMessageBox(0, ref clsid, "Mono File Not Found",
                "Mono file not found. Would you like to load an existing file?", string.Empty, 0,
                OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_INFO, 0, out result);

            if (result == 6)
            {
                return MonoFile.SelectMonoFile();
            }
            return null;
        }
    }
}
