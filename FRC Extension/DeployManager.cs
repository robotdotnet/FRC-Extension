using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace RobotDotNet.FRC_Extension
{
    class DeployManager
    {
        //We need our DTE so we can grab our solution
        private readonly DTE m_dte;

        public DeployManager(DTE dte)
        {
            m_dte = dte;
        }

        public void DeployCode(SettingsPageGrid page, Action settingsPopup)
        {
            //Grab the team number, and check if it is set to 0;
            string teamNumber = page.TeamNumber.ToString();
            if (teamNumber == "0")
            {
                //If its 0, we pop up a window asking teams to set it.
                settingsPopup();
                return;

            }

            var writer = OutputWriter.Instance;

            //Connect to Robot Async
            OutputWriter.Instance.WriteLine("Attempting to Connect to RoboRIO");
            GlobalConnections.connectionManager.ConnectionComplete += ConnectCompleted;
            GlobalConnections.connectionManager.ConnectAsync(teamNumber);

            //Build Code
            writer.WriteLine("Building Robot Code");
            var sb = (SolutionBuild2)m_dte.Solution.SolutionBuild;
            sb.Build(true);

            //If Build Succeded
            if (sb.LastBuildInfo == 0)
            {
                writer.WriteLine("Successfully Built Robot Code");
                string path = GetStartupAssemblyPath();
                string robotExe = Path.GetFileName(path);
                string buildDir = Path.GetDirectoryName(path);


                writer.WriteLine("Parsing Robot Files");
                //While connecting, parse all of the output files.
                bool wpilib = false;
                bool nt = false;
                bool halbase = false; 
                bool halrio = false;
                bool libHAL = false;
                List<string> files = new List<string>();
                if (Directory.Exists(buildDir))
                {
                    foreach (string f in Directory.GetFiles(buildDir))
                    {
                        if (f.Contains("pdb") || f.Contains("vshost") || f.Contains(".config") || f.Contains(".manifest") || f.Contains("deploy.bat"))
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
                        if (f.Contains(".so"))
                        {
                            if (f.Contains("libHALAthena_shared.so"))
                            {
                                OutputWriter.Instance.WriteLine("Found libHALAthena_shared.so");
                                libHAL = true;
                                files.Add(f);
                                continue;

                            }
                        }
                        writer.WriteLine(Path.GetFileName("Found " + f));
                        files.Add(f);
                    }
                }
                writer.WriteLine("Parsed All Files.");
                if (nt && wpilib && halbase && halrio && libHAL)
                {
                    writer.WriteLine("Found all needed WPILib files.");
                }
                else
                {
                    writer.WriteLine("Did not find all needed files. Will return after connection to RoboRIO is finished.");
                }

                writer.WriteLine("Waiting for Connection to Finish");
                while (!connected)
                {
                    System.Threading.Thread.Sleep(100);
                }
                GlobalConnections.connectionManager.ConnectionComplete -= ConnectCompleted;
                OutputWriter.Instance.WriteLine(GlobalConnections.connectionManager.GetConnectionStatus());

                if (nt && wpilib && halbase && halrio && libHAL && GlobalConnections.connectionManager.Connected)
                {
                    OutputWriter.Instance.WriteLine("Successfully Connected to RoboRIO. Starting File deploy.");
                    //Force making mono directory
                    GlobalConnections.commandManager.RunCommands(new [] {"mkdir -p /home/lvuser/mono"});
                    bool retVal = GlobalConnections.fileDeployManager.DeployFiles(files.ToArray(), "/home/lvuser/mono");
                    if (!retVal)
                    {
                        OutputWriter.Instance.WriteLine("File deploy failed.");
                        return;
                    }
                    OutputWriter.Instance.WriteLine("Successfully Deployed Files. Starting Code.");
                    UploadCode(robotExe, page);
                    OutputWriter.Instance.WriteLine("Successfully started code.");
                }
                else if (! GlobalConnections.connectionManager.Connected)
                {
                    writer.WriteLine("Failed to Connect to RoboRIO. Exiting.");
                }
            }
            else
            {
                OutputWriter.Instance.WriteLine("Robot Code Build Failed.");
            }
        }

        private bool connected = false;

        private void ConnectCompleted()
        {
            connected = true;
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

        static string deployDir = "/home/lvuser";
        static string monoDeployDir = deployDir + "/mono";

        public void UploadCode(string robotName, SettingsPageGrid page)
        {
            if (page.Netconsole)
            {
                StartNetConsole();
            }
            string deployedCmd;
            string deployedCmdFrame;
            string extraCmd;
            if (false)
            {
                deployedCmd = "env LD_PRELOAD=/lib/libstdc++.so.6.0.20 /usr/local/frc/bin/netconsole-host mono --debug " + monoDeployDir + "/" + robotName;
                deployedCmdFrame = "robotDebugCommand";
                extraCmd = "touch /tmp/frcdebug; chown lvuser:ni /tmp/frcdebug";
            }
            else
            {
                deployedCmd = "env LD_PRELOAD=/lib/libstdc++.so.6.0.20 /usr/local/frc/bin/netconsole-host mono " + monoDeployDir + "/" + robotName;
                deployedCmdFrame = "robotCommand";
                extraCmd = "";
            }

            List<string> commands = new List<string>();
            commands.Add("echo " + deployedCmd + " > " + deployDir + "/" + deployedCmdFrame);

            OutputWriter.Instance.WriteLine("Starting Robot Code.");
            GlobalConnections.commandManager.RunCommands(commands.ToArray());

            commands.Clear();

            commands.Add(
                "/bin/bash -ce '[ ! -f /var/local/natinst/log/FRC_UserProgram.log ] || rm -f /var/local/natinst/log/FRC_UserProgram.log;. /etc/profile.d/natinst-path.sh; chown -R lvuser:ni /home/lvuser/mono; /usr/local/frc/bin/frcKillRobot.sh -t -r'");
            GlobalConnections.commandManager.RunCommands(commands.ToArray());
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
