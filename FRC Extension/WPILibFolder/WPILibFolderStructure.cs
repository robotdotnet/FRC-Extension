using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
