using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using helpers;
using System.Xml;
using helpers.extensions;

namespace ingenie.initiator
{
	public class Preferences : helpers.Preferences
	{
		public class Process
		{
			public string sName;
			public string sOwner;
			public string sArguments;
			public bool bHideConsole;
			public System.Diagnostics.ProcessPriorityClass enPriority;

            public string sConfigMain;
			public string sConfigBKP;
			private string _sConfig;
			public string sConfig
			{
				get
				{
					return _sConfig;
				}
				set
				{
					if (null != value)
					{
						if (null == sConfigMain)
						{
							sConfigMain = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, sName + ".exe.config");
							sConfigBKP = sConfigMain + "_bkb";
						}
						_sConfig = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, value);
					}
					else
						_sConfig = null;
				}
			}
		}
		public class Blender
		{
			public string sPath;
			public string sTasks;
			public byte nQueue;
		}
		static private Preferences _cInstance = new Preferences();
		static public string sRestartFile
		{
			get
			{
				return _cInstance._sRestartFile;
			}
		}
		static public Process[] aProcesses
		{
			get
			{
				return _cInstance._aProcesses;
			}
		}
		static public Blender cBlender
		{
			get
			{
				return _cInstance._cBlender;
			}
		}

		private string _sRestartFile;
		private Process[] _aProcesses;
		private Blender _cBlender;

		public Preferences()
			: base("//ingenie/initiator")
		{
		}
		override protected void LoadXML(XmlNode cXmlNode)
		{
            if (null == cXmlNode || _bInitialized)
				return;
			_sRestartFile = cXmlNode.AttributeValueGet("restart", false);
            string sRestartDir = System.IO.Path.GetDirectoryName(_sRestartFile);
            if (null != _sRestartFile)
            {
                if (System.IO.Directory.Exists(sRestartDir))
                    (new Logger()).WriteNotice("restart dir exists [" + sRestartDir + "]");
                else
                    (new Logger()).WriteWarning("restart dir DOES NOT exists [" + sRestartDir + "]");
            }
            XmlNode[] aNodeChilds = cXmlNode.NodesGet("process", false);
            if (null != aNodeChilds)
            {
                List<Process> aProcesses = new List<Process>();
                for (int nIndx = 0; aNodeChilds.Length > nIndx; nIndx++)
                {
                    aProcesses.Add(new Process()
                    {
                        sName = aNodeChilds[nIndx].AttributeValueGet("name"),
                        sOwner = aNodeChilds[nIndx].AttributeValueGet("owner"),
                        sArguments = aNodeChilds[nIndx].AttributeValueGet("arguments", false),
                        sConfig = aNodeChilds[nIndx].AttributeValueGet("config", false),
                        bHideConsole = aNodeChilds[nIndx].AttributeOrDefaultGet<bool>("hide", false),
                        enPriority = aNodeChilds[nIndx].AttributeOrDefaultGet<System.Diagnostics.ProcessPriorityClass>("priority", System.Diagnostics.ProcessPriorityClass.Normal),
                    });
                }
                _aProcesses = aProcesses.ToArray();
            }
            else
                _aProcesses = new Process[0];
            XmlNode cXNBlender = cXmlNode.NodeGet("blender", false);
			if (null != cXNBlender)
			{
				_cBlender = new Blender() { sPath = cXNBlender.AttributeValueGet("path"), sTasks = cXNBlender.AttributeValueGet("tasks"), nQueue = (cXNBlender.AttributeValueGet("queue", false) ?? "1").ToByte() };
				if (!System.IO.File.Exists(_cBlender.sPath) || !System.IO.Directory.Exists(_cBlender.sTasks))
					throw new Exception("cannot find blender executable or tasks folder[" + _cBlender.sPath +"][" + _cBlender.sTasks + "]");
			}
		}
	}
}