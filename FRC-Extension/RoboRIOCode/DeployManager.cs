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
using RobotDotNet.FRC_Extension.SettingsPages;

namespace RobotDotNet.FRC_Extension.RoboRIOCode
{
    public class DeployManager : IDisposable
    {
        //We need our DTE so we can grab our solution
        private readonly DTE m_dte;

        private RoboRioConnection m_roboRioConnection;

        public DeployManager(DTE dte)
        {
            m_dte = dte;
        }

        public void Dispose()
        {
            m_roboRioConnection?.Dispose();
        }

        //Uploads code to the robot and then runs it.
        public async Task<bool> DeployCodeAsync(string teamNumber, bool debug, Project robotProject)
        {

            var writer = OutputWriter.Instance;

            if (robotProject == null)
            {
                await writer.WriteLineAsync("Robot Project not valid. Contact RobotDotNet for support.").ConfigureAwait(false);
                return false;
            }

            //Connect to Robot Async
            await OutputWriter.Instance.WriteLineAsync("Attempting to Connect to RoboRIO").ConfigureAwait(false);

            Task<RoboRioConnection> rioConnectionTask = RoboRioConnection.StartConnectionTaskAsync(teamNumber);

            CodeReturnStruct codeReturn = await BuildAndPrepareCodeAsync(debug, robotProject).ConfigureAwait(false);

            if (codeReturn == null) return false;

            await writer.WriteLineAsync("Waiting for Connection to Finish").ConfigureAwait(false);
            // Kill our connection if we have already ran once with this same object.
            m_roboRioConnection?.Dispose();
            m_roboRioConnection = await rioConnectionTask.ConfigureAwait(false);
            if (m_roboRioConnection.Connected)
            {
                //Connected successfully
                await OutputWriter.Instance.WriteLineAsync("Successfully Connected to RoboRIO.").ConfigureAwait(false);

                if (!await CheckMonoInstallAsync().ConfigureAwait(false))
                {
                    //TODO: Make this error message better
                    await OutputWriter.Instance.WriteLineAsync("Mono not properly installed. Please try reinstalling to Mono Runtime.").ConfigureAwait(false);
                    return false;
                }
                await OutputWriter.Instance.WriteLineAsync("Mono correctly installed").ConfigureAwait(false);

                await OutputWriter.Instance.WriteLineAsync("Checking RoboRIO Image").ConfigureAwait(false);
                if (!await CheckRoboRioImageAsync().ConfigureAwait(false))
                {
                    // Ignore image requirement on selected option
                    if (!SettingsProvider.ExtensionSettingsPage.IgnoreImageRequirements)
                    {
                        await OutputWriter.Instance.WriteLineAsync(
                            "RoboRIO Image does not match plugin, allowed image versions: " +
                            string.Join(", ", DeployProperties.RoboRioAllowedImages.ToArray())).ConfigureAwait(false);
                        await OutputWriter.Instance.WriteLineAsync(
                            "Please follow FIRST's instructions on imaging your RoboRIO, and try again.").ConfigureAwait(false);
                        return false;
                    }
                }
                await OutputWriter.Instance.WriteLineAsync("RoboRIO Image Correct").ConfigureAwait(false);
                //Force making mono directory
                await CreateMonoDirectoryAsync().ConfigureAwait(false);

                bool nativeDeploy =
                    await
                        CachedFileHelper.CheckAndDeployNativeLibrariesAsync(DeployProperties.UserLibraryDir, "WPI_Native_Libraries",
                            await GetProjectPathAsync(robotProject).ConfigureAwait(false) + "wpinative" + Path.DirectorySeparatorChar,
                            new List<string>(), m_roboRioConnection).ConfigureAwait(false);

                if (!nativeDeploy)
                {
                    await OutputWriter.Instance.WriteLineAsync("Failed to deploy native files.").ConfigureAwait(false);
                    return false;
                }

                //DeployAllFiles
                bool retVal = await DeployRobotFilesAsync(codeReturn.RobotFiles).ConfigureAwait(false);
                if (!retVal)
                {
                    await OutputWriter.Instance.WriteLineAsync("File deploy failed.").ConfigureAwait(false);
                    return false;
                }
                await OutputWriter.Instance.WriteLineAsync("Successfully Deployed Files. Starting Code.").ConfigureAwait(false);
                await StartRobotCodeAsync(codeReturn.RobotExe, debug, robotProject).ConfigureAwait(false);
                await OutputWriter.Instance.WriteLineAsync("Successfully started robot code.").ConfigureAwait(false);
                return true;
            }
            else
            {
                //Failed to connect
                await writer.WriteLineAsync("Failed to Connect to RoboRIO. Exiting.").ConfigureAwait(false);
                return false;
            }
        }

        

        private class CodeReturnStruct
        {
            public string RobotExe { get; }
            public List<string> RobotFiles { get; }

            public CodeReturnStruct(string exe, List<string> files)
            {
                RobotExe = exe;
                RobotFiles = files;
            }
        }

        private async Task<CodeReturnStruct> BuildAndPrepareCodeAsync(bool debug, Project robotProject)
        {
            var writer = OutputWriter.Instance;
            //Build Code
            await writer.WriteLineAsync("Building Robot Code").ConfigureAwait(false);
            await ThreadHelperExtensions.SwitchToUiThread();
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
            sb.BuildProject(configuration, robotProject.FullName, true);

            //If Build Succeded
            if (sb.LastBuildInfo == 0)
            {
                await writer.WriteLineAsync("Successfully Built Robot Code").ConfigureAwait(false);
                //string path = GetStartupAssemblyPath();
                string path = await GetAssemblyPathAsync(robotProject).ConfigureAwait(false);
                string robotExe = Path.GetFileName(path);
                string buildDir = Path.GetDirectoryName(path);

                await writer.WriteLineAsync("Parsing Robot Files").ConfigureAwait(false);
                //While connecting, parse all of the output files.
                List<string> files = new List<string>();
                if (Directory.Exists(buildDir))
                {
                    await Task.Run(() =>
                    {
                        files.AddRange(Directory.GetFiles(buildDir).Where(f => !DeployProperties.IgnoreFiles.Any(f.Contains)));
                    }).ConfigureAwait(false);
                }
                await writer.WriteLineAsync("Parsed All Files.").ConfigureAwait(false);

                bool foundAll = true;

                foreach (var requiredFile in DeployProperties.RequiredFiles)
                {
                    bool found = files.Any(file => file.Contains(requiredFile));
                    if (!found)
                    {
                        //Did not find a required file.
                        foundAll = false;
                        await writer.WriteLineAsync($"Cound not find requred file: {requiredFile}").ConfigureAwait(false);
                    }
                }

                if (SettingsProvider.ExtensionSettingsPage.IgnoreFileRequirements)
                {
                    // Ignore requirements for all files to be found
                    foundAll = true;
                }

                if (foundAll)
                {
                    await writer.WriteLineAsync("Found all needed WPILib files.").ConfigureAwait(false);
                    return new CodeReturnStruct(robotExe, files);
                }
                else
                {
                    await writer.WriteLineAsync("Did not find all needed files. Canceling Deploy").ConfigureAwait(false);
                    await writer.WriteLineAsync("Please make sure the WPILib is the newest version from NuGet.").ConfigureAwait(false);
                    return null;
                }
            }
            else
            {
                await writer.WriteLineAsync("Code build failed. Canceling Deploy").ConfigureAwait(false);
                return null;
            }
        }

        public async Task<string> GetCommandLineArgumentsAsync(Project robotProject)
        {
            if (!SettingsProvider.TeamSettingsPage.ConsoleArgs)
            {
                return "";
            }

            await ThreadHelperExtensions.SwitchToUiThread();
            Configuration configuration = robotProject.ConfigurationManager.ActiveConfiguration;

            return (string)configuration.Properties.Item("StartArguments").Value;
        }

        public async Task<bool> DeployRobotFilesAsync(List<string> files)
        {
            await OutputWriter.Instance.WriteLineAsync("Deploying robot files").ConfigureAwait(false);
            return await m_roboRioConnection.DeployFiles(files, DeployProperties.DeployDir, ConnectionUser.LvUser).ConfigureAwait(false);
        }

        public async Task CreateMonoDirectoryAsync()
        {
            await OutputWriter.Instance.WriteLineAsync("Creating Mono Deploy Directory").ConfigureAwait(false);
            await m_roboRioConnection.RunCommandAsync($"mkdir -p {DeployProperties.DeployDir}", ConnectionUser.LvUser).ConfigureAwait(false);
        }

        public async Task<bool> CheckMonoInstallAsync()
        {
            await OutputWriter.Instance.WriteLineAsync("Checking for Mono install").ConfigureAwait(false);
            var retVal = await m_roboRioConnection.RunCommandAsync($"test -e {DeployProperties.RoboRioMonoBin}", ConnectionUser.LvUser).ConfigureAwait(false);
            return retVal.ExitStatus == 0;
        }

        public async Task<bool> CheckRoboRioImageAsync()
        {
            using (WebClient wc = new WebClient())
            {

                byte[] result = await wc.UploadValuesTaskAsync($"http://{m_roboRioConnection.IPAddress.ToString()}/nisysapi/server", "POST",
                    new NameValueCollection
                    {
                        {"Function", "GetPropertiesOfItem"},
                        {"Plugins", "nisyscfg"},
                        {"Items", "system"}
                    }).ConfigureAwait(false);

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

        internal async Task<string> GetProjectPathAsync(Project vsProject)
        {
            await ThreadHelperExtensions.SwitchToUiThread();
            return vsProject.Properties.Item("FullPath").Value.ToString();
        }

        internal async Task<string> GetAssemblyPathAsync(Project vsProject)
        {
            await ThreadHelperExtensions.SwitchToUiThread();
            string fullPath = vsProject.Properties.Item("FullPath").Value.ToString();
            string outputPath =
                vsProject.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
            string outputDir = Path.Combine(fullPath, outputPath);
            string outputFileName = vsProject.Properties.Item("OutputFileName").Value.ToString();
            string assemblyPath = Path.Combine(outputDir, outputFileName);
            return assemblyPath;
        }

        public async Task StartRobotCodeAsync(string robotName, bool debug, Project robotProject)
        {
            //TODO: Make debug work. Forcing debug to false for now so code always runs properly.
            debug = false;

            if (SettingsProvider.TeamSettingsPage.Netconsole)
            {
                await StartNetConsoleAsync().ConfigureAwait(false);
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

            string args = await GetCommandLineArgumentsAsync(robotProject).ConfigureAwait(false);

            //Kill the currently running robot program
            await m_roboRioConnection.RunCommandAsync(DeployProperties.KillOnlyCommand, ConnectionUser.LvUser).ConfigureAwait(false);

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
            await m_roboRioConnection.RunCommandsAsync(commands.ToArray(), ConnectionUser.LvUser).ConfigureAwait(false);

            //Run sync so files are written to disk.
            await m_roboRioConnection.RunCommandAsync("sync", ConnectionUser.LvUser).ConfigureAwait(false);
        }

        /// <summary>
        /// Starts NetConsole
        /// </summary>
        public static async Task StartNetConsoleAsync()
        {
            //If NetConsole is already running, don't do anything
            if (System.Diagnostics.Process.GetProcessesByName("NetConsole.exe").Length == 0)
            {
                //Else Start Netconsole
                //There are 2 locations it could be. Check both.
                await OutputWriter.Instance.WriteLineAsync("Starting netconsole").ConfigureAwait(false);
                if (File.Exists(@"C:\Program Files (x86)\NetConsole for cRIO\NetConsole.exe"))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(
                            @"C:\Program Files (x86)\NetConsole for cRIO\NetConsole.exe");
                        await OutputWriter.Instance.WriteLineAsync("netconsole started.").ConfigureAwait(false);
                        return;
                    }
                    catch
                    {
                        await OutputWriter.Instance.WriteLineAsync("Could not start netconsole").ConfigureAwait(false);
                        return;
                    }
                }
                if (File.Exists(@"C:\Program Files\NetConsole for cRIO\NetConsole.exe"))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(@"C:\Program Files\NetConsole for cRIO\NetConsole.exe");
                        await OutputWriter.Instance.WriteLineAsync("netconsole started.").ConfigureAwait(false);
                        return;
                    }
                    catch
                    {
                        await OutputWriter.Instance.WriteLineAsync("Could not start netconsole").ConfigureAwait(false);
                        return;
                    }
                }
                await OutputWriter.Instance.WriteLineAsync("Could not start netconsole").ConfigureAwait(false);
            }
        }
    }
}
