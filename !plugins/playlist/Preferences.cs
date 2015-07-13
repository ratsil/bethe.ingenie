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
        public Area stArea
        {
            get
            {
                return _stArea;
            }
        }
        public bool bCuda
        {
            get
            {
                return _bCuda;
            }
        }
        public ushort nLayer
        {
            get
            {
                return _nLayer;
            }
        }

        private Area _stArea;
        private bool _bCuda;
        private ushort _nLayer;

        public Preferences(string sData)
        {
            XmlDocument cXmlDocument = new XmlDocument();
            cXmlDocument.LoadXml(sData);
            XmlNode cXmlNode = cXmlDocument.NodeGet("data");

            XmlNode cNodeChild = cXmlNode.NodeGet("playlist");
            _bCuda = cNodeChild.AttributeGet<bool>("cuda");
            _nLayer = cNodeChild.AttributeGet<ushort>("layer");
            cNodeChild = cNodeChild.NodeGet("area");
            _stArea = new Area(
                    cNodeChild.AttributeGet<short>("left"),
                    cNodeChild.AttributeGet<short>("top"),
                    cNodeChild.AttributeGet<ushort>("width"),
                    cNodeChild.AttributeGet<ushort>("height")
                );
        }
    }
}