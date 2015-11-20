using WPILib;
using WPILib.Commands;
using WPILib.Extras.AttributedCommandModel;

namespace $safeprojectname$.Subsystems
{
    [ExportSubsystem(DefaultCommandType = typeof($safeprojectname$.Commands.ExampleCommand))]
    public class ExampleSubsystem : Subsystem
    {
        // Put methods for controlling this subsystem
        // here. Call these from Commands.

        protected override void InitDefaultCommand()
        {
            // This can be left empty since the default command will be set during initialization automatically
        }
    }
}
