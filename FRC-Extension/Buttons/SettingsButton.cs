using System;
using RobotDotNet.FRC_Extension.SettingsPages;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class SettingsButton : ButtonBase
    {
        public SettingsButton(Frc_ExtensionPackage package) : base(package, false, GuidList.guidFRC_ExtensionCmdSet, (int)PkgCmdIDList.cmdidSettings)
        {
        }

        public override void ButtonCallback(object sender, EventArgs e)
        {
            Package.ShowOptionPage(typeof(TeamSettingsPage));
        }
    }
}
