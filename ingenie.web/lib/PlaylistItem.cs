using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ingenie.web.lib
{
	[Serializable]
	public class PlaylistItem
	{
		public enum PLIType
		{
			AdvBlock,
			AdvBlockItem,
			Clip,
			File,
			JustString,
			Studio
		}
		public PLIType _eType;
		public bool bIsFirstItemInBlock;
		public string sFilename { get; set; }
		public string sFilenameFull { get; set; }
		public string sName { get; set; }
		public long _nFramesQty;
		public string sStorageName;
		public bool bFileExist;
		public bool bFileIsImage;
		public ushort nTransDuration;
		public ushort nEndTransDuration;
		public DateTime dtStartReal;
		public DateTime dtStopReal;
		public int nAtomHashCode;
		public int nEffectID; // это BTL effect hash
		public long nID;
		public Advertisement _cAdvertSCR;
		public Clip _cClipSCR;
		public PlaylistItem()
		{
			dtStartReal = DateTime.MinValue;
			dtStopReal = DateTime.MinValue;
			_cAdvertSCR = null;
			_cClipSCR = null;
			bFileExist = true;
			bFileIsImage = false;
		}
	}
}