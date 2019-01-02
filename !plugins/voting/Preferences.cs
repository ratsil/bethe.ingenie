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
using System.IO;

namespace ingenie.plugins
{
    class Preferences
    {
		public class Poll
		{
            public int nMidGap;
            public string sOuterXMLMidRoll;
            public class Candidate
			{
				public string sName;
				public string sImage;
				public string sDescription;
                private int _nVotesQtyNew;
                private int _nVotesQtyOld;
                public int nVotesQtyOld
                {
                    get
                    {
                        return _nVotesQtyOld;
                    }
                }
                public int nVotesQtyNew
                {
                    get
                    {
                        return _nVotesQtyNew;
                    }
                    set
                    {
                        _nVotesQtyOld = _nVotesQtyNew;
                        _nVotesQtyNew = value;
                    }
                }
                public Candidate(XmlNode cNode)
                {
                    sName = cNode.AttributeValueGet("name").ToLower();
                    sImage = cNode.AttributeValueGet("image");
                    sDescription = cNode.AttributeValueGet("description");
                    _nVotesQtyOld = -1;
                    _nVotesQtyNew = -1;
                }
            }
            public string sName;
			public DateTime dtLast;
			public Candidate[] aCandidates;
			public string[] aDescription;
            public string[] aVotesNew
            {
                get
                {
                    if (aCandidates[0].nVotesQtyNew < 0 || aCandidates[1].nVotesQtyNew < 0)
                        return new string[2] { "", "" };
                    float nSum = aCandidates[0].nVotesQtyNew + aCandidates[1].nVotesQtyNew;
                    return new string[2] { (nSum == 0 ? 0 : (aCandidates[0].nVotesQtyNew * 100f / nSum)).ToString("0.0") + "%", (nSum == 0 ? 0 : (aCandidates[1].nVotesQtyNew * 100f / nSum)).ToString("0.0") + "%" };
                }
            }
            public string[] aVotesOld
            {
                get
                {
                    if (aCandidates[0].nVotesQtyOld < 0 || aCandidates[1].nVotesQtyOld < 0)
                        return new string[2] { "", "" };
                    float nSum = aCandidates[0].nVotesQtyOld + aCandidates[1].nVotesQtyOld;
                    return new string[2] { (aCandidates[0].nVotesQtyOld * 100f / nSum).ToString("0.0") + "%", (aCandidates[1].nVotesQtyOld * 100f / nSum).ToString("0.0") + "%" };
                }
            }
            public btl.Roll NewRollMidGet()
            {
                XmlDocument cXmlDocument = new XmlDocument();
                string[] aOld = aVotesOld, aNew = aVotesNew;
                string sOuterXML = sOuterXMLMidRoll.Replace("%%MID_TXT_L_OLD%%", aOld[0]);
                sOuterXML = sOuterXML.Replace("%%MID_TXT_R_OLD%%", aOld[1]);
                sOuterXML = sOuterXML.Replace("%%MID_TXT_L_NEW%%", aNew[0]);
                sOuterXML = sOuterXML.Replace("%%MID_TXT_R_NEW%%", aNew[1]);
                cXmlDocument.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n" + sOuterXML);
                List<XmlNode> aToDelete = new List<XmlNode>();
                XmlNode cXmlEff = cXmlDocument.NodeGet("roll/effects");
                foreach (XmlNode cXml in cXmlEff.ChildNodes)
                {
                    switch (cXml.AttributeValueGet("name"))
                    {
                        case "mid_left_old":
                            if (aOld[0] == "")
                                aToDelete.Add(cXml);
                            break;
                        case "mid_right_old":
                            if (aOld[1] == "")
                                aToDelete.Add(cXml);
                            break;
                        case "mid_left_new":
                            if (aNew[0] == "")
                                aToDelete.Add(cXml);
                            break;
                        case "mid_right_new":
                            if (aNew[1] == "")
                                aToDelete.Add(cXml);
                            break;
                    }
                }
                foreach (XmlNode cXml in aToDelete)
                    cXmlEff.RemoveChild(cXml);
                Roll cRetVal= new Roll(cXmlDocument.NodeGet("roll"));
                Text cMLeftO = (Text)cRetVal.EffectGet("mid_left_old");
                Text cMLeftN = (Text)cRetVal.EffectGet("mid_left_new");
                Text cMRightO = (Text)cRetVal.EffectGet("mid_right_old");
                Text cMRightN = (Text)cRetVal.EffectGet("mid_right_new");
                int nWL = (cRetVal.stArea.nWidth - nMidGap) / 2;
                if (null != cMLeftO)
                    cMLeftO.cDock.cOffset.nLeft += (short)((nWL - cMLeftO.stArea.nWidth) / 2);
                cMLeftN.cDock.cOffset.nLeft += (short)((nWL - cMLeftN.stArea.nWidth) / 2);
                if (null != cMRightO)
                    cMRightO.cDock.cOffset.nLeft += (short)(nWL + nMidGap + (nWL - cMRightO.stArea.nWidth) / 2);
                cMRightN.cDock.cOffset.nLeft += (short)(nWL + nMidGap + (nWL - cMRightN.stArea.nWidth) / 2);
                return cRetVal;
            }
            public Poll(XmlNode cNode)
            {
                sName = cNode.AttributeValueGet("name", false);
                dtLast = cNode.AttributeOrDefaultGet<DateTime>("dt", DateTime.MinValue);

                aDescription = cNode.NodeGet("description").InnerText.Remove("\r").Split('\n').Select(o => o.Trim()).Where(o => !o.IsNullOrEmpty()).ToArray();
                if (aDescription.Length < 4)
                    throw new Exception("there are only [count=" + aDescription.Length + "] strings in description! Must be 4");
                else if (aDescription.Length > 4)
                    throw new Exception("there are too many [count=" + aDescription.Length + "] strings in description! Must be 4");

                aCandidates = cNode.NodesGet("candidate").Select(o => new Candidate(o)).ToArray();
                if (aCandidates.Length < 2)
                    throw new Exception("there are only [count=" + aCandidates.Length + "] candidates! Must be 2");
                else if (aCandidates.Length > 2)
                    throw new Exception("there are too many [count=" + aCandidates.Length + "] candidates! Must be 2");
            }
            public bool IsVotesPercentageChanged(int nLeft, int nRight)
            {
                float nSum = nLeft + nRight;
                string sLeft = (nLeft * 100f / nSum).ToString("0.0") + "%";
                string sRight = (nRight * 100f / nSum).ToString("0.0") + "%";
                string[] aVotesCurrent = aVotesNew;
                return sLeft != aVotesCurrent[0] || sRight != aVotesCurrent[1];
            }
        }

        public Poll cPoll { get { return _cPoll; } }
        public TimeSpan tsUpdateInterval { get { return _tsUpdateInterval; } }
        public int nRollPrerenderQueueMax { get { return _nRollPrerenderQueueMax; } }
        public btl.Roll cRollImages { get { return _cRollImages; } }
        public btl.Roll cRollTop { get { return _cRollTop; } }
        public btl.Roll cRollBot { get { return _cRollBot; } }
        public btl.Roll cRollMid { get { return _cRollMid; } }
        public int nImagesLoopDur { get { return _nImagesLoopDur; } }
        public int nImagesInterval { get { return _nImagesInterval; } }
        public int nTopLoopDur { get { return _nTopLoopDur; } }
        public int nBotBlueWindowWidth { get { return _nBotBlueWindowWidth; } }

        public btl.Roll _cRollImages;
        public btl.Roll _cRollTop;
        public btl.Roll _cRollBot;
        public btl.Roll _cRollMid;
        private string _sWorkFolder;
		private Poll _cPoll;
        private string _sRequest;
        private byte _nTemplate;
        private TimeSpan _tsUpdateInterval;
        private int _nRollPrerenderQueueMax;
        private int _nTextInTopGap;
        private int _nTopLoopDur;
        private int _nImagesLoopDur;
        private int _nImagesInterval;
        private int _nBotBlueWindowWidth;
        private Dock.Offset _cGlobalOffset;

        public Preferences(string sData)
        {
            try
            {
                XmlDocument cXmlDocument = new XmlDocument();
                cXmlDocument.LoadXml(sData);
                XmlNode cXmlNode = cXmlDocument.NodeGet("data");
                _sRequest = cXmlNode.AttributeOrDefaultGet<string>("request", "polls.zed");
                _nTemplate = cXmlNode.AttributeOrDefaultGet<byte>("template", 0);
                _sWorkFolder = cXmlNode.AttributeGet<string>("work_folder");
                if (!System.IO.Directory.Exists(_sWorkFolder))
                    throw new Exception("work foldeer doesn't exist! [folder=" + _sWorkFolder + "]");
                _tsUpdateInterval = cXmlNode.AttributeOrDefaultGet<TimeSpan>("update_interval", new TimeSpan(0, 0, 10));
                _nRollPrerenderQueueMax = cXmlNode.AttributeOrDefaultGet<int>("render_queue", 30);
                _cPoll = new Poll(cXmlNode.NodeGet("poll"));

                _cGlobalOffset = new Dock.Offset(cXmlNode.NodeGet("offset"));

                string sName, sOuterXML, sHeadXML = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n";
                XmlNode cNodeRoll;
                foreach (XmlNode cNode in cXmlNode.NodesGet("roll"))
                {
                    sName = cNode.AttributeValueGet("name");

                    cNodeRoll = cNode;
                    switch (sName)
                    {
                        case "images":
                            cXmlDocument = new XmlDocument();
                            sOuterXML = cNode.OuterXml.Replace("%%IMG_LEFT%%", System.IO.Path.Combine(_sWorkFolder, _cPoll.aCandidates[0].sImage));
                            sOuterXML = sOuterXML.Replace("%%IMG_RIGHT%%", System.IO.Path.Combine(_sWorkFolder, _cPoll.aCandidates[1].sImage));
                            cXmlDocument.LoadXml(sHeadXML + sOuterXML);
                            cNodeRoll = cXmlDocument.NodeGet("roll");
                            _nImagesLoopDur = cNodeRoll.AttributeOrDefaultGet<int>("_loop_dur", 75);
                            _nImagesInterval = cNodeRoll.AttributeOrDefaultGet<int>("_interval", 600);
                            _cRollImages = new Roll(cNodeRoll);
                            _cRollImages.stArea = _cRollImages.stArea.Move(_cGlobalOffset);
                            break;
                        case "top":
                            cXmlDocument = new XmlDocument();
                            sOuterXML = cNode.OuterXml.Replace("%%TOP_TXT1%%", _cPoll.aDescription[0]);
                            sOuterXML = sOuterXML.Replace("%%TOP_TXT2_1%%", _cPoll.aDescription[1]);
                            sOuterXML = sOuterXML.Replace("%%TOP_TXT2_2%%", _cPoll.aDescription[2]);
                            sOuterXML = sOuterXML.Replace("%%TOP_TXT2_3%%", _cPoll.aDescription[3]);
                            cXmlDocument.LoadXml(sHeadXML + sOuterXML);
                            cNodeRoll = cXmlDocument.NodeGet("roll");
                            _nTextInTopGap = cNodeRoll.AttributeOrDefaultGet<int>("_text_in_top_gap", 30);
                            _nTopLoopDur = cNodeRoll.AttributeOrDefaultGet<int>("_loop_dur", 200);
                            _cRollTop = new Roll(cNodeRoll);
                            Text cInit = (Text)_cRollTop.EffectGet("top_init");
                            cInit.cDock.cOffset.nLeft += (short)((_cRollTop.stArea.nWidth - cInit.stArea.nWidth) / 2);
                            Text cLeft = (Text)_cRollTop.EffectGet("top_left");
                            Text cMid = (Text)_cRollTop.EffectGet("top_mid");
                            Text cRight = (Text)_cRollTop.EffectGet("top_right");
                            int nWTotal = cLeft.stArea.nWidth + cMid.stArea.nWidth + cRight.stArea.nWidth + 2 * _nTextInTopGap;
                            short nLeft = (short)((_cRollTop.stArea.nWidth - nWTotal) / 2);
                            cLeft.cDock.cOffset.nLeft += nLeft;
                            cMid.cDock.cOffset.nLeft += (short)(nLeft + cLeft.stArea.nWidth + _nTextInTopGap);
                            cRight.cDock.cOffset.nLeft += (short)(nLeft + cLeft.stArea.nWidth + cMid.stArea.nWidth + 2 * _nTextInTopGap);
                            _cRollTop.stArea = _cRollTop.stArea.Move(_cGlobalOffset);
                            break;
                        case "bot":
                            cXmlDocument = new XmlDocument();
                            sOuterXML = cNode.OuterXml.Replace("%%BOT_TXT_L%%", _cPoll.aCandidates[0].sDescription);
                            sOuterXML = sOuterXML.Replace("%%BOT_TXT_R%%", _cPoll.aCandidates[1].sDescription);
                            cXmlDocument.LoadXml(sHeadXML + sOuterXML);
                            cNodeRoll = cXmlDocument.NodeGet("roll");
                            _nBotBlueWindowWidth = cNodeRoll.AttributeOrDefaultGet<int>("_blue_window_width", 372);
                            _cRollBot = new Roll(cNodeRoll);
                            Text cBLeft = (Text)_cRollBot.EffectGet("bot_left");
                            Text cBRight = (Text)_cRollBot.EffectGet("bot_right");
                            cBLeft.cDock.cOffset.nLeft += (short)((_nBotBlueWindowWidth - cBLeft.stArea.nWidth) / 2);
                            cBRight.cDock.cOffset.nLeft += (short)((_nBotBlueWindowWidth - cBRight.stArea.nWidth) / 2);
                            _cRollBot.stArea = _cRollBot.stArea.Move(_cGlobalOffset);
                            break;
                        case "mid":
                            PollUpdate();
                            _cPoll.nMidGap = cNode.AttributeOrDefaultGet<int>("_mid_gap", 46);
                            _cPoll.sOuterXMLMidRoll = cNode.OuterXml;
                            _cRollMid = _cPoll.NewRollMidGet();
                            _cRollMid.stArea = _cRollMid.stArea.Move(_cGlobalOffset);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                (new Logger()).WriteNotice("Preferences error: [error=" + ex.ToString() + "][source_xml=" + sData + "]");
                throw;
            }
        }

        public bool PollUpdate()
		{
            XmlNode cVotingData = Data.Get(_sRequest, _nTemplate, _cPoll.sName);
            string sName;
            int nLeft = -1, nRight = -1;
            bool bRetVal = false;
            foreach (XmlNode cNode in cVotingData.NodesGet("item"))
            {
                sName = cNode.AttributeValueGet("name");
                if (sName == _cPoll.aCandidates[0].sName)
                    nLeft = cNode.AttributeGet<int>("votes");
                else if (sName == _cPoll.aCandidates[1].sName)
                    nRight = cNode.AttributeGet<int>("votes");
            }
            if (nLeft < 0 || nRight < 0)
            {
                (new Logger()).WriteError("не для обоих кандидатов получены голоса [left=" + nLeft + "][right=" + nRight + "]<br>" + cVotingData.OuterXml);
                if (_cPoll.aCandidates[0].nVotesQtyNew < 0 || _cPoll.aCandidates[1].nVotesQtyNew < 0)
                    throw new Exception("при старте голосования не удалось получить голоса для обоих кандидатов");
            }
            else
            {
                bRetVal = _cPoll.IsVotesPercentageChanged(nLeft, nRight);
                if (bRetVal)
                {
                    _cPoll.aCandidates[0].nVotesQtyNew = nLeft;
                    _cPoll.aCandidates[1].nVotesQtyNew = nRight;
                    (new Logger()).WriteDebug("votes updated: [left=" + nLeft + "][right=" + nRight + "]");
                }
            }
            return bRetVal;
		}
        /* cVotingData:
            <item name="odin" votes="1092" />
            <item name="odin1" votes="500" />
            <item name="dva" votes="790" />
            <item name="dva1" votes="390" />
         */
    }
}
