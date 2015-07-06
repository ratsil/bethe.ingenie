using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Collections;
using System.Xml;
using helpers;
using helpers.extensions;

namespace ingenie.userspace
{
	public class Template
	{
		public class Logger : helpers.Logger
		{
			static public string sFile = null;

			public Logger()
				: base("template", sFile)
			{ }
		}
        public class Inclusion
        {
            public enum ACTION
            {
				Wait,
                Start,
                Stop,
				Stepback
            }
            public Template cParent;
            private Atom.Delay _cDelay;

            public ulong nDelay
            {
                get
                {
                    return _cDelay.nDelay;
                }
                set
                {
                    _cDelay.nDelay = value;
                }
            }
            public ACTION eAction { get; set; }
            public string sFile { get; set; }

			private Inclusion()
			{
				throw new NotImplementedException();
			}
            public Inclusion(Template cParent)
            {
                this.cParent = cParent;
                _cDelay = new Atom.Delay();
            }
            public void LoadXML(XmlNode cXmlNode)
            {
                if (null == cXmlNode)
                    return;
                if (0 < cXmlNode.Attributes.Count)
                {
                    foreach (XmlAttribute cAttr in cXmlNode.Attributes)
                    {
                        switch (cAttr.Name)
                        {
                            case "action":
                                try
                                {
                                    eAction = (ACTION)Enum.Parse(typeof(ACTION), System.Text.RegularExpressions.Regex.Replace(cAttr.Value.Trim(), "\\W", "_"), true);
                                }
                                catch
                                {
                                    throw new Exception("указано некорректное действие для включаемого шаблона [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
                                }
                                break;
                            case "file":
                                sFile = cAttr.Value.Trim();
                                break;
                            case "delay":
                                _cDelay.LoadXML(cAttr);
                                break;
                        }
                    }
                }
            }
        }
		public enum Status
		{
			Creating = 0,
			Created = 1,
			Preparing = 2,
			Prepared = 3,
			Starting = 4,
			Started = 5,
			Stopping = 6,
			Stopped = 7,
			Disposing = 8,
			Disposed = 9,
			Failed = 10
		}
		public enum COMMAND
		{
			unknown,
			show,
			hide
		}
		public enum LIFETIME
		{
			PLI,
			Infinite,
			Seconds
		}
		public enum DESTROY_TYPE
		{
			Manual,
			Self
		}
		public enum EXISTS_BEHAVIOR
		{
			Replace,
			Skip,
			Ignore
		}
        public delegate void DoneDelegate(Template cTemplate);
		public delegate string MacroExecuteDelegate(string sMacro);
		public delegate string RuntimeGetDelegate(string sRuntime);
		public delegate void ParseDoneDelegate(Template cTemplate);

		public event DoneDelegate Done;
		public MacroExecuteDelegate MacroExecute;
		public RuntimeGetDelegate RuntimeGet;
		public event ParseDoneDelegate ParseDone;

		protected List<Atom> _aAtoms;

		public Status eStatus { get; protected set; }
		public string sFile { get; set; }
		public COMMAND eCommand { get; set; }
		public LIFETIME eLifeTime { get; set; }
		public int nLifeTimeSeconds { get; set; }
		public EXISTS_BEHAVIOR eOnExists { get; set; }
        public DESTROY_TYPE eDestroyType { get; set; }
		public Template cFollowingTemplate { get; set; }
		public Template cPrecedingTemplate { get; set; }
		public object cTag { get; set; }
        public Inclusion[] aInclusions { get; set; }
        public bool bMispreparedShow { get; set; }

		public Template(string sFile)
            : this(sFile, COMMAND.unknown)
		{ }
		public Template(string sFile, COMMAND eCommand)
		{
			eStatus = Status.Creating;
			eLifeTime = LIFETIME.PLI;
			nLifeTimeSeconds = -1;
			eOnExists = EXISTS_BEHAVIOR.Replace;
			eDestroyType = DESTROY_TYPE.Manual;
			_aAtoms = new List<Atom>();
			cFollowingTemplate = null;
			cPrecedingTemplate = null;
			this.sFile = sFile;
			this.eCommand = eCommand;
			eStatus = Status.Created;
            aInclusions = null;
            bMispreparedShow = true;
		}
		~Template()
		{
			Dispose();
		}
		public void Dispose()
		{
			try
			{
				eStatus = Status.Disposing;
				if (null != _aAtoms)
				{
					foreach (Atom cAtom in _aAtoms.ToArray())
					{
						try
						{
							cAtom.Dispose();
						}
						catch (Exception ex)
						{
							(new Logger()).WriteError(ex);
						}
					}
					_aAtoms = null;
					//GC.Collect();
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			eStatus = Status.Disposed;
		}

		private void ParseXML()
		{
            if ("" == sFile) // т.е. файл не неправильный, а его просто нет
                return;
			_aAtoms.Clear();
			if (!System.IO.File.Exists(sFile))
				throw new Exception("отсутствует указанный файл шаблона [FL:" + sFile + "]"); //TODO LANG
			XmlDocument cXMLTemplate = new XmlDocument();
			string sXML = System.IO.File.ReadAllText(sFile);
			sXML = ProcessMacros(sXML);

			cXMLTemplate.Load(new System.IO.StringReader(sXML));
			XmlNode cXmlNode = cXMLTemplate.GetElementsByTagName("template")[0];

			if (0 < cXmlNode.Attributes.Count)
			{
				foreach (XmlAttribute cAttr in cXmlNode.Attributes)
					switch (cAttr.Name)
					{
						case "destroy":
							try
							{
								eDestroyType = (Template.DESTROY_TYPE)Enum.Parse(typeof(Template.DESTROY_TYPE), System.Text.RegularExpressions.Regex.Replace(cAttr.Value.Trim(), "\\W", "_"), true);
							}
							catch
							{
								throw new Exception("указан некорректный тип удаления [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
					}
			}
			XmlNodeList aChildNodes = cXmlNode.ChildNodes;
			Atom cAtom;
            this.aInclusions = null;
            List<Inclusion> aInclusions = new List<Inclusion>();
			for (int nIndx = 0; nIndx < aChildNodes.Count; nIndx++)
			{
				cAtom = null;
				switch (aChildNodes[nIndx].Name)
				{
					case "show":
                        #region show
						if (0 < aChildNodes[nIndx].Attributes.Count)
						{
                            foreach (XmlAttribute cAttr in aChildNodes[nIndx].Attributes)
                            {
                                switch (cAttr.Name)
                                {
                                    case "exists":
                                        try
                                        {
                                            eOnExists = (Template.EXISTS_BEHAVIOR)Enum.Parse(typeof(Template.EXISTS_BEHAVIOR), System.Text.RegularExpressions.Regex.Replace(cAttr.Value.Trim(), "\\W", "_"), true);
                                        }
                                        catch
                                        {
                                            throw new Exception("указан некорректный тип замещения [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
                                        }
                                        break;
                                    case "lifetime":
                                        try
                                        {
                                            eLifeTime = (Template.LIFETIME)Enum.Parse(typeof(Template.LIFETIME), cAttr.Value.Trim(), true);
                                        }
                                        catch
                                        {
                                            try
                                            {
                                                nLifeTimeSeconds = cAttr.Value.Trim().ToInt32();
                                                eLifeTime = LIFETIME.Seconds;
                                            }
                                            catch
                                            {
                                                nLifeTimeSeconds = -1;
                                            }
                                            if (0 > nLifeTimeSeconds)
                                                throw new Exception("указана некорректная продолжительность существования [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
                                        }
                                        break;
                                    case "misprepared":
                                        try
                                        {
                                            bMispreparedShow = cAttr.Value.Trim().ToBool();
                                        }
                                        catch
                                        {
                                            throw new Exception("указано некорректное значение поведения при нецелевой подготовке [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
                                        }
                                        break;
                                }
                            }
						}
                        #endregion
						break;
                    case "include":
                    case "inclusion":
                    case "template":
                        Inclusion cInclusion = new Inclusion(this);
                        cInclusion.LoadXML(aChildNodes[nIndx]);
                        aInclusions.Add(cInclusion);
                        break;
					case "plugin":
						cAtom = new Plugin();
						break;
					case "video":
						cAtom = new Video();
						break;
					case "audio":
						cAtom = new Audio();
						break;
					case "animation":
						cAtom = new Animation();
						break;
					case "clock":
						cAtom = new Clock();
						break;
					case "text":
						cAtom = new Text();
						break;
					case "playlist":
						cAtom = new Playlist();
						break;
					case "roll":
						cAtom = new Roll();
						break;
				}
				if (null != cAtom)
				{
					cAtom.cTemplate = this;
                    cAtom.LoadXML(aChildNodes[nIndx]);
					if (!(cAtom is Effect) || ((Effect)cAtom).cShow.bShow)
						_aAtoms.Add(cAtom);
				}
                if(0 < aInclusions.Count)
                    this.aInclusions = aInclusions.ToArray();
			}
		}
		internal protected string ProcessMacros(string sText)
		{
			string sRetVal = ProcessRuntimes(sText);
			System.Text.RegularExpressions.MatchCollection cMatches;
			string sValue = null;
			cMatches = System.Text.RegularExpressions.Regex.Matches(sRetVal, @"\{\%MACRO\:\:.*?%}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			if (0 < cMatches.Count)
			{
				foreach (System.Text.RegularExpressions.Match cMatch in cMatches)
				{
					try
					{
						sValue = null;
						if (null == MacroExecute)
							throw new Exception("отсутствует привязка макро-строки [" + cMatch.Value + "]"); //TODO LANG
						sValue = MacroExecute(cMatch.Value);
						sValue = ProcessRuntimes(sValue);
						sRetVal = sRetVal.Replace(cMatch.Value, sValue.ForXML());
					}
					catch
					{
						(new Logger()).WriteNotice("got error while processing macro [" + cMatch.Value + "]");
						throw;
					}
				}
			}
			return sRetVal;
		}
		internal protected string ProcessRuntimes(string sText)
		{
			string sRetVal = sText;
			System.Text.RegularExpressions.MatchCollection cMatches;
			cMatches = System.Text.RegularExpressions.Regex.Matches(sRetVal, @"\{\%RUNTIME\:\:.*?%}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			if (0 < cMatches.Count)
			{
				string sValue;
				foreach (System.Text.RegularExpressions.Match cMatch in cMatches)
				{
					sValue = null;
					if (null == RuntimeGet)
						throw new Exception("отсутствует привязка runtime-свойства [" + cMatch.Value + "]"); //TODO LANG
					sValue = RuntimeGet(cMatch.Value);
					sRetVal = sRetVal.Replace(cMatch.Value, sValue.ForXML());
				}
			}
			return sRetVal;
		}

		virtual protected bool IsActual()
		{
			return true;
		}
		
		public void PrepareAsync()
		{
			System.Threading.ThreadPool.QueueUserWorkItem(Prepare, new System.Diagnostics.StackFrame(1, true));
		}
		private void Prepare(object cState)
		{
			System.Diagnostics.StackFrame cStackFrame = null;
			try
			{
				if(null != cState)
					cStackFrame = (System.Diagnostics.StackFrame)cState;
				Prepare();
			}
			catch (Exception ex)
			{
				if (null != cStackFrame)
					ex = new Exception(ex.Message + " stacktrace:" + cStackFrame.GetMethod().Name + " at " + cStackFrame.GetFileName() + ":" + cStackFrame.GetFileLineNumber());
				(new Logger()).WriteError(ex);
			}
		}
		public void Prepare()
		{
			if (Status.Created != eStatus)
				throw new Exception("template status must be created instead of " + eStatus.ToString());
			eStatus = Status.Preparing;
			try
			{
				ParseXML();
				if (null != ParseDone)
					ParseDone(this);
				foreach (Atom cAtom in _aAtoms)
				{
					cAtom.Stopped += OnAtomDone;
					cAtom.Prepare();
				}
				eStatus = Status.Prepared;
			}
			catch
			{
				eStatus = Status.Failed;
				throw;
			}
		}
		
		public void StartAsync()
		{
			System.Threading.ThreadPool.QueueUserWorkItem(Start, new System.Diagnostics.StackFrame(1, true));
		}
		private void Start(object cState)
		{
			System.Diagnostics.StackFrame cStackFrame = null;
			try
			{
				if (null != cState)
					cStackFrame = (System.Diagnostics.StackFrame)cState;
				Start();
			}
			catch (Exception ex)
			{
				if (null != cStackFrame)
					ex = new Exception(ex.Message + " stacktrace:" + cStackFrame.GetMethod().Name + " at " + cStackFrame.GetFileName() + ":" + cStackFrame.GetFileLineNumber());
				(new Logger()).WriteError(ex);
			}
		}
		virtual public void Start()
		{
			try
			{
				if (Status.Prepared < eStatus)
                    throw new Exception("template must be created or prepared... [s:" + eStatus + "][f:" + sFile + "]");
				if (Status.Preparing == eStatus)
				{
					(new Logger()).WriteWarning("WAITING FOR PREPARING IN START:" + sFile);
					while (Status.Preparing == eStatus) //UNDONE возможный deadlock
						Thread.Sleep(5);
				}
				else if (Status.Prepared != eStatus)
				{
					(new Logger()).WriteWarning("PREPARE IN START:" + sFile);
					Prepare();
				}

				if (IsActual())
				{
					eStatus = Status.Starting;
					if (0 < _aAtoms.Count)
					{
						(new Logger()).WriteNotice("стартуем атомы:" + sFile);
						foreach (Atom cAtom in _aAtoms)
						{
							cAtom.Start();
						}
						(new Logger()).WriteNotice("шаблон графического оформления запущен:" + sFile);
						eStatus = Status.Started;
					}
					else
						OnAtomDone(null);
				}
			}
			catch
			{
				eStatus = Status.Failed;
				throw;
			}
		}

		public void StopAsync()
		{
			System.Threading.ThreadPool.QueueUserWorkItem(Stop, new System.Diagnostics.StackFrame(1, true));
		}
		private void Stop(object cState)
		{
			System.Diagnostics.StackFrame cStackFrame = null;
			try
			{
				if (null != cState)
					cStackFrame = (System.Diagnostics.StackFrame)cState;
				(new Logger()).WriteDebug3("Stop(): before stop" + sFile + ": status = " + eStatus);
				Stop();
				(new Logger()).WriteDebug3("Stop(): after stop" + sFile + ": status = " + eStatus);
			}
			catch (Exception ex)
			{
				if (null != cStackFrame)
					ex = new Exception(ex.Message + " stacktrace:" + cStackFrame.GetMethod().Name + " at " + cStackFrame.GetFileName() + ":" + cStackFrame.GetFileLineNumber() + "<br>" + ex.StackTrace);
				(new Logger()).WriteError(ex);
			}
		}
		public void Stop()
		{
			try
			{
				if (Status.Stopping == eStatus)
					return;
				eStatus = Status.Stopping;
				if (0 < _aAtoms.Count)
				{
					foreach (Atom cAtom in _aAtoms.ToArray())
					{
						(new Logger()).WriteDebug4("atom_stop: [status=" + cAtom.eStatus + "]");
                        if (Atom.Status.Prepared != cAtom.eStatus && Atom.Status.Started != cAtom.eStatus)
                        {
                            cAtom._bDone = true;
                            OnAtomDone(cAtom);
                        }
                        else
                            cAtom.Stop();
                    }
				}
				else
					OnAtomDone(null);
			}
			catch
			{
				eStatus = Status.Failed;
				throw;
			}
		}
		private void OnAtomDone(Atom cSender)
		{

			(new Logger()).WriteDebug3("OnAtomDone: Template=" + (null != cSender && null != cSender.cTemplate && null != cSender.cTemplate.sFile ? cSender.cTemplate.sFile : "???") + " cTag=" + (null != cSender && null != cSender.oTag ? cSender.oTag.ToString() : "???"));
			if (null != cSender)
				cSender.Stopped -= OnAtomDone;
			if (null == _aAtoms && (Status.Disposing != eStatus || Status.Disposed != eStatus))
				(new Logger()).WriteError(new Exception("template: отсутствует массив эффектов [file:" + sFile + "]"));

			if (null != _aAtoms)
			{
				foreach (Atom cAtom in _aAtoms)
				{
					if (!cAtom._bDone)       // (cAtom.eStatus == Atom.Status.Prepared || cAtom.eStatus == Atom.Status.Started)
						return;
				}
			}
			if (Status.Stopped == eStatus)
				return;
			eStatus = Status.Stopped;
			if (null != Done)
				ThreadPool.QueueUserWorkItem(DoneWorker);
		}
		private void DoneWorker(object cState)
		{
			try
			{
				if (null != Done)
					Done(this);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
	}
}
