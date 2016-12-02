namespace RobotDotNet.FRC_Extension.RoboRIOCode
{
    public static class DeployProperties
    {

        public const string UserName = "lvuser";
        public const string DeployDir = "/home/lvuser/mono";
        public static readonly string[] DeployKillCommand =
        {
            ". /etc/profile.d/natinst-path.sh; /usr/local/frc/bin/frcKillRobot.sh -t -r;",
        };

        public const string KillOnlyCommand = ". /etc/profile.d/natinst-path.sh; /usr/local/frc/bin/frcKillRobot.sh -t";

        public const string DebugFlagDir = "/tmp/";

        public static readonly string[] DebugFlagCommand =
        {
            "touch /tmp/frcdebug",
            $"chown lvuser:ni {DebugFlagDir}frcdebug"
        };

        public const string RoboRioMonoBin = "/usr/bin/mono";

        public const string RioImageSearchString = "FRC_roboRIO";
        public static readonly int[] RoboRioAllowedImages = { 8 };

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
            "FRC.NetworkTables.Core.dll",
        };

        public const string CommandDir = "/home/lvuser";

        public const string RobotCommandDebug = "env LD_LIBRARY_PATH=$LD_LIBRARY_PATH:/opt/GenICam_v3_0_NI/bin/Linux32_ARM/:/usr/local/frc/lib mono --debug \"" + DeployDir + "/{0}\"";

        public const string RobotCommandDebugFileName = "robotDebugCommand";

        public const string RobotCommand = "env LD_LIBRARY_PATH=$LD_LIBRARY_PATH:/opt/GenICam_v3_0_NI/bin/Linux32_ARM/:/usr/local/frc/lib mono \"" + DeployDir + "/{0}\"";

        public const string RobotCommandFileName = "robotCommand";

        public const string MonoMd5 = "1852022171552091945152452461853193134197150";

        public const string MonoVersion = "Mono4.2.1.zip";

        public const string MonoUrl = "https://dl.bintray.com/robotdotnet/Mono/";

        public const string RoboRioOpgkLocation = "/home/admin/opkg";

        public static readonly string OpkgInstallCommand = $"opkg install {RoboRioOpgkLocation}/*.ipk";

    }
}
