using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using RobotDotNet.FRC_Extension.MonoCode;
using RobotDotNet.FRC_Extension.WPILibFolder;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class NetConsoleButton
    {

        private readonly OutputWriter m_output;

        private readonly Frc_ExtensionPackage m_package;



        public NetConsoleButton(Frc_ExtensionPackage package)
        {
            m_output = OutputWriter.Instance;
            m_package = package;


            OleMenuCommandService mcs =
                m_package.PublicGetService(typeof (IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                //Adds a command so we can open NetConsole. 
                CommandID netconsoleCommandID = new CommandID(GuidList.guidFRC_ExtensionCmdSet,
                    (int)PkgCmdIDList.cmdidNetconsole);
                OleMenuCommand netconsoleItem = new OleMenuCommand(NetconsoleCallback, netconsoleCommandID);
                netconsoleItem.BeforeQueryStatus += QueryNetConsole;
                mcs.AddCommand(netconsoleItem);
            }
        }

        //Check to see if NetConsole exits. If so we can enable the open button.
        private void QueryNetConsole(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                bool visable = File.Exists(@"C:\Program Files\NetConsole for cRIO\NetConsole.exe") ||
                               File.Exists(@"C:\Program Files (x86)\NetConsole for cRIO\NetConsole.exe");



                menuCommand.Visible = visable;
                if (menuCommand.Visible)
                {
                    menuCommand.Enabled = true;
                }
            }
        }

        /// <summary>
        /// This function is called when the NetConsole button is pressed.
        /// </summary>
        private async void NetconsoleCallback(object sender, EventArgs e)
        {
            await DeployManager.StartNetConsole();
        }
    }
}
