using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobotDotNet.FRC_Extension.MonoCode
{
    public class MonoFileSelector
    {
        public static string SelectMonoFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "Zip Files(*.zip)|*.zip";
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.FileName;
            }
            return null;
        }


    }
}
