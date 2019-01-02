using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using helpers;
using helpers.extensions;
using System.Drawing;

namespace ingenie.plugins
{
	public class Preferences
	{
        public enum BroadcastType
        {
            linear = 0,
            live = 1
        }
        public class SMS
        {
            public string sSmilesFolder;
            public string sFlagsFolder;
            private Font _cFont;
            public Font cFont
            {
                get { return new Font(_cFont.FontFamily, _cFont.Size, _cFont.Style); }
                set { _cFont = new Font(value.FontFamily, value.Size, value.Style); }
            }
            public Color stColor;
            public float nBorderWidth;
            public Color stBorderColor;
			public bool bToUpper;
			public byte nOpacity;
			public byte nInDissolve;
			public short nShiftTop;
			public short nPressBottom;
			public short nCenterShift;
			public short nLineSpace;
			public MergingMethod stMerging;
		}
        public class Roll
        {
            private int nMinUndisplayedMessages = 10;
            private int nMaxUndisplayedMessages = 100;
            private float nMinSecondsPerLine = 2.0f;  // in sec
            public ushort nLayer;
            public MergingMethod stMerging;
            public float nSpeed;
            private float _nSecondsPerLine;
            public float nSecondsPerLine
            {
                get
                {
                    if (nUndisplayedMessages <= nMinUndisplayedMessages)
                        return _nSecondsPerLine;
                    if (nUndisplayedMessages >= nMaxUndisplayedMessages)
                        return nMinSecondsPerLine;
                    if (_nSecondsPerLine <= nMinSecondsPerLine)
                        return _nSecondsPerLine;

                    return nMinSecondsPerLine + (nMaxUndisplayedMessages - nUndisplayedMessages) * (_nSecondsPerLine - nMinSecondsPerLine) / (nMaxUndisplayedMessages - nMinUndisplayedMessages);
                }
            }
            public float nSecondsPerPause;
            public string sMaskPageSingle;
            public string sMaskPageMulti;
            public Rectangle stSize;
            public SMS cSMSCommon;
            public SMS cSMSVIP;
            public SMS cSMSPromo;
            public SMS cSMSPhoto;
            public void SecondsPerLineSet(float nSeconds)
            {
                _nSecondsPerLine = nSeconds;
            }
        }
        public class Crawl
        {
            public Rectangle stSize;
            public float nSpeed;
            public MergingMethod stMerging;
            public Font cFont;
            public Color stColor;
            public Color stBorderColor;
            public float nBorderWidth;
            public ushort nLayer;
			public short nShiftTop;
			public short nPressBottom;
			public bool bRenderFields;
			public string sText;
            public string sRegistryInfo;
            public byte[] aStartPoints;
            public string sInfoStringNight = "";
            public string sCrowlLastStart = "";
        }
        public class Mat
        {
            public ushort nBadgeLayer;
            public Rectangle stBadgeSize;
            public string sBadgeIn;
            public string sBadgeLoop;
            public string sBadgeOut;
            public ushort nShowLayer;
            public Rectangle stShowSize;
            public string sShowLoop;
            public string sShowOut;
            public string sShowTransition;
            public string sStandbyIn;
            public string sStandbyLoop;
            public string sStandbyOut;
            public string sStandbyTransition;
			public ushort nOutLayer;
			public Rectangle stOutSize;
			public string sOut;
        }
        public class Promo
        {
            public int nID;
            public DateTime dtLastShow;
            public List<WeeklyRange> aWeeklyRange;
            public bool bEnabled;
            public string sText;
        }
        public class VIP
        {
            public TimeSpan tsPromoPeriod;
            public List<Promo> aPromos;
            public string sFile;
            public Font cFont;
            public string sPrefix;
        }

        private string _sWorkFolder;
        static public ushort nMessagesQty
        {
            get
            {
                return 7;
            }
        }
        static public int nUndisplayedMessages;
        public bool bMessagesRelease;
        public BroadcastType eBroadcastType;
        public int nSMSQtty;
        public Crawl cCrawl;
        public Roll cRoll;
        public Mat cMat;
        public VIP cVIP;

        public string sPhotoPrefix = "PHOTO";
        static public DB.Credentials cDBCredentials;

        public Preferences(string sWorkFolder, string sData)
        {
            _sWorkFolder = sWorkFolder;

            XmlDocument cXmlDocument = new XmlDocument();
            cXmlDocument.LoadXml(sData);
            XmlNode cXmlNode = cXmlDocument.NodeGet("data");

            cDBCredentials = new DB.Credentials(cXmlNode.NodeGet("database"));

            bMessagesRelease = cXmlNode.AttributeOrDefaultGet<bool>("release", false);
            eBroadcastType = cXmlNode.AttributeOrDefaultGet<BroadcastType>("type", BroadcastType.linear);
            nSMSQtty = cXmlNode.AttributeGet<int>("queue");

			XmlNode cNodeChild = cXmlNode.NodeGet("vip");
            if (null != cNodeChild)
            {
                cVIP = new VIP();
                XmlNode cXNPromos = cNodeChild.NodeGet("promos", false);
                if (null != cXNPromos)
                {
                    cVIP.tsPromoPeriod = cXNPromos.AttributeGet<TimeSpan>("period");
                    cVIP.aPromos = new List<Promo>();
                    List<Promo> aPromos = new List<Promo>();
                    List<WeeklyRange> aWeeklyRanges;
                    foreach (XmlNode cPromo in cXNPromos.NodesGet("promo", false))
                    {
                        aWeeklyRanges = new List<WeeklyRange>();
                        foreach (XmlNode cWeeklyRange in cPromo.NodesGet("weeklyrange", false))
                            aWeeklyRanges.Add(new WeeklyRange(
                                cWeeklyRange.AttributeValueGet("dayin"),
                                cWeeklyRange.AttributeValueGet("timein"),
                                cWeeklyRange.AttributeValueGet("dayout"),
                                cWeeklyRange.AttributeValueGet("timeout")
                                ));
                        aPromos.Add(new Promo()
                        {
                            nID = cPromo.InnerXml.GetHashCode(),
                            aWeeklyRange = aWeeklyRanges,
                            bEnabled = cPromo.AttributeGet<bool>("enabled"),
                            sText = cPromo.NodeGet("text").InnerXml
                        });
                    }
                    cVIP.aPromos.AddRange(aPromos);
                }
                cVIP.sFile = System.IO.Path.Combine(_sWorkFolder, "data/vip.dat");
                cVIP.sPrefix = "VIP";
                cVIP.cFont = FontParse(cNodeChild.NodeGet("font"));
            }
            #region . ROLL .
            cNodeChild = cXmlNode.NodeGet("roll");
            cRoll = new Roll();
            cRoll.nLayer = cNodeChild.AttributeGet<ushort>("layer");
			cRoll.nSpeed = cNodeChild.AttributeGet<float>("speed");
			cRoll.stMerging = new MergingMethod(cNodeChild);
            XmlNode cXNGrandChild = cNodeChild.NodeGet("holds");
            cRoll.SecondsPerLineSet(cXNGrandChild.AttributeGet<float>("line"));
            cRoll.nSecondsPerPause = cXNGrandChild.AttributeGet<float>("pause");
            cXNGrandChild = cNodeChild.NodeGet("masks");
            cRoll.sMaskPageSingle = cXNGrandChild.NodeGet("single").InnerXml;
            cRoll.sMaskPageMulti = cXNGrandChild.NodeGet("multi").InnerXml;
            cRoll.stSize = SizeParse(cNodeChild.NodeGet("size"));

            cRoll.cSMSCommon = RollCategoryGet(cNodeChild.NodeGet("common"));
            cRoll.cSMSVIP = RollCategoryGet(cNodeChild.NodeGet("vip"));
            cRoll.cSMSPromo = RollCategoryGet(cNodeChild.NodeGet("promo"));
            cRoll.cSMSPhoto = RollCategoryGet(cNodeChild.NodeGet("photo"));
            #endregion
            #region . CRAWL .
            cNodeChild = cXmlNode.NodeGet("crawl");
            cCrawl = new Crawl();
            cCrawl.sInfoStringNight = System.IO.Path.Combine(sWorkFolder, "data/info_night.dat");
            cCrawl.sCrowlLastStart = System.IO.Path.Combine(sWorkFolder, "data/crowl_last_start.dat"); ;
			cCrawl.stMerging = new MergingMethod(cNodeChild);
			cCrawl.nSpeed = cNodeChild.AttributeGet<float>("speed");
            cCrawl.nLayer = cNodeChild.AttributeGet<ushort>("layer");
			cCrawl.nShiftTop = cXmlNode.AttributeOrDefaultGet<short>("shift_top", 0);
			cCrawl.nPressBottom = cXmlNode.AttributeOrDefaultGet<short>("press_bot", 0);
			cCrawl.bRenderFields = cXmlNode.AttributeOrDefaultGet<bool>("render_fields", false);
			cCrawl.stSize = SizeParse(cNodeChild.NodeGet("size"));
            cCrawl.sText = cNodeChild.NodeGet("text").InnerXml;
            cCrawl.sRegistryInfo = cNodeChild.NodeGet("registration").InnerXml;

            cCrawl.cFont = FontParse(cNodeChild = cNodeChild.NodeGet("font"));
            cCrawl.stColor = ColorParse(cNodeChild.NodeGet("color"));
            cNodeChild = cNodeChild.NodeGet("border");
            cCrawl.nBorderWidth = cNodeChild.AttributeGet<float>("width");
            cCrawl.stBorderColor = ColorParse(cNodeChild.NodeGet("color", false));
            #endregion
            #region . ANIMATION .
            cNodeChild = cXmlNode.NodeGet("mat");
            cMat = new Mat();
            cXNGrandChild = cNodeChild.NodeGet("badge");
            cMat.nBadgeLayer = cXNGrandChild.AttributeGet<ushort>("layer");
            cMat.stBadgeSize = SizeParse(cXNGrandChild.NodeGet("size"));
            cMat.sBadgeIn = cXNGrandChild.NodeGet("in").InnerXml;
            cMat.sBadgeLoop = cXNGrandChild.NodeGet("loop").InnerXml;
            cMat.sBadgeOut = cXNGrandChild.NodeGet("out").InnerXml;
            
            cXNGrandChild = cNodeChild.NodeGet("show");
            cMat.nShowLayer = cXNGrandChild.AttributeGet<ushort>("layer");
            cMat.stShowSize = SizeParse(cXNGrandChild.NodeGet("size"));
            cMat.sShowLoop = cXNGrandChild.NodeGet("loop").InnerXml;
            cMat.sShowOut = cXNGrandChild.NodeGet("out").InnerXml;
            cMat.sShowTransition = cXNGrandChild.NodeGet("transition").InnerXml;
            
            cXNGrandChild = cXNGrandChild.NodeGet("standby");
            cMat.sStandbyIn = cXNGrandChild.NodeGet("in").InnerXml;
            cMat.sStandbyLoop = cXNGrandChild.NodeGet("loop").InnerXml;
            cMat.sStandbyOut = cXNGrandChild.NodeGet("out").InnerXml;
            cMat.sStandbyTransition = cXNGrandChild.NodeGet("transition").InnerXml;

			cXNGrandChild = cNodeChild.NodeGet("out");
			cMat.nOutLayer = cXNGrandChild.AttributeGet<ushort>("layer");
			cMat.stOutSize = SizeParse(cXNGrandChild.NodeGet("size"));
			cMat.sOut = cXNGrandChild.NodeGet("animation").InnerXml;
            #endregion
        }
        SMS RollCategoryGet(XmlNode cXmlNode)
        {
            SMS cRetVal = new SMS()
            {
                sSmilesFolder = System.IO.Path.Combine(_sWorkFolder, "footages/smiles/"),
                sFlagsFolder = System.IO.Path.Combine(_sWorkFolder, "footages/flags/")
            };
			cRetVal.bToUpper = cXmlNode.AttributeOrDefaultGet<bool>("to_upper", false);
			cRetVal.nOpacity = cXmlNode.AttributeOrDefaultGet<byte>("opacity", 255);
			cRetVal.nInDissolve = cXmlNode.AttributeOrDefaultGet<byte>("in_dissolve", 0);
			cRetVal.nShiftTop = cXmlNode.AttributeOrDefaultGet<short>("shift_top", 0);
			cRetVal.nPressBottom = cXmlNode.AttributeOrDefaultGet<short>("press_bot", 0);
			cRetVal.nCenterShift = cXmlNode.AttributeOrDefaultGet<short>("center_shift", 0);  // вниз на сколько надо сдвнуть строку, чтобы в своей area она стала по центру
			cRetVal.nLineSpace = cXmlNode.AttributeOrDefaultGet<short>("line_cpace", 0);    
			XmlNode cXNChild = cXmlNode.NodeGet("font");
            cRetVal.cFont = FontParse(cXNChild);
            cRetVal.stColor = ColorParse(cXNChild.NodeGet("color"));
            cXNChild = cXNChild.NodeGet("border");
            cRetVal.nBorderWidth = cXNChild.AttributeGet<float>("width");
            cRetVal.stBorderColor = ColorParse(cXNChild.NodeGet("color", false));
			cRetVal.stMerging = cRoll.stMerging;
            return cRetVal;
        }
        Color ColorParse(XmlNode cXmlNode)
        {
            if (null == cXmlNode)
                return Color.Black;
            return Color.FromArgb(cXmlNode.AttributeOrDefaultGet<byte>("alpha", 255), cXmlNode.AttributeGet<byte>("red"), cXmlNode.AttributeGet<byte>("green"), cXmlNode.AttributeGet<byte>("blue"));
        }
        Rectangle SizeParse(XmlNode cXmlNode)
        {
            return new Rectangle(
                cXmlNode.AttributeGet<int>("left"),
                cXmlNode.AttributeGet<int>("top"),
                cXmlNode.AttributeGet<int>("width"),
                cXmlNode.AttributeGet<int>("height")
                );
        }
        Font FontParse(XmlNode cXmlNode)
        {
			return new Font(cXmlNode.AttributeValueGet("name"), cXmlNode.AttributeGet<float>("size"), cXmlNode.AttributeOrDefaultGet<FontStyle>("style", FontStyle.Regular));
        }
    }
}
