using System;
using System.Collections.Generic;
$if$ ($targetframeworkversion$ >= 3.5)using System.Linq;
$endif$using System.Text;
using WPILib;

namespace $safeprojectname$
{
    /**
     * The VM is configured to automatically run this class, and to call the
     * functions corresponding to each mode, as described in the IterativeRobot
     * documentation. 
     */
    public class $safeprojectname$ : IterativeRobot
    {
        /**
         * This function is run when the robot is first started up and should be
         * used for any initialization code.
         */
        public override void RobotInit()
        {
            
        }

        /**
         * This function is called periodically during autonomous
         */
        public override void AutonomousPeriodic()
        {

        }

        /**
         * This function is called periodically during operator control
         */
        public override void TeleopPeriodic() 
        {
        
        }
    
        /**
         * This function is called periodically during test mode
         */
        public override void TestPeriodic() 
        {
    
        }
    }
}
