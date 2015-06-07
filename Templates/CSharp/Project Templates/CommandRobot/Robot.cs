using System;
using System.Collections.Generic;
$if$ ($targetframeworkversion$ >= 3.5)using System.Linq;
$endif$using System.Text;
using WPILib;
using WPILib.Commands;
using WPILib.livewindow;
using $safeprojectname$.Subsystems
using $safeprojectname$.Commands

namespace $safeprojectname$
{
    /**
     * The VM is configured to automatically run this class, and to call the
     * functions corresponding to each mode, as described in the IterativeRobot
     * documentation. 
     */
    public class $safeprojectname$ : IterativeRobot
    {
        public static readonly ExampleSubsystem exampleSubsystem = new ExampleSubsystem();
	    public static OI oi;

        Command autonomousCommand;

        /**
         * This function is run when the robot is first started up and should be
         * used for any initialization code.
         */
        public override void RobotInit() {
		    oi = new OI();
            // instantiate the command used for the autonomous period
            autonomousCommand = new ExampleCommand();
        }
	
	    public override void DisabledPeriodic() {
		    Scheduler.GetInstance().Run();
	    }

        public override void AutonomousInit() {
            // schedule the autonomous command (example)
            if (autonomousCommand != null) autonomousCommand.Start();
        }

        /**
         * This function is called periodically during autonomous
         */
        public override void AutonomousPeriodic() {
            Scheduler.GetInstance().Run();
        }

        public override void TeleopInit() {
		    // This makes sure that the autonomous stops running when
            // teleop starts running. If you want the autonomous to 
            // continue until interrupted by another command, remove
            // this line or comment it out.
            if (autonomousCommand != null) autonomousCommand.Cancel();
        }

        /**
         * This function is called when the disabled button is hit.
         * You can use it to reset subsystems before shutting down.
         */
        public override void DisabledInit(){

        }

        /**
         * This function is called periodically during operator control
         */
        public override void TeleopPeriodic() {
            Scheduler.GetInstance().Run();
        }
    
        /**
         * This function is called periodically during test mode
         */
        public override void TestPeriodic() {
            LiveWindow.Run();
        }
    }
}
