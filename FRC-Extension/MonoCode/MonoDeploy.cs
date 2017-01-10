using System.Collections.Generic;
using System.Threading.Tasks;
using RobotDotNet.FRC_Extension.RoboRIOCode;

namespace RobotDotNet.FRC_Extension.MonoCode
{
    public class MonoDeploy
    {
        private readonly DeployManager m_deployManager;
        private readonly MonoFile m_monoFile;
        private readonly string m_teamNumber;

        public MonoDeploy(string teamNumber, DeployManager deployManager, MonoFile monoFile)
        {
            m_deployManager = deployManager;
            m_teamNumber = teamNumber;
            m_monoFile = monoFile;
        }

        internal async Task DeployMonoAsync()
        {
            var writer = OutputWriter.Instance;
            await writer.ClearAsync().ConfigureAwait(false);

            //Connect to RoboRIO
            await writer.WriteLineAsync("Attempting to Connect to RoboRIO").ConfigureAwait(false);

            Task<bool> rioConnectionTask = m_deployManager.StartConnectionTaskAsync(m_teamNumber);
            Task delayTask = Task.Delay(10000);

            bool success = await m_monoFile.UnzipMonoFileAsync().ConfigureAwait(false);

            if (!success) return;

            //Successfully extracted files.

            await writer.WriteLineAsync("Waiting for Connection to Finish").ConfigureAwait(false);
            if (await Task.WhenAny(rioConnectionTask, delayTask).ConfigureAwait(false) == rioConnectionTask)
            {
                //Completed
                if (rioConnectionTask.Result)
                {
                    await writer.WriteLineAsync("Successfully Connected to RoboRIO").ConfigureAwait(false);

                    List<string> deployFiles = m_monoFile.GetUnzippedFileList();

                    await writer.WriteLineAsync("Creating Opkg Directory").ConfigureAwait(false);

                    await RoboRIOConnection.RunCommandAsync($"mkdir -p {DeployProperties.RoboRioOpgkLocation}", ConnectionUser.Admin).ConfigureAwait(false);

                    await writer.WriteLineAsync("Deploying Mono Files").ConfigureAwait(false);

                    success = await RoboRIOConnection.DeployFiles(deployFiles, DeployProperties.RoboRioOpgkLocation, ConnectionUser.Admin).ConfigureAwait(false);

                    if (!success)
                    {
                        return;
                    }

                    await writer.WriteLineAsync("Installing Mono").ConfigureAwait(false);

                    var monoRet = await RoboRIOConnection.RunCommandAsync(DeployProperties.OpkgInstallCommand, ConnectionUser.Admin).ConfigureAwait(false);

                    //Check for success.
                    bool monoSuccess = await m_deployManager.CheckMonoInstallAsync().ConfigureAwait(false);

                    if (monoSuccess)
                    {
                        await writer.WriteLineAsync("Mono Installed Successfully").ConfigureAwait(false);
                    }
                    else
                    {
                        await writer.WriteLineAsync("Mono not installed successfully. Please try again.").ConfigureAwait(false);
                    }

                    await writer.WriteLineAsync("Cleaning up installation").ConfigureAwait(false);
                    // Set allow realtime on Mono instance
                    await
                        RoboRIOConnection.RunCommandAsync("setcap cap_sys_nice=pe /usr/bin/mono-sgen", ConnectionUser.Admin).ConfigureAwait(false);

                    //Removing ipk files from the RoboRIO
                    await RoboRIOConnection.RunCommandAsync($"rm -rf {DeployProperties.RoboRioOpgkLocation}", ConnectionUser.Admin).ConfigureAwait(false);

                    await writer.WriteLineAsync("Done. You may now deploy code to your robot.").ConfigureAwait(false);
                }
                else
                {
                    //Did not successfully connect
                    await writer.WriteLineAsync("Failed to Connect to RoboRIO. Exiting.").ConfigureAwait(false);
                }
            }
            else
            {
                //Timedout
                await writer.WriteLineAsync("RoboRIO connection timedout. Exiting.").ConfigureAwait(false);
            }
        }
    }
}
