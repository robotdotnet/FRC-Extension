using System.Collections.Generic;
using System.Threading.Tasks;
using EnvDTE;
using RobotDotNet.FRC_Extension.RoboRIOCode;

namespace RobotDotNet.FRC_Extension.MonoCode
{
    public class MonoDeploy
    {
        private readonly MonoFile m_monoFile;
        private readonly string m_teamNumber;

        public MonoDeploy(string teamNumber, MonoFile monoFile)
        {
            m_teamNumber = teamNumber;
            m_monoFile = monoFile;
        }

        public async Task<bool> CheckMonoInstallAsync(RoboRioConnection rioConn)
        {
            await OutputWriter.Instance.WriteLineAsync("Checking for Mono install").ConfigureAwait(false);
            var retVal = await rioConn.RunCommandAsync($"test -e {DeployProperties.RoboRioMonoBin}", ConnectionUser.LvUser).ConfigureAwait(false);
            return retVal.ExitStatus == 0;
        }

        internal async Task DeployMonoAsync()
        {
            var writer = OutputWriter.Instance;
            await writer.ClearAsync().ConfigureAwait(false);

            //Connect to RoboRIO
            await writer.WriteLineAsync("Attempting to Connect to RoboRIO").ConfigureAwait(false);

            Task<RoboRioConnection> rioConnectionTask = RoboRioConnection.StartConnectionTaskAsync(m_teamNumber);


            bool success = await m_monoFile.UnzipMonoFileAsync().ConfigureAwait(false);

            if (!success) return;

            //Successfully extracted files.

            await writer.WriteLineAsync("Waiting for Connection to Finish").ConfigureAwait(false);
            using (var roboRioConnection = await rioConnectionTask.ConfigureAwait(false))
            {
                if (roboRioConnection.Connected)
                {
                    await writer.WriteLineAsync("Successfully Connected to RoboRIO").ConfigureAwait(false);

                    List<string> deployFiles = m_monoFile.GetUnzippedFileList();

                    await writer.WriteLineAsync("Creating Opkg Directory").ConfigureAwait(false);

                    await
                        roboRioConnection.RunCommandAsync($"mkdir -p {DeployProperties.RoboRioOpgkLocation}",
                            ConnectionUser.Admin).ConfigureAwait(false);

                    await writer.WriteLineAsync("Deploying Mono Files").ConfigureAwait(false);

                    success =
                        await
                            roboRioConnection.DeployFiles(deployFiles, DeployProperties.RoboRioOpgkLocation,
                                ConnectionUser.Admin).ConfigureAwait(false);

                    if (!success)
                    {
                        return;
                    }

                    await writer.WriteLineAsync("Installing Mono").ConfigureAwait(false);

                    var monoRet =
                        await
                            roboRioConnection.RunCommandAsync(DeployProperties.OpkgInstallCommand, ConnectionUser.Admin)
                                .ConfigureAwait(false);

                    //Check for success.
                    bool monoSuccess = await CheckMonoInstallAsync(roboRioConnection).ConfigureAwait(false);

                    if (monoSuccess)
                    {
                        await writer.WriteLineAsync("Mono Installed Successfully").ConfigureAwait(false);
                    }
                    else
                    {
                        await
                            writer.WriteLineAsync("Mono not installed successfully. Please try again.")
                                .ConfigureAwait(false);
                    }

                    await writer.WriteLineAsync("Cleaning up installation").ConfigureAwait(false);
                    // Set allow realtime on Mono instance
                    await
                        roboRioConnection.RunCommandAsync("setcap cap_sys_nice=pe /usr/bin/mono-sgen",
                            ConnectionUser.Admin).ConfigureAwait(false);

                    //Removing ipk files from the RoboRIO
                    await
                        roboRioConnection.RunCommandAsync($"rm -rf {DeployProperties.RoboRioOpgkLocation}",
                            ConnectionUser.Admin).ConfigureAwait(false);

                    await writer.WriteLineAsync("Done. You may now deploy code to your robot.").ConfigureAwait(false);
                }
                else
                {
                    //Did not successfully connect
                    await writer.WriteLineAsync("Failed to Connect to RoboRIO. Exiting.").ConfigureAwait(false);
                }
            }
        }
    }
}
