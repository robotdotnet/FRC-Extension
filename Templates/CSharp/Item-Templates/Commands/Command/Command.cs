using System;
using System.Collections.Generic;
$if$ ($targetframeworkversion$ >= 3.5)using System.Linq;
$endif$using System.Text;
using WPILib;
using WPILib.Commands;

namespace $rootnamespace$
{
	public class $safeitemname$ : Command
	{
         public $safeitemname$() {
            // Use Requires() here to declare subsystem dependencies
            // eg. Requires(chassis);
        }

        // Called just before this Command runs the first time
        protected override void Initialize() {
        }

        // Called repeatedly when this Command is scheduled to run
        protected override void Execute() {
        }

        // Make this return true when this Command no longer needs to run Execute()
        protected override bool IsFinished() {
            return false;
        }

        // Called once after isFinished returns true
        protected override void End() {
        }

        // Called when another command which requires one or more of the same
        // subsystems is scheduled to run
        protected override void Interrupted() {
        }
	}
}
