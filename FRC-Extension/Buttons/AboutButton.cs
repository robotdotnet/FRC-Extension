using System;
using System.Globalization;
using Microsoft.VisualStudio.Shell.Interop;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class AboutButton : ButtonBase
    { 

        public AboutButton(Frc_ExtensionPackage package) : base(package, false, GuidList.guidFRC_ExtensionCmdSet, (int)PkgCmdIDList.cmdidAboutButton)
        {
        }

        public override void ButtonCallback(object sender, EventArgs e)
        {
            //TODO: Get version

            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)Package.PublicGetService(typeof(SVsUIShell));
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
