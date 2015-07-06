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
			public class Candidate
			{
				public string sName;
				public string sImage;
				public string sDescription;
				public uint nVotesQty;
			}
			private int _nID = x.ToInt(null);

			public int nID
			{
				get
				{
					if(x.ToInt(null) == _nID)
						_nID = (sDescription + aCandidates.Select(o => o.sName + o.sImage + o.sDescription).Aggregate((r, o) => r += o)).GetHashCode();
					return _nID;
				}
			}
			public string sName;
			public DateTime dtLast;
			public Candidate[] aCandidates;
			public string sDescription;
		}
	
		static private List<Poll> _aPollsPreparing = new List<Poll>();

		public short nLeft { get; private set; }
		public short nTop { get; private set; }
		public bool bCUDA { get; private set; }
		public ushort nLayer { get; private set; }
		public string sFolderBlender { get; private set; }
		public string sFolderMat { get; private set; }
		public string sFolderVotes { get; private set; }
		public XmlNode cMat { get; private set; }
		public XmlNode cVotes { get; private set; }

		public Poll cPoll
		{
			get
			{
				return _cPoll;
			}
		}

		private string _sWorkFolder;
		private string _sFolderFootages;
		private string _sFolderPoll;
		private Poll _cPoll;

		public Preferences(string sWorkFolder, string sData)
        {
			nTop = nLeft = 0;
			bCUDA = true;
			if (!Directory.Exists(_sFolderFootages = Path.Combine(_sWorkFolder = sWorkFolder, "footages")))
				Directory.CreateDirectory(_sFolderFootages);
			if (!Directory.Exists(_sFolderPoll = Path.Combine(_sFolderFootages, "polls")))
				Directory.CreateDirectory(_sFolderPoll);

			XmlDocument cXmlDocument = new XmlDocument();
			cXmlDocument.Load(Path.Combine(sWorkFolder, "preferences.xml"));

			if(null == (
				//берем самое старое голосование
				_cPoll = cXmlDocument.NodesGet("preferences/polls/poll").Select(o => new Poll()
						{
							sName = o.AttributeValueGet("name", false),
							dtLast = o.AttributeGet<DateTime>("dt", false),
							sDescription = o.NodeGet("description").InnerText,
							aCandidates = o.NodesGet("candidate").Select(o1 => new Poll.Candidate() {
								sName = o1.AttributeValueGet("name").ToLower(),
								sImage = o1.AttributeValueGet("image"),
								sDescription = o1.AttributeValueGet("description"),
								nVotesQty = 0
							}).ToArray()
						}).Where(o => 1 > _aPollsPreparing.Count(o1 => o1.nID == o.nID)).OrderBy(o => (x.ToDT(null) > o.dtLast ? o.dtLast.Ticks : 0)).FirstOrDefault()
					?? _aPollsPreparing.OrderBy(o => (x.ToDT(null) > o.dtLast ? o.dtLast.Ticks : 0)).FirstOrDefault()
			))
				throw new Exception("no poll specified");
			XmlNode cXmlNode = cXmlDocument.NodeGet("preferences/blender");
			sFolderBlender = cXmlNode.AttributeValueGet("folder");

			XmlDocument cXD = new XmlDocument();
			XmlAttribute cXA;


			XmlNode cXNBlend = cXmlNode.NodeGet("mat");
			cMat = cXD.CreateElement("data");
			cXA = cXD.CreateAttribute("effect");
			cXA.Value = "render";
			cMat.Attributes.Append(cXA);
			cXA = cXD.CreateAttribute("blend");
			cXA.Value = Path.Combine(sWorkFolder, "blender", cXNBlend.AttributeValueGet("blend"));
			cMat.Attributes.Append(cXA);
			cXA = cXD.CreateAttribute("threads");
			cXA.Value = cXNBlend.AttributeValueGet("threads");
			cMat.Attributes.Append(cXA);

			XmlNode cPython = cXD.CreateElement("python");
			cPython.InnerText = cXNBlend.InnerText.
				Replace("{%_IMAGE_LEFT_%}", _cPoll.aCandidates[0].sImage).
				Replace("{%_IMAGE_RIGHT_%}", _cPoll.aCandidates[1].sImage).
				Replace("{%_TEXT_TOP_ARRAY_%}", "\"" + _cPoll.sDescription.Remove("\r").Split('\n').Select(o => o.Trim().Replace("\"", "\\\"")).Where(o => !o.IsNullOrEmpty()).Aggregate((r, o) => r += "\",\"" + o) + "\"").
				Replace("{%_TEXT_LEFT_%}", _cPoll.aCandidates[0].sDescription).
				Replace("{%_TEXT_RIGHT_%}", _cPoll.aCandidates[1].sDescription);
			cMat.AppendChild(cPython);

			cXNBlend = cXmlNode.NodeGet("votes");
			cVotes = cXD.CreateElement("data");
			cXA = cXD.CreateAttribute("effect");
			cXA.Value = "render";
			cVotes.Attributes.Append(cXA);
			cXA = cXD.CreateAttribute("blend");
			cXA.Value = Path.Combine(sWorkFolder, "blender", cXNBlend.AttributeValueGet("blend"));
			cVotes.Attributes.Append(cXA);
			cXA = cXD.CreateAttribute("threads");
			cXA.Value = cXNBlend.AttributeValueGet("threads");
			cVotes.Attributes.Append(cXA);
			cPython = cXD.CreateElement("python");
			cPython.InnerText = cXNBlend.InnerText;
			cVotes.AppendChild(cPython);

			if (!Directory.Exists(_sFolderPoll = Path.Combine(_sFolderPoll, (cMat.InnerText + cVotes.InnerText).GetHashCode().ToStr())))
				Directory.CreateDirectory(_sFolderPoll);

			cXA = cXD.CreateAttribute("output");
			cXA.Value = "*" + (sFolderMat = Path.Combine(_sFolderPoll, "mat"));
			cMat.Attributes.Append(cXA);
			cMat.InnerXml = cMat.InnerXml.Replace("{%_PATH_%}", sFolderMat.Replace("\\", "/"));
			if (!Directory.Exists(sFolderMat))
				Directory.CreateDirectory(sFolderMat);

			cXA = cXD.CreateAttribute("output");

			cXA.Value = "*" + (sFolderVotes = Path.Combine(_sFolderPoll, "votes"));
			cVotes.Attributes.Append(cXA);
			cVotes.InnerXml = cVotes.InnerXml.Replace("{%_PATH_%}", sFolderVotes.Replace("\\", "/"));
			if (!Directory.Exists(sFolderVotes))
				Directory.CreateDirectory(sFolderVotes);

			cXmlDocument.LoadXml(sData);
			cXmlNode = cXmlDocument.NodeGet("data");
			nLeft = cXmlNode.AttributeGet<short>("left");
			nTop = cXmlNode.AttributeGet<short>("top");
			bCUDA = cXmlNode.AttributeGet<bool>("cuda");
			nLayer = cXmlNode.AttributeGet<ushort>("layer");
		}
		public void PollUpdate()
		{
			string sFile = Path.Combine(_sWorkFolder, "preferences.xml");
			XmlDocument cXmlDocument = new XmlDocument();
			cXmlDocument.Load(sFile);
			XmlNode cXNPoll = cXmlDocument.NodesGet("preferences/polls/poll").Where(o => (new Poll()
			{
				sName = o.AttributeValueGet("name", false),
				dtLast = o.AttributeGet<DateTime>("dt", false),
				sDescription = o.NodeGet("description").InnerText,
				aCandidates = o.NodesGet("candidate").Select(o1 => new Poll.Candidate()
				{
					sName = o1.AttributeValueGet("name").ToLower(),
					sImage = o1.AttributeValueGet("image"),
					sDescription = o1.AttributeValueGet("description"),
					nVotesQty = 0
				}).ToArray()
			}).nID == _cPoll.nID).FirstOrDefault();

			if (null == cXNPoll)
				throw new Exception("cannot find target poll");
			cXNPoll.AttributeAdd("dt", _cPoll.dtLast = DateTime.Now);
			cXmlDocument.Save(sFile);
		}
	}
}
/*
sFileLeft = "zemfira.jpg"
sBotTextLeft = 'ОТПРАВЬ ДАША НА НОМЕР 2543'
bIntro = True

sFileLeft = "zlatoslava.jpg"
sBotTextRight = 'ОТПРАВЬ МАША НА НОМЕР 2543'
bIntro = False

sTopText_1 = 'ЗА КОГО ОТДАШЬ ГОЛОС ТЫ?'
sTopText_2 = 'ДАША ИЛИ МАША?'

sMidTextLeft_1 = "87%"
sMidTextRight_1 = "13%"
sMidTextLeft_2 = "92788"
sMidTextRight_2 = "12394"

*/