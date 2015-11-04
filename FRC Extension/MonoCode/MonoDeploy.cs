using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
using RobotDotNet.FRC_Extension.RoboRIO_Code;

namespace RobotDotNet.FRC_Extension.MonoCode
{
    public class MonoDeploy
    {
        private DeployManager m_deployManager;
        private MonoFile m_monoFile;
        private string m_teamNumber;

        public MonoDeploy(string teamNumber, DeployManager deployManager, MonoFile monoFile)
        {
            m_deployManager = deployManager;
            m_teamNumber = teamNumber;
            m_monoFile = monoFile;
        }

        internal void DeployMono()
        {
            var writer = OutputWriter.Instance;

            //Connect to RoboRIO
            writer.WriteLine("Attempting to Connect to RoboRIO");

            Task<bool> rioConnectionTask = m_deployManager.StartConnectionTask(m_teamNumber);


            bool success = m_monoFile.UnzipMonoFile();

            if (!success) return;

            //Successfully extracted files.

            

            writer.WriteLine("Waiting for Connection to Finish");
            bool taskTimeout = rioConnectionTask.Wait(10000);

            //If our connection did not timeout
            if (taskTimeout && rioConnectionTask.Result == true)
            {
                ConnectionInfo lvuser, admin;
                m_deployManager.GetConnectionInfos(out lvuser, out admin);

                if (admin == null) return;

                writer.WriteLine("Successfully Connected to RoboRIO");

                List<string> deployFiles = m_monoFile.GetUnzippedFileList();

                writer.WriteLine("Creating Opkg Directory");

                string deployLocation = "/home/admin/opkg";

                RoboRIOConnection.RunCommand($"mkdir -p {deployLocation}", admin);

                writer.WriteLine("Deploying Mono Files");

                success = RoboRIOConnection.DeployFiles(deployFiles, deployLocation, admin);

                if (!success)
                {
                    return;
                }

                writer.WriteLine("Installing Mono");

                var monoRet = RoboRIOConnection.RunCommand("opgk install *.ipk", admin);

                //Check for success.

                if (m_deployManager.CheckMonoInstall())
                {
                    writer.WriteLine("Mono Installed Successfully");
                }
                else
                {
                    writer.WriteLine("Mono not installed successfully");
                }

            }
            else
            {
                //Our connection timed out or did not connect
                writer.WriteLine("Failed to Connect to RoboRIO. Exiting.");
            }
        }
    }
}
