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
        }
        public class Roll
        {
            public ushort nLayer;
            public float nSpeed;
			public int nCorrectDelay;
			public int nCorrectPosition;
            public uint nSecondsPerLine;
            public byte nSecondsPerPause;
            public string sMaskPageSingle;
            public string sMaskPageMulti;
            public Rectangle stSize;
            public SMS cSMSCommon;
            public SMS cSMSVIP;
            public SMS cSMSPromo;
            public SMS cSMSPhoto;
        }
        public class Crawl
        {
            public Rectangle stSize;
            public float nSpeed;
            public Font cFont;
            public Color stColor;
            public Color stBorderColor;
            public float nBorderWidth;
            public ushort nLayer;
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
		static public DB.Credentials DBCredentials //TODO убрать это свойство вапче - класс БД может брать все это напрямую из файла переференсов
		{
			get
            {
				return new DB.Credentials()
				{
					sServer = "db.channel.replica",
					nPort = 5432,
					sDatabase = "replica",
					sUser = "",
					sPassword = "",
					nTimeout = 240
				};
            }
        }
        static public ushort nMessagesQty
        {
            get
            {
                return 7;
            }
        }
        public bool bMessagesRelease;
        public BroadcastType eBroadcastType;
        public int nSMSQtty;
        public Crawl cCrawl;
        public Roll cRoll;
        public Mat cMat;
        public VIP cVIP;

        public string sPhotoPrefix = "PHOTO";

        public Preferences(string sWorkFolder, string sData)
        {
            _sWorkFolder = sWorkFolder;

            XmlDocument cXmlDocument = new XmlDocument();
            cXmlDocument.LoadXml(sData);
            XmlNode cXmlNode = cXmlDocument.NodeGet("data");

            bMessagesRelease = cXmlNode.AttributeGet<bool>("release", false);
            eBroadcastType = cXmlNode.AttributeGet<BroadcastType>("type", false);
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
			cRoll.nCorrectDelay = cNodeChild.AttributeGet<int>("correct_delay");
			cRoll.nCorrectPosition = cNodeChild.AttributeGet<int>("correct_position");
            XmlNode cXNGrandChild = cNodeChild.NodeGet("holds");
            cRoll.nSecondsPerLine = cXNGrandChild.AttributeGet<byte>("line");
            cRoll.nSecondsPerPause = cXNGrandChild.AttributeGet<byte>("pause");
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

            cCrawl.nSpeed = cNodeChild.AttributeGet<float>("speed");
            cCrawl.nLayer = cNodeChild.AttributeGet<ushort>("layer");
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
			cRetVal.bToUpper = null == cXmlNode.Attributes["to_upper"] ? false : cXmlNode.AttributeGet<bool>("to_upper");
			cRetVal.nOpacity = null == cXmlNode.Attributes["opacity"] ? (byte)255 : cXmlNode.AttributeGet<byte>("opacity");
			cRetVal.nInDissolve = null == cXmlNode.Attributes["in_dissolve"] ? (byte)0 : cXmlNode.AttributeGet<byte>("in_dissolve");
            XmlNode cXNChild = cXmlNode.NodeGet("font");
            cRetVal.cFont = FontParse(cXNChild);
            cRetVal.stColor = ColorParse(cXNChild.NodeGet("color"));
            cXNChild = cXNChild.NodeGet("border");
            cRetVal.nBorderWidth = cXNChild.AttributeGet<float>("width");
            cRetVal.stBorderColor = ColorParse(cXNChild.NodeGet("color", false));
            return cRetVal;
        }
        Color ColorParse(XmlNode cXmlNode)
        {
            if (null == cXmlNode)
                return Color.Black;
            return Color.FromArgb((null == cXmlNode.Attributes["alpha"] ? 255 : cXmlNode.AttributeGet<byte>("alpha")), cXmlNode.AttributeGet<byte>("red"), cXmlNode.AttributeGet<byte>("green"), cXmlNode.AttributeGet<byte>("blue"));
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
            return new Font(cXmlNode.AttributeValueGet("name"), cXmlNode.AttributeGet<float>("size"), (null == cXmlNode.Attributes["style"] ? FontStyle.Regular : cXmlNode.AttributeGet<FontStyle>("style")));
        }
    }
}
