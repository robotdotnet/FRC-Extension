using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class AboutButton
    {
        private readonly OutputWriter m_output;

        private readonly Frc_ExtensionPackage m_package;



        public AboutButton(Frc_ExtensionPackage package)
        {
            m_output = OutputWriter.Instance;
            m_package = package;


            OleMenuCommandService mcs =
                m_package.PublicGetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                //For settings, we just want to pop up the standard settings menu.
                CommandID aboutCommandID = new CommandID(GuidList.guidFRC_ExtensionCmdSet,
                    (int)PkgCmdIDList.cmdidAboutButton);
                MenuCommand aboutItem = new MenuCommand(((sender, e) => OpenAbout()), aboutCommandID);
                mcs.AddCommand(aboutItem);
            }
        }

        public void OpenAbout()
        {
            //TODO: Get version

            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)m_package.PublicGetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "FRC Extension",
                       string.Format(CultureInfo.CurrentCulture, "", this.ToString()),
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_INFO,
                       0,        // false
                       out result));
        }
    }
}
