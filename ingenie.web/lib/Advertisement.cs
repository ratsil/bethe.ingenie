using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ingenie.web.lib
{
	public class Advertisement
	{
		public long nAssetID;
		public long nPlaylistID;
		public string sName;
		public string sFilename;
		public long nFramesQty;
		public string sDuration;
		public string sStartPlanned;
		public DateTime dtStartSoft;
		public DateTime dtStartReal;
		public DateTime dtStartPlanned;
		public string sStorageName;
		public string sStoragePath;
		public bool bFileExist;
		public string sClassName;
		public bool bLogoBinding;
	}
}