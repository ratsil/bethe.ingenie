using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using BTL.Play;
using btl = BTL.Play;
using helpers;
using System.Xml;
using helpers.extensions;
using System.Text;

namespace ingenie.plugins
{
    class Preferences
    {
        public class Item
        {
            public class Text : Item
            {
                public System.Drawing.Font cFont;
                public System.Drawing.Color stColor;
                public System.Drawing.Color stColorBorder;
                public float nBorderWidth;
				public short nTopOffset;

                static public Text Parse(XmlNode cXmlNode)
                {
                    Text cRetVal = new Text();
                    XmlNode cNodeFont = cXmlNode.NodeGet("font");
                    cRetVal.cFont = new System.Drawing.Font(cNodeFont.AttributeValueGet("name"), cNodeFont.AttributeGet<int>("size"), cNodeFont.AttributeGet<System.Drawing.FontStyle>("style"));
					cRetVal.nTopOffset = cNodeFont.AttributeGet<short>("offset", false);
					if (cRetVal.nTopOffset == short.MaxValue)
						cRetVal.nTopOffset = 0;
                    cXmlNode = cNodeFont.NodeGet("color");
                    cRetVal.stColor = System.Drawing.Color.FromArgb(cXmlNode.AttributeGet<byte>("alpha"), cXmlNode.AttributeGet<byte>("red"), cXmlNode.AttributeGet<byte>("green"), cXmlNode.AttributeGet<byte>("blue"));
                    if (null != (cXmlNode = cNodeFont.NodeGet("border", false)))
                    {
                        cRetVal.nBorderWidth = cXmlNode.AttributeValueGet("width").ToFloat();
                        cXmlNode = cXmlNode.NodeGet("color");
                        cRetVal.stColorBorder = System.Drawing.Color.FromArgb(cXmlNode.AttributeGet<byte>("alpha"), cXmlNode.AttributeGet<byte>("red"), cXmlNode.AttributeGet<byte>("green"), cXmlNode.AttributeGet<byte>("blue"));
                    }
                    return cRetVal;
                }
            }
            public string sID;
            public bool bCuda;

            static public Item Parse(XmlNode cXmlNode)
            {
                Item cRetVal;
                switch(cXmlNode.AttributeValueGet("type", false))
                {
                    case "text":
                    case null:
                        cRetVal = Text.Parse(cXmlNode);
                        break;
                    default:
                        throw new Exception("uknown item type");
                }
                cRetVal.sID = cXmlNode.AttributeValueGet("id", false);
                cRetVal.bCuda = ("true" == cXmlNode.AttributeValueGet("cuda", false).ToLower());
                return cRetVal;
            }
        }
        public btl.Roll.Direction eDirection
        {
            get
            {
                return _eDirection;
            }
        }
        public float nSpeed
        {
            get
            {
                return _nSpeed;
            }
        }
        public Area stArea
        {
            get
            {
                return _stArea;
            }
        }
        public bool bRollCuda
        {
            get
            {
                return _bRollCuda;
            }
        }
        public ushort nLayer
        {
            get
            {
                return _nLayer;
            }
        }
        public byte nQueueLength
        {
            get
            {
                return _nQueueLength;
            }
        }
        public int nPause
        {
            get
            {
                return _nPause;
            }
        }


        public int nCheckInterval
        {
            get
            {
                return _nCheckInterval;
            }
        }
        public string sRequest { get; private set; }
        public byte nTemplate { get; private set; }
        public string sValue { get; private set; }

        [System.Runtime.CompilerServices.IndexerName("aItems")]
        public Item this[string sID]
        {
            get
            {
                Item cRetVal = _aItems.FirstOrDefault(o => sID == o.sID);
                if(null == cRetVal)
                    cRetVal = _aItems.FirstOrDefault(o => null == o.sID);
                return cRetVal;
            }
        }
     
        private btl.Roll.Direction _eDirection;
        private float _nSpeed;
        private Area _stArea;
        private bool _bRollCuda;
        private ushort _nLayer;
        private byte _nQueueLength;
        private int _nPause;

        private int _nCheckInterval;

        private List<Item> _aItems;

        public Preferences(string sData)
        {
            XmlDocument cXmlDocument = new XmlDocument();
            cXmlDocument.LoadXml(sData);
            XmlNode cXmlNode = cXmlDocument.NodeGet("data");
            sRequest = cXmlNode.AttributeValueGet("request");
            nTemplate = cXmlNode.AttributeGet<byte>("template");
            sValue = cXmlNode.AttributeValueGet("value", false);
            _nCheckInterval = cXmlNode.AttributeGet<int>("interval");

            XmlNode cNodeChild = cXmlNode.NodeGet("roll");
            _eDirection = cNodeChild.AttributeGet<btl.Roll.Direction>("direction");
            _nSpeed = cNodeChild.AttributeGet<float>("speed");
            _bRollCuda = cNodeChild.AttributeGet<bool>("cuda");
            _nLayer = cNodeChild.AttributeGet<ushort>("layer");
            _nQueueLength = cNodeChild.AttributeGet<byte>("queue");
			if (int.MaxValue == (_nPause = cNodeChild.AttributeGet<int>("pause", false)))
				_nPause = 0;
            cNodeChild = cNodeChild.NodeGet("area");
            _stArea = new Area(
                    cNodeChild.AttributeGet<short>("left"),
                    cNodeChild.AttributeGet<short>("top"),
                    cNodeChild.AttributeGet<ushort>("width"),
                    cNodeChild.AttributeGet<ushort>("height")
                );
            _aItems = cXmlNode.NodesGet("item").Select(o => Item.Parse(o)).ToList();
        }
    }
}