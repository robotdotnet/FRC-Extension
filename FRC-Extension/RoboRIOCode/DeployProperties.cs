using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotDotNet.FRC_Extension
{
    public static class DeployProperties
    {

        public const string UserName = "lvuser";
        public const string DeployDir = "/home/lvuser/mono";
        public static readonly string[] DeployKillCommand =
        {
            ". /etc/profile.d/natinst-path.sh; /usr/local/frc/bin/frcKillRobot.sh -t -r",
        };

        public const string KillOnlyCommand = "/usr/local/frc/bin/frcKillRobot.sh";

        public const string DebugFlagDir = "/tmp/";

        public static readonly string[] DebugFlagCommand =
        {
            "touch /tmp/frcdebug",
            $"chown lvuser:ni {DebugFlagDir}frcdebug"
        };

        public const string RoboRioMonoBin = "/usr/bin/mono";

        public const string RioImageSearchString = "FRC_roboRIO";
        public static readonly int[] RoboRioAllowedImages = { 15 };

        public static readonly string[] IgnoreFiles =
        {
            ".pdb",
            ".vshost",
            ".config",
            ".manifest",
        };

        public static readonly string[] RequiredFiles =
        {
            "WPILib.dll",
            "HAL.dll",
            "NetworkTables.dll",
        };

        public const string CommandDir = "/home/lvuser";

        public const string RobotCommandDebug = "env LD_LIBRARY_PATH=/usr/local/frc/rpath-lib/ /usr/local/frc/bin/netconsole-host mono --debug \"" + DeployDir + "/{0}\"";

        public const string RobotCommandDebugFileName = "robotDebugCommand";

        public const string RobotCommand = "env LD_LIBRARY_PATH=/usr/local/frc/rpath-lib/ /usr/local/frc/bin/netconsole-host mono \"" + DeployDir + "/{0}\"";

        public const string RobotCommandFileName = "robotCommand";

        public const string MonoMd5 = "1852022171552091945152452461853193134197150";

        public const string MonoVersion = "Mono4.2.1.zip";

        public const string MonoUrl = "https://dl.bintray.com/robotdotnet/Mono/";

        public const string RoboRioOpgkLocation = "/home/admin/opkg";

        public static readonly string OpkgInstallCommand = $"opkg install {RoboRioOpgkLocation}/*.ipk";

    }
}
