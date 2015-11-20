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
            "[ ! -f /var/local/natinst/log/FRC_UserProgram.log ] || rm -f /var/local/natinst/log/FRC_UserProgram.log",
            $"chown -R lvuser:ni {DeployDir}",
            ". /etc/profile.d/natinst-path.sh",
            "/usr/local/frc/bin/frcKillRobot.sh -t -r"
        };

        public const string DebugFlagDir = "/tmp/";

        public static readonly string[] DebugFlagCommand =
        {
            "touch /tmp/frcdebug",
            $"chown lvuser:ni {DebugFlagDir}frcdebug"
        };

        public const string RoboRioMonoBin = "/usr/bin/mono";

        public const string RioImageSearchString = "FRC_roboRIO";
        public static readonly int[] RoboRioAllowedImages = { 23 };

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
            "HAL-Base.dll",
            "HAL-RoboRIO.dll",
            "NetworkTables.dll",
        };

        public const string CommandDir = "/home/lvuser";

        public static readonly string RobotCommandDebug =
            "env LD_PRELOAD=/lib/libstdc++.so.6.0.20 /usr/local/frc/bin/netconsole-host mono --debug \"" + DeployDir + "/{0}\"";

        public const string RobotCommandDebugFileName = "robotDebugCommand";

        public static readonly string RobotCommand =
            "env LD_PRELOAD=/lib/libstdc++.so.6.0.20 /usr/local/frc/bin/netconsole-host mono \"" + DeployDir + "/{0}\"";

        public const string RobotCommandFileName = "robotCommand";

        public const string MonoMd5 = "145126881924420175520132501465663102223";

        public const string MonoVersion = "Mono4.0.1.zip";

        public const string MonoUrl = "https://dl.bintray.com/robotdotnet-admin/Mono/";


        public const string RoboRioOpgkLocation = "/home/admin/opkg";

        public const string OpkgInstallCommand = "opgk install *.ipk";

    }
}
