using System;
using System.Collections.Generic;
$if$ ($targetframeworkversion$ >= 3.5)using System.Linq;
$endif$using WPILib;
using WPILib.Commands;

namespace $rootnamespace$
{
    public class $safeitemname$ : InstantCommand
    {
         public $safeitemname$() {
            // Use Requires() here to declare subsystem dependencies
            // eg. Requires(chassis);
        }

        // Called just before this Command runs the first time
        protected override void Initialize() {
        }
    }
}
