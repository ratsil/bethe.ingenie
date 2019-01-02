using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ingenie.server
{
	class Logger : helpers.Logger
	{
        new public static string sPreferencesFile
        {
            set
            {
                helpers.Logger.sPreferencesFile = value;
            }
        }
        public Logger()
			: base("ingenie:server", "igServer[" + System.Diagnostics.Process.GetCurrentProcess().Id + "]")
		{
        }
	}
}
    