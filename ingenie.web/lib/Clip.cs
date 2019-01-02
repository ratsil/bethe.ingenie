using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ingenie.web.lib
{
	public class Clip
	{
		public long nID;
		public string sName;
		public string sFilename;
		public long nFramesQty;
		public string sDuration;
		public string sStorageName;
		public string sStoragePath;
		public string sSong;
		public string sArtist;
		public string sRotation;
		public bool bLocked;
		public bool bSmoking;
        public bool bCached;
        public helpers.replica.pl.Class[] aClasses;
	}
}
