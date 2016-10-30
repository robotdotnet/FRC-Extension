using System;
using System.Globalization;
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
            : base(package, false, GuidList.guidFRC_ExtensionCmdSet, (int) PkgCmdIDList.cmdidInstallMono)
        {
            m_monoFile = monoFile;
        }

        internal OleMenuCommand GetMenuCommand()
        {
            return OleMenuItem;
        }

        public override async void ButtonCallback(object sender, EventArgs e)
        {
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
                        m_installing = true;
                        menuCommand.Visible = false;
                        await DeployMono(menuCommand);
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
                                await DeployMono(menuCommand);
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
                    Output.WriteLine("Connection to RoboRIO lost. Install aborted.");
                    m_installing = false;
                    menuCommand.Visible = true;
                    Output.ProgressBarLabel = "Mono Install Failed";
                }
                catch (Exception ex)
                {
                    Output.WriteLine(ex.ToString());
                    m_installing = false;
                    menuCommand.Visible = true;
                    Output.ProgressBarLabel = "Mono Install Failed";
                }
            }
        }
        internal async Task DeployMono(OleMenuCommand menuCommand)
        {
            try
            {

                string teamNumber = Package.GetTeamNumber();

                if (teamNumber == null) return;



                //Disable Install Button
                m_installing = true;
                menuCommand.Visible = false;

                DeployManager m = new DeployManager(Package.PublicGetService(typeof(DTE)) as DTE);
                MonoDeploy deploy = new MonoDeploy(teamNumber, m, m_monoFile);

                await deploy.DeployMono();

                m_installing = false;
                menuCommand.Visible = true;

            }
            catch (Exception ex)
            {
                Output.WriteLine(ex.ToString());
                m_installing = false;
                menuCommand.Visible = true;
            }
        }

        public void DownloadMonoPopup()
        {
            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)Package.PublicGetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "Please Download Mono. This can be done by clicking the \nDownload Mono button.",
                       string.Format(CultureInfo.CurrentCulture, "", this.ToString()),
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
            IVsUIShell uiShell = (IVsUIShell)Package.PublicGetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "Mono file is Invalid. Please try again.",
                       string.Format(CultureInfo.CurrentCulture, "", this.ToString()),
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
            IVsUIShell uiShell = (IVsUIShell)Package.PublicGetService(typeof(SVsUIShell));
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
