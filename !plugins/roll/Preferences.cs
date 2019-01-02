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
				public ushort nWidthMax;

				static public Text Parse(XmlNode cXmlNode)
                {
                    Text cRetVal = new Text();
                    XmlNode cNodeFont = cXmlNode.NodeGet("font");
                    cRetVal.cFont = new System.Drawing.Font(cNodeFont.AttributeValueGet("name"), cNodeFont.AttributeGet<int>("size"), cNodeFont.AttributeGet<System.Drawing.FontStyle>("style"));
					cRetVal.nWidthMax = cNodeFont.AttributeValueGet("width_max", false) == null ? ushort.MaxValue : cNodeFont.AttributeGet<ushort>("width_max");
					cRetVal.nTopOffset = cNodeFont.AttributeOrDefaultGet<short>("offset", 0);
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
            public MergingMethod stMerging;

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
                cRetVal.stMerging = new MergingMethod(cXmlNode);
                return cRetVal;
            }
        }
		public class Background
		{
			public string sIn;
			public string sLoop;
			public string sOut;
			public string sMaskIn;
			public string sMaskLoop;
			public string sMaskOut;
			public string sMaskAllOff;
			public Area stArea;
			public Background(XmlNode cXmlNode)
			{
				XmlNode cNodeChild;
				cNodeChild = cXmlNode.NodeGet("back_in");
				sIn = cNodeChild.AttributeValueGet("folder");
				cNodeChild = cXmlNode.NodeGet("back_loop");
				sLoop = cNodeChild.AttributeValueGet("folder");
				cNodeChild = cXmlNode.NodeGet("back_out");
				sOut = cNodeChild.AttributeValueGet("folder");
				cNodeChild = cXmlNode.NodeGet("mask_in", false);
				if (cNodeChild != null)
				{
					sMaskIn = cNodeChild.AttributeValueGet("folder");
					cNodeChild = cXmlNode.NodeGet("mask_loop");
					sMaskLoop = cNodeChild.AttributeValueGet("folder");
					cNodeChild = cXmlNode.NodeGet("mask_out");
					sMaskOut = cNodeChild.AttributeValueGet("folder");
					cNodeChild = cXmlNode.NodeGet("mask_all_off");
					sMaskAllOff = cNodeChild.AttributeValueGet("folder");
				}
				cNodeChild = cXmlNode.NodeGet("area");
				stArea = new Area(
						cNodeChild.AttributeGet<short>("left"),
						cNodeChild.AttributeGet<short>("top"),
						cNodeChild.AttributeGet<ushort>("width"),
						cNodeChild.AttributeGet<ushort>("height")
					);
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
        public MergingMethod stRollMerging
        {
            get
            {
                return _stRollMerging;
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
		public int nDelay
		{
			get
			{
				return _nDelay;
			}
		}


		public int nCheckInterval
        {
            get
            {
                return _nCheckInterval;
            }
        }
		public int nLoops
		{
			get
			{
				return _nLoops;
			}
		}
		public Background cBackground
		{
			get
			{
				return _cBackground;
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
        private MergingMethod _stRollMerging;
        private ushort _nLayer;
        private byte _nQueueLength;
        private int _nPause;
		private int _nDelay;

		private int _nCheckInterval;
		private ushort _nLoops;

		private List<Item> _aItems;
		private Background _cBackground;

		public Preferences(string sData)
        {
            XmlDocument cXmlDocument = new XmlDocument();
            cXmlDocument.LoadXml(sData);
            XmlNode cXmlNode = cXmlDocument.NodeGet("data");
            sRequest = cXmlNode.AttributeValueGet("request");
            nTemplate = cXmlNode.AttributeGet<byte>("template");
            sValue = cXmlNode.AttributeValueGet("value", false);
            _nCheckInterval = cXmlNode.AttributeGet<int>("interval");
			_nLoops = null == cXmlNode.AttributeValueGet("loop", false) ? (ushort)0 : cXmlNode.AttributeGet<ushort>("loop");

			XmlNode cNodeChild = cXmlNode.NodeGet("roll");
            _eDirection = cNodeChild.AttributeGet<btl.Roll.Direction>("direction");
            _nSpeed = cNodeChild.AttributeGet<float>("speed");
            _stRollMerging = new MergingMethod(cNodeChild);
            _nLayer = cNodeChild.AttributeGet<ushort>("layer");
            _nQueueLength = cNodeChild.AttributeGet<byte>("queue");
			_nPause = cNodeChild.AttributeOrDefaultGet<int>("pause", 0);
			_nDelay = cNodeChild.AttributeOrDefaultGet<int>("delay", 0);
			cNodeChild = cNodeChild.NodeGet("area");
            _stArea = new Area(
                    cNodeChild.AttributeGet<short>("left"),
                    cNodeChild.AttributeGet<short>("top"),
                    cNodeChild.AttributeGet<ushort>("width"),
                    cNodeChild.AttributeGet<ushort>("height")
                );
            _aItems = cXmlNode.NodesGet("item").Select(o => Item.Parse(o)).ToList();

			if (null != (cNodeChild = cXmlNode.NodeGet("background", false)))
			{
				_cBackground = new Background(cNodeChild);
			}
        }
    }
}