using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;
using RobotDotNet.FRC_Extension.RoboRIO_Code;

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

        internal async Task DeployMono()
        {
            var writer = OutputWriter.Instance;

            //Connect to RoboRIO
            writer.WriteLine("Attempting to Connect to RoboRIO");

            Task<bool> rioConnectionTask = m_deployManager.StartConnectionTask(m_teamNumber);
            Task delayTask = Task.Delay(10000);


            bool success = await m_monoFile.UnzipMonoFile();

            if (!success) return;

            //Successfully extracted files.

            writer.WriteLine("Waiting for Connection to Finish");
            if (await Task.WhenAny(rioConnectionTask, delayTask) == rioConnectionTask)
            {
                //Completed
                if (rioConnectionTask.Result == true)
                {
                    writer.WriteLine("Successfully Connected to RoboRIO");

                    List<string> deployFiles = m_monoFile.GetUnzippedFileList();

                    writer.WriteLine("Creating Opkg Directory");

                    await RoboRIOConnection.RunCommand($"mkdir -p {DeployProperties.RoboRioOpgkLocation}", ConnectionUser.Admin);

                    writer.WriteLine("Deploying Mono Files");

                    success = await RoboRIOConnection.DeployFiles(deployFiles, DeployProperties.RoboRioOpgkLocation, ConnectionUser.Admin);

                    if (!success)
                    {
                        return;
                    }

                    writer.WriteLine("Installing Mono");

                    var monoRet = await RoboRIOConnection.RunCommand(DeployProperties.OpkgInstallCommand, ConnectionUser.Admin);

                    //Check for success.

                    if (await m_deployManager.CheckMonoInstall())
                    {
                        writer.WriteLine("Mono Installed Successfully");
                    }
                    else
                    {
                        writer.WriteLine("Mono not installed successfully");
                    }

                    //TODO : Cleanup files on RIO

                    await RoboRIOConnection.RunCommand($"rm -rf {DeployProperties.RoboRioOpgkLocation}", ConnectionUser.Admin);
                }
                else
                {
                    //Did not successfully connect
                    writer.WriteLine("Failed to Connect to RoboRIO. Exiting.");
                }
            }
            else
            {
                //Timedout
                writer.WriteLine("RoboRIO connection timedout. Exiting.");
            }
        }
    }
}
