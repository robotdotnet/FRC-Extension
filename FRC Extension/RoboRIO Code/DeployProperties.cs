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

        public const string RoboRIOMonoBin = "/usr/bin/mono";

        public const string RIOImageSearchString = "FRC_roboRIO";
        public static readonly int[] RoboRIOAllowedImages = { 23 };

        public static readonly string[] IgnoreFiles =
        {
            ".pdb",
            ".vshost",
            ".config",
            ".manifest",
        };

        public const string CommandDir = "/home/lvuser";

        public static readonly string RobotCommandDebug =
            "env LD_PRELOAD=/lib/libstdc++.so.6.0.20 /usr/local/frc/bin/netconsole-host mono --debug " + DeployDir + "/{0}";

        public const string RobotCommandDebugFileName = "robotDebugCommand";

        public static readonly string RobotCommand =
            "env LD_PRELOAD=/lib/libstdc++.so.6.0.20 /usr/local/frc/bin/netconsole-host mono " + DeployDir + "/{0}";

        public const string RobotCommandFileName = "robotCommand";

    }
}
