using System;
using System.Collections.Generic;
$if$ ($targetframeworkversion$ >= 3.5)using System.Linq;
$endif$using System.Text;
using WPILib;
using WPILib.Buttons;

namespace $rootnamespace$
{
	public class $safeitemname$ : Trigger
	{
        public override bool Get()
	    {
	        return false;
	    }
	}
}
