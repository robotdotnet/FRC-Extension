using WPILib;
using WPILib.Commands;

namespace $rootnamespace$
{
    public class $safeitemname$ : PIDSubsystem
    {
        // Initialize your subsystem here
        public $safeitemname$() : base()
        {
            // Use these to get going:
            // SetSetpoint() -  Sets where the PID controller should move the system
            //                  to
            // Enable() - Enables the PID controller.
        }

        protected override void InitDefaultCommand()
        {
            // Set the default command for a subsystem here.
            //SetDefaultCommand(new MySpecialCommand());
        }

        protected override double ReturnPIDInput()
        {
            // Return your input value for the PID loop
            // e.g. a sensor, like a potentiometer:
            // yourPot.GetAverageVoltage() / kYourMaxVoltage;
            return 0.0;
        }

        protected override void UsePIDOutput(double output)
        {
            // Use output to drive your system, like a motor
            // e.g. yourMotor.Set(output);
        }
    }
}
