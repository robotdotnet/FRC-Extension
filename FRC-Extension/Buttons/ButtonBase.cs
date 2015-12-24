using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public abstract class ButtonBase
    {
        protected readonly OutputWriter Output;

        protected readonly Frc_ExtensionPackage Package;

        protected readonly OleMenuCommand OleMenuItem;

        protected ButtonBase(Frc_ExtensionPackage package, bool buttonNeedsQuery, Guid commandSetGuid, int pkgCmdIdOfButton)
        {
            Output =OutputWriter.Instance;
            Package = package;

            OleMenuCommandService mcs = Package.PublicGetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs != null)
            {
                CommandID commandId = new CommandID(commandSetGuid, pkgCmdIdOfButton);
                OleMenuItem = new OleMenuCommand(ButtonCallback, commandId);
                if (buttonNeedsQuery)
                {
                    OleMenuItem.BeforeQueryStatus += QueryCallback;
                }
                mcs.AddCommand(OleMenuItem);
            }
        }

        public abstract void ButtonCallback(object sender, EventArgs e);

        public virtual void QueryCallback(object sender, EventArgs e)
        {
            
        }

    }
}
