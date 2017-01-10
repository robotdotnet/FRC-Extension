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

                    DeployManager m = new DeployManager(Package.PublicGetService<DTE>());

                    var writer = OutputWriter.Instance;

                    menuCommand.Visible = false;
                    m_killing = true;

                    //Connect to RoboRIO
                    await writer.WriteLineAsync("Attempting to Connect to RoboRIO").ConfigureAwait(false);

                    Task<bool> rioConnectionTask = m.StartConnectionTaskAsync(teamNumber);
                    Task delayTask = Task.Delay(10000);

                    //Successfully extracted files.

                    await writer.WriteLineAsync("Waiting for Connection to Finish").ConfigureAwait(false);
                    if (await Task.WhenAny(rioConnectionTask, delayTask).ConfigureAwait(false) == rioConnectionTask)
                    {
                        //Connected
                        if (rioConnectionTask.Result)
                        {
                            await writer.WriteLineAsync("Killing currently running robot code.").ConfigureAwait(false);
                            await RoboRIOConnection.RunCommandAsync(DeployProperties.KillOnlyCommand, ConnectionUser.LvUser).ConfigureAwait(false);
                            await writer.WriteLineAsync("Done.").ConfigureAwait(false);
                            await ThreadHelperExtensions.SwitchToUiThread();
                            m_killing = false;
                            menuCommand.Visible = true;
                        }
                        else
                        {
                            //Did not successfully connect
                            await writer.WriteLineAsync("Failed to Connect to RoboRIO. Exiting.").ConfigureAwait(false);
                            await ThreadHelperExtensions.SwitchToUiThread();
                            m_killing = false;
                            menuCommand.Visible = true;
                        }
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
