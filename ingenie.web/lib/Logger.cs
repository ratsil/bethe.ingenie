using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ingenie.web.lib
{
	public class Logger : helpers.Logger
	{
        public Logger(string sCategory)
            : base(sCategory, "ingenie.web")
        {
        }
        public Logger(helpers.Logger.Level eLevel, string sCategory)
            : base(eLevel, sCategory, "ingenie.web")
        {
        }
    }
}