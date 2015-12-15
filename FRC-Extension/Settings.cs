using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;

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

        [Category("Extension Options"), DisplayName("Enable Verbose Output"), Description("Enables verbose output during the deploy process")]
        public bool Verbose { get; set; }

        [Category("Extension Options"), DisplayName("Enable Extension Debug Mode"), Description("Enables debug mode for the extension. Make sure this is off for normal operation.")]
        public bool DebugMode { get; set; }
    }
}
