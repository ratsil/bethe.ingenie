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
        public class TextItem
        {
            public XmlNode cNode;
            public btl.Text cBTLText;
            public int nDuration;
            public int nTransDuration;
            public TextItem cNext;
            public TextItem cPrev;
        }
        class RollNode
        {
            public List<TextItem> aTextItems;
            public XmlNode cNodePL;
            private void AddItem(XmlNode cNode)
            {
                TextItem cTI = new TextItem();
                cTI.cNode = cNode;
                cTI.nDuration = cNode.AttributeGet<int>("duration");
                cTI.nTransDuration = cNode.AttributeGet<int>("trans_dur");
                cTI.cBTLText = new Text(cNode);
                cTI.cBTLText.nDuration = ulong.MaxValue;
                aTextItems.Add(cTI);
            }
            private void OrderItems()
            {
                for (int nI = 0; nI < aTextItems.Count(); nI++)
                {
                    if (nI == aTextItems.Count() - 1)
                        aTextItems[nI].cNext = aTextItems[0];
                    else
                        aTextItems[nI].cNext = aTextItems[nI + 1];

                    if (nI == 0)
                        aTextItems[nI].cPrev = aTextItems[aTextItems.Count() - 1];
                    else
                        aTextItems[nI].cPrev = aTextItems[nI - 1];
                }
            }
            public RollNode(XmlNode cNodeRoll)
            {
                aTextItems = new List<TextItem>();
                foreach (XmlNode cNodeChild in cNodeRoll.SelectNodes("effects/text"))
                {
                    AddItem(cNodeChild);
                    cNodeChild.ParentNode.RemoveChild(cNodeChild);
                }
                if (aTextItems.Count() <= 1)
                    throw new Exception("not enough text items. must be > 1 [count=" + aTextItems.Count() + "]");

                foreach (XmlNode cNodeChild in cNodeRoll.SelectNodes("effects/playlist"))
                {
                    if (cNodePL == null)
                    {
                        cNodePL = cNodeChild;
                    }
                    cNodeChild.ParentNode.RemoveChild(cNodeChild);
                }

                OrderItems();
            }
        }
        public TextItem TextItemGet(btl.Text cText)
        {
            return _aTextItems.FirstOrDefault(o => o.cBTLText == cText);
        }
        public btl.Roll cRoll { get { return _cRoll; } }
        public List<TextItem> aTextItems { get { return _aTextItems; } }
        public int nRollPrerenderQueueMax { get { return _nRollPrerenderQueueMax; } }
        public btl.Playlist cPlaylist { get { return _cPlaylist; }}

        private btl.Roll _cRoll;
        private btl.Playlist _cPlaylist;
        private List<TextItem> _aTextItems;
        private int _nRollPrerenderQueueMax;

        public Preferences(string sData)
        {
            try
            {
                XmlDocument cXmlDocument = new XmlDocument();
                cXmlDocument.LoadXml(sData);
                XmlNode cXmlNode = cXmlDocument.NodeGet("data");
                XmlNode cNodeRoll = cXmlNode.NodeGet("roll");

                _nRollPrerenderQueueMax = cNodeRoll.AttributeOrDefaultGet<int>("prerender_queue", 40);

                RollNode cRN = new RollNode(cNodeRoll);
                if (null != cRN.aTextItems.FirstOrDefault(o => o.nDuration < nRollPrerenderQueueMax * 2))
                    throw new Exception("duration of any text must be > " + nRollPrerenderQueueMax * 2);
                _aTextItems = cRN.aTextItems;
                _cPlaylist = new Playlist(cRN.cNodePL);
                _cRoll = new btl.Roll(cNodeRoll);
            }
            catch (Exception ex)
            {
                (new Logger()).WriteDebug("Preferences error: [error=" + ex.ToString() + "][source_xml=" + sData + "]");
                throw;
            }
        }
    }
}