using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotDotNet.FRC_Extension.SettingsPages
{
    public static class SettingsProvider
    {
        public static TeamSettingsPage TeamSettingsPage { get; private set; }
        public static DependencySettingsPage DependencySettingsPage { get; private set; }
        public static ExtensionSettingsPage ExtensionSettingsPage { get; private set; }


        public static void SetSettingsPages(TeamSettingsPage teamSettings, ExtensionSettingsPage extensionSettings, DependencySettingsPage dependencySettings)
        {
            TeamSettingsPage = teamSettings;
            ExtensionSettingsPage = extensionSettings;
            DependencySettingsPage = dependencySettings;
        }
    }
}
