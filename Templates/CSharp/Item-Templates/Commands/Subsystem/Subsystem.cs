using System;
using System.Collections.Generic;
$if$ ($targetframeworkversion$ >= 3.5)using System.Linq;
$endif$using System.Text;
using WPILib;
using WPILib.Commands;

namespace $rootnamespace$
{
	public class $safeitemname$ : Subsystem
	{
        // Put methods for controlling this subsystem
        // here. Call these from Commands.

        protected override void InitDefaultCommand() 
        {
            // Set the default command for a subsystem here.
            //SetDefaultCommand(new MySpecialCommand());
        }
	}
}
