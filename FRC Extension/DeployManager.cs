using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace RobotDotNet.FRC_Extension
{
    class DeployManager
    {
        public void DeployCode(SettingsPageGrid page)
        {
            string teamNumber = page.TeamNumber.ToString();
            bool forceUSB = page.ForceUSB;

            //Build Code


            //Connect to Robot Syncronously


        }
    }
}
