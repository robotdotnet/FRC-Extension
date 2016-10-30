using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace RobotDotNet.FRC_Extension.SettingsPages
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [CLSCompliant(false), ComVisible(true)]
    public class DependencySettingsPage : DialogPage
    {
        [Category("Dependency Options"), DisplayName("Use Local Native HAL"), Description("True to only use local copies of the native HAL")]
        public bool UseLocalNativeHAL { get; set; }

        [Category("Dependency Options"), DisplayName("Native HAL Path"), Description("Location to the Native HAL (File name included or not")]
        public string LocalNativeHALPath { get; set; }
    }
}
