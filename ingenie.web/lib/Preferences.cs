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
		static private Preferences _cInstance = new Preferences();
		static public void Reload()
		{
			_cInstance = new Preferences();
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
						public bool bAutostart;
						public FirstAction eFirstAction;
					}
					public class PlayerParameters:Parameters
					{
						public bool bClipChooserVisible;
						public bool bClipChooserOpened;
						public bool bOpened;
						public string sFolder;
					}
					public Bind eBind;
					public string sFile;
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

		static public object[] _UrlAttribute = { new UrlAttribute("tcp://localhost:1238") };
		static public string sPlayerLogFolder;
		static public string sClipStartStopAutomationFile;
		static public int nFrameDuration_ms;
		static public int nStopOffset;
		static public int nQueuesCompensation;

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
			(new Logger()).WriteWarning("________________load XML   begin");

			if (null == cXmlNode)
				return;
			XmlNode cXmlNodeChild;
			XmlNode[] aXmlNodeChilds;
            XmlNode cXmlNodeClient = cXmlNode.NodeGet("clients/scr", false);
			if (null != cXmlNodeClient)
			{
				#region presets
				aXmlNodeChilds = cXmlNodeClient.NodesGet("presets/preset");
				_cClientSCR = new Clients.SCR();
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
				#endregion
				#region plaques
				aXmlNodeChilds = cXmlNodeClient.NodesGet("plaques/plaque");
				List<Clients.SCR.Plaque> aPlaques = new List<Clients.SCR.Plaque>();
				Clients.SCR.Plaque cPlaque;
				foreach (XmlNode cXmlNodePreset in aXmlNodeChilds)
				{
					cPlaque = new Clients.SCR.Plaque();
					cPlaque.nPresetID = cXmlNodePreset.AttributeGet<int>("id_preset", false);
					cPlaque.bOpened = cXmlNodePreset.AttributeGet<bool>("opened", false);
					cPlaque.nHeight = cXmlNodePreset.AttributeGet<ushort>("height", false);
					aPlaques.Add(cPlaque);
				}
				_cClientSCR.aPlaques = aPlaques.ToArray();
				#endregion
				#region templates
				XmlNode cXmlNodeTemplates = cXmlNodeClient.NodeGet("templates");
                if (null != (cXmlNodeChild = cXmlNodeTemplates.NodeGet("masks", false)))
				{
                    _cClientSCR.sTemplateChannelMask = cXmlNodeChild.AttributeValueGet("channel", false) ?? "";
                    _cClientSCR.sTemplatePresetMask = cXmlNodeChild.AttributeValueGet("preset", false) ?? "";
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
                    if (null != (cXmlNodeChild = cXmlNodeTemplate.NodeGet("scale_video", false)))
					{
						short x, y;
						ushort w, h;
						string sAspect;
                        x = cXmlNodeChild.AttributeGet<short>("x", false);
                        y = cXmlNodeChild.AttributeGet<short>("y", false);
                        w = cXmlNodeChild.AttributeGet<ushort>("width", false);
                        h = cXmlNodeChild.AttributeGet<ushort>("height", false);
                        sAspect = cXmlNodeChild.AttributeValueGet("pixel_aspect_ratio", false);
						if (0 == w || 0 == h)
							cTemplate.stScaleVideo = Area.stEmpty;
						else
							cTemplate.stScaleVideo = new Area(x, y, w, h);
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
						cTemplate.aParameters = GetPlayerParameters(cXmlNodeChild.NodesGet("parameters"));
					aTemplates.Add(cTemplate);
				}
				_cClientSCR.aTemplates = aTemplates.ToArray();
				#endregion
                XmlNode cXmlNodeAutomation = cXmlNodeClient.NodeGet("automation", false);
                sClipStartStopAutomationFile = cXmlNodeAutomation.AttributeValueGet("file", false);
                nFrameDuration_ms = cXmlNodeAutomation.AttributeGet<int>("frame_ms", false);
                nStopOffset = cXmlNodeAutomation.AttributeGet<int>("stop_offset", false);
                XmlNode cXmlNodeOthers = cXmlNodeClient.NodeGet("others", false);
                nQueuesCompensation = cXmlNodeOthers.AttributeGet<int>("queues_compensation", false);
                sPlayerLogFolder = cXmlNodeOthers.AttributeValueGet("player_log", false);
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
					nPresetID = cXmlNode.AttributeGet<int>("id_preset", false), 
					nOffsetIn = cXmlNode.AttributeGet<int>("in", false), 
					nOffsetOut = cXmlNode.AttributeGet<int>("out", false), 
					nDurationSafe = cXmlNode.AttributeGet<int>("safe", false),
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
                nID = cXmlNodeConflict.AttributeGet<int>("id_preset", false);
                eBind = cXmlNodeConflict.AttributeGet<Clients.SCR.Template.Bind>("bind", false);
				aINPs.Add(new Clients.SCR.Template.Conflict() { nPresetID = nID, eBind = eBind });
			}
			return aINPs.ToArray();
		}
		private Clients.SCR.Template.Parameters[] GetParameters(XmlNode[] aXmlNodes)
		{
			if (aXmlNodes.IsNullOrEmpty())
				return null;
			List<Clients.SCR.Template.Parameters> aParams = new List<Clients.SCR.Template.Parameters>();
			int nID;
			string sText;
			bool bIsVisible = true;
			bool bIsEnabled = true;
			bool bAutostart = true;
			Clients.SCR.Template.FirstAction eFirstAction;
			foreach (XmlNode cXmlNode in aXmlNodes)
			{
				nID = cXmlNode.AttributeGet<int>("id_preset", false);
				sText = cXmlNode.AttributeValueGet("text", false);
				if (!bool.TryParse(cXmlNode.AttributeValueGet("is_visible", false), out bIsVisible))
					bIsVisible = true;
				if (!bool.TryParse(cXmlNode.AttributeValueGet("is_enabled", false), out bIsEnabled))
					bIsEnabled = true;
				if (!bool.TryParse(cXmlNode.AttributeValueGet("autostart", false), out bAutostart))
					bAutostart = true;
				if (!Clients.SCR.Template.FirstAction.TryParse(cXmlNode.AttributeValueGet("first_action", false), true, out eFirstAction))
					eFirstAction = Clients.SCR.Template.FirstAction.start;
				aParams.Add(new Clients.SCR.Template.Parameters() { nPresetID = nID, sText = sText, bIsEnabled = bIsEnabled, bIsVisible = bIsVisible, bAutostart = bAutostart, eFirstAction = eFirstAction });
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
			string sFolder;
			int nID;
			if (null != aParameters)
			{
				foreach (XmlNode cXmlNode in aXmlNodes)
				{
					nID = cXmlNode.AttributeGet<int>("id_preset", false);
					cParameters = aParameters.FirstOrDefault(o => o.nPresetID == nID);
					bChooserVisible = true;
					bChooserOpened = true;
					if (null != cParameters)
					{
						bool.TryParse(cXmlNode.AttributeValueGet("clip_chooser_visible", false), out bChooserVisible);
						bool.TryParse(cXmlNode.AttributeValueGet("clip_chooser_opened", false), out bChooserOpened);
						bool.TryParse(cXmlNode.AttributeValueGet("opened", false), out bOpened);
						sFolder = cXmlNode.AttributeValueGet("folder", false);
						aRetVal.Add(new Clients.SCR.Template.PlayerParameters() { nPresetID = nID, sText = cParameters.sText, bIsEnabled = cParameters.bIsEnabled, bIsVisible = cParameters.bIsVisible, eFirstAction = cParameters.eFirstAction, bOpened = bOpened, bClipChooserVisible = bChooserVisible, bClipChooserOpened = bChooserOpened, sFolder = sFolder });
					}
				}
			}
			return aRetVal.ToArray();
		}
	}
}
