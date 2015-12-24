using System;
using System.IO;
using Microsoft.VisualStudio.Shell;
using RobotDotNet.FRC_Extension.RoboRIOCode;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class NetConsoleButton : ButtonBase
    {
        public NetConsoleButton(Frc_ExtensionPackage package) : base(package, true, GuidList.guidFRC_ExtensionCmdSet, (int)PkgCmdIDList.cmdidNetconsole)
        {
        }

        //Check to see if NetConsole exits. If so we can enable the open button.
        public override void QueryCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                bool visable = File.Exists(@"C:\Program Files\NetConsole for cRIO\NetConsole.exe") ||
                               File.Exists(@"C:\Program Files (x86)\NetConsole for cRIO\NetConsole.exe");

                menuCommand.Enabled = visable;

                menuCommand.Visible = true;
            }
        }

        /// <summary>
        /// This function is called when the NetConsole button is pressed.
        /// </summary>
        public override async void ButtonCallback(object sender, EventArgs e)
        {
            await DeployManager.StartNetConsole();
        }
    }
}
