﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web.Services;
using System.Drawing;
using helpers;
using helpers.extensions;
using ingenie.web.lib;
using System.Security;

namespace ingenie.web.services
{
	[WebService(Namespace = "http://replica/ig/services/Cues.asmx")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	public class Management : System.Web.Services.WebService
	{
		private class Logger : lib.Logger
		{
			public Logger()
				: base("management")
			{
			}
		}

		public Management()
		{
		}

		[WebMethod(EnableSession = true)]
		public List<userspace.Helper.EffectInfo> BaetylusEffectsInfoGet()
		{
			userspace.Helper cHelper = new userspace.Helper();
			List<userspace.Helper.EffectInfo> aRetVal = new List<userspace.Helper.EffectInfo>();
			try
			{
				aRetVal = cHelper.BaetylusEffectsInfoGet();
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return aRetVal;
		}
		[WebMethod(EnableSession = true)]
		public List<int> BaetylusEffectStop(userspace.Helper.EffectInfo[] aEffects)
		{
			userspace.Helper cHelper = new userspace.Helper();
			List<int> aHashes = new List<int>();
			List<int> aRetVal = new List<int>();
			try
			{
				foreach (userspace.Helper.EffectInfo cEI in aEffects)
					aHashes.Add(cEI.nHashCode);
				aRetVal = cHelper.BaetylusEffectStop(aHashes);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return aRetVal;
		}
		[WebMethod(EnableSession = true)]
		public void RestartServices()
		{
			try
            {
                (new Logger()).WriteNotice("RestartServices: [pref=" + ingenie.web.Preferences.cClientManagement?.sRestartFile + "]");
                string sFileName = @"c:\Program Files\replica\ingenie\server\restart\restart";
                if (ingenie.web.Preferences.cClientManagement != null)
                    sFileName = ingenie.web.Preferences.cClientManagement.sRestartFile;

                if (System.IO.File.Exists(sFileName + "!"))
					System.IO.File.Move(sFileName + "!", sFileName);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
        [WebMethod(EnableSession = true)]
        public void MgmtWriteNotice(string sText)
        {
            (new Logger()).WriteNotice(sText);
        }
        [WebMethod(EnableSession = true)]
        public void MgmtWriteError(string sEx)
        {
            (new Logger()).WriteError(sEx);
        }
    }
}
