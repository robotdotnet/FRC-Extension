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
        readonly string defaultAuto = "Default";
        readonly string customAuto = "My Auto";
        SendableChooser chooser; 


        public $safeprojectname$()
        {
            myRobot = new RobotDrive(0, 1);
            myRobot.Expiration = 0.1;
            stick = new Joystick(0);
        }

        public override void RobotInit()
        {
            chooser = new SendableChooser();
            chooser.AddDefault("Default Auto", defaultAuto);
            chooser.AddObject("My Auto", customAuto);
        }

        // This autonomous (along with the sendable chooser above) shows how to select between
        // different autonomous modes using the dashboard. The senable chooser code works with
        // the Java SmartDashboard. If you prefer the LabVIEW Dashboard, remove all the chooser
        // code an uncomment the GetString code to get the uto name from the text box below
        // the gyro.
        // You can add additional auto modes by adding additional comparisons to the if-else
        // structure below with additional strings. If using the SendableChooser
        // be sure to add them to the chooser code above as well.
        public override void Autonomous()
        {
            autoSelected = (string) chooser.GetSelected();
            //autoSelected = SmartDashboard.GetString("Auto Selector", defaultAuto);
            Console.WriteLine("Auto selected: " + autoSelected);

            switch (autoSelected)
            {
                case customAuto:
                    myRobot.SafetyEnabled = false;
                    myRobot.Drive(-0.5, 1.0); //Spin at half speed
                    Timer.Delay(2.0);         //for 2 seconds
                    myRobot.Drive(0.0, 0.0);  //Stop robot
                    break;
                case defaultAuto:
                default:
                    myRobot.SafetyEnabled = false;
                    myRobot.Drive(-0.5, 0.0); //Drive forward half speed
                    Timer.Delay(2.0);         //For 2 seconds
                    myRobot.Drive(0.0, 0.0);  //Stop Robot
                    break;
            }
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
        public override void Test() 
        {
        }
    }
}
