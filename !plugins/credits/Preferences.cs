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
        class RollNode
        {
            class BackMaskItem
            {
                public XmlNode cNodeBack;
                public XmlNode cNodeMask;
                public int nTextLengthMax;
            }
            private Dictionary<int, BackMaskItem> ahItems = new Dictionary<int, BackMaskItem>();
            private int nTextMaxFound;
            private BackMaskItem cGoodSizedItem;
            private XmlNode cNodeRoll;
            public RollNode(XmlNode cNodeRoll)
            {
                this.cNodeRoll = cNodeRoll;
            }
            public void MakeNodeRollFitInSize()
            {
                MaxTextLengthSearch();
                ItemsLoad();
                GoodSizedItemGet();
                WrongItemsRemove();
            }
            private void MaxTextLengthSearch()
			{
				string sText;
				btl.Text cBTLText;
				nTextMaxFound = 0;
				foreach (XmlNode cNodeChild in cNodeRoll.SelectNodes("effects/text"))
				{
					sText = cNodeChild.NodeGet("value").FirstChild.Value;
					cBTLText = new BTL.Play.Text(cNodeChild);
					if (nTextMaxFound < cBTLText.stArea.nWidth)
						nTextMaxFound = cBTLText.stArea.nWidth;
				}
			}
            private void ItemsLoad()
			{
				string sName;
				int nI;
				foreach (XmlNode cNodeChild in cNodeRoll.SelectNodes("effects/animation"))
				{
					if (null != (sName = cNodeChild.AttributeValueGet("name", false)) && sName.StartsWith("back_"))
					{
						nI = sName.Substring(5, 1).ToInt();
						if (!ahItems.ContainsKey(nI))
							ahItems.Add(nI, new BackMaskItem());
						ahItems[nI].cNodeBack = cNodeChild;
						ahItems[nI].nTextLengthMax = cNodeChild.AttributeGet<int>("text_length");
					}
					if (null != (sName = cNodeChild.AttributeValueGet("name", false)) && sName.StartsWith("mask_"))
					{
						nI = sName.Substring(5, 1).ToInt();
						if (!ahItems.ContainsKey(nI))
							ahItems.Add(nI, new BackMaskItem());
						ahItems[nI].cNodeMask = cNodeChild;
					}
				}
			}
            private void GoodSizedItemGet()
			{
                cGoodSizedItem = null;
				foreach (BackMaskItem cBG in ahItems.Values.OrderBy(o => o.nTextLengthMax))
				{
                    cGoodSizedItem = cBG;
					if (cBG.nTextLengthMax > nTextMaxFound)
						break;
				}
			}
            private void WrongItemsRemove()
            {
                XmlNode cNodeEffects = cNodeRoll.NodeGet("effects");
                foreach (BackMaskItem cI in ahItems.Values)
                    if (cI.nTextLengthMax != cGoodSizedItem.nTextLengthMax)
                    {
                        //sErrorLog += "[length="+ cI.nTextLengthMax + "][good_length"+ cGoodSizedItem.nTextLengthMax + "]<br>[back="+ cI.cNodeBack.OuterXml + "]<br>[mask=" + cI.cNodeMask.OuterXml + "]<br>";
                        //xNode.ParentNode.RemoveChild(xNode);
                        cNodeEffects.RemoveChild(cI.cNodeBack);
                        cNodeEffects.RemoveChild(cI.cNodeMask);
                    }
                foreach (XmlNode cNodeChild in cNodeEffects.SelectNodes("text"))
                {
                    cNodeChild.Attributes["width_max"].Value = cGoodSizedItem.nTextLengthMax.ToString();
                }
            }
        }

        public btl.Roll cRoll { get { return _cRoll; } }

		private btl.Roll _cRoll;

        public Preferences(string sData)
        {
            try
            {
                XmlDocument cXmlDocument = new XmlDocument();
                cXmlDocument.LoadXml(sData);
                XmlNode cXmlNode = cXmlDocument.NodeGet("data");
                XmlNode cNodeRoll = cXmlNode.NodeGet("roll");

                RollNode cRN = new RollNode(cNodeRoll);
                cRN.MakeNodeRollFitInSize();

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