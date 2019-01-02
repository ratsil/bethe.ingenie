using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using helpers;
using helpers.extensions;
using System.Runtime.Remoting.Activation;

namespace ingenie.web
{
	public class Preferences : helpers.Preferences
	{
        static public Preferences _cInstance = new Preferences();
		static public void Reload()
		{
			_cInstance = new Preferences();
            bReLoaded = true;
        }
		[System.Xml.Serialization.XmlType(TypeName = "Preferences")]
		public class Clients
		{
			public class SCR : Clients
			{
				public class Preset : IdNamePair
				{
					public string sChannel;
					public string sFolder;
					public string sCaption;
				}
				public class Plaque
				{
					public int nPresetID;
					public bool bOpened;
					public ushort nHeight;
				}
				public class Template
				{
					public enum Bind
					{
						unknown,
						playlist,
						channel_credits,
						channel_logo,
						channel_chat,
						channel_user1,
						channel_user2,
						channel_user3,
						channel_user4,
						preset_logo,
						preset_credits,
						preset_notice,
						preset_credits_advert,
						preset_notice_advert,
						preset_credits_trail,
						preset_notice_trail,
						preset_bumper,
						preset_user1,
						preset_user2,
						preset_user3,
						preset_user4,
						preset_sequence,
						dtmf_in,
						dtmf_out
					}
					public enum FirstAction
					{
						prepare,
						start
					}
					public class Conflict
					{
						public int nPresetID;
						public Bind eBind;
					}
					public class Offset
					{
						public int nPresetID;
						public int nOffsetIn;
						public int nOffsetOut;
						public int nDurationSafe;
						public string sType;
						public string sClass;
						public string sPreType;
						public string sPreClass;
						public string sNextType;
						public string sNextClass;
						public bool bDoOnlyIfLast;
					}
					public class ScaleVideo
					{
						public int nPresetID;
						public int nOffsetIn;
						public int nOffsetOut;
						public int nDurationSafe;
					}
					public class Parameters
					{
						public int nPresetID;
						public string sText;
						public bool bIsVisible;
						public bool bIsEnabled;
						public bool bPlayWith;
						public int nDelayPlayWith;
						public bool bAutostart;
						public FirstAction eFirstAction;
					}
					public class PlayerParameters:Parameters
					{
						public bool bClipChooserVisible;
						public bool bClipChooserOpened;
						public bool bOpened;
						public string sFolder;
						public bool bPlayTop;
					}
					public Bind eBind;
					public string sFile;
                    public string sTag;
                    public Offset[] aOffsets;
					public Conflict[] aConflicts;
					public Parameters[] aParameters;
					public PlayerParameters[] aPlayerParameters;
					public Area stScaleVideo;
					public float nPixelAspectRatio;
				}
				public Preset[] aPresets;
				public Template[] aTemplates;
				public Plaque[] aPlaques;
				public string sTemplateChannelMask;
				public string sTemplatePresetMask;
				public string sChannelName;
                public string sDBUser;
                public string sDBPasswd;
                public string sDBIServerName;
                public int nFragmentBeforeNow;  // minutes
                public int nFragmentAfterNow;  // minutes

                public SCR()
				{
				}
			}
			public class Presentation : Clients
			{
				public string[] aFontFamilies;

				public Presentation()
				{
					aFontFamilies = System.Windows.Media.Fonts.SystemFontFamilies.Select(o => o.Source).Distinct().ToArray();
				}
			}
		}

		static public Clients.SCR cClientReplica
		{
			get
			{
				return _cInstance._cClientSCR;
			}
		}
		static public Clients.Presentation cClientPresentation = new Clients.Presentation();

		static public int nRemovingDelayInSeconds
		{
			get
			{
				return _cInstance._nRemovingDelayInSeconds;
			}
		}
		static public int nDelayForBrowserSwitching
		{
			get
			{
				return _cInstance._nDelayForBrowserSwitching;
			}
		}
		static public int nMaxTextWidth
		{
			get
			{
				return _cInstance._nMaxTextWidth;
			}
		}

		static public string sPlayerLogFolder = "c:/logs/scr/";
		static public string sClipStartStopAutomationFile;
		static public string sCacheFolder;
        static public int nFrameDuration_ms = 40;  // ms
		static public int nStopOffset = 0;
		static public int nQueuesCompensation = 2;  // sec
		static public bool bDontCheckFiles = false;
        static public bool bReLoaded;
        static public int nCopyDelayMiliseconds;
        static public int nCopyPeriodToDelayMiliseconds;

        private Clients.SCR _cClientSCR;
		private int _nRemovingDelayInSeconds = 600;
		private int _nDelayForBrowserSwitching = 20; // в секундах
		private int _nMaxTextWidth = 720;

		public Preferences()
			: base("//ingenie/web")
		{
		}
		override protected void LoadXML(XmlNode cXmlNode)
		{
            try
            {
                (new Logger()).WriteNotice("________________load XML   begin");

                if (null == cXmlNode)
                    return;
                (new Logger()).WriteNotice("\n\n" + cXmlNode.OuterXml + "\n\n");

                XmlNode cXmlNodeChild;
                XmlNode[] aXmlNodeChilds;
                XmlNode cXmlNodeClient = cXmlNode.NodeGet("clients/scr", false);

                if (null != cXmlNodeClient)
                {
                    _cClientSCR = new Clients.SCR();
                    _cClientSCR.sChannelName = cXmlNodeClient.AttributeOrDefaultGet<string>("name", "SCR");   // нужна только в клиенте

                    // нужна и в клиенте и в (cues || player)
                    #region presets
                    if (null != cXmlNodeClient.NodeGet("presets", false))
                    {
                        aXmlNodeChilds = cXmlNodeClient.NodesGet("presets/preset");
                        List<Clients.SCR.Preset> aPresets = new List<Clients.SCR.Preset>();
                        Clients.SCR.Preset cPreset;
                        foreach (XmlNode cXmlNodePreset in aXmlNodeChilds)
                        {
                            cPreset = new Clients.SCR.Preset();
                            cPreset.sName = cXmlNodePreset.AttributeValueGet("name");
                            cPreset.nID = cXmlNodePreset.AttributeIDGet("id");
                            cPreset.sFolder = cXmlNodePreset.AttributeValueGet("folder");
                            cPreset.sChannel = cXmlNodePreset.AttributeValueGet("channel");
                            cPreset.sCaption = cXmlNodePreset.AttributeValueGet("caption");
                            if (0 < aPresets.Count(o => (cPreset.nID == o.nID || cPreset.sName == o.sName) && cPreset.sChannel == o.sChannel))
                                throw new Exception("пресет указан повторно [channel:" + cPreset.sChannel + "][id:" + cPreset.nID + "][name:" + cPreset.sName + "][" + cXmlNodePreset.Name + "][" + cXmlNodeClient.Name + "]"); //TODO LANG
                            aPresets.Add(cPreset);
                        }
                        _cClientSCR.aPresets = aPresets.ToArray();
                    }
                    #endregion

                    // нужна и в клиенте и в (cues || player)
                    #region plaques
                    if (null != cXmlNodeClient.NodeGet("plaques", false))
                    {
                        aXmlNodeChilds = cXmlNodeClient.NodesGet("plaques/plaque");
                        List<Clients.SCR.Plaque> aPlaques = new List<Clients.SCR.Plaque>();
                        Clients.SCR.Plaque cPlaque;
                        foreach (XmlNode cXmlNodePreset in aXmlNodeChilds)
                        {
                            cPlaque = new Clients.SCR.Plaque();
                            cPlaque.nPresetID = cXmlNodePreset.AttributeOrDefaultGet<int>("id_preset", 0);
                            cPlaque.bOpened = cXmlNodePreset.AttributeOrDefaultGet<bool>("opened", false);
                            cPlaque.nHeight = cXmlNodePreset.AttributeOrDefaultGet<ushort>("height", 300);
                            aPlaques.Add(cPlaque);
                        }
                        _cClientSCR.aPlaques = aPlaques.ToArray();
                    }
                    #endregion

                    // нужна и в клиенте и в (cues || player)
                    #region templates
                    XmlNode cXmlNodeTemplates = cXmlNodeClient.NodeGet("templates");
                    if (null != (cXmlNodeChild = cXmlNodeTemplates.NodeGet("masks", false)))    // нужна только в клиенте
                    {
                        _cClientSCR.sTemplateChannelMask = cXmlNodeChild.AttributeOrDefaultGet<string>("channel", "");
                        _cClientSCR.sTemplatePresetMask = cXmlNodeChild.AttributeOrDefaultGet<string>("preset", "");
                    }
                    aXmlNodeChilds = cXmlNodeTemplates.NodesGet("template");
                    List<Clients.SCR.Template> aTemplates = new List<Clients.SCR.Template>();
                    Clients.SCR.Template cTemplate = null;

                    foreach (XmlNode cXmlNodeTemplate in aXmlNodeChilds)
                    {
                        cTemplate = new Clients.SCR.Template();
                        cTemplate.eBind = cXmlNodeTemplate.AttributeGet<Clients.SCR.Template.Bind>("bind");
                        if (0 < aTemplates.Count(o => o.eBind == cTemplate.eBind))
                            throw new Exception("шаблон с указанной привязкой уже был добавлен [" + cTemplate.eBind + "][" + cXmlNodeTemplate.Name + "][" + cXmlNodeClient.Name + "]"); //TODO LANG
                        cTemplate.sFile = cXmlNodeTemplate.AttributeValueGet("file");
                        cTemplate.sTag = cXmlNodeTemplate.AttributeValueGet("tag", false);
                        cTemplate.nPixelAspectRatio = 1;
                        if (null != (cXmlNodeChild = cXmlNodeTemplate.NodeGet("scale_video", false)))    // нужна только в плеере - это принудительно искажать видео и фото (было нужно для пульта SD)
                        {
                            short x, y;
                            ushort w, h;
                            string sAspect;
                            x = cXmlNodeChild.AttributeOrDefaultGet<short>("x", 0);
                            y = cXmlNodeChild.AttributeOrDefaultGet<short>("y", 0);
                            w = cXmlNodeChild.AttributeGet<ushort>("width");
                            h = cXmlNodeChild.AttributeGet<ushort>("height");
                            if (0 == w || 0 == h)
                                cTemplate.stScaleVideo = Area.stEmpty;
                            else
                                cTemplate.stScaleVideo = new Area(x, y, w, h);

                            sAspect = cXmlNodeChild.AttributeOrDefaultGet<string>("pixel_aspect_ratio", "1");
                            if (null != sAspect)
                                cTemplate.nPixelAspectRatio = float.Parse(sAspect.Replace('.', ','));
                        }
                        if (null != (cXmlNodeChild = cXmlNodeTemplate.NodeGet("offsets", false)))
                            cTemplate.aOffsets = GetOffsets(cXmlNodeChild.NodesGet("offset"));
                        if (null != (cXmlNodeChild = cXmlNodeTemplate.NodeGet("conflicts", false)))
                            cTemplate.aConflicts = GetConflicts(cXmlNodeChild.NodesGet("conflict"));
                        if (null != (cXmlNodeChild = cXmlNodeTemplate.NodeGet("preset_parameters", false)))
                            cTemplate.aParameters = GetParameters(cXmlNodeChild.NodesGet("parameters"));
                        if (null != (cXmlNodeChild = cXmlNodeTemplate.NodeGet("player_parameters", false)))
                        {
                            _cClientSCR.nFragmentBeforeNow = cXmlNodeChild.AttributeOrDefaultGet("fragment_before_now", 60);  // нужна только в плеере
                            _cClientSCR.nFragmentAfterNow = cXmlNodeChild.AttributeOrDefaultGet("fragment_after_now", 180);  // нужна только в плеере

                            sCacheFolder = cXmlNodeChild.AttributeValueGet("cache_folder", false);              // нужна только в плеере
                            nCopyDelayMiliseconds = cXmlNodeChild.AttributeOrDefaultGet("copy_delay", 0);              // нужна только в плеере
                            nCopyPeriodToDelayMiliseconds = cXmlNodeChild.AttributeOrDefaultGet("copy_period", 0);              // нужна только в плеере
                            if (null != cXmlNodeChild.NodeGet("parameters", false))
                                cTemplate.aParameters = GetPlayerParameters(cXmlNodeChild.NodesGet("parameters"));             // нужна только в клиенте
                        }
                        aTemplates.Add(cTemplate);
                    }
                    _cClientSCR.aTemplates = aTemplates.ToArray();
                    #endregion

                    XmlNode cDBI = cXmlNodeClient.NodeGet("dbi_web_service", false);       // нужна только в клиенте
                    if (null != cDBI)
                    {
                        _cClientSCR.sDBIServerName = cDBI.AttributeGet<string>("server");
                        _cClientSCR.sDBUser = cDBI.AttributeGet<string>("user_name");
                        _cClientSCR.sDBPasswd = cDBI.AttributeGet<string>("user_pass");
                    }

                    XmlNode cXmlNodeAutomation = cXmlNodeClient.NodeGet("automation", false);  // нужна только в плеере
                    if (null != cXmlNodeAutomation)
                    {
                        sClipStartStopAutomationFile = cXmlNodeAutomation.AttributeValueGet("file");
                        if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(sClipStartStopAutomationFile)))
                            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(sClipStartStopAutomationFile));

                        nFrameDuration_ms = cXmlNodeAutomation.AttributeGet<int>("frame_ms");
                        nStopOffset = cXmlNodeAutomation.AttributeGet<int>("stop_offset");
                    }

                    XmlNode cXmlNodeOthers = cXmlNodeClient.NodeGet("others", false);  // нужна только в плеере
                    if (null != cXmlNodeOthers)
                    {
                        nQueuesCompensation = cXmlNodeOthers.AttributeGet<int>("queues_compensation");
                        sPlayerLogFolder = cXmlNodeOthers.AttributeValueGet("player_log");
                        bDontCheckFiles = cXmlNodeOthers.AttributeGet<bool>("dont_check_files");
                    }
                }
            (new Logger()).WriteNotice("________________load XML   end  [sPlayerLogFolder="+ sPlayerLogFolder + "][sClipStartStopAutomationFile=" + sClipStartStopAutomationFile + "][sDBIServerName=" + _cClientSCR.sDBIServerName + "]");
            }
            catch(Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
        }
		private Clients.SCR.Template.Offset[] GetOffsets(XmlNode[] aXmlNodes)
		{
            if (aXmlNodes.IsNullOrEmpty())
                return null;
			List<Clients.SCR.Template.Offset> aINPs = new List<Clients.SCR.Template.Offset>();
			bool bDoOnlyIfLast = false;
            foreach (XmlNode cXmlNode in aXmlNodes)
			{
				if (!bool.TryParse(cXmlNode.AttributeValueGet("do_if_last", false), out bDoOnlyIfLast))
					bDoOnlyIfLast = false;

				aINPs.Add(new Clients.SCR.Template.Offset() 
				{ 
					nPresetID = cXmlNode.AttributeOrDefaultGet<int>("id_preset", 0), 
					nOffsetIn = cXmlNode.AttributeOrDefaultGet<int>("in", int.MaxValue), 
					nOffsetOut = cXmlNode.AttributeOrDefaultGet<int>("out", int.MaxValue), 
					nDurationSafe = cXmlNode.AttributeOrDefaultGet<int>("safe", int.MaxValue),   // минимальный хр, когда можно рименять этот оффсет
					bDoOnlyIfLast = bDoOnlyIfLast, 
					sNextClass = cXmlNode.AttributeValueGet("next_class", false),
					sNextType = cXmlNode.AttributeValueGet("next_type", false),
					sPreClass = cXmlNode.AttributeValueGet("pre_class", false),
					sPreType = cXmlNode.AttributeValueGet("pre_type", false),
					sClass = cXmlNode.AttributeValueGet("class", false),
					sType = cXmlNode.AttributeValueGet("type", false) 
				});
			}
			return aINPs.ToArray();
		}
		private Clients.SCR.Template.Conflict[] GetConflicts(XmlNode[] aXmlNodes)
		{
			if (aXmlNodes.IsNullOrEmpty())
				return null;
			List<Clients.SCR.Template.Conflict> aINPs = new List<Clients.SCR.Template.Conflict>();
			int nID;
			Clients.SCR.Template.Bind eBind;
			foreach (XmlNode cXmlNodeConflict in aXmlNodes)
			{
                nID = cXmlNodeConflict.AttributeOrDefaultGet<int>("id_preset", 0);
                eBind = cXmlNodeConflict.AttributeOrDefaultGet<Clients.SCR.Template.Bind>("bind", Clients.SCR.Template.Bind.unknown);
				aINPs.Add(new Clients.SCR.Template.Conflict() { nPresetID = nID, eBind = eBind });
			}
			return aINPs.ToArray();
		}
		private Clients.SCR.Template.Parameters[] GetParameters(XmlNode[] aXmlNodes)
		{
			if (aXmlNodes.IsNullOrEmpty())
				return null;
			List<Clients.SCR.Template.Parameters> aParams = new List<Clients.SCR.Template.Parameters>();
			int nID, nDelay = 0;
			string sText;
			bool bIsVisible = true;
			bool bIsEnabled = true;
			bool bAutostart = true, bPW = false;
			Clients.SCR.Template.FirstAction eFirstAction;
			foreach (XmlNode cXmlNode in aXmlNodes)
			{
				nID = cXmlNode.AttributeOrDefaultGet<int>("id_preset", 0);
				sText = cXmlNode.AttributeValueGet("text", false);
				if (!bool.TryParse(cXmlNode.AttributeValueGet("is_visible", false), out bIsVisible))
					bIsVisible = true;
				if (!bool.TryParse(cXmlNode.AttributeValueGet("is_enabled", false), out bIsEnabled))
					bIsEnabled = true;
				if (!bool.TryParse(cXmlNode.AttributeValueGet("autostart", false), out bAutostart))
					bAutostart = true;
				bool.TryParse(cXmlNode.AttributeValueGet("play_with", false), out bPW);
				int.TryParse(cXmlNode.AttributeValueGet("play_with_delay", false), out nDelay);
				if (!Clients.SCR.Template.FirstAction.TryParse(cXmlNode.AttributeValueGet("first_action", false), true, out eFirstAction))
					eFirstAction = Clients.SCR.Template.FirstAction.start;
				aParams.Add(new Clients.SCR.Template.Parameters() { nPresetID = nID, sText = sText, bIsEnabled = bIsEnabled, bIsVisible = bIsVisible, bAutostart = bAutostart, eFirstAction = eFirstAction, bPlayWith = bPW, nDelayPlayWith = nDelay });
			}
			return aParams.ToArray();
		}
		private Clients.SCR.Template.PlayerParameters[] GetPlayerParameters(XmlNode[] aXmlNodes)
		{
			Clients.SCR.Template.Parameters[] aParameters = GetParameters(aXmlNodes);
			List<Clients.SCR.Template.PlayerParameters> aRetVal = new List<Clients.SCR.Template.PlayerParameters>();
			Clients.SCR.Template.Parameters cParameters;
			bool bChooserVisible;
			bool bChooserOpened;
			bool bOpened;
			bool bPT = false;
			string sFolder;
			int nID;
			if (null != aParameters)
			{
				foreach (XmlNode cXmlNode in aXmlNodes)
				{
					nID = cXmlNode.AttributeOrDefaultGet<int>("id_preset", 0);
					cParameters = aParameters.FirstOrDefault(o => o.nPresetID == nID);
					bChooserVisible = true;
					bChooserOpened = true;
					if (null != cParameters)
					{
						bool.TryParse(cXmlNode.AttributeValueGet("clip_chooser_visible", false), out bChooserVisible);
						bool.TryParse(cXmlNode.AttributeValueGet("clip_chooser_opened", false), out bChooserOpened);
						bool.TryParse(cXmlNode.AttributeValueGet("opened", false), out bOpened);
						bool.TryParse(cXmlNode.AttributeValueGet("play_top", false), out bPT);
						sFolder = cXmlNode.AttributeValueGet("folder", false);
						aRetVal.Add(new Clients.SCR.Template.PlayerParameters()
						{
							nPresetID = nID,
							sText = cParameters.sText,
							bIsEnabled = cParameters.bIsEnabled,
							bIsVisible = cParameters.bIsVisible,
							eFirstAction = cParameters.eFirstAction,
							bOpened = bOpened,
							bClipChooserVisible = bChooserVisible,
							bClipChooserOpened = bChooserOpened,
							sFolder = sFolder,
							bPlayTop = bPT,
							bPlayWith = cParameters.bPlayWith,
							nDelayPlayWith = cParameters.nDelayPlayWith });
					}
				}
			}
			return aRetVal.ToArray();
		}
	}
}
