using System;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using RobotDotNet.FRC_Extension.RoboRIOCode;
using Task = System.Threading.Tasks.Task;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class KillButton : ButtonBase
    {
        private bool m_killing;

        public KillButton(Frc_ExtensionPackage package)
            : base(package, false, GuidList.guidFRC_ExtensionCmdSet, (int)PkgCmdIDList.cmdidKillButton)
        {

        }

        public override async void ButtonCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
            {
                return;
            }
            if (!m_killing)
            {
                try
                {
                    SettingsPageGrid page;
                    string teamNumber = Package.GetTeamNumber(out page);

                    if (teamNumber == null) return;

                    DeployManager m = new DeployManager(Package.PublicGetService(typeof (DTE)) as DTE);

                    var writer = OutputWriter.Instance;

                    menuCommand.Visible = false;
                    m_killing = true;

                    //Connect to RoboRIO
                    writer.WriteLine("Attempting to Connect to RoboRIO");

                    Task<bool> rioConnectionTask = m.StartConnectionTask(teamNumber);
                    Task delayTask = Task.Delay(10000);

                    //Successfully extracted files.

                    writer.WriteLine("Waiting for Connection to Finish");
                    if (await Task.WhenAny(rioConnectionTask, delayTask) == rioConnectionTask)
                    {
                        //Connected
                        if (rioConnectionTask.Result)
                        {
                            writer.WriteLine("Killing currently running robot code.");
                            await RoboRIOConnection.RunCommand(DeployProperties.KillOnlyCommand, ConnectionUser.LvUser);
                            writer.WriteLine("Done.");
                            m_killing = false;
                            menuCommand.Visible = true;
                        }
                        else
                        {
                            //Did not successfully connect
                            writer.WriteLine("Failed to Connect to RoboRIO. Exiting.");
                            m_killing = false;
                            menuCommand.Visible = true;
                        }
                    }
                    else
                    {
                        //Timedout
                        writer.WriteLine("RoboRIO connection timedout. Exiting.");
                        m_killing = false;
                        menuCommand.Visible = true;
                    }
                }
                catch (Exception ex)
                {
                    Output.WriteLine(ex.ToString());
                    m_killing = false;
                    menuCommand.Visible = true;
                    OutputWriter.Instance.WriteLine("Code Kill Failed");
                }
            }
        }
    }
}
