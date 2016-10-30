using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace RobotDotNet.FRC_Extension.SettingsPages
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [CLSCompliant(false), ComVisible(true)]
    public class ExtensionSettingsPage : DialogPage
    {
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
