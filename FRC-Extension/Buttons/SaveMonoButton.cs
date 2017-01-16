using System;
using Microsoft.VisualStudio.Shell;
using RobotDotNet.FRC_Extension.MonoCode;
using Task = System.Threading.Tasks.Task;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class SaveMonoButton : ButtonBase
    {
        private readonly MonoFile m_monoFile;

        public SaveMonoButton(Frc_ExtensionPackage package, MonoFile monoFile)
            : base(package, true, GuidList.guidFRC_ExtensionCmdSet, (int) PkgCmdIDList.cmdidSaveMonoFile)
        {
            m_monoFile = monoFile;
        }

        public override void ButtonCallback(object sender, EventArgs e)
        {
            m_monoFile.SaveMonoFile();
        }

        public override void QueryCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
            {
                return;
            }

            try
            {
                bool fileExistsAndValid = m_monoFile.CheckFileValid();
                menuCommand.Enabled = fileExistsAndValid;
            }
            catch (Exception ex)
            {
                ThreadHelper.JoinableTaskFactory.Run(() => OutputWriter.Instance.WriteLineAsync(ex.StackTrace));
            }
        }

        protected override Task ButtonCallbackAsync(object sender, EventArgs e)
        {
            return Task.FromResult(false);
        }
    }
}
