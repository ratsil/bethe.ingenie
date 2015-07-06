using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Net;
using helpers;
using helpers.extensions;
using System.Xml;

namespace ingenie.userspace
{
	class Preferences : helpers.Preferences
	{
		static private Preferences _cInstance = new Preferences();

		static public IPAddress cServerIP
		{
			get
			{
				return _cInstance._cServerIP;
			}
		}
		static public System.Runtime.Remoting.Activation.UrlAttribute cUrlAttribute
		{
			get
			{
				return new System.Runtime.Remoting.Activation.UrlAttribute("tcp://" + Preferences.cServerIP + ":" + shared.Preferences.nPort);
			}
		}

		private IPAddress _cServerIP;

		public Preferences()
			: base("//ingenie/userspace")
		{
		}
		override protected void LoadXML(XmlNode cXmlNode)
		{
            if (null == cXmlNode || _bInitialized)
				return;
			_cServerIP = cXmlNode.AttributeGet<IPAddress>("server");
		}
	}
}