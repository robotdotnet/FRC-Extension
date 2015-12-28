using System;
using System.Collections.Generic;
$if$ ($targetframeworkversion$ >= 3.5)using System.Linq;
$endif$
using WPILib;
using WPILib.Commands;
using WPILib.LiveWindow;
using $safeprojectname$.Subsystems
using $safeprojectname$.Commands

namespace $safeprojectname$
{
    /// <summary>
    /// The VM is configured to automatically run this class, and to call the
    /// functions corresponding to each mode, as described in the IterativeRobot
    /// documentation. 
    /// </summary>
    public class $safeprojectname$ : IterativeRobot
    {
        public static readonly ExampleSubsystem exampleSubsystem = new ExampleSubsystem();
	    public static OI oi;

        Command autonomousCommand;
        SendableChooser chooser;

        // This function is run when the robot is first started up and should be
        // used for any initialization code.
        //
        public override void RobotInit()
        {
		    oi = new OI();
            // instantiate the command used for the autonomous period
            autonomousCommand = new ExampleCommand();
            chooser = new SendableChooser();
            chooser.AddDefault("Default Auto", new ExampleCommand());
            //chooser.AddObject("My Auto", new MyAutoCommand);
        }
	
	    public override void DisabledPeriodic()
        {
		    Scheduler.GetInstance().Run();
	    }

        // This autonomous (along with the sendable chooser above) shows how to select between
        // different autonomous modes using the dashboard. The senable chooser code works with
        // the Java SmartDashboard. If you prefer the LabVIEW Dashboard, remove all the chooser
        // code an uncomment the GetString code to get the uto name from the text box below
        // the gyro.
        // You can add additional auto modes by adding additional commands to the chooser code
        // above (like the commented example) or additional comparisons to the switch structure
        // below with additional strings and commands.
        public override void AutonomousInit()
        {
            autonomousCommand = (Command) chooser.GetSelected();
            
            /*
            string autoSelected = SmartDashboard.GetString("Auto Selector", "Default");
            switch(autoSelected)
            {
            case "My Auto":
                autonomousCommand = new MyAutoCommand();
                break;
            case "Default Auto"
            default:
                autonomousCommand = new ExampleCommand();
                break;
            }
            */
            // schedule the autonomous command (example)
            if (autonomousCommand != null) autonomousCommand.Start();
        }


        // This function is called periodically during autonomous
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

        //
        // This function is called when the disabled button is hit.
        // You can use it to reset subsystems before shutting down.
        //
        public override void DisabledInit(){

        }

        //
        // This function is called periodically during operator control
        //
        public override void TeleopPeriodic() {
            Scheduler.GetInstance().Run();
        }
    
        //
        // This function is called periodically during test mode
        //
        public override void TestPeriodic() {
            LiveWindow.Run();
        }
    }
}
