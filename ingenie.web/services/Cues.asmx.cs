using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web.Services;
using System.Drawing;
using helpers;
using helpers.extensions;
using ingenie.web.lib;

namespace ingenie.web.services
{
	[WebService(Namespace = "http://replica/ig/services/Cues.asmx")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	public class Cues : Common
	{
		private class Logger : lib.Logger
		{
			public Logger()
				: base("cues")
			{
			}
		}
		public class UserReplacement
		{
			public string sKey;
			public string sValue;
		}
		public class Template : Item
		{
			private lib.Template _cTemplate;
			override public Status eStatus
			{
				get
				{
					Status eStatus;
					if (base.eStatus != Status.Error && base.eStatus != Status.Stopped)
						if (null != _cTemplate && Status.Unknown != (eStatus = (Status)_cTemplate.AtomsStatusGet()))
							base.eStatus = eStatus;
					return base.eStatus;
				}
				set
				{
					base.eStatus = value;
				}
			}
			public Template() { }
			public Template(string sFile, Dictionary<string, string> ahUserReplacements)
			{
				_cTemplate = new lib.Template(sFile);
				if (null != ahUserReplacements)
					_cTemplate.SetCallbackForUserCues(ahUserReplacements);
				_cTemplate.SetCallbackForMacroCues();
			}
			override public void Prepare()
			{
				_cTemplate.Prepare();
			}
			override public void Start()
			{
				_cTemplate.Start();
			}
			override public void Stop()
			{
				_cTemplate.Stop();
			}
			public bool AddTextToRoll(string sText)
			{
                return _cTemplate.AddTextToRollIfFound(sText);
			}
		}
		public Cues()
		{
		}

		[WebMethod(EnableSession = true)]
		[System.Xml.Serialization.XmlInclude(typeof(Template))]
		public Item ItemCreate(string sPreset, string sFile, UserReplacement[] aUserReplacements)  // aUserReplacements  может быть null, nAssetsID  может быть 0
		{
			Item cRetVal = null;
			try
			{
				if (0 < Client.nID)
				{
					string sInfo = "Template, " + sFile;
					if (null != (cRetVal = GarbageCollector.ItemGet(sInfo)) && cRetVal.eStatus != Item.Status.Stopped && cRetVal.eStatus != Item.Status.Error)
						return cRetVal;
					cRetVal = new Template(sFile, (null == aUserReplacements ? null : aUserReplacements.ToDictionary(row => row.sKey, row => row.sValue)));
					cRetVal.sPreset = sPreset;
					cRetVal.sInfo = sInfo;
					GarbageCollector.ItemAdd(cRetVal);
					(new Logger()).WriteDebug3("create: " + sInfo + " hash:[" + cRetVal.GetHashCode() + "]");
				}
				else
					(new Logger()).WriteError("create: не инициирован механизм регистрации клиента");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return cRetVal;
		}
		[WebMethod(EnableSession = true)]
		public Item[] ItemsRunningGet()
		{
			return GarbageCollector.ItemsStartedGet().Where(row => row is Template).ToArray();
		}
		[WebMethod(EnableSession = true)]
		public void StartDTMF(string sFile)
		{
			(new Logger()).WriteNotice("cues.asmx.cs:StartDTMF: [file = " + sFile + "]");
			userspace.Template cTempl;
			cTempl = new userspace.Template(sFile, userspace.Template.COMMAND.unknown);
			cTempl.Prepare();
			cTempl.Start();
		}
		[WebMethod(EnableSession = true)]
		public string[] DirectoriesSCRGet(string sFolder)
		{
			List<string> aFilenames = new List<string>();
			try
			{
				userspace.Helper cHelper = new userspace.Helper();
				aFilenames.AddRange(cHelper.DirectoriesNamesGet(sFolder));
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return aFilenames.ToArray();
		}

		[WebMethod(EnableSession = true)]
		public bool AddTextToRoll(string sTemplateInfo, string sText)
		{
			Item cItem = GarbageCollector.ItemGet(sTemplateInfo);
			Template cTemplate;
			if (null != cItem && cItem is Template && (cTemplate = (Template)cItem).eStatus == Item.Status.Started)
				return ((Template)cItem).AddTextToRoll(sText);
			return false;
		}
	}
}
