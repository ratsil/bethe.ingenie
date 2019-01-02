using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using BTL.Play;
using helpers;
using System.Xml;
using helpers.extensions;
using System.Text;

namespace ingenie.plugins
{
    class Preferences
    {
        public Roll.Direction eDirection
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
        public Uri cRSS
        {
            get
            {
                return _cRSS;
            }
        }
        public string sTag { get; private set; }
        public Encoding cEncoding { get; private set; }
        public string sSplitter { get; private set; }
        public string sSeparator { get; private set; }

        public bool bTextCuda
        {
            get
            {
                return _bTextCuda;
            }
        }
        public System.Drawing.Font cFont
        {
            get
            {
                return _cFont;
            }
        }
        public System.Drawing.Color stColor
        {
            get
            {
                return _stColor;
            }
        }
        public System.Drawing.Color stColorBorder
        {
            get
            {
                return _stColorBorder;
            }
        }
        public float nBorderWidth
        {
            get
            {
                return _nBorderWidth;
            }
        }

        private Roll.Direction _eDirection;
        private float _nSpeed;
        private Area _stArea;
        private bool _bRollCuda;
        private ushort _nLayer;
        private byte _nQueueLength;
        private int _nPause;

        private Uri _cRSS;
        private int _nCheckInterval;

        private bool _bTextCuda;
        private System.Drawing.Font _cFont;
        private System.Drawing.Color _stColor;
        private System.Drawing.Color _stColorBorder;
        private float _nBorderWidth;

        public Preferences(string sData)
        {
            XmlDocument cXmlDocument = new XmlDocument();
            cXmlDocument.LoadXml(sData);
            XmlNode cXmlNode = cXmlDocument.NodeGet("data");
            _cRSS = new Uri(cXmlNode.AttributeValueGet("uri"));
            if (null == (sSplitter = cXmlNode.AttributeValueGet("splitter", false)))
                sSplitter = "";
            if (null == (sSeparator = cXmlNode.AttributeValueGet("separator", false)))
                sSeparator = "";
            _nCheckInterval = cXmlNode.AttributeGet<int>("interval");
            if (null == (sTag = cXmlNode.AttributeValueGet("tag", false)))
                sTag = "title";
            string sEncoding = cXmlNode.AttributeValueGet("encoding", false);
            if (null == sEncoding)
                sEncoding = "utf-8";
            cEncoding = Encoding.GetEncoding(sEncoding);

            XmlNode cNodeChild = cXmlNode.NodeGet("roll");
            _eDirection = cNodeChild.AttributeGet<Roll.Direction>("direction");
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

            cNodeChild = cXmlNode.NodeGet("text");
            _bTextCuda = cNodeChild.AttributeGet<bool>("cuda");
            XmlNode cNodeFont = cNodeChild.NodeGet("font");
            _cFont = new System.Drawing.Font(cNodeFont.AttributeValueGet("name"), cNodeFont.AttributeGet<int>("size"), cNodeFont.AttributeGet<System.Drawing.FontStyle>("style"));
            cNodeChild = cNodeFont.NodeGet("color");
            _stColor = System.Drawing.Color.FromArgb(cNodeChild.AttributeGet<byte>("alpha"), cNodeChild.AttributeGet<byte>("red"), cNodeChild.AttributeGet<byte>("green"), cNodeChild.AttributeGet<byte>("blue"));
            cNodeChild = cNodeFont.NodeGet("border");
            _nBorderWidth = cNodeChild.AttributeValueGet("width").ToFloat();
            cNodeChild = cNodeChild.NodeGet("color");
            _stColorBorder = System.Drawing.Color.FromArgb(cNodeChild.AttributeGet<byte>("alpha"), cNodeChild.AttributeGet<byte>("red"), cNodeChild.AttributeGet<byte>("green"), cNodeChild.AttributeGet<byte>("blue"));
        }
    }
}