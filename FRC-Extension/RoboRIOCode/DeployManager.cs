using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using EnvDTE;
using EnvDTE80;
using RobotDotNet.FRC_Extension.MonoCode;
using RobotDotNet.FRC_Extension.SettingsPages;

namespace RobotDotNet.FRC_Extension.RoboRIOCode
{
    public class DeployManager
    {
        //We need our DTE so we can grab our solution
        private readonly DTE m_dte;

        ConnectionReturn m_connectionValues;

        public DeployManager(DTE dte)
        {
            m_dte = dte;
        }

        
        //Uploads code to the robot and then runs it.
        public async Task<bool> DeployCode(string teamNumber, bool debug, Project robotProject)
        {

            var writer = OutputWriter.Instance;

            if (robotProject == null)
            {
                writer.WriteLine("Robot Project not valid. Contact RobotDotNet for support.");
                return false;
            }

            //Connect to Robot Async
            OutputWriter.Instance.WriteLine("Attempting to Connect to RoboRIO");
            Task<bool> rioConnectionTask = StartConnectionTask(teamNumber);
            Task delayTask = Task.Delay(10000);

            CodeReturnStruct codeReturn = await BuildAndPrepareCode(debug, robotProject);

            if (codeReturn == null) return false;

            writer.WriteLine("Waiting for Connection to Finish");
            if (await Task.WhenAny(rioConnectionTask, delayTask) == rioConnectionTask)
            {
                //Completed on time
                if (rioConnectionTask.Result)
                {
                    //Connected successfully
                    OutputWriter.Instance.WriteLine("Successfully Connected to RoboRIO.");

                    if (!await CheckMonoInstall())
                    {
                        //TODO: Make this error message better
                        OutputWriter.Instance.WriteLine("Mono not properly installed. Please try reinstalling to Mono Runtime.");
                        return false;
                    }
                    OutputWriter.Instance.WriteLine("Mono correctly installed");

                    OutputWriter.Instance.WriteLine("Checking RoboRIO Image");
                    if (!await CheckRoboRioImage())
                    {
                        // Ignore image requirement on selected option
                        if (!SettingsProvider.ExtensionSettingsPage.IgnoreImageRequirements)
                        {
                            OutputWriter.Instance.WriteLine(
                                "RoboRIO Image does not match plugin, allowed image versions: " +
                                string.Join(", ", DeployProperties.RoboRioAllowedImages.ToArray()));
                            OutputWriter.Instance.WriteLine(
                                "Please follow FIRST's instructions on imaging your RoboRIO, and try again.");
                            return false;
                        }
                    }
                    OutputWriter.Instance.WriteLine("RoboRIO Image Correct");
                    //Force making mono directory
                    await CreateMonoDirectory();

                    bool nativeDeploy = await CheckAndDeployNativeLibraries(robotProject);

                    if (!nativeDeploy)
                    {
                        OutputWriter.Instance.WriteLine("Failed to deploy native files.");
                        return false;
                    }

                    //DeployAllFiles
                    bool retVal = await DeployRobotFiles(codeReturn.RobotFiles);
                    if (!retVal)
                    {
                        OutputWriter.Instance.WriteLine("File deploy failed.");
                        return false;
                    }
                    OutputWriter.Instance.WriteLine("Successfully Deployed Files. Starting Code.");
                    await UploadCode(codeReturn.RobotExe, debug, robotProject);
                    OutputWriter.Instance.WriteLine("Successfully started robot code.");
                    return true;
                }
                else
                {
                    //Failed to connect
                    writer.WriteLine("Failed to Connect to RoboRIO. Exiting.");
                    return false;
                }
            }
            else
            {
                //Timedout
                writer.WriteLine("Failed to Connect to RoboRIO. Exiting.");
                return false;
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
                    m_connectionValues = connected;
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
            public string RobotExe { get; }
            public List<string> RobotFiles { get; }

            public CodeReturnStruct(string exe, List<string> files)
            {
                RobotExe = exe;
                RobotFiles = files;
            } 
        }


        public async Task<CodeReturnStruct> BuildAndPrepareCode(bool debug, Project robotProject)
        {

            var writer = OutputWriter.Instance;
            //Build Code
            writer.WriteLine("Building Robot Code");
            var sb = (SolutionBuild2)m_dte.Solution.SolutionBuild;
            //Check if we are building for debug or release
            string configuration;
            if (debug)
            {
                configuration = "Debug";
                //Switch build to debug mode
                sb.SolutionConfigurations.Item(configuration).Activate();
            }
            else
            {
                configuration = "Release";
                //Switch to release
                sb.SolutionConfigurations.Item(configuration).Activate();
            }
            await Task.Run(() => sb.BuildProject(configuration, robotProject.FullName, true));

            

            //If Build Succeded
            if (sb.LastBuildInfo == 0)
            {
                writer.WriteLine("Successfully Built Robot Code");
                //string path = GetStartupAssemblyPath();
                string path = GetAssemblyPath(robotProject);
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

                if (SettingsProvider.ExtensionSettingsPage.IgnoreFileRequirements)
                {
                    // Ignore requirements for all files to be found
                    foundAll = true;
                }

                if (foundAll)
                {
                    writer.WriteLine("Found all needed WPILib files.");
                    return new CodeReturnStruct(robotExe, files);
                }
                else
                {
                    writer.WriteLine("Did not find all needed files. Canceling Deploy");
                    writer.WriteLine("Please make sure the WPILib is the newest version from NuGet.");
                    return null;
                }
            }
            else
            {
                writer.WriteLine("Code build failed. Canceling Deploy");
                return null;
            }
        }

        public string GetCommandLineArguments(Project robotProject)
        {
            if (!SettingsProvider.TeamSettingsPage.ConsoleArgs)
            {
                return "";
            }

            Configuration configuration = robotProject.ConfigurationManager.ActiveConfiguration;

            return (string) configuration.Properties.Item("StartArguments").Value;
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

                byte[] result = await wc.UploadValuesTaskAsync($"http://{m_connectionValues.ConnectionIp}/nisysapi/server", "POST",
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

        internal string GetProjectPath(Project vsProject)
        {
            return vsProject.Properties.Item("FullPath").Value.ToString();
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

        public async Task<bool> CheckAndDeployNativeLibraries(Project robotProject)
        {
            MemoryStream stream = new MemoryStream();
            bool readFile =
                    await
                        RoboRIOConnection.ReceiveFile("/usr/local/frc/lib/NetWPILib.properties", stream,
                            ConnectionUser.LvUser);

            string nativeLoc = GetProjectPath(robotProject) + "wpinative" + Path.DirectorySeparatorChar;

            var fileMd5List = await GetMD5ForFiles(nativeLoc);
            if (!readFile)
            {
                // Libraries definitely do not exist, deploy
                return await DeployNativeLibraries(fileMd5List);
            }

            stream.Position = 0;

            bool foundError = false;
            int readCount = 0;
            StreamReader reader = new StreamReader(stream);
            string line = null;
            while (!string.IsNullOrWhiteSpace(line = reader.ReadLine()))
            {
                // Split line at =
                string[] split = line.Split('=');
                if (split.Length < 2) continue;
                readCount++;
                foreach (Tuple<string, string> tuple in fileMd5List)
                {
                    if (split[0] == tuple.Item1)
                    {
                        // Found a match file name
                        if (split[1] != tuple.Item2)
                        {
                            foundError = true;
                        }
                        break;
                    }
                }
                if (foundError) break;
            }

            reader.Dispose();

            if (foundError || readCount != fileMd5List.Count)
            {
                return await DeployNativeLibraries(fileMd5List);
            }
            
            OutputWriter.Instance.WriteLine("Native libraries exist. Skipping deploy");
            return true;
        }

        public async Task<List<Tuple<string, string>>> GetMD5ForFiles(string fileLocation)
        {
            if (Directory.Exists(fileLocation))
            {
                string[] files = Directory.GetFiles(fileLocation);
                List<Tuple<string,string>> retList = new List<Tuple<string, string>>(files.Length);
                await Task.Run(() =>
                {
                    foreach (var file in files)
                    {
                        string md5 = MD5Helper.Md5Sum(file);
                        retList.Add(new Tuple<string, string>(file, md5));
                    }
                });
                return retList;
            }
            else
            {
                return null;
            }
        }

        public async Task<bool> DeployNativeLibraries(List<Tuple<string, string>> files)
        {
            List<string> fileList = new List<string>(files.Count);

            MemoryStream memStream = new MemoryStream();
            StreamWriter writer = new StreamWriter(memStream);

            foreach (Tuple<string, string> tuple in files)
            {
                fileList.Add(tuple.Item1);
                await writer.WriteLineAsync($"{tuple.Item1}={tuple.Item2}");
            }

            writer.Flush();

            memStream.Position = 0;

            OutputWriter.Instance.WriteLine("Deploying native files");
            bool nativeDeploy = await RoboRIOConnection.DeployFiles(fileList, "/usr/local/frc/lib", ConnectionUser.Admin);
            bool md5Deploy = await RoboRIOConnection.DeployFile(memStream, "/usr/local/frc/lib/NetWPILib.properties", ConnectionUser.Admin);

            writer.Dispose();
            memStream.Dispose();

            return nativeDeploy && md5Deploy;
        }

        public async Task UploadCode(string robotName, bool debug, Project robotProject)
        {
            //TODO: Make debug work. Forcing debug to false for now so code always runs properly.
            debug = false;

            if (SettingsProvider.TeamSettingsPage.Netconsole)
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

            string args = GetCommandLineArguments(robotProject);

            //Kill the currently running robot program
            await RoboRIOConnection.RunCommand(DeployProperties.KillOnlyCommand, ConnectionUser.LvUser);

            //Combining all other commands, since they should be safe running together.
            List<string> commands = new List<string>();

            //Write the robotCommand file
            commands.Add($"echo {deployedCmd} {args} > {DeployProperties.CommandDir}/{deployedCmdFrame}");
            if (debug)
            {
                //If debug write the debug flag.
                commands.AddRange(DeployProperties.DebugFlagCommand);
            }
            //Add all commands to restart
            commands.AddRange(DeployProperties.DeployKillCommand);
            //run all commands
            await RoboRIOConnection.RunCommands(commands.ToArray(), ConnectionUser.LvUser);

            //Run sync so files are written to disk.
            await RoboRIOConnection.RunCommand("sync", ConnectionUser.LvUser);
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
