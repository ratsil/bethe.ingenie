using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using BTL.Play;
using helpers;
using System.Xml;
using helpers.extensions;
using System.Text;
using helpers.replica.mam;

namespace ingenie.plugins
{
    class Preferences
    {
        public Area stArea
        {
            get
            {
                return _stArea;
            }
        }
        public MergingMethod stMerging
        {
            get
            {
                return _stMerging;
            }
        }
        public ushort nLayer
        {
            get
            {
                return _nLayer;
            }
        }
		public static DB.Credentials DBCredentials
		{
			get
			{
				return _cDBCredentials;
            }
		}
        public long nPlaylistID
		{
			get
			{
				if (_nPlaylistID > 0)
					return _nPlaylistID;   
				return -1;
			}
		}
		private Area _stArea;
        private MergingMethod _stMerging;
        private ushort _nLayer;
		private static DB.Credentials _cDBCredentials;
		private long _nPlaylistID;

		public Preferences(string sData)
        {
            XmlDocument cXmlDocument = new XmlDocument();
            cXmlDocument.LoadXml(sData);
            XmlNode cXmlNode = cXmlDocument.NodeGet("data");

            XmlNode cNodeChild = cXmlNode.NodeGet("playlist");
            _stMerging = new MergingMethod(cNodeChild);
            _nLayer = cNodeChild.AttributeGet<ushort>("layer");
			_nPlaylistID = cNodeChild.AttributeGet<long>("id");
			cNodeChild = cNodeChild.NodeGet("area");
            _stArea = new Area(
                    cNodeChild.AttributeGet<short>("left"),
                    cNodeChild.AttributeGet<short>("top"),
                    cNodeChild.AttributeGet<ushort>("width"),
                    cNodeChild.AttributeGet<ushort>("height")
                );
			_cDBCredentials = new DB.Credentials(cXmlNode.NodeGet("database"));
        }
    }
}