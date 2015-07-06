using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ingenie.server
{
	class Logger : helpers.Logger
	{
		public Logger()
			: base("ingenie:server", "igServer[" + System.Diagnostics.Process.GetCurrentProcess().Id + "]")
		{ }
	}
}
