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
		public List<btl.Roll> aRoll { get { return _aRoll; } }
		public btl.Animation cHashTag { get { return _cHashTag; } }
		public btl.Animation cOverTop { get { return _cOverTop; } }
		public btl.Animation cOverBot { get { return _cOverBot; } }
		public string sCredits { get { return _sCredits; } }
		public ushort nVideoFramesDelay { get { return _nVideoFramesDelay; } }

		private List<btl.Roll> _aRoll;
		private btl.Animation _cHashTag;
		private btl.Animation _cOverTop;
		private btl.Animation _cOverBot;
		private string _sCredits;
		private ushort _nVideoFramesDelay;

		public Preferences(string sData)
		{
			(new Logger()).WriteDebug("[source=" + sData + "]");
			XmlDocument cXmlDocument = new XmlDocument();
			cXmlDocument.LoadXml(sData);
			XmlNode cNodeChild, cXmlNode = cXmlDocument.NodeGet("data");
			_nVideoFramesDelay = cXmlNode.AttributeOrDefaultGet<ushort>("video_prepare_delay", 0);

			cNodeChild = cXmlNode.NodeGet("credits");
			_sCredits = cNodeChild.ToStr(0);

			cNodeChild = cXmlNode.NodeGet("hashtag");
			_cHashTag = new btl.Animation(cNodeChild);

			cNodeChild = cXmlNode.NodeGet("overlay_top");
			_cOverTop = new btl.Animation(cNodeChild);

			cNodeChild = cXmlNode.NodeGet("overlay_bottom");
			_cOverBot = new btl.Animation(cNodeChild);

			while (null != (cNodeChild = cXmlNode.NodeGet("roll", false)))
			{
				if (null == _aRoll)
					_aRoll = new List<btl.Roll>();
				_aRoll.Add(new btl.Roll(cNodeChild));
				cXmlNode.RemoveChild(cNodeChild);
			}
		}
	}
}