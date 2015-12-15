using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VSLangProj;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class SetMainRobotButton : ButtonBase
    {
        public SetMainRobotButton(Frc_ExtensionPackage package) : base(package, true, GuidList.guidFRC_ExtensionCmdSet, (int)PkgCmdIDList.cmdidSetRobotProject)
        {
        }

        public override void QueryCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                menuCommand.Visible = false;
                menuCommand.Enabled = false;
            }
        }

        public override void ButtonCallback(object sender, EventArgs e)
        {
            OutputWriter.Instance.WriteLine("Set Pressed");
        }
    }
}
