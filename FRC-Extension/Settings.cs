using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace RobotDotNet.FRC_Extension
{
    //This contains the properties for the extension.
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [CLSCompliant(false), ComVisible(true)]
    public class SettingsPageGrid : DialogPage
    {
        [Category("Team Options"), DisplayName("Team Number"), Description("Enter your team number here.")]
        public int TeamNumber { get; set; }

        [Category("Team Options"), DisplayName("Auto Start Netconsole"), Description("Auto start the netconsole viewer when running code")]
        public bool Netconsole { get; set; }

        [Category("Robot Options"), DisplayName("Send Command Line Arguments"), Description("Sends the command line arguments for the project to the robot.")]
        public bool ConsoleArgs { get; set; }

        [Category("Extension Options"), DisplayName("Enable Verbose Output"), Description("Enables verbose output during the deploy process")]
        public bool Verbose { get; set; }

        [Category("Extension Options"), DisplayName("Enable Extension Debug Mode"), Description("Enables debug mode for the extension. Make sure this is off for normal operation.")]
        public bool DebugMode { get; set; }

        [Category("Extension Options"), DisplayName("Ignore RoboRio Image Version"), Description("Sets deploy to ignore RoboRio image. Used for Beta Testing. Do not use in season")]
        public bool IgnoreImageRequirements { get; set; }

        [Category("Extension Options"), DisplayName("Ignore Deploy File Requirements"), Description("Ignores the need for specific files during deploy. Used for Beta Testing. Do not use in season")]
        public bool IgnoreFileRequirements { get; set; }
    }
}
