using System;
using System.Collections.Generic;
using System.Collections.Specialized;
//using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using EnvDTE;
using EnvDTE80;
using System.Threading.Tasks;
using System.Xml;
using Renci.SshNet;
using RobotDotNet.FRC_Extension.RoboRIO_Code;

namespace RobotDotNet.FRC_Extension
{
    public class DeployManager
    {
        //We need our DTE so we can grab our solution
        private readonly DTE m_dte;

        //Holds our connection variables
        private ConnectionInfo m_adminConnectionInfo;
        private ConnectionInfo m_lvuserConnectionInfo;
        private ConnectionType m_connectionType;
        private string m_connectionIp;

        public DeployManager(DTE dte)
        {
            m_dte = dte;
        }

        
        //Uploads code to the robot and then runs it.
        public void DeployCode(string teamNumber, SettingsPageGrid page, bool debug)
        {

            var writer = OutputWriter.Instance;

            //Connect to Robot Async
            OutputWriter.Instance.WriteLine("Attempting to Connect to RoboRIO");
            Task<bool> rioConnectionTask = StartConnectionTask(teamNumber);

            string robotExe;
            List<string> files;

            if (!BuildAndPrepareCode(debug, out robotExe, out files))
            {
                return;
            }

            writer.WriteLine("Waiting for Connection to Finish");
            bool taskTimeout = rioConnectionTask.Wait(10000);

            //If our connection did not timeout
            if (taskTimeout && rioConnectionTask.Result == true)
            {
                OutputWriter.Instance.WriteLine("Successfully Connected to RoboRIO.");

                if (!CheckMonoInstall())
                {
                    //TODO: Make this error message better
                    OutputWriter.Instance.WriteLine("Mono not properly installed. ");
                    return;
                }
                OutputWriter.Instance.WriteLine("Mono correctly installed");
                OutputWriter.Instance.WriteLine("Checking RoboRIO Image");
                if (!CheckRoboRioImage())
                {
                    OutputWriter.Instance.WriteLine("roboRIO Image does not match plugin, allowed image versions: " + string.Join(", ", DeployProperties.RoboRioAllowedImages.ToArray()));
                    return;
                }
                OutputWriter.Instance.WriteLine("RoboRIO Image Correct");
                //Force making mono directory
                CreateMonoDirectory();

                //DeployAllFiles
                bool retVal = DeployRobotFiles(files);
                if (!retVal)
                {
                    OutputWriter.Instance.WriteLine("File deploy failed.");
                    return;
                }
                OutputWriter.Instance.WriteLine("Successfully Deployed Files. Starting Code.");
                UploadCode(robotExe, page, debug);
                OutputWriter.Instance.WriteLine("Successfully started robot code.");
            }
            else
            {
                writer.WriteLine("Failed to Connect to RoboRIO. Exiting.");
            }
        }

        public Task<bool> StartConnectionTask(string teamNumber)
        {
            return Task.Run(() =>
            {
                bool connected = RoboRIOConnection.CheckConnection(teamNumber, out m_connectionType, out m_connectionIp,
                    out m_lvuserConnectionInfo, out m_adminConnectionInfo);
                if (connected)
                {
                    StringBuilder builder = new StringBuilder();
                    builder.AppendLine("Connected to RoboRIO...");
                    builder.AppendLine("Interface: " + m_connectionType);
                    builder.Append("IP Address: " + m_connectionIp);
                    OutputWriter.Instance.WriteLine(builder.ToString());
                    return true;
                }
                else
                {
                    OutputWriter.Instance.WriteLine("Failed to Connect to RoboRIO...");
                    return false;
                }
            });
        }


        public bool BuildAndPrepareCode(bool debug, out string robotExe, out List<string> files)
        {
            var writer = OutputWriter.Instance;
            //Build Code
            writer.WriteLine("Building Robot Code");
            var sb = (SolutionBuild2)m_dte.Solution.SolutionBuild;
            //Check if we are building for debug or release
            if (debug)
            {
                //Switch build to debug mode
                sb.SolutionConfigurations.Item("Debug").Activate();
            }
            else
            {
                //Switch to release
                sb.SolutionConfigurations.Item("Release").Activate();
            }
            sb.Build(true);

            //If Build Succeded
            if (sb.LastBuildInfo == 0)
            {
                writer.WriteLine("Successfully Built Robot Code");
                string path = GetStartupAssemblyPath();
                robotExe = Path.GetFileName(path);
                string buildDir = Path.GetDirectoryName(path);


                writer.WriteLine("Parsing Robot Files");
                //While connecting, parse all of the output files.
                bool wpilib = false;
                bool nt = false;
                bool halbase = false;
                bool halrio = false;
                files = new List<string>();
                if (Directory.Exists(buildDir))
                {
                    foreach (string f in Directory.GetFiles(buildDir))
                    {
                        if (DeployProperties.IgnoreFiles.Any(f.Contains))
                            continue;
                        if (f.Contains(".dll"))
                        {
                            // Special cases for HAL-Base, WPILib and NetworkTables. Also
                            // ignoring any other HAL files.
                            if (f.Contains("WPILib.dll"))
                            {
                                OutputWriter.Instance.WriteLine("Found WPILib");
                                wpilib = true;
                                files.Add(f);
                                continue;
                            }
                            if (f.Contains("HAL-Base.dll"))
                            {
                                OutputWriter.Instance.WriteLine("Found HAL Base");
                                halbase = true;
                                files.Add(f);
                                continue;
                            }
                            if (f.Contains("NetworkTables.dll"))
                            {
                                OutputWriter.Instance.WriteLine("Found Network Tables");
                                nt = true;
                                files.Add(f);
                                continue;
                            }
                            if (f.Contains("HAL-RoboRIO.dll"))
                            {
                                OutputWriter.Instance.WriteLine("Found HAL RoboRIO");
                                halrio = true;
                                files.Add(f);
                                continue;
                            }
                            if (f.Contains("HAL"))
                            {
                                continue;
                            }

                        }
                        files.Add(f);
                    }
                }
                writer.WriteLine("Parsed All Files.");
                if (nt && wpilib && halbase && halrio)
                {
                    writer.WriteLine("Found all needed WPILib files.");
                    return true;
                }
                else
                {
                    writer.WriteLine("Did not find all needed files. Canceling Deploy");
                    return false;
                }
            }
            else
            {
                writer.WriteLine("Code build failed. Canceling Deploy");
                robotExe = null;
                files = null;
                return false;
            }
        }

        public void GetConnectionInfos(out ConnectionInfo lvuser, out ConnectionInfo admin)
        {
            lvuser = m_lvuserConnectionInfo;
            admin = m_adminConnectionInfo;
        }

        public bool DeployRobotFiles(List<string> files)
        {
            OutputWriter.Instance.WriteLine("Deploying robot files");
            return RoboRIOConnection.DeployFiles(files, DeployProperties.DeployDir, m_lvuserConnectionInfo);
        }

        public void CreateMonoDirectory()
        {
            OutputWriter.Instance.WriteLine("Creating Mono Deploy Directory");
            RoboRIOConnection.RunCommand($"mkdir -p {DeployProperties.DeployDir}", m_lvuserConnectionInfo);
        }

        public bool CheckMonoInstall()
        {
            OutputWriter.Instance.WriteLine("Checking for Mono install");
            var retVal = RoboRIOConnection.RunCommand($"test -e {DeployProperties.RoboRioMonoBin}", m_lvuserConnectionInfo);
            return retVal.ExitStatus == 0;
        }

        public bool CheckRoboRioImage()
        {
            WebClient wc = new WebClient();

            byte[] result = wc.UploadValues($"http://{m_connectionIp}/nisysapi/server", "POST", new NameValueCollection
            {
                {"Function", "GetPropertiesOfItem" },
                {"Plugins", "nisyscfg" },
                {"Items", "system" }
            });

            var sstring = Encoding.Unicode.GetString(result);

            var doc = new XmlDocument();
            doc.LoadXml(sstring);

            var vals = doc.GetElementsByTagName("Property");

            string str = null;

            foreach (XmlElement val in vals.Cast<XmlElement>().Where(val => val.InnerText.Contains("FRC_roboRIO")))
            {
                str = val.InnerText;
            }

            return DeployProperties.RoboRioAllowedImages.Any(rio => str != null && str.Contains(rio.ToString()));
        }

        internal string GetStartupAssemblyPath()
        {
            Project startupProject = GetStartupProject();
            return GetAssemblyPath(startupProject);
        }

        private Project GetStartupProject()
        {
            var sb = (SolutionBuild2)m_dte.Solution.SolutionBuild;
            string project = ((Array)sb.StartupProjects).Cast<string>().First();
            Project startupProject = m_dte.Solution.Item(project);
            return startupProject;
        }

        internal string GetAssemblyPath(Project vsProject)
        {
            string fullPath = vsProject.Properties.Item("FullPath").Value.ToString();
            string outputPath =
                vsProject.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
            string outputDir = Path.Combine(fullPath, outputPath);
            string outputFileName = vsProject.Properties.Item("OutputFileName").Value.ToString();
            string assemblyPath = Path.Combine(outputDir, outputFileName);
            return assemblyPath;
        }

        public void UploadCode(string robotName, SettingsPageGrid page, bool debug)
        {
            if (page.Netconsole)
            {
                StartNetConsole();
            }
            string deployedCmd;
            string deployedCmdFrame;
            if (debug)
            {
                deployedCmd = string.Format(DeployProperties.RobotCommandDebug, robotName);
                deployedCmdFrame = DeployProperties.RobotCommandDebugFileName;
            }
            else
            {
                deployedCmd = string.Format(DeployProperties.RobotCommand, robotName);
                deployedCmdFrame = DeployProperties.RobotCommandFileName;
            }

            //Must be run as admin, so is seperate
            RoboRIOConnection.RunCommand("killall netconsole-host", m_adminConnectionInfo);

            //Combining all other commands, since they should be safe running together.
            List<string> commands = new List<string>();
            commands.Add($"echo {deployedCmd} > {DeployProperties.CommandDir}/{deployedCmdFrame}");
            if (debug)
            {
                commands.AddRange(DeployProperties.DebugFlagCommand);
            }
            commands.AddRange(DeployProperties.DeployKillCommand);
            RoboRIOConnection.RunCommands(commands.ToArray(), m_lvuserConnectionInfo);
        }

        /// <summary>
        /// Starts NetConsole
        /// </summary>
        public static void StartNetConsole()
        {
            //If NetConsole is already running, don't do anything
            if (System.Diagnostics.Process.GetProcessesByName("NetConsole.exe").Length == 0)
            {
                //Else Start Netconsole
                //There are 2 locations it could be. Check both.
                OutputWriter.Instance.WriteLine("Starting netconsole");
                if (File.Exists(@"C:\Program Files (x86)\NetConsole for cRIO\NetConsole.exe"))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(@"C:\Program Files (x86)\NetConsole for cRIO\NetConsole.exe");
                        OutputWriter.Instance.WriteLine("netconsole started.");
                        return;
                    }
                    catch
                    {
                        OutputWriter.Instance.WriteLine("Could not start netconsole");
                        return;
                    }
                }
                if (File.Exists(@"C:\Program Files\NetConsole for cRIO\NetConsole.exe"))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(@"C:\Program Files\NetConsole for cRIO\NetConsole.exe");
                        OutputWriter.Instance.WriteLine("netconsole started.");
                        return;
                    }
                    catch
                    {
                        OutputWriter.Instance.WriteLine("Could not start netconsole");
                        return;
                    }
                }
                OutputWriter.Instance.WriteLine("Could not start netconsole");
            }
        }
    }
}
