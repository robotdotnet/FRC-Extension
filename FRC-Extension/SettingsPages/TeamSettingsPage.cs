using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace RobotDotNet.FRC_Extension.SettingsPages
{
    //This contains the properties for the extension.
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [CLSCompliant(false), ComVisible(true)]
    public class TeamSettingsPage : DialogPage
    {
        [Category("Team Options"), DisplayName("Team Number"), Description("Enter your team number here.")]
        public int TeamNumber { get; set; }

        [Category("Team Options"), DisplayName("Auto Start Netconsole"), Description("Auto start the netconsole viewer when running code")]
        public bool Netconsole { get; set; }

        [Category("Robot Options"), DisplayName("Send Command Line Arguments"), Description("Sends the command line arguments for the project to the robot.")]
        public bool ConsoleArgs { get; set; }
    }
}
