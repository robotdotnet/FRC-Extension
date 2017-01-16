using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Renci.SshNet;
using Renci.SshNet.Common;
using RobotDotNet.FRC_Extension.SettingsPages;
using Task = System.Threading.Tasks.Task;

namespace RobotDotNet.FRC_Extension.RoboRIOCode
{
    public enum ConnectionType
    {
        USB,
        MDNS,
        LAN,
        IP,
        None
    }

    public enum ConnectionUser
    {
        Admin,
        LvUser
    }

    public class ConnectionStatus
    {
        public ConnectionType ConnectionType { get; private set; }
        public IPAddress ConnectionIp { get; private set; }

        public ConnectionStatus(ConnectionType type, IPAddress ip)
        {
            ConnectionType = type;
            ConnectionIp = ip;
        }
    }



    public class RoboRioConnection : IDisposable
    {
        public const string RoboRioMdnsFormatString = "roborio-{0}-FRC.local";
        public const string RoboRioLanFormatString = "roborio-{0}-FRC.lan";
        public const string RoboRioUSBIp = "172.22.11.2";
        public const string RoboRioIpFormatString = "10.{0}.{1}.2";

        private int m_teamNumber;
        private IPAddress m_remoteIp;
        private TimeSpan m_sshTimeout;
        private ConnectionType m_connectionType = ConnectionType.None;

        private ConnectionInfo m_adminConnectionInfo;
        private ConnectionInfo m_lvUserConnectionInfo;

        private SshClient m_sshAdminClient;
        private SshClient m_sshUserClient;
        private ScpClient m_scpUserClient;
        private ScpClient m_scpAdminClient;

        public IPAddress IPAddress => m_remoteIp;

        public ConnectionStatus ConnectionStatus
        {
            get
            {
                if (m_remoteIp == null)
                {
                    return null;
                }
                return new ConnectionStatus(m_connectionType, m_remoteIp);
            }
        }

        public bool Connected => m_remoteIp != null;


        public RoboRioConnection(int teamNumber, TimeSpan sshTimeout)
        {
            m_teamNumber = teamNumber;
            m_sshTimeout = sshTimeout;
        }

        public void Dispose()
        {
            m_scpAdminClient?.Dispose();
            m_sshUserClient?.Dispose();
            m_scpUserClient?.Dispose();
            m_scpAdminClient?.Dispose();
        }

        public static async Task<RoboRioConnection> StartConnectionTaskAsync(string teamNumberS)
        {
            int teamNumber;
            int.TryParse(teamNumberS, out teamNumber);
            if (teamNumber < 0)
            {
                teamNumber = 0;
            }
            var m_roboRioConnection = new RoboRioConnection(teamNumber, TimeSpan.FromSeconds(2));
            bool connected = await m_roboRioConnection.GetConnectionAsync().ConfigureAwait(false);
            if (connected)
            {
                StringBuilder builder = new StringBuilder();
                ConnectionStatus status = m_roboRioConnection.ConnectionStatus;
                builder.AppendLine("Connected to RoboRIO...");
                builder.AppendLine("Interface: " + status.ConnectionType);
                builder.Append("IP Address: " + status.ConnectionIp.ToString());
                await OutputWriter.Instance.WriteLineAsync(builder.ToString()).ConfigureAwait(false);
            }
            else
            {
                await OutputWriter.Instance.WriteLineAsync("Failed to Connect to RoboRIO...").ConfigureAwait(false);
            }
            return m_roboRioConnection;
        }

        public async Task<bool> GetConnectionAsync()
        {
            if (m_teamNumber < 0)
            {
                m_teamNumber = 0;
            }
            string roboRioMDNS = string.Format(RoboRioMdnsFormatString, m_teamNumber);
            string roboRioLan = string.Format(RoboRioLanFormatString, m_teamNumber);
            string roboRIOIP = string.Format(RoboRioIpFormatString, m_teamNumber / 100, m_teamNumber % 100);

            using (TcpClient usbClient = new TcpClient())
            using (TcpClient mDnsClient = new TcpClient())
            using (TcpClient lanClient = new TcpClient())
            using (TcpClient ipClient = new TcpClient())
            {

                Task usb = usbClient.ConnectAsync(RoboRioUSBIp, 80);
                Task mDns = mDnsClient.ConnectAsync(roboRioMDNS, 80);
                Task lan = lanClient.ConnectAsync(roboRioLan, 80);
                Task ip = ipClient.ConnectAsync(roboRIOIP, 80);
                Task delayTask = Task.Delay(10000);

                // http://stackoverflow.com/questions/24441474/await-list-of-async-predicates-but-drop-out-on-first-false
                List<Task> tasks = new List<Task>()
                {
                    usb, mDns, lan, ip, delayTask
                };

                while (tasks.Count != 0)
                {
                    var finished = await Task.WhenAny(tasks);

                    if (finished == delayTask)
                    {
                        return false;
                    }
                    else if (finished.IsCompleted && !finished.IsFaulted && !finished.IsCanceled)
                    {
                        // A task finished, find our host
                        TcpClient foundHost = null;
                        if (finished == usb)
                        {
                            foundHost = usbClient;
                            m_connectionType = ConnectionType.USB;
                        }
                        else if (finished == mDns)
                        {
                            foundHost = mDnsClient;
                            m_connectionType = ConnectionType.MDNS;
                        }
                        else if (finished == lan)
                        {
                            foundHost = lanClient;
                            m_connectionType = ConnectionType.LAN;
                        }
                        else if (finished == ip)
                        {
                            foundHost = ipClient;
                            m_connectionType = ConnectionType.IP;
                        }
                        else
                        {
                            // Error
                            return false;
                        }

                        var ep = foundHost.Client.RemoteEndPoint;
                        var ipEp = ep as IPEndPoint;
                        if (ipEp == null)
                        {
                            // Continue, we cannot use this one

                        }
                        else
                        {
                            bool finishedConnect = await OnConnectionFound(ipEp.Address);
                            if (finishedConnect)
                            {
                                m_remoteIp = ipEp.Address;
                            }
                            return finishedConnect;
                        }
                    }
                    tasks.Remove(finished);
                }
                // If we have ever gotten here, return false
                return false;
            }
        }

        private async Task<bool> OnConnectionFound(IPAddress ip)
        {
            //User auth method
            KeyboardInteractiveAuthenticationMethod authMethod = new KeyboardInteractiveAuthenticationMethod("lvuser");
            PasswordAuthenticationMethod pauth = new PasswordAuthenticationMethod("lvuser", "");

            authMethod.AuthenticationPrompt += (sender, e) =>
            {
                foreach (
                    AuthenticationPrompt p in
                        e.Prompts.Where(
                            p => p.Request.IndexOf("Password:", StringComparison.InvariantCultureIgnoreCase) != -1))
                {
                    p.Response = "";
                }
            };

            //Admin Auth Method
            KeyboardInteractiveAuthenticationMethod authMethodAdmin = new KeyboardInteractiveAuthenticationMethod("admin");
            PasswordAuthenticationMethod pauthAdmin = new PasswordAuthenticationMethod("admin", "");

            authMethodAdmin.AuthenticationPrompt += (sender, e) =>
            {
                foreach (
                    AuthenticationPrompt p in
                        e.Prompts.Where(
                            p => p.Request.IndexOf("Password:", StringComparison.InvariantCultureIgnoreCase) != -1))
                {
                    p.Response = "";
                }
            };

            m_lvUserConnectionInfo = new ConnectionInfo(ip.ToString(), "lvuser", pauth, authMethod) { Timeout = m_sshTimeout };


            m_adminConnectionInfo = new ConnectionInfo(ip.ToString(), "admin", pauthAdmin, authMethodAdmin) { Timeout = m_sshTimeout };

            try
            {
                m_sshUserClient = new SshClient(m_lvUserConnectionInfo);
                await Task.Run(() => m_sshUserClient.Connect()).ConfigureAwait(false);
            }
            catch (SocketException e)
            {
                return false;
            }
            catch (SshOperationTimeoutException e )
            {
                return false;
            }

            try
            {
                m_scpUserClient = new ScpClient(m_lvUserConnectionInfo);
                await Task.Run(() => m_scpUserClient.Connect()).ConfigureAwait(false);
            }
            catch (SocketException e)
            {
                return false;
            }
            catch (SshOperationTimeoutException e)
            {
                return false;
            }

            try
            {
                m_sshAdminClient = new SshClient(m_adminConnectionInfo);
                await Task.Run(() => m_sshAdminClient.Connect()).ConfigureAwait(false);
            }
            catch (SocketException)
            {
                return false;
            }
            catch (SshOperationTimeoutException)
            {
                return false;
            }

            try
            {
                m_scpAdminClient = new ScpClient(m_adminConnectionInfo);
                await Task.Run(() => m_scpAdminClient.Connect()).ConfigureAwait(false);
            }
            catch (SocketException)
            {
                return false;
            }
            catch (SshOperationTimeoutException)
            {
                return false;
            }

            return true;
        }

        public async Task<Dictionary<string, SshCommand>> RunCommandsAsync(IList<string> commands,
            ConnectionUser user)
        {
            SshClient ssh;
            switch (user)
            {
                case ConnectionUser.Admin:
                    ssh = m_sshAdminClient;
                    break;
                case ConnectionUser.LvUser:
                    ssh = m_sshUserClient;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(user), user, null);
            }

            Dictionary<string, SshCommand> retCommands = new Dictionary<string, SshCommand>();

            var settings = SettingsProvider.ExtensionSettingsPage;
            bool verbose = settings.Verbose || settings.DebugMode;
            foreach (string s in commands)
            {
                if (verbose)
                {
                    await OutputWriter.Instance.WriteLineAsync($"Running command: {s}").ConfigureAwait(false);
                }
                await Task.Run(() =>
                {
                    var x = ssh.RunCommand(s);

                    retCommands.Add(s, x);
                }).ConfigureAwait(false);
            }
            return retCommands;
        }

        public async Task<SshCommand> RunCommandAsync(string command, ConnectionUser user)
        {
            SshClient ssh;
            switch (user)
            {
                case ConnectionUser.Admin:
                    ssh = m_sshAdminClient;
                    break;
                case ConnectionUser.LvUser:
                    ssh = m_sshUserClient;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(user), user, null);
            }
            var settings = SettingsProvider.ExtensionSettingsPage;
            bool verbose = settings.Verbose || settings.DebugMode;
            if (verbose)
            {
                await OutputWriter.Instance.WriteLineAsync($"Running command: {command}").ConfigureAwait(false);
            }
            return await Task.Run(() => ssh.RunCommand(command)).ConfigureAwait(false);
        }

        public async Task<bool> ReceiveFileAsync(string remoteFile, Stream receiveStream, ConnectionUser user)
        {
            ScpClient scp;
            switch (user)
            {
                case ConnectionUser.Admin:
                    scp = m_scpAdminClient;
                    break;
                case ConnectionUser.LvUser:
                    scp = m_scpUserClient;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(user), user, null);
            }

            if (receiveStream == null || !receiveStream.CanWrite)
            {
                return false;
            }

            var settings = SettingsProvider.ExtensionSettingsPage;
            bool verbose = settings.Verbose || settings.DebugMode;
            if (verbose)
            {
                await OutputWriter.Instance.WriteLineAsync($"Receiving File: {remoteFile}").ConfigureAwait(false);
            }
            try
            {
                await Task.Run(() => scp.Download(remoteFile, receiveStream)).ConfigureAwait(false);
            }
            catch (SshException)
            {
                return false;
            }
            return true;
        }

        public async Task<bool> DeployFileAsync(Stream file, string deployLocation, ConnectionUser user)
        {
            ScpClient scp;
            switch (user)
            {
                case ConnectionUser.Admin:
                    scp = m_scpAdminClient;
                    break;
                case ConnectionUser.LvUser:
                    scp = m_scpUserClient;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(user), user, null);
            }

            var settings = SettingsProvider.ExtensionSettingsPage;
            bool verbose = settings.Verbose || settings.DebugMode;
            if (verbose)
            {
                await OutputWriter.Instance.WriteLineAsync($"Deploying File: {deployLocation}").ConfigureAwait(false);
            }
            await Task.Run(() => scp.Upload(file, deployLocation)).ConfigureAwait(false);
            return true;
        }

        public async Task<bool> DeployFiles(IEnumerable<string> files, string deployLocation, ConnectionUser user)
        {
            ScpClient scp;
            switch (user)
            {
                case ConnectionUser.Admin:
                    scp = m_scpAdminClient;
                    break;
                case ConnectionUser.LvUser:
                    scp = m_scpUserClient;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(user), user, null);
            }

            var settings = SettingsProvider.ExtensionSettingsPage;
            bool verbose = settings.Verbose || settings.DebugMode;
            foreach (FileInfo fileInfo in from string s in files where File.Exists(s) select new FileInfo(s))
            {
                if (verbose)
                {
                    await OutputWriter.Instance.WriteLineAsync($"Deploying File: {fileInfo.Name}").ConfigureAwait(false);
                }
                await Task.Run(() => scp.Upload(fileInfo, deployLocation)).ConfigureAwait(false);
            }
            return true;
        }
    }
}
