using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ingenie.plugins
{
	class Logger : helpers.Logger
	{
		public Logger()
			: base(Level.debug2, "ingenie:plugins:voting", "voting")
		{ }
	}
}
