using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using RobotDotNet.FRC_Extension.MonoCode;

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

        public override async void ButtonCallback(object sender, EventArgs e)
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
                OutputWriter.Instance.WriteLine(ex.StackTrace);
            }
        }
    }
}
