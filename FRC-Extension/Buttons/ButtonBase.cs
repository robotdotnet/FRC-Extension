using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public abstract class ButtonBase
    {
        protected readonly OutputWriter Output;

        protected readonly Frc_ExtensionPackage Package;

        protected readonly OleMenuCommand OleMenuItem;

        protected ButtonBase(Frc_ExtensionPackage package, bool buttonNeedsQuery, Guid commandSetGuid, int pkgCmdIdOfButton)
        {
            Output = OutputWriter.Instance;
            Package = package;

            var mcs = Package.PublicGetService<OleMenuCommandService, IMenuCommandService>();
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

#pragma warning disable IDE1006 // Naming Styles
        public virtual async void ButtonCallback(object sender, EventArgs e)
#pragma warning restore IDE1006 // Naming Styles
        {
            await ButtonCallbackAsync(sender, e).ConfigureAwait(false);
        }

        protected abstract Task ButtonCallbackAsync(object sender, EventArgs e);

        public virtual void QueryCallback(object sender, EventArgs e)
        {
        }
    }
}
