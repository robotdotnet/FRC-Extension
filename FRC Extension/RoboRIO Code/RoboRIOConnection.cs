using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace RobotDotNet.FRC_Extension.RoboRIO_Code
{
    public enum ConnectionType
    {
        USB,
        MDNS,
        IP,
        None
    }

    public static class RoboRIOConnection
    {
        public static readonly string RoboRioMdnsFormatString = "roborio-{0}.local";
        public static readonly string RoboRioUSBIp = "172.22.11.2";
        public static readonly string RoboRioIpFormatString = "10.{0}.{1}.2";

        public static bool CheckConnection(string teamNumberS, out ConnectionType type, out string conIP,
            out ConnectionInfo lvuserConnection, out ConnectionInfo adminConnection)
        {
            return CheckConnection(teamNumberS, out type, out conIP, TimeSpan.FromSeconds(2), out lvuserConnection, out adminConnection);
        }

        public static bool CheckConnection(string teamNumberS, out ConnectionType type, out string conIP,
            TimeSpan timeout, out ConnectionInfo lvuserConnection, out ConnectionInfo adminConnection)
        {
            int teamNumber = 0;
            int.TryParse(teamNumberS, out teamNumber);


            if (teamNumber < 0)
            {
                teamNumber = 0;
            }
            string roboRioMDNS = string.Format(RoboRioMdnsFormatString, teamNumber);
            string roboRIOIP = string.Format(RoboRioIpFormatString, teamNumber / 100, teamNumber % 100);

            if (GetWorkingConnectionInfo(roboRioMDNS, timeout, out lvuserConnection, out adminConnection))
            {
                type = ConnectionType.MDNS;
                conIP = roboRioMDNS;
                return true;
            }
            else if (GetWorkingConnectionInfo(RoboRioUSBIp, timeout, out lvuserConnection, out adminConnection))
            {
                type = ConnectionType.USB;
                conIP = RoboRioUSBIp;
                return true;
            }
            else if (GetWorkingConnectionInfo(roboRIOIP, timeout, out lvuserConnection, out adminConnection))
            {
                type = ConnectionType.IP;
                conIP = roboRIOIP;
                return true;
            }
            type = ConnectionType.None;
            conIP = null;
            return false;
        }

        private static bool GetWorkingConnectionInfo(string ip, TimeSpan timeout, out ConnectionInfo lvuserInfo, out ConnectionInfo adminInfo)
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

            lvuserInfo = new ConnectionInfo(ip, "lvuser", pauth, authMethod) { Timeout = timeout };


            adminInfo = new ConnectionInfo(ip, "admin", pauthAdmin, authMethodAdmin) { Timeout = timeout };
            using (SshClient zeroConfClient = new SshClient(lvuserInfo))
            {
                try
                {
                    zeroConfClient.Connect();
                    return true;
                }
                catch (SocketException)
                {
                    return false;
                }
                catch (SshOperationTimeoutException)
                {
                    return false;
                }
            }
        }

        public static Dictionary<string, SshCommand> RunCommands(string[] commands, ConnectionInfo connectionInfo)
        {
            Dictionary<string, SshCommand> retCommands = new Dictionary<string, SshCommand>();
            using (SshClient ssh = new SshClient(connectionInfo))
            {
                try
                {
                    ssh.Connect();
                }
                catch (SshOperationTimeoutException)
                {

                }
                foreach (string s in commands)
                {
                    var x = ssh.RunCommand(s);
                    
                    retCommands.Add(s, x);
                }
            }
            return retCommands;
        }

        public static SshCommand RunCommand(string command, ConnectionInfo connectionInfo)
        {
            using (SshClient ssh = new SshClient(connectionInfo))
            {
                try
                {
                    ssh.Connect();
                }
                catch (SshOperationTimeoutException)
                {
                    return null;
                }
                return ssh.RunCommand(command);
            }
        }

        public static bool DeployFiles(IEnumerable files, string deployLocation, ConnectionInfo connectionInfo)
        {
            if (connectionInfo == null) return false;
            using (ScpClient scp = new ScpClient(connectionInfo))
            {
                try
                {
                    scp.Connect();
                }
                catch (SshOperationTimeoutException)
                {
                    return false;
                }
                foreach (FileInfo fileInfo in from string s in files where File.Exists(s) select new FileInfo(s))
                {
                    scp.Upload(fileInfo, deployLocation);
                }
            }
            return true;
        }
    }
}
