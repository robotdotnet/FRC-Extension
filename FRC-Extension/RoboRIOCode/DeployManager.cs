using System;
using System.Collections.Generic;
using System.Collections.Specialized;
//using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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

        ConnectionReturn connectionValues = null;

        public DeployManager(DTE dte)
        {
            m_dte = dte;
        }

        
        //Uploads code to the robot and then runs it.
        public async Task DeployCode(string teamNumber, SettingsPageGrid page, bool debug)
        {

            var writer = OutputWriter.Instance;

            //Connect to Robot Async
            OutputWriter.Instance.WriteLine("Attempting to Connect to RoboRIO");
            Task<bool> rioConnectionTask = StartConnectionTask(teamNumber);
            Task delayTask = Task.Delay(10000);

            CodeReturnStruct codeReturn = await BuildAndPrepareCode(debug);

            if (codeReturn == null) return;

            writer.WriteLine("Waiting for Connection to Finish");
            if (await Task.WhenAny(rioConnectionTask, delayTask) == rioConnectionTask)
            {
                //Completed on time
                if (rioConnectionTask.Result == true)
                {
                    //Connected successfully
                    OutputWriter.Instance.WriteLine("Successfully Connected to RoboRIO.");

                    if (!(await CheckMonoInstall()))
                    {
                        //TODO: Make this error message better
                        OutputWriter.Instance.WriteLine("Mono not properly installed. ");
                        return;
                    }
                    OutputWriter.Instance.WriteLine("Mono correctly installed");
                    OutputWriter.Instance.WriteLine("Checking RoboRIO Image");
                    if (!(await CheckRoboRioImage()))
                    {
                        OutputWriter.Instance.WriteLine("roboRIO Image does not match plugin, allowed image versions: " + string.Join(", ", DeployProperties.RoboRioAllowedImages.ToArray()));
                        return;
                    }
                    OutputWriter.Instance.WriteLine("RoboRIO Image Correct");
                    //Force making mono directory
                    await CreateMonoDirectory();

                    //DeployAllFiles
                    bool retVal = await DeployRobotFiles(codeReturn.RobotFiles);
                    if (!retVal)
                    {
                        OutputWriter.Instance.WriteLine("File deploy failed.");
                        return;
                    }
                    OutputWriter.Instance.WriteLine("Successfully Deployed Files. Starting Code.");
                    await UploadCode(codeReturn.RobotExe, page, debug);
                    OutputWriter.Instance.WriteLine("Successfully started robot code.");
                }
                else
                {
                    //Failed to connect
                    writer.WriteLine("Failed to Connect to RoboRIO. Exiting.");
                }
            }
            else
            {
                //Timedout
                writer.WriteLine("Failed to Connect to RoboRIO. Exiting.");
            }
        }

        public async Task<bool> StartConnectionTask(string teamNumber)
        {
                ConnectionReturn connected = await RoboRIOConnection.CheckConnection(teamNumber);
                if (connected != null)
                {
                    StringBuilder builder = new StringBuilder();
                    builder.AppendLine("Connected to RoboRIO...");
                    builder.AppendLine("Interface: " + connected.ConnectionType);
                    builder.Append("IP Address: " + connected.ConnectionIp);
                    OutputWriter.Instance.WriteLine(builder.ToString());
                    connectionValues = connected;
                    return true;
                }
                else
                {
                    OutputWriter.Instance.WriteLine("Failed to Connect to RoboRIO...");
                    return false;
                }
        }

        public class CodeReturnStruct
        {
            public string RobotExe { get; private set; }
            public List<string> RobotFiles { get; private set; }

            public CodeReturnStruct(string exe, List<string> files)
            {
                RobotExe = exe;
                RobotFiles = files;
            } 
        }


        public async Task<CodeReturnStruct> BuildAndPrepareCode(bool debug)
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
            await Task.Run(() => sb.Build(true));

            //If Build Succeded
            if (sb.LastBuildInfo == 0)
            {
                writer.WriteLine("Successfully Built Robot Code");
                string path = GetStartupAssemblyPath();
                string robotExe = Path.GetFileName(path);
                string buildDir = Path.GetDirectoryName(path);


                writer.WriteLine("Parsing Robot Files");
                //While connecting, parse all of the output files.
                List<string> files = new List<string>();
                if (Directory.Exists(buildDir))
                {
                    await Task.Run(() =>
                    {
                        files.AddRange(Directory.GetFiles(buildDir).Where(f => !DeployProperties.IgnoreFiles.Any(f.Contains)));
                    });
                }
                writer.WriteLine("Parsed All Files.");

                bool foundAll = true;

                foreach (var requiredFile in DeployProperties.RequiredFiles)
                {
                    bool found = files.Any(file => file.Contains(requiredFile));
                    if (!found)
                    {
                        //Did not find a required file.
                        foundAll = false;
                        writer.WriteLine($"Cound not find requred file: {requiredFile}");
                    }
                }

                if (foundAll)
                {
                    writer.WriteLine("Found all needed WPILib files.");
                    return new CodeReturnStruct(robotExe, files);
                }
                else
                {
                    writer.WriteLine("Did not find all needed files. Canceling Deploy");
                    return null;
                }
            }
            else
            {
                writer.WriteLine("Code build failed. Canceling Deploy");
                return null;
            }
        }

        public async Task<bool> DeployRobotFiles(List<string> files)
        {
            OutputWriter.Instance.WriteLine("Deploying robot files");
            return await RoboRIOConnection.DeployFiles(files, DeployProperties.DeployDir, ConnectionUser.LvUser);
        }

        public async Task CreateMonoDirectory()
        {
            OutputWriter.Instance.WriteLine("Creating Mono Deploy Directory");
            await RoboRIOConnection.RunCommand($"mkdir -p {DeployProperties.DeployDir}", ConnectionUser.LvUser);
        }

        public async Task<bool> CheckMonoInstall()
        {
            OutputWriter.Instance.WriteLine("Checking for Mono install");
            var retVal =  await RoboRIOConnection.RunCommand($"test -e {DeployProperties.RoboRioMonoBin}", ConnectionUser.LvUser);
            return retVal.ExitStatus == 0;
        }

        public async Task<bool> CheckRoboRioImage()
        {
            using (WebClient wc = new WebClient())
            {

                byte[] result = await wc.UploadValuesTaskAsync($"http://{connectionValues.ConnectionIp}/nisysapi/server", "POST",
                    new NameValueCollection
                    {
                        {"Function", "GetPropertiesOfItem"},
                        {"Plugins", "nisyscfg"},
                        {"Items", "system"}
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

        public async Task UploadCode(string robotName, SettingsPageGrid page, bool debug)
        {
            if (page.Netconsole)
            {
                await StartNetConsole();
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
            await RoboRIOConnection.RunCommand("killall -q netconsole-host || :", ConnectionUser.Admin);

            //Combining all other commands, since they should be safe running together.
            List<string> commands = new List<string>();
            commands.Add($"echo {deployedCmd} > {DeployProperties.CommandDir}/{deployedCmdFrame}");
            if (debug)
            {
                commands.AddRange(DeployProperties.DebugFlagCommand);
            }
            commands.AddRange(DeployProperties.DeployKillCommand);
            await RoboRIOConnection.RunCommands(commands.ToArray(), ConnectionUser.LvUser);
        }

        /// <summary>
        /// Starts NetConsole
        /// </summary>
        public static async Task StartNetConsole()
        {
            await Task.Run(() =>
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
                            System.Diagnostics.Process.Start(
                                @"C:\Program Files (x86)\NetConsole for cRIO\NetConsole.exe");
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
            });
        }
    }
}
