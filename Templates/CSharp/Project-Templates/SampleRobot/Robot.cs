using System;
using System.Collections.Generic;
$if$ ($targetframeworkversion$ >= 3.5)using System.Linq;
$endif$using WPILib;

namespace $safeprojectname$
{
    /**
 * This is a demo program showing the use of the RobotDrive class.
 * The SampleRobot class is the base of a robot application that will automatically call your
 * Autonomous and OperatorControl methods at the right time as controlled by the switches on
 * the driver station or the field controls.
 *
 * The VM is configured to automatically run this class, and to call the
 * functions corresponding to each mode, as described in the SampleRobot
 * documentation.
 *
 * WARNING: While it may look like a good choice to use for your code if you're inexperienced,
 * don't. Unless you know what you are doing, complex code will be much more difficult under
 * this system. Use IterativeRobot or Command-Based instead if you're new.
 */
    public class $safeprojectname$ : SampleRobot
    {
        RobotDrive myRobot;
        Joystick stick;
        public $safeprojectname$()
        {
            myRobot = new RobotDrive(0, 1);
            myRobot.SetExpiration(0.1);
            stick = new Joystick(0);
        }
        /**
         * Drive left & right motors for 2 seconds then stop
         */
        public override void Autonomous()
        {
            myRobot.SafetyEnabled = false;
            myRobot.Drive(-0.5, 0.0);	// drive forwards half speed
            Timer.Delay(2.0);		//    for 2 seconds
            myRobot.Drive(0.0, 0.0);	// stop robot
        }

        /**
         * Runs the motors with arcade steering.
         */
        public override void OperatorControl() {
            myRobot.SafetyEnabled = true;
            while (IsOperatorControl && IsEnabled) {
                myRobot.ArcadeDrive(stick); // drive with arcade style (use right stick)
                Timer.Delay(0.005);		// wait for a motor update time
            }
        }

        /**
         * Runs during test mode
         */
        public void test() 
        {
        }
    }
}
