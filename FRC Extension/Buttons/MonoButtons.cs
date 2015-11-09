using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using RobotDotNet.FRC_Extension.MonoCode;
using RobotDotNet.FRC_Extension.WPILibFolder;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class MonoButtons
    {
        private readonly OutputWriter m_output;

        private readonly Frc_ExtensionPackage m_package;

        private OleMenuCommand m_downloadMonoMenuItem;
        private OleMenuCommand m_installMonoMenuItem;

        private readonly MonoFile m_monoFile;

        private bool m_installing = false;
        private bool m_downloading = false;

        public MonoButtons(Frc_ExtensionPackage package)
        {
            m_output = OutputWriter.Instance;
            m_package = package;

            string monoFolder = WPILibFolderStructure.CreateMonoFolder();

            string monoFile = monoFolder + Path.DirectorySeparatorChar + DeployProperties.MonoVersion;

            m_monoFile = new MonoFile(monoFile);


            OleMenuCommandService mcs =
                m_package.PublicGetService(typeof (IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                //Download Mono Command Id
                CommandID downloadMonoCommandId = new CommandID(GuidList.guidFRC_ExtensionCmdSet, (int)PkgCmdIDList.cmdidDownloadMono);
                m_downloadMonoMenuItem = new OleMenuCommand(DownloadMonoCallback, downloadMonoCommandId);
                m_downloadMonoMenuItem.Visible = true;
                m_downloadMonoMenuItem.Enabled = true;
                mcs.AddCommand(m_downloadMonoMenuItem);

                CommandID installMonoCommandId = new CommandID(GuidList.guidFRC_ExtensionCmdSet, (int)PkgCmdIDList.cmdidInstallMono);
                m_installMonoMenuItem = new OleMenuCommand(InstallMonoCallback, installMonoCommandId);
                m_installMonoMenuItem.Visible = true;
                m_installMonoMenuItem.Enabled = true;
                mcs.AddCommand(m_installMonoMenuItem);
            }
        }

        private async void DownloadMonoCallback(object sender, EventArgs e)
        {
            bool haveInternet = false;

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

            if (!haveInternet) return;


            string monoFolder = WPILibFolderStructure.CreateMonoFolder();

            string monoFile = monoFolder + Path.DirectorySeparatorChar + DeployProperties.MonoVersion;

            m_monoFile.FileName = monoFile;

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
        }

        private async void InstallMonoCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
            {
                return;
            }

            bool properFileExists = true;

            properFileExists = m_monoFile.CheckFileValid();

            if (properFileExists)
            {
                //We can deploy
                await DeployMono(menuCommand);
            }
            else
            {
                //Ask to see if we want to load the file or download it
                string retVal = LoadMonoPopup();

                if (!string.IsNullOrEmpty(retVal))
                {
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

        private async System.Threading.Tasks.Task DeployMono(OleMenuCommand menuCommand)
        {
            try
            {
                    SettingsPageGrid page;
                    string teamNumber = m_package.GetTeamNumber(out page);

                    if (teamNumber == null) return;



                    //Disable Install Button
                    m_installing = true;
                    menuCommand.Visible = false;

                    DeployManager m = new DeployManager(m_package.PublicGetService(typeof(DTE)) as DTE);
                    MonoDeploy deploy = new MonoDeploy(teamNumber, m, m_monoFile);

                    await deploy.DeployMono();

                    m_installing = false;
                    menuCommand.Visible = true;

            }
            catch (Exception ex)
            {
                m_output.WriteLine(ex.ToString());
                m_installing = false;
                menuCommand.Visible = true;
            }
        }

        public void DownloadMonoPopup()
        {
            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)m_package.PublicGetService(typeof(SVsUIShell));
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
            IVsUIShell uiShell = (IVsUIShell)m_package.PublicGetService(typeof(SVsUIShell));
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
            IVsUIShell uiShell = (IVsUIShell)m_package.PublicGetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;

            uiShell.ShowMessageBox(0, ref clsid, "Mono File Not Found",
                $"Mono file not found. Would you like to load an existing file?", string.Empty, 0,
                OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_INFO, 0, out result);

            if (result == 6)
            {
                return MonoFile.SelectMonoFile();
            }
            return null;
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
    }

}
