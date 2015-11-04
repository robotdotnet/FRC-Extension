using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class SettingsButton
    {
        private readonly OutputWriter m_output;

        private readonly Frc_ExtensionPackage m_package;



        public SettingsButton(Frc_ExtensionPackage package)
        {
            m_output = OutputWriter.Instance;
            m_package = package;


            OleMenuCommandService mcs =
                m_package.PublicGetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                //For settings, we just want to pop up the standard settings menu.
                CommandID settingsCommandID = new CommandID(GuidList.guidFRC_ExtensionCmdSet,
                    (int)PkgCmdIDList.cmdidSettings);
                MenuCommand settingsItem = new MenuCommand(((sender, e) => OpenSettings()), settingsCommandID);
                mcs.AddCommand(settingsItem);
            }
        }

        public void OpenSettings()
        {
            m_package.ShowOptionPage(typeof(SettingsPageGrid));
        }
    }
}
