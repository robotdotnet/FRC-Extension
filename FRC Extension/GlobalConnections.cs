using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoboRIO_Tool;

namespace RobotDotNet.FRC_Extension
{
    internal class GlobalConnections
    {
        public static ConnectionManager connectionManager;
        public static FileDeployManager fileDeployManager;
        public static CommandManager commandManager;

        public static void Initialize()
        {
            connectionManager = new ConnectionManager(OutputWriter.Instance);
            fileDeployManager = new FileDeployManager(ref connectionManager, OutputWriter.Instance);
            commandManager = new CommandManager(ref connectionManager, OutputWriter.Instance);
        }
    }
}
