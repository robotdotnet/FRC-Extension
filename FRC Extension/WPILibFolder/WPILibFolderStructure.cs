using System;
using System.IO;

namespace RobotDotNet.FRC_Extension.WPILibFolder
{
    public class WPILibFolderStructure
    {
        public static readonly string WPILibDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                                                  Path.DirectorySeparatorChar + "wpilib" + Path.DirectorySeparatorChar +
                                                  "dotnet";

        public static void CreateWPILibFolder()
        {
            Directory.CreateDirectory(WPILibDir);
        }

        public static string CreateMonoFolder()
        {
            string monoDir = WPILibDir + Path.DirectorySeparatorChar + "mono";
            Directory.CreateDirectory(monoDir);
            return monoDir;
        }
    }
}
