using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ingenie.web.lib
{
	static public class Client
	{
		static private object _cSyncRoot = new object();
		static public ulong nID { get; private set; }
		static public DateTime dtPing = DateTime.MinValue;
		static public ulong nCurrentClientBrowserID = 0;

		static public void Init(ulong nBrowserID)
		{
			dtPing = DateTime.Now;
			nCurrentClientBrowserID = nBrowserID;
			lock (_cSyncRoot)
				nID++;
		}
	}
}