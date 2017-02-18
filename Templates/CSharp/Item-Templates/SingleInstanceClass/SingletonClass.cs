using System;
using System.Collections.Generic;
using WPILib;

namespace $rootnamespace$
{
    public class $safeitemname$
    {
        private static $safeitemname$ s_instance;

        public static $safeitemname$ Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new $safeitemname$();
                return s_instance;
            }
        }

        private $safeitemname$()
        {
            
        }
    }
}
