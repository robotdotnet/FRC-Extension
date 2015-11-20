using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public abstract class ButtonBase
    {
        protected readonly OutputWriter m_output;

        protected readonly Frc_ExtensionPackage m_package;

        protected readonly OleMenuCommand m_oleMenuItem;

        protected ButtonBase(Frc_ExtensionPackage package, bool buttonNeedsQuery, Guid commandSetGuid, int pkgCmdIdOfButton)
        {
            m_output =OutputWriter.Instance;
            m_package = package;

            OleMenuCommandService mcs = m_package.PublicGetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs != null)
            {
                CommandID commandId = new CommandID(commandSetGuid, pkgCmdIdOfButton);
                m_oleMenuItem = new OleMenuCommand(ButtonCallback, commandId);
                if (buttonNeedsQuery)
                {
                    m_oleMenuItem.BeforeQueryStatus += QueryCallback;
                }
                mcs.AddCommand(m_oleMenuItem);
            }
        }

        public abstract void ButtonCallback(object sender, EventArgs e);

        public virtual void QueryCallback(object sender, EventArgs e)
        {
            
        }

    }
}
