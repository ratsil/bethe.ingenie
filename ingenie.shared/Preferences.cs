using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using helpers.extensions;

namespace ingenie.shared
{
	public class Preferences : helpers.Preferences
	{
		static private Preferences _cInstance = new Preferences();

		static public int nPort
		{
			get
			{
				return _cInstance._nPort;
			}
		}
        static public void Reload()
        {
            _cInstance = new Preferences();
        }


        private int _nPort;

		public Preferences()
			: base("//ingenie/shared")
		{
		}
		override protected void LoadXML(XmlNode cXmlNode)
		{
            if (null == cXmlNode || _bInitialized)
				return;
            _nPort = cXmlNode.AttributeGet<int>("port");
		}
	}
}