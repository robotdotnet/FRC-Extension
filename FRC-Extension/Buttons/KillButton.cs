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

        protected override async Task ButtonCallbackAsync(object sender, EventArgs e)
        {
            await ThreadHelperExtensions.SwitchToUiThread();
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
            {
                return;
            }
            if (!m_killing)
            {
                try
                {
                    string teamNumber = await Package.GetTeamNumberAsync().ConfigureAwait(true);

                    if (teamNumber == null) return;

                    var writer = OutputWriter.Instance;

                    menuCommand.Visible = false;
                    m_killing = true;

                    //Connect to RoboRIO
                    await writer.WriteLineAsync("Attempting to Connect to RoboRIO").ConfigureAwait(false);
                    using (var rioConn = await RoboRioConnection.StartConnectionTaskAsync(teamNumber).ConfigureAwait(false))
                    {
                        if (rioConn.Connected)
                        {
                            //Connected
                            await
                                writer.WriteLineAsync("Killing currently running robot code.").ConfigureAwait(false);
                            await
                                rioConn.RunCommandAsync(DeployProperties.KillOnlyCommand,
                                    ConnectionUser.LvUser).ConfigureAwait(false);
                            await writer.WriteLineAsync("Done.").ConfigureAwait(false);
                            await ThreadHelperExtensions.SwitchToUiThread();
                            m_killing = false;
                            menuCommand.Visible = true;
                        }
                        else
                        {
                            //Timedout
                            await writer.WriteLineAsync("RoboRIO connection timedout. Exiting.").ConfigureAwait(false);
                            await ThreadHelperExtensions.SwitchToUiThread();
                            m_killing = false;
                            menuCommand.Visible = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    await Output.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
                    await ThreadHelperExtensions.SwitchToUiThread();
                    m_killing = false;
                    menuCommand.Visible = true;
                    await OutputWriter.Instance.WriteLineAsync("Code Kill Failed").ConfigureAwait(false);
                }
            }
        }
    }
}
