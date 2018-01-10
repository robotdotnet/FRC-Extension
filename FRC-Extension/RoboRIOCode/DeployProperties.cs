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

        public const string UserLibraryDir = "/usr/local/frc/lib";

        public const string KillOnlyCommand = ". /etc/profile.d/natinst-path.sh; /usr/local/frc/bin/frcKillRobot.sh -t";

        public const string DebugFlagDir = "/tmp/";

        public static readonly string[] DebugFlagCommand =
        {
            "touch /tmp/frcdebug",
            $"chown lvuser:ni {DebugFlagDir}frcdebug"
        };

        public const string RoboRioMonoBin = "/usr/bin/mono";

        public const string RioImageSearchString = "FRC_roboRIO";
        public static readonly int[] RoboRioAllowedImages = { 16 };

        public static readonly string[] IgnoreFiles =
        {
            ".pdb",
            ".vshost",
            ".config",
            ".manifest",
            "FRC.NetworkTables.Core.DesktopLibraries.dll",
            "FRC.OpenCvSharp.DesktopLibraries.dll",
            "FRC.HAL.DesktopLibraries.dll"
        };

        public static readonly string[] RequiredFiles =
        {
            "WPILib.dll",
            "HAL.dll",
            "FRC.NetworkTables.dll",
            "FRC.CameraServer.dll",
            "FRC.OpenCvSharp.dll",
            "NativeLibraryUtilities.dll"
        };

        public const string CommandDir = "/home/lvuser";

        public const string RobotCommandDebug = "env LD_LIBRARY_PATH=$LD_LIBRARY_PATH:/usr/local/frc/lib mono --debug \"" + DeployDir + "/{0}\"";

        public const string RobotCommandDebugFileName = "robotDebugCommand";

        public const string RobotCommand = "env LD_LIBRARY_PATH=$LD_LIBRARY_PATH:/usr/local/frc/lib mono \"" + DeployDir + "/{0}\"";

        public const string RobotCommandFileName = "robotCommand";

        public const string MonoMd5 = "1010724125414723619010587175662351173512114";

        public const string MonoVersion = "Mono5.4.1.zip";

        public const string MonoUrl = "https://dl.bintray.com/robotdotnet/Mono/";

        public const string RoboRioOpgkLocation = "/home/admin/opkg";

        public static readonly string OpkgInstallCommand = $"opkg install {RoboRioOpgkLocation}/*.ipk";

    }
}
