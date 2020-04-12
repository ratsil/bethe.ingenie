using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using helpers;
using helpers.extensions;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Lifetime;

namespace ingenie.userspace
{
	public delegate void EventDelegate(Atom cSender);
	abstract public class Atom
	{
        public enum Status
        {
			Error = -1,
            Unknown = 0,
            Idle = 1,
            Prepared = 2,
            Started = 3,
            Stopped = 4
		}
		protected class Callbacks : shared.RemotelyDelegatableObject, ISponsor
		{
			private Atom _cAtom = null;
			public Callbacks()
			{ }
			public Callbacks(Atom cAtom)
			{
				_cAtom = cAtom;
			}
			~Callbacks()
			{
				try
				{
					Dispose();
					//(new Logger()).WriteNotice("~Callbacks()");
				}
				catch (Exception ex)
				{
					(new Logger()).WriteError(ex);
				}
			}
			public void Dispose()
			{
				(new Logger()).WriteDebug3("Callbacks:Dispose:" + (null == _cAtom ? "no" : "yes:" + _cAtom.GetHashCode()));
				_cAtom = null;
			}

			protected sealed override void CallbackTransport(shared.EffectEventType eType, shared.Effect cEffect)
			{
				if (null != _cAtom)
				{
					switch (eType)
					{
						case shared.EffectEventType.prepared:
							_cAtom.OnPrepared(cEffect);
							break;
						case shared.EffectEventType.started:
							_cAtom.OnStarted(cEffect);
							break;
						case shared.EffectEventType.stopped:
							_cAtom.OnStopped(cEffect);
							break;
						case shared.EffectEventType.failed:
							_cAtom.OnStopped(cEffect);
							break;
						default:
							throw new Exception("неизвестный тип события:" + eType.ToString()); //TODO LANG
					}
				}
				else
					(new Logger()).WriteWarning("effect:callback:transport: atom is null [" + eType.ToString() + "]");
			}
			protected sealed override void ContainerCallbackTransport(shared.ContainerEventType eType, shared.Effect cEffect)
			{
				(new Logger()).WriteDebug4("container:callback:transport:in:" + eType.ToString());
				if (null != _cAtom)
				{
					if (_cAtom is Container)
					{
						Container cContainer = (Container)_cAtom;
						switch (eType)
						{
							case shared.ContainerEventType.added:
								cContainer.OnEffectAdded(cEffect);
								break;
							case shared.ContainerEventType.prepared:
								cContainer.OnEffectPrepared(cEffect);
								break;
							case shared.ContainerEventType.started:
								cContainer.OnEffectStarted(cEffect);
								break;
							case shared.ContainerEventType.stopped:
								cContainer.OnEffectStopped(cEffect);
								break;
							case shared.ContainerEventType.onscreen:
								cContainer.OnEffectOnScreen(cEffect);
								break;
							case shared.ContainerEventType.offscreen:
								cContainer.OnEffectOffScreen(cEffect);
								break;
							case shared.ContainerEventType.failed:
								cContainer.OnEffectFailed(cEffect);
								break;
							default:
								throw new Exception("неизвестный тип события контейнера:" + eType.ToString()); //TODO LANG
						}
					}
					else
						throw new Exception("эффект не является контейнером:" + _cAtom.ToString() + ". событие контейнера:" + eType.ToString()); //TODO LANG
				}
				else
					(new Logger()).WriteWarning("container:callback:transport: atom is null [" + eType.ToString() + "]");
				(new Logger()).WriteDebug4("container:callback:transport:out:" + eType.ToString());
			}
			public override object InitializeLifetimeService()
			{
				ILease iLease = (ILease)base.InitializeLifetimeService();
				iLease.InitialLeaseTime = TimeSpan.FromSeconds(20);
				iLease.SponsorshipTimeout = TimeSpan.FromSeconds(20);
				iLease.RenewOnCallTime = TimeSpan.FromSeconds(20);
				iLease.Register(this);
				return iLease;
			}
			//[SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
			public TimeSpan Renewal(ILease lease)
			{
				(new Logger()).WriteDebug4("lease:renewal:" + (null == _cAtom ? "no" : "yes:" + _cAtom.GetHashCode()));
				if(null != _cAtom)
					return TimeSpan.FromSeconds(20);
				return TimeSpan.Zero;
			}
		}
		internal interface IContainer
		{
			event Container.EventDelegate EffectAdded;
			event Container.EventDelegate EffectPrepared;
			event Container.EventDelegate EffectStarted;
			event Container.EventDelegate EffectStopped;
			event Container.EventDelegate EffectOnScreen;
			event Container.EventDelegate EffectOffScreen;
			event Container.EventDelegate EffectFailed;
			void OnEffectAdded(shared.Effect cEffect);
			void OnEffectPrepared(shared.Effect cEffect);
			void OnEffectStarted(shared.Effect cEffect);
			void OnEffectStopped(shared.Effect cEffect);
			void OnEffectOnScreen(shared.Effect cEffect);
			void OnEffectOffScreen(shared.Effect cEffect);
			void OnEffectFailed(shared.Effect cEffect);
		}
        public class Delay
        {
            public ulong nDelay;

            public Delay()
            {
                nDelay = 0;
            }
            public void LoadXML(XmlAttribute cXmlAttribute)
            {
                if (null == cXmlAttribute)
                    return;
                try
                {
                    nDelay = cXmlAttribute.Value.Trim().ToUInt64();
                }
                catch
                {
                    throw new Exception("указана некорректная задержка [" + cXmlAttribute.Name + "=" + cXmlAttribute.Value + "][TPL:" + cXmlAttribute.BaseURI + "]"); //TODO LANG
                }
            }
            virtual public void LoadXML(XmlNode cXmlNode)
            {
                if (null == cXmlNode)
                    return;
                LoadXML(cXmlNode.Attributes["delay"]);
            }
        }
        public class SHOW : Delay //EMERGENCY:l мне кажется это все нахуй не нужно... оставить просто параметр nDelay и все) не хотим показывать - комментируем атом внутри файла шаблона... 
        {
            public bool bShow;
            public void LoadXML(XmlNode cXmlNode)
            {
                if (null == cXmlNode)
                    return;
                if (null != cXmlNode.FirstChild && null != cXmlNode.FirstChild.Value)
                    bShow = bool.Parse(cXmlNode.FirstChild.Value);
                base.LoadXML(cXmlNode);
            }
        }
        public class HIDE : Delay //EMERGENCY:l мне кажется это все нахуй не нужно... оставить внутри класса Container параметр типа eHideType и все... нахрен нужна задержка при уходе? либо она не нужна, либо я забыл какой-то специфический случай
        {
            public enum TYPE
            {
                stop,
                skip
            }

            public TYPE enType;
            public void LoadXML(XmlNode cXmlNode)
            {
                if (null == cXmlNode)
                    return;
                base.LoadXML(cXmlNode);
                if (null != cXmlNode.Attributes["type"])
                {
                    try
                    {
                        enType = (TYPE)Enum.Parse(typeof(TYPE), System.Text.RegularExpressions.Regex.Replace(cXmlNode.Attributes["type"].Value.Trim(), "\\W", "_"), true);
                    }
                    catch
                    {
                        throw new Exception("указан некорректный тип остановки [" + cXmlNode.Attributes["type"].Name + "=" + cXmlNode.Attributes["type"].Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
                    }
                }
            }
        }

		internal static shared.Effect RemoteEffectGet(Effect cEffect)
		{
			return cEffect._cEffect;
		}
		protected Callbacks _cCallbacks;
		public bool _bDone;
		public bool _bCreating;
		public event EventDelegate Prepared;
		public event EventDelegate Started;
		public event EventDelegate Stopped;
		public event EventDelegate Failed;
		public Status eStatus
        {
            get
            {
				lock (_oSyncRoot)
				{
					if (_bDone)
						return Status.Stopped;
					if (_bCreating)
						return Status.Idle;
					if (null != _cEffect)
						return StatusTranslate(_cEffect.eStatus);
				}
				return Status.Unknown;
            }
			//set
			//{
			//    if (null != _cEffect && (int)_cEffect.eStatus != (int)value)
			//        _cEffect.eStatus = (shared.Status)value;
			//    _eStatus = value;
			//}
        }

        protected shared.Effect _cEffect;
		public Template cTemplate { get; set; }
        public SHOW cShow;
        public HIDE cHide;
        public ulong nDelay
        {
            get
            {
                if (null != _cEffect)
                    return ((shared.Effect)_cEffect).nDelay;
                return cShow.nDelay;
            }
            set
            {
                if (null != _cEffect)
                    ((shared.Effect)_cEffect).nDelay = value;
                cShow.nDelay = value;
            }
        }

        public object oTag { get; set; }
		private object _oSyncRoot = new object();

		public Atom()
		{
            cShow = new SHOW();
            cShow.nDelay = 0;
            cShow.bShow = true;
            cHide = new HIDE();
            cHide.nDelay = 0;
            cHide.enType = HIDE.TYPE.stop;
            oTag = ""; // logging
			_cEffect = null;
			_bDone = false;
			_bCreating = false;
			cTemplate = null;
		}
		~Atom()
		{
            try
            {
                Dispose();
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
        }
		virtual public void Dispose()
		{
            try
            {
				lock (_oSyncRoot)
					if (null != _cEffect)
					{
						int nHC = _cEffect.GetHashCode();
						_cCallbacks.Dispose();
						cTemplate = null;
						oTag = null;
						OnStopped(_cEffect);
						try
						{
							_cEffect.EventRaised -= _cCallbacks.Callback;
							_cEffect.Dispose();
						}
						catch (Exception ex)
						{
							(new Logger()).WriteDebug2(ex.Message);
						}
						_cEffect = null;
						(new Logger()).WriteDebug4("atom dispose [hc:" + nHC + "]");//TODO LANG
					}
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
		}

		static private Status StatusTranslate(shared.Status eStatus)
		{
			switch (eStatus)
			{
				case shared.Status.Error:
					return Status.Error;
				case shared.Status.Idle:
					return Status.Idle;
				case shared.Status.Prepared:
					return Status.Prepared;
				case shared.Status.Started:
					return Status.Started;
				case shared.Status.Stopped:
					return Status.Stopped;
			}
			return Status.Unknown;
		}

		abstract internal void Create();
		protected void Create(Type cTypeShared)
		{
			byte nN = 0;
			System.Runtime.Remoting.Activation.UrlAttribute cUrlAttribute_debug = null;
			try
			{
				Helper.InitializeTCPChannel();
				nN = 1;
				cUrlAttribute_debug = Preferences.cUrlAttribute;
				nN = 2;
				_cEffect = (shared.Effect)Activator.CreateInstance(cTypeShared, null, new object[] { cUrlAttribute_debug });
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError("creating atom [nN=" + nN + "][url=" + (cUrlAttribute_debug == null ? "null" : cUrlAttribute_debug.Name + " -- " + cUrlAttribute_debug.UrlValue) + "][type=" + (null == cTypeShared ? "null" : cTypeShared.Name) + "]", ex);
				throw ex;
			}
			if (null == _cEffect)
				throw new Exception("невозможно создать удаленный эффект"); //TODO LANG
			(new Logger()).WriteDebug3("effect:create: [type:" + cTypeShared.ToString() + "][hc:" + _cEffect.GetHashCode() + "][hcl:" + GetHashCode() + "]");
			_cCallbacks = new Callbacks(this);
			_cEffect.EventRaised += _cCallbacks.Callback;
			if (this is Container && _cEffect is shared.Container)
				((shared.Container) _cEffect).ContainerEventRaised += _cCallbacks.ContainerCallback;
			(new Logger()).WriteDebug3("effect:create:return [type:" + cTypeShared.ToString() + "][hc:" + _cEffect.GetHashCode() + "]");
		}
        virtual public void LoadXML(XmlNode cXmlNode)
        {
            if (null == cXmlNode)
                return;
            XmlNode cChildNode = cXmlNode.SelectSingleNode("show");
            if (null != cChildNode)
                cShow.LoadXML(cChildNode);
            cChildNode = cXmlNode.SelectSingleNode("hide");
            if (null != cChildNode)
                cHide.LoadXML(cChildNode);
        }

		void OnPrepared(shared.Effect cEffect)
		{
			if (null != Prepared)
			{
				(new Logger()).WriteDebug3("before Prepared(this)");
				Prepared(this);
				(new Logger()).WriteDebug4("after Prepared(this)");
			}
			else
				(new Logger()).WriteDebug3("empty Prepared");
		}
		void OnStarted(shared.Effect cEffect)
		{
			if (null != Started)
			{
				(new Logger()).WriteDebug3("before Started(this)");
				Started(this);
				(new Logger()).WriteDebug4("after Started(this)");
			}
			else
				(new Logger()).WriteDebug3("empty Started");
		}
		void OnStopped(shared.Effect cEffect)
		{
			lock (_oSyncRoot)
			{
				if (_bDone)
					return;
				_bDone = true;
			}
			if (null != Stopped)
				Stopped(this);
		}
		void OnFailed(shared.Effect cEffect)
		{
			lock (_oSyncRoot)   //UNDONE !!!!!!!!!!!!!!!!!!  просто скопировал со стоппеда
			{
				if (_bDone)
					return;
				_bDone = true;
			}
			if (null != Failed)
				Failed(this);
		}

		virtual public void Prepare()
		{
			if (null == _cEffect)
				Create();
			(new Logger()).WriteDebug3("effect:prepare:before: [hc:" + _cEffect.GetHashCode() + "]");
			if (!_cEffect.Prepare())
				throw new Exception("effect can't be prepared");
			(new Logger()).WriteDebug4("effect:prepare:after: [hc:" + _cEffect.GetHashCode() + "]");
		}
        virtual public void Start()
		{
			if (null == _cEffect || shared.Status.Prepared != _cEffect.eStatus)
				Prepare();
            if (!_cEffect.Start())
				throw new Exception("effect can't be started");
			(new Logger()).WriteDebug3("effect:start: [hc:" + _cEffect.GetHashCode() + "]");
		}
		virtual public void Stop()
		{
			if (null != _cEffect)
			{
				(new Logger()).WriteDebug3("effect:stop: [hc:" + _cEffect.GetHashCode() + "]");
				_cEffect.Stop();
			}
			else
				(new Logger()).WriteDebug3("effect:stop: [hc:NULL]");
		}
	}
	public class Plugin : Atom
	{
		public string sFile { get; set; }
		public string sClass { get; set; }
		public string sData { get; set; }
		public Plugin()
			: base()
		{
			sFile = "";
		}
		override public void LoadXML(XmlNode cXmlNode)
		{
			if (null == cXmlNode)
				return;
            base.LoadXML(cXmlNode);
            if((sFile = cXmlNode.AttributeValueGet("file", false)).IsNullOrEmpty())
			    throw new Exception("указано некорректное значение атрибута file плагина [TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
            if((sClass = cXmlNode.AttributeValueGet("class", false)).IsNullOrEmpty())
                throw new Exception("указано некорректное значение атрибута class плагина [TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
            if (null != (cXmlNode = cXmlNode.NodeGet("data", false)))
                sData = cXmlNode.OuterXml;
        }
		override internal void Create()
		{
			_bCreating = true;
			base.Create(typeof(shared.Plugin));
			((shared.Plugin)_cEffect).Create(sFile, sClass, sData);
			_bCreating = false;
		}
	}
	abstract public class Effect : Atom
	{
		protected ushort _nLayer;
		public List<Roll.Keyframe> aKeyframes;
		public bool bWaitForEmptySpace;
		public bool bIsMaskForAllUpper;
		public float nSpeed;
		public ushort nLayer
		{
			get
			{
				if (null != _cEffect)
					return ((shared.Effect)_cEffect).nLayer;
				return _nLayer;
			}
			set
			{
				if (null != _cEffect)
					((shared.Effect)_cEffect).nLayer = value;
				_nLayer = value;
			}
		}
		public ulong nDuration { get; set; }
        internal XmlNode _cXmlNode;
		public Effect()
			: base()
		{
			nLayer = 2;
			nDuration = ulong.MaxValue;   // TODO надо учитывать переходы как-то.... //давай мож переходы всегда за счет видео, чтобы всегда был материал для перехода.... т.е. реальный_дюр = дюр - переход_дюр/2
                                          //к сожалению, не всегда у нас есть видео...
        }
        override public void LoadXML(XmlNode cXmlNode)
		{
			if (null == cXmlNode)
				return;
            base.LoadXML(cXmlNode);
			if (0 < cXmlNode.Attributes.Count)
			{
				foreach (XmlAttribute cAttr in cXmlNode.Attributes)
					switch (cAttr.Name)
					{
						case "z-buffer":
						case "layer":
							try
							{
								nLayer = cAttr.Value.Trim().ToUInt16();
							}
							catch
							{
								throw new Exception("указан некорректный параметр: [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
						case "duration":
							try
							{
								nDuration = cAttr.Value.Trim().ToULong();
							}
							catch
							{
								throw new Exception("указана некорректная параметр: [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
						case "wait_empty_space":
							try
							{
								bWaitForEmptySpace = cAttr.Value.Trim().ToBool();
							}
							catch
							{
								throw new Exception("указан некорректный параметр: [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
						case "mask_all_upper":
							try
							{
								bIsMaskForAllUpper = cAttr.Value.Trim().ToBool();
							}
							catch
							{
								throw new Exception("указан некорректный параметр: [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
						case "speed":
							try
							{
								nSpeed = cAttr.Value.Trim().ToFloat();
							}
							catch
							{
								throw new Exception("указан некорректный параметр: [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
					}
			}
			XmlNode cChildNode = cXmlNode.SelectSingleNode("keyframes");
			if (null != cChildNode)
			{
				aKeyframes = new List<Roll.Keyframe>();
				while (0 < cChildNode.ChildNodes.Count)
				{
					if (cChildNode.ChildNodes[0].Name == "keyframe")
					{
						aKeyframes.Add(new Roll.Keyframe());
						aKeyframes[aKeyframes.Count - 1].LoadXML(cChildNode.ChildNodes[0]);
					}
					cChildNode.RemoveChild(cChildNode.ChildNodes[0]);
				}
			}
		}
    }
	abstract public class EffectVideo : Effect
	{
		public Dock cDock { get; set; }
		public bool bOpacity { get; set; }
		public Area stArea;
		public ulong nFrameStart { get; set; }
        private ulong? _nFramesTotal;
        public ulong nFramesTotal
        {
            get
            {
                if (null != _nFramesTotal)
                    return _nFramesTotal.Value;
                if (null != _cEffect)
                {
                    _nFramesTotal = ((shared.EffectVideo)_cEffect).nFramesTotal;
                    return _nFramesTotal.Value;
                }
                return ulong.MaxValue;
            }
        }

        protected void AreaSet(Area stArea)
		{
			if (null != _cEffect)
				((shared.EffectVideo)_cEffect).stArea = stArea;
			this.stArea = stArea;
		}
		protected Area AreaGet()
		{
			if (null != _cEffect)
				return ((shared.EffectVideo)_cEffect).stArea;
			return this.stArea;
		}
		public EffectVideo()
			: base()
		{
			cDock = new Dock(Dock.Corner.upper_left, new Dock.Offset(0, 0));
			bOpacity = true;
			stArea = Area.stEmpty;
		}
		override public void LoadXML(XmlNode cXmlNode)
		{
			if (null == cXmlNode)
				return;
			base.LoadXML(cXmlNode);
			if (0 < cXmlNode.Attributes.Count)
			{
				foreach (XmlAttribute cAttr in cXmlNode.Attributes)
					switch (cAttr.Name)
					{
						case "opacity":
							try
							{
								bOpacity = cAttr.Value.Trim().ToBool();
							}
							catch
							{
								throw new Exception("указана некорректная непрозрачность [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
						case "start":
							try
							{
								nFrameStart = cAttr.Value.Trim().ToULong();
							}
							catch
							{
								throw new Exception("указан некорректный начальный кадр [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
					}
			}
			XmlNode cChildNode = cXmlNode.SelectSingleNode("dock");
			if (null != cChildNode)
			{
				if (0 < cChildNode.Attributes.Count)
				{
					foreach (XmlAttribute cAttr in cChildNode.Attributes)
					{
						switch (cAttr.Name)
						{
							case "corner":
								try
								{
									cDock.eCorner = (Dock.Corner)Enum.Parse(typeof(Dock.Corner), System.Text.RegularExpressions.Regex.Replace(cAttr.Value.Trim(), "\\W", "_"), true);
								}
								catch
								{
									throw new Exception("указан некорректный угол привязки [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
								}
								break;
						}
					}
				}
				cChildNode = cChildNode.SelectSingleNode("offset");
				if (null != cChildNode)
				{
					Position cPosition = new Position();
					cPosition.LoadXML(cChildNode);
					cDock.cOffset.nLeft = cPosition.nLeft;
					cDock.cOffset.nTop = cPosition.nTop;
				}
			}
			cChildNode = cXmlNode.SelectSingleNode("size");
			if (null != cChildNode)
			{
				if (0 < cChildNode.Attributes.Count)
				{
					string sValue;
					string[] aSpecials = new string[] { "max", "maximum", "auto" };
					foreach (XmlAttribute cAttr in cChildNode.Attributes)
					{
						sValue = cAttr.Value.Trim().ToLower();
						switch (cAttr.Name)
						{
							case "width":
								try
								{
									if (aSpecials.Contains(sValue))
										stArea.nWidth = ushort.MaxValue;
									else
										stArea.nWidth = sValue.ToUInt16();
								}
								catch
								{
									throw new Exception("указано некорректное значение ширины [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
								}
								break;
							case "height":
								try
								{
									if (aSpecials.Contains(sValue))
										stArea.nHeight = ushort.MaxValue;
									else
										stArea.nHeight = cAttr.Value.Trim().ToUInt16();
								}
								catch
								{
									throw new Exception("указано некорректное значение высоты [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
								}
								break;
						}
					}
				}
				cChildNode = cXmlNode.SelectSingleNode("offset");
				if (null != cChildNode)
				{
					Position cPosition = new Position();
					cPosition.LoadXML(cChildNode);
					cDock.cOffset.nLeft = cPosition.nLeft;
					cDock.cOffset.nTop = cPosition.nTop;
				}
			}
		}
	}
	abstract public class EffectAudio : Effect
	{
		protected byte[] _aChannels;

		public byte[] aChannels
		{
			get
			{
				if (null != _cEffect)
					return ((shared.EffectAudio)_cEffect).aChannels;
				return _aChannels;
			}
			set
			{
				if (null != _cEffect)
					((shared.EffectAudio)_cEffect).aChannels = value;
				_aChannels = value;
			}
		}
		public EffectAudio()
			: base()
		{
			aChannels = null;
		}
		override public void LoadXML(XmlNode cXmlNode)
		{
			if (null == cXmlNode)
				return;
			base.LoadXML(cXmlNode);
			aChannels = ParseXML(cXmlNode);
		}
		static internal byte[] ParseXML(XmlNode cXmlNode)
		{
			byte[] aRetVal = null;
			if (null != cXmlNode)
			{
				XmlNode cChildNode = cXmlNode.SelectSingleNode("channels");
				if (null != cChildNode)
				{
					XmlNodeList cChannelNodes = cChildNode.SelectNodes("channel");
					int nSource, nTarget;
					aRetVal = new byte[cChannelNodes.Count];
					for (int nIndx = 0; nIndx < cChannelNodes.Count; nIndx++)
					{
						nSource = nTarget = -1;
						foreach (XmlAttribute cXmlAttribute in cChannelNodes[nIndx].Attributes)
						{
							switch (cXmlAttribute.Name)
							{
								case "source":
									try
									{
										nSource = cXmlAttribute.Value.Trim().ToByte();
									}
									catch
									{
										throw new Exception("указан некорректный исходный аудиоканал [" + cXmlAttribute.Name + "=" + cXmlAttribute.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
									}
									break;
								case "target":
									try
									{
										nTarget = cXmlAttribute.Value.Trim().ToByte();
									}
									catch
									{
										throw new Exception("указан некорректный целевой аудиоканал [" + cXmlAttribute.Name + "=" + cXmlAttribute.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
									}
									break;
							}
						}
						if (-1 < nSource && -1 < nTarget)
						{
							aRetVal[nSource] = (byte)nTarget;
						}
						else
							throw new Exception("указана некорректная пара исходного и целевого аудиоканалов [TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
					}
				}
			}
			return aRetVal;
		}
	}
	abstract public class EffectVideoAudio : EffectVideo
	{
		protected byte[] _aChannels;

		public byte[] aChannels
		{
			get
			{
				if (null != _cEffect)
					return ((shared.EffectAudio)_cEffect).aChannels;
				return _aChannels;
			}
			set
			{
				if (null != _cEffect)
					((shared.EffectAudio)_cEffect).aChannels = value;
				_aChannels = value;
			}
		}

		public EffectVideoAudio()
			: base()
		{ }
		override public void LoadXML(XmlNode cXmlNode)
		{
			if (null == cXmlNode)
				return;
			base.LoadXML(cXmlNode);
			aChannels = EffectAudio.ParseXML(cXmlNode);
		}
	}
	abstract public class Container : EffectVideoAudio, Atom.IContainer
	{
		protected class Item
		{
			public ushort nOrder;
			public userspace.Effect cEffect;
			public shared.Effect cEffectShared;
			public Item(ushort nOrder, userspace.Effect cEffect)
			{
				this.nOrder = nOrder;
				this.cEffect = cEffect;
			}
		}
		public delegate void EventDelegate(Container cContainer, userspace.Effect cEffect);
		#region Events
		public event EventDelegate EffectAdded;
		public event EventDelegate EffectPrepared;
		public event EventDelegate EffectStarted;
		public event EventDelegate EffectStopped;
		public event EventDelegate EffectOnScreen;
		public event EventDelegate EffectOffScreen;
		public event EventDelegate EffectFailed;

		internal void OnEffectAdded(shared.Effect cEffect)
		{
			if (null != EffectAdded)
				EffectAdded(this, ItemGet(cEffect).cEffect);
		}
		internal void OnEffectPrepared(shared.Effect cEffect)
		{
			if (null != EffectPrepared)
				EffectPrepared(this, ItemGet(cEffect).cEffect);
		}
		internal void OnEffectStarted(shared.Effect cEffect)
		{
			if (null != EffectStarted)
				EffectStarted(this, ItemGet(cEffect).cEffect);
		}
		internal void OnEffectStopped(shared.Effect cEffect)
		{
			Item cItem = ItemGet(cEffect);
			if (null != cItem)
			{
				if (null != EffectStopped) // null == cItem  бывает, если удалили элемент плейлиста с помощью PLItemsDelete и он уже отремувлен похоже...
					EffectStopped(this, cItem.cEffect);
				ItemRemove(cItem);
			}
		}
		internal void OnEffectOnScreen(shared.Effect cEffect)
		{
			if (null != EffectOnScreen)
				EffectOnScreen(this, ItemGet(cEffect).cEffect);
		}
		internal void OnEffectOffScreen(shared.Effect cEffect)
		{
			if (null != EffectOffScreen)
				EffectOffScreen(this, ItemGet(cEffect).cEffect);
		}
		internal void OnEffectFailed(shared.Effect cEffect)
		{
			Item cItem = ItemGet(cEffect);
			if (null != cItem)
			{
				if (null != EffectFailed) // null == cItem  бывает, если удалили элемент плейлиста с помощью PLItemsDelete и он уже отремувлен похоже...
					EffectFailed(this, cItem.cEffect);
				ItemRemove(cItem);
			}
		}
		#endregion
		protected LinkedList<Item> _aItems;
        public ushort nEffectsQty
        {
            get
            {
                if (null != _cEffect)
                    return ((shared.Container)_cEffect).nEffectsQty;
                return 0;
            }
        }
		public ulong nSumDuration
		{
			get
			{
				if (null != _cEffect)
					return ((shared.Container)_cEffect).nSumDuration;
				return 0;
			}
		}
		public Container()
			: base()
		{
			_aItems = new LinkedList<Item>();
		}
		override public void Dispose()
		{
			try
			{
				if (null != _cEffect)
				{
					int nHC = _cEffect.GetHashCode();
					((shared.Container)_cEffect).ContainerEventRaised -= _cCallbacks.ContainerCallback;
					Item[] aItems;
					lock (_aItems)
						aItems = _aItems.ToArray();
					foreach (Item cItem in aItems)
						cItem.cEffect.Dispose();
					lock (_aItems)
						_aItems.Clear();
					(new Logger()).WriteDebug3("container dispose [hc:" + nHC + "]");//TODO LANG
					base.Dispose();
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}

		override public void LoadXML(XmlNode cXmlNode)
		{
			if (null == cXmlNode)
				return;
			base.LoadXML(cXmlNode);
			XmlNode cChildNode = cXmlNode.SelectSingleNode("effects");
			if (null != cChildNode)
			{
				ushort nOrder = 0;
				while (0 < cChildNode.ChildNodes.Count)
				{
					if (0 < cChildNode.ChildNodes[0].Attributes.Count && null != cChildNode.ChildNodes[0].Attributes["order"])
					{
						try
						{
							nOrder = cChildNode.ChildNodes[0].Attributes["order"].Value.ToUShort();
							lock (_aItems)
								if (null != _aItems.FirstOrDefault(o => o.nOrder == nOrder))
									throw new Exception("эффект с таким порядковым номером уже добавлен [order = " + nOrder + "]");
						}
						catch
						{
							throw new Exception("указан некорректный параметр [order=" + cChildNode.ChildNodes[0].Attributes["order"] + "][TPL:" + cChildNode.ChildNodes[0].BaseURI + "]"); //TODO LANG
						}
					}
					else
						throw new Exception("отсутствует необходимый параметр [order][TPL:" + cChildNode.ChildNodes[0].BaseURI + "]"); //TODO LANG   
					Item cItem;
					switch (cChildNode.ChildNodes[0].Name)
					{
						case "animation":
							cItem = new Item(nOrder, new Animation());
							break;
						case "video":
							cItem = new Item(nOrder, new Video());
							break;
						case "text":
							cItem = new Item(nOrder, new Text());
							break;
						case "clock":
							cItem = new Item(nOrder, new Clock());
							break;
						default:
							throw new Exception("указан некорректный эффект для плейлиста [" + cChildNode.ChildNodes[0].Name + "]"); //TODO LANG
					}
					cItem.cEffect.cTemplate = this.cTemplate;
					cItem.cEffect.LoadXML(cChildNode.ChildNodes[0]);
					lock (_aItems)
						_aItems.AddLast(cItem);
					cChildNode.RemoveChild(cChildNode.ChildNodes[0]);
				}
			}
		}
		protected void ItemRemove(Item cItem)
		{
			lock (_aItems)
				_aItems.Remove(cItem);
			cItem.cEffect.Dispose();
		}
		protected Item ItemGet(shared.Effect cEffect)
		{
			lock (_aItems)
				return _aItems.FirstOrDefault(o => o.cEffectShared == cEffect);
		}
		protected Item ItemGet(userspace.Effect cEffect)
		{
			lock (_aItems)
				return _aItems.FirstOrDefault(o => o.cEffect == cEffect);
		}

		event EventDelegate Atom.IContainer.EffectAdded
		{
			add
			{
				this.EffectAdded += value;
			}
			remove
			{
				this.EffectAdded -= value;
			}
		}
		event EventDelegate Atom.IContainer.EffectPrepared
		{
			add
			{
				this.EffectPrepared += value;
			}
			remove
			{
				this.EffectPrepared -= value;
			}
		}
		event EventDelegate Atom.IContainer.EffectStarted
		{
			add
			{
				this.EffectStarted += value;
			}
			remove
			{
				this.EffectStarted -= value;
			}
		}
		event EventDelegate Atom.IContainer.EffectStopped
		{
			add
			{
				this.EffectStopped += value;
			}
			remove
			{
				this.EffectStopped -= value;
			}
		}
		event EventDelegate Atom.IContainer.EffectOnScreen
		{
			add
			{
				this.EffectOnScreen += value;
			}
			remove
			{
				this.EffectOnScreen -= value;
			}
		}
		event EventDelegate Atom.IContainer.EffectOffScreen
		{
			add
			{
				this.EffectOffScreen += value;
			}
			remove
			{
				this.EffectOffScreen -= value;
			}
		}
		event EventDelegate Atom.IContainer.EffectFailed
		{
			add
			{
				this.EffectFailed += value;
			}
			remove
			{
				this.EffectFailed -= value;
			}
		}

		void Atom.IContainer.OnEffectAdded(shared.Effect cEffect)
		{
			this.OnEffectAdded(cEffect);
		}
		void Atom.IContainer.OnEffectPrepared(shared.Effect cEffect)
		{
			this.OnEffectPrepared(cEffect);
		}
		void Atom.IContainer.OnEffectStarted(shared.Effect cEffect)
		{
			this.OnEffectStarted(cEffect);
		}
		void Atom.IContainer.OnEffectStopped(shared.Effect cEffect)
		{
			this.OnEffectStopped(cEffect);
		}
		void Atom.IContainer.OnEffectOnScreen(shared.Effect cEffect)
		{
			this.OnEffectOnScreen(cEffect);
		}
		void Atom.IContainer.OnEffectOffScreen(shared.Effect cEffect)
		{
			this.OnEffectOffScreen(cEffect);
		}
		void Atom.IContainer.OnEffectFailed(shared.Effect cEffect)
		{
			this.OnEffectFailed(cEffect);
		}
	}
	public class Video : EffectVideoAudio
    {
        public string sFile { get; set; }
        public ushort nLoopsQty { get; set; }
        public Video()
            : base()
        {
            sFile = null;
            nLoopsQty = 0;
        }
        public Video(XmlNode cNode)
            : base()
        {
            _cXmlNode = cNode;
        }
        override public void LoadXML(XmlNode cXmlNode)
        {
            sFile = null;
            nLoopsQty = 0;
            base.LoadXML(cXmlNode);
            foreach (XmlAttribute cAttr in cXmlNode.Attributes)
            {
                try
                {
                    switch (cAttr.Name)
                    {
                        case "loop":
                            nLoopsQty = cAttr.Value.ToUInt16();
                            break;
                        case "file":
                            sFile = cAttr.Value;
                            oTag = sFile; // logging
                            break;
                    }
                }
                catch
                {
                    throw new Exception("указан некорректный параметр [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
                }
            }
            if (null == sFile)
                throw new Exception("отсутствует необходимый параметр file [TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
                                                                                                             //TODO сделать корректно:
                                                                                                             //if (!System.IO.File.Exists(sFile))
                                                                                                             //    throw new Exception("отсутствует указанный файл [" + sFile + "]"); //TODO LANG
        }
        override internal void Create()
        {
            _bCreating = true;
            base.Create(typeof(shared.Video));
            shared.Video cVideoRemote = (shared.Video)_cEffect;
            if (_cXmlNode == null)
            {
                cVideoRemote.Create(sFile, cDock, _nLayer, nFrameStart, nDuration, bOpacity, cShow.nDelay);
                AreaSet(stArea);
                cVideoRemote.cDock = cDock;
                cVideoRemote.aChannels = _aChannels;
            }
            else
            {
                cVideoRemote.Create(_cXmlNode.OuterXml);
            }
            _bCreating = false;
        }
    }
    public class Audio : EffectAudio
	{
		public string sFile { get; set; }
		public Audio()
			: base()
		{
			sFile = null;
		}
        public Audio(XmlNode cNode)
            : base()
        {
            _cXmlNode = cNode;
        }
        override public void LoadXML(XmlNode cXmlNode)
        {
            sFile = null;
            base.LoadXML(cXmlNode);
            foreach (XmlAttribute cAttr in cXmlNode.Attributes)
            {
                try
                {
                    switch (cAttr.Name)
                    {
                        case "file":
                            sFile = cAttr.Value;
                            oTag = sFile; // logging
                            break;
                    }
                }
                catch
                {
                    throw new Exception("указан некорректный параметр [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
                }
            }
            if (null == sFile)
                throw new Exception("отсутствует необходимый параметр file [TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
                                                                                                             //TODO сделать корректно:
                                                                                                             //if (!System.IO.File.Exists(sFile))
                                                                                                             //    throw new Exception("отсутствует указанный файл [" + sFile + "]"); //TODO LANG
        }
        override internal void Create()
        {
            _bCreating = true;
            (new Logger()).WriteDebug3("audio:create:in [hcl:" + GetHashCode() + "]");
            base.Create(typeof(shared.Audio));
            shared.Audio cRemote = (shared.Audio)_cEffect;
            if (_cXmlNode == null)
            {
                cRemote.Create(sFile);
                nDelay = cShow.nDelay;
                aChannels = _aChannels;
            }
            else
            {
                cRemote.Create(_cXmlNode.OuterXml);
            }
            (new Logger()).WriteDebug4("audio:create:return [hc:" + _cEffect.GetHashCode() + "]");
            _bCreating = false;
        }
    }
    public class Animation : EffectVideo
    {
        public string sFolder { get; set; }
        public ushort nLoopsQty { get; set; }
		public bool bKeepAlive { get; set; }
		public float nPixelAspectRatio { get; set; }
		public bool bTurnOffQueue { get; set; }
		public Animation()
			: base()
		{
			sFolder = "";
			nLoopsQty = 1;
			bKeepAlive = true;
			bTurnOffQueue = false;
		}
        public Animation(XmlNode cNode)
            : base()
        {
            _cXmlNode = cNode;
        }
        override public void LoadXML(XmlNode cXmlNode)
		{
            if (null == cXmlNode)
                return;
            base.LoadXML(cXmlNode);
			foreach (XmlAttribute cAttr in cXmlNode.Attributes)
			{
				try
				{
					switch (cAttr.Name)
					{
						case "loop":
							nLoopsQty = cAttr.Value.ToUInt16();
							break;
						case "keep_alive":
							bKeepAlive = cAttr.Value.ToBool();
							break;
						case "folder":
							sFolder = cAttr.Value;
							oTag = sFolder; // logging
							break;
						case "turn_off_queue":
							bTurnOffQueue = cAttr.Value.ToBool();
							break;
					}
				}
				catch
				{
					throw new Exception("указан некорректный параметр [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
				}
			}
            if (null == sFolder)
                throw new Exception("отсутствует необходимый параметр file [TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
            //TODO сделать корректно
            /* 
            if (!System.IO.Directory.Exists(sFolder))
                throw new Exception("отсутствует указанная папка [" + sFolder + "]"); //TODO LANG
            if (1 > System.IO.Directory.GetFiles(sFolder).Length) //UNDONE нужно еще проверять тип файлов
                throw new Exception("в указанной папке отсутствуют файлы [" + sFolder + "]"); //TODO LANG
            */
		}
		override internal void Create()
        {
            _bCreating = true;
            base.Create(typeof(shared.Animation));
            if (_cXmlNode == null)
            {
                ((shared.Animation)_cEffect).Create(sFolder, nLoopsQty, bKeepAlive, cDock, _nLayer, bOpacity, cShow.nDelay, nPixelAspectRatio, bTurnOffQueue);
                AreaSet(stArea);
            }
            else
            {
                ((shared.Animation)_cEffect).Create(_cXmlNode.OuterXml);
            }
            _bCreating = false;
        }
    }
    public class Text : EffectVideo
	{
		public Font cFont { get; set; }
		public string sText { get; set; }
		public byte nInDissolve { get; set; }
		public byte nOutDissolve { get; set; }
		public ushort nWidthMax { get; set; }
		public ushort nHeightMax { get; set; }
		public Text()
			: base()
		{
			cFont = new Font();
			sText = "";
			nWidthMax = ushort.MaxValue;
			nHeightMax = ushort.MaxValue;
		}
        public Text(XmlNode cNode)
            : base()
        {
            _cXmlNode = cNode;
        }
        override public void LoadXML(XmlNode cXmlNode)
		{
			if (null == cXmlNode)
				return;
			base.LoadXML(cXmlNode);
			foreach (XmlAttribute cAttr in cXmlNode.Attributes)
				switch (cAttr.Name)
				{
					case "in_dissolve":
						try
						{															
							nInDissolve = cAttr.Value.Trim().ToByte();
						}
						catch
						{
							throw new Exception("указана некорректная длительность эффекта [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
						}
						break;
					case "out_dissolve":
						try
						{
							nOutDissolve = cAttr.Value.Trim().ToByte();
						}
						catch
						{
							throw new Exception("указана некорректная длительность эффекта [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
						}
						break;
					case "width_max":
						try
						{
							nWidthMax = cAttr.Value.Trim().ToUInt16();
						}
						catch
						{
							throw new Exception("указана некорректная максимальная ширина [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
						}
						break;
					case "height_max":
						try
						{
							nHeightMax = cAttr.Value.Trim().ToUInt16();
						}
						catch
						{
							throw new Exception("указана некорректная максимальная высота [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
						}
						break;
				}
			XmlNode cChildNode = cXmlNode.SelectSingleNode("value");
			if (null != cChildNode)
			{
				if (null == cChildNode.FirstChild || null == cChildNode.FirstChild.Value || 1 > cChildNode.FirstChild.Value.Length)
					sText = " ";
				else
					sText = cChildNode.FirstChild.Value.FromXML();
					
				cXmlNode.RemoveChild(cChildNode);
			}
			cChildNode = cXmlNode.SelectSingleNode("font");
			if (null != cChildNode)
			{
				cFont.cTemplate = cTemplate;
				cFont.LoadXML(cChildNode);
				cXmlNode.RemoveChild(cChildNode);
			}
		}
		override internal void Create()
		{
			_bCreating = true;
			base.Create(typeof(shared.Text));
            if (_cXmlNode == null)
            {
                object[] aArgs = { sText, cFont.sName, cFont.nSize, (System.Drawing.FontStyle)cFont.nFontStyle,
                                                 cFont.cColor.nRed, cFont.cColor.nGreen, cFont.cColor.nBlue,
                                                 cFont.cBorder.nWidth,
                                                 cFont.cBorder.cColor.nRed, cFont.cBorder.cColor.nGreen, cFont.cBorder.cColor.nBlue,
                                                 cDock.cOffset.nLeft, cDock.cOffset.nTop,
                                                 _nLayer, cShow.nDelay, nDuration, bOpacity,
                                                 nInDissolve, nOutDissolve,
                                                 cFont.cColor.nAlpha, cFont.cBorder.cColor.nAlpha,
                                                 cDock.eCorner, nWidthMax, nHeightMax
                             };
                ((shared.Text)_cEffect).Create(aArgs);
                AreaSet(stArea);
                oTag = sText; // logging
            }
            else
            {
                ((shared.Text)_cEffect).Create(_cXmlNode.OuterXml);
            }
			_bCreating = false;
		}
	}
	public class Clock : Text
	{
		public string sFormat { get; set; }
		public string sSuffix
		{
			get
			{
				return sText;
			}
			set
			{
				sText = value;
			}
		}
		public Clock()
			: base()
		{
			sFormat = "HH:mm";
		}
        public Clock(XmlNode cNode)
            : base()
        {
            _cXmlNode = cNode;
        }
        override public void LoadXML(XmlNode cXmlNode)
		{
			if (null == cXmlNode)
				return;
			base.LoadXML(cXmlNode);
			if (0 < cXmlNode.Attributes.Count)
			{
				foreach (XmlAttribute cAttr in cXmlNode.Attributes)
					switch (cAttr.Name)
					{
						case "format":
							try
							{
								sFormat = cAttr.Value.Trim().FromXML();
								DateTime.Now.ToString(sFormat); //проверка на корректность
							}
							catch
							{
								throw new Exception("указан некорректный параметр [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
					}
			}
			XmlNode cChildNode = cXmlNode.SelectSingleNode("suffix");
			if (null != cChildNode)
			{
				sSuffix = cChildNode.FirstChild.Value.FromXML();
				if (null == sSuffix || 1 > sSuffix.Length)
					sSuffix = "";
			}
		}
		override internal void Create()
		{
			_bCreating = true;
			base.Create(typeof(shared.Clock));
            if (_cXmlNode == null)
            {
                object[] aArgs = { sFormat, cFont.sName, cFont.nSize, (System.Drawing.FontStyle)cFont.nFontStyle,
                                                 cFont.cColor.nRed, cFont.cColor.nGreen, cFont.cColor.nBlue,
                                                 cFont.cBorder.nWidth,
                                                 cFont.cBorder.cColor.nRed, cFont.cBorder.cColor.nGreen, cFont.cBorder.cColor.nBlue,
                                                 cDock.cOffset.nLeft, cDock.cOffset.nTop,
                                                 _nLayer, cShow.nDelay, nDuration, sSuffix, bOpacity,
                                                 cFont.cColor.nAlpha, cFont.cBorder.cColor.nAlpha,
                                                 cDock.eCorner
                             };
                ((shared.Clock)_cEffect).Create(aArgs);
                oTag = sFormat;
            }
            else
            {
                ((shared.Clock)_cEffect).Create(_cXmlNode.OuterXml);
            }
			_bCreating = false;
		}
	}
	public class Playlist : Container
	{
		public bool bStopOnEmpty { get; set; }
        public bool bSkipLastEffect;
        public ushort nSkipTransitionDuration;
		public Playlist()
			: base()
		{
            bSkipLastEffect = true;
            nSkipTransitionDuration = 0;
			bStopOnEmpty = true;
			aChannels = new byte[0];
		}
        public Playlist(XmlNode cNode)
            : base()
        {
            _cXmlNode = cNode;
        }
        override public void LoadXML(XmlNode cXmlNode)
		{
			if (null == cXmlNode)
				return;
			base.LoadXML(cXmlNode);
			if (0 < cXmlNode.Attributes.Count)
			{
				foreach (XmlAttribute cAttr in cXmlNode.Attributes)
				{
					try
					{
						switch (cAttr.Name)
						{
							case "stop_on_empty":
								bStopOnEmpty = cAttr.Value.Trim().ToBool();
								break;
						}
					}
					catch
					{
						throw new Exception("указан некорректный параметр [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
					}
				}
			}
		}

		override internal void Create()
		{
			_bCreating = true;
            (new Logger()).WriteDebug3("playlist:create:in [hcl:" + GetHashCode() + "]");
            base.Create(typeof(shared.Playlist));
            shared.Playlist cPlaylistRemote = (shared.Playlist)_cEffect;
            if (_cXmlNode == null)
            {
                oTag = "playlist + nz=" + _nLayer + " + cDock=" + cDock.eCorner.ToString() + "x " + cDock.cOffset.nLeft + "y " + cDock.cOffset.nTop;
                cPlaylistRemote.Create(cDock, _nLayer, bStopOnEmpty, bOpacity, cShow.nDelay);
                AreaSet(stArea);
                cPlaylistRemote.cDock = cDock;
                cPlaylistRemote.aChannels = _aChannels;
                cPlaylistRemote.nDuration = nDuration;
                Item[] aItems;
                lock (_aItems)
                    aItems = _aItems.OrderBy(o => o.nOrder).ToArray();
                for (int nIndx = 0; aItems.Length > nIndx; nIndx++)
                    EffectAdd(aItems[nIndx].cEffect);
            }
            else
            {
                cPlaylistRemote.Create(_cXmlNode.OuterXml);
            }
			(new Logger()).WriteDebug4("playlist:create:return [hc:" + _cEffect.GetHashCode() + "]");
			_bCreating = false;
		}
		override public void Stop()
		{
			if (cHide.enType == userspace.Effect.HIDE.TYPE.skip && eStatus == Status.Started) // препэред не скипится.
			{
				((shared.Playlist)_cEffect).nDuration = ((shared.Playlist)_cEffect).nFrameCurrent + 250;        // выше-ниже сказанное оказалось верно - попытка поправить, но не два чего-то теперь, а вечное отключение титрования из-за "still waiting for"
				((shared.Playlist)_cEffect).Skip(false, 0, null);                             // и строго говоря, не скипиться должен стартед, который ещё не проплеел свой "in" - потом бесконечный loop.
			}
			else                                                                              // и может два чего-то в эфире - это из-за этого...  не знаю что делать...
				base.Stop();
		}
		public void EffectAdd(userspace.Effect cEffect)
        {
            EffectAdd(cEffect, 0);
        }
		public void EffectAdd(userspace.Effect cEffect, ushort nTransDur)
		{
            if (null == _cEffect)
                Create();
			cEffect.Create();
			Item cItem = new Item(ushort.MaxValue, cEffect);
			cItem.cEffectShared = RemoteEffectGet(cEffect);
			lock (_aItems)
				_aItems.AddLast(cItem);
			((shared.Playlist)_cEffect).EffectAdd(cItem.cEffectShared, nTransDur);
		}
        public void EndTransDurationSet(ushort nEndTransDuration)
        {
            ((shared.Playlist)_cEffect).EndTransDurationSet(nEndTransDuration); 
        }
		public void Skip()
		{
			Skip(null);
		}
		public void Skip(userspace.Effect cEffect)
		{
			(new Logger()).WriteDebug("skip: [e:" + (null == cEffect ? "null" : cEffect.nDuration.ToString()) + "]");
			Item cItem = null;
			shared.Effect cEffectShared = null;
			if (null != cEffect)
			{
				lock(_aItems)
					if (null == (cItem = _aItems.FirstOrDefault(o => o.cEffect == cEffect)))
						throw new Exception("эффект для скипа не найден!");
				cEffectShared = cItem.cEffectShared;
			}
			((shared.Playlist)_cEffect).Skip(bSkipLastEffect, nSkipTransitionDuration, cEffectShared); //UNDONE
		}
		public void PLItemsDelete(List<int> aEffectIDs)
        {
			Item[] aItems;
			//int n1 = aEffectIDs[0];
			//int n2 = _aItems.ElementAt(0).cEffect.GetHashCode();
			//int n3 = _aItems.ElementAt(1).cEffect.GetHashCode();
			lock (_aItems)
				aItems = _aItems.Where(o => aEffectIDs.Contains(o.cEffect.GetHashCode())).ToArray();
			shared.Effect[] aShareds = aItems.Select(o => o.cEffectShared).ToArray();
			((shared.Playlist)_cEffect).PLItemDelete(aShareds);
			foreach (Item cItem in aItems)
				ItemRemove(cItem);
		}
	}
    public class Composite : Container
    {
        public Composite()
            : base()
        {
            throw new Exception("not realized Composite() - move composite to 'effects' node");
        }
        override public void LoadXML(XmlNode cXmlNode)
        {
            throw new Exception("not realized LoadXML - move composite to 'effects' node");
        }
        public Composite(XmlNode cNode)
            : base()
        {
            _cXmlNode = cNode;
        }
        override internal void Create()
        {
            _bCreating = true;
            (new Logger()).WriteDebug3("roll:create:in [hcl:" + GetHashCode() + "]");
            base.Create(typeof(shared.Composite));
            shared.Composite cRollRemote = (shared.Composite)_cEffect;
            if (_cXmlNode != null)
            {
                cRollRemote.Create(_cXmlNode.OuterXml);
            }
            (new Logger()).WriteDebug4("roll:create:return [hc:" + _cEffect.GetHashCode() + "]");
            _bCreating = false;
        }
    }

    public class Roll : Container
	{
		public class Keyframe
		{
			public enum Type
			{
				calculated = 0,
				hold = 1,
				linear = 2,
				bezier = 3
			}
			public float nPosition;
			public long nFrame;
			public float nBesierControlPointFrames;
			public float nBesierControlPointPixels;
			public Type eType;
			public Keyframe()
			{
				nPosition = 0;
				nFrame = 0;
				nBesierControlPointFrames = 0;
				nBesierControlPointPixels = 0;
				eType = Type.linear;
			}
			public void LoadXML(XmlNode cXmlNode)
			{
				if (null == cXmlNode)
					return;
				if (0 < cXmlNode.Attributes.Count)
				{
					string sValue;
					foreach (XmlAttribute cAttr in cXmlNode.Attributes)
					{
						try
						{
							switch (cAttr.Name)
							{
								case "type":
									eType = (Type)Enum.Parse(typeof(Type), cAttr.Value.Trim(), true);
									break;
								case "frame":
									sValue = cAttr.Value.Trim();
									if (null == sValue || 1 > sValue.Length)
										sValue = "0";
									nFrame = sValue.ToLong();
									break;
								case "position":
									sValue = cAttr.Value.Trim();
									if (null == sValue || 1 > sValue.Length)
										sValue = "0";
									nPosition = sValue.ToFloat();
									break;
								case "control_point_frame":
									sValue = cAttr.Value.Trim();
									if (null == sValue || 1 > sValue.Length)
										sValue = "0";
									nBesierControlPointFrames = sValue.ToFloat();
									break;
								case "control_point_position":
									sValue = cAttr.Value.Trim();
									if (null == sValue || 1 > sValue.Length)
										sValue = "0";
									nBesierControlPointPixels = sValue.ToFloat();
									break;
							}
						}
						catch
						{
							throw new Exception("указан некорректный параметр [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
						}
					}
				}
			}
		}
		public enum Direction
		{
			LeftToRight,
			RightToLeft,
			UpToDown,
			DownToUp
		}

		private Direction _eDirection;
		private float _nSpeed;

		public Direction eDirection
		{
			get
			{
				if(null != _cEffect)
					return (Direction)Enum.Parse(typeof(Direction), ((shared.Roll)_cEffect).eDirection.ToString(), true);
				return _eDirection;
			}
			set
			{
				if (null != _cEffect)
					((shared.Roll)_cEffect).eDirection = (shared.Roll.Direction)Enum.Parse(typeof(shared.Roll.Direction), value.ToString(), true);
				_eDirection = value;
			}
		}
		public float nSpeed //кол-во пикселей в секунду
		{
			get
			{
				if (null != _cEffect)
					return ((shared.Roll)_cEffect).nSpeed;
				return _nSpeed;
			}
			set
			{
				if (null != _cEffect)
					((shared.Roll)_cEffect).nSpeed = value;
				_nSpeed = value;
			}
		}
		private bool bTextSplit;
		public bool bCuda { get; set; }
		public bool bStopOnEmpty { get; set; }
		public Dictionary<ushort, Roll.Keyframe[]> ahKeyframes;
		public Roll()
			: base()
		{
			_nSpeed = 25;
			bTextSplit = false;
			bCuda = false;
		}
        public Roll(XmlNode cNode)
            : base()
        {
            _cXmlNode = cNode;   
        }
		override public void LoadXML(XmlNode cXmlNode)
		{
			if (null == cXmlNode)
				return;
			base.LoadXML(cXmlNode);
			if (0 < cXmlNode.Attributes.Count)
			{
				foreach (XmlAttribute cAttr in cXmlNode.Attributes)
				{
					try
					{
						switch (cAttr.Name)
						{
							case "direction":
								eDirection = (Direction)Enum.Parse(typeof(Direction), cAttr.Value.Trim(), true);
								break;
							case "speed":
								string sValue = cAttr.Value.Trim();
								if (null == sValue || 1 > sValue.Length)
									sValue = "25";
								_nSpeed = sValue.ToFloat();
								break;
							case "textsplit":
								bTextSplit = cAttr.Value.Trim().ToBool();
								break;
							case "cuda":
								bCuda = cAttr.Value.Trim().ToBool();
								break;
							case "stop_on_empty":
								bStopOnEmpty = cAttr.Value.Trim().ToBool();
								break;
						}
					}
					catch
					{
						throw new Exception("указан некорректный параметр [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
					}
				}
			}
		}

		override internal void Create()
		{
			_bCreating = true;
			(new Logger()).WriteDebug3("roll:create:in [hcl:" + GetHashCode() + "]");
			base.Create(typeof(shared.Roll));
			shared.Roll cRollRemote = (shared.Roll)_cEffect;
            if (_cXmlNode == null)
            {
                cRollRemote.Create();
                eDirection = _eDirection;
                nSpeed = _nSpeed;
                AreaSet(stArea);
                if (null != cDock)
                    cRollRemote.cDock = cDock;
                nLayer = _nLayer;
                nDelay = cShow.nDelay;

                Item[] aItems;
                lock (_aItems)
                    aItems = _aItems.OrderBy(o => o.nOrder).ToArray();
                for (int nIndx = 0; aItems.Length > nIndx; nIndx++)
                {
                    EffectAdd(aItems[nIndx].cEffect);
                }
            }
            else
            {
                cRollRemote.Create(_cXmlNode.OuterXml);
            }
            (new Logger()).WriteDebug4("roll:create:return [hc:" + _cEffect.GetHashCode() + "]");
			_bCreating = false;
		}
		public void EffectAdd(userspace.Effect cEffect)
		{
			if (cEffect.nSpeed <= 0)
				EffectAdd(cEffect, float.MaxValue);
			else
				EffectAdd(cEffect, cEffect.nSpeed);
		}
		public void EffectAdd(userspace.Effect cEffect, float nSpeed)
		{
			lock (_aItems)
				if (null == _cEffect)
					Create();
			cEffect.Create();
			shared.Effect cEffectRemote = RemoteEffectGet(cEffect);
			ulong nDelay = 0;
			if (cEffectRemote.nDelay > 0 && cEffectRemote.nDelay < ulong.MaxValue)
			{
				nDelay = cEffectRemote.nDelay;
				cEffectRemote.nDelay = 0;
            }
            ((shared.Roll)_cEffect).EffectAdd(cEffectRemote, nSpeed, (cEffect.aKeyframes == null ? null : SharedKeyframesMake(cEffect.aKeyframes.ToArray())), cEffect.bWaitForEmptySpace, cEffect.bIsMaskForAllUpper, nDelay);
			Item cItem = ItemGet(cEffect);
			if (null == cItem)
			{
				cItem = new Item(ushort.MaxValue, cEffect);
				lock (_aItems)
					_aItems.AddLast(cItem);
			}
			cItem.cEffectShared = cEffectRemote;
		}
		private shared.Roll.Keyframe[] SharedKeyframesMake(Keyframe[] aKeyframes)
		{
			if (null == aKeyframes || aKeyframes.Length <= 0)
				return null;
			List<shared.Roll.Keyframe> aKFs = new List<shared.Roll.Keyframe>();
			foreach (Keyframe cKF in aKeyframes)
				aKFs.Add(((shared.Roll)_cEffect).KeyframeMake(cKF.eType.ToInt(), cKF.nPosition, cKF.nFrame, cKF.nBesierControlPointFrames, cKF.nBesierControlPointPixels));
			return aKFs.ToArray();
		}
		public List<userspace.Effect> EffectsGet()
		{
			lock (_aItems)
				if (null == _cEffect)  // манипуляции с эффектами безопасны только до create
					return _aItems.Select(o => o.cEffect).ToList();
				else
					return null;
		}
		public void EffectRemove(userspace.Effect cEffect)
		{
			Item cItem;
			lock (_aItems)
				if (null == _cEffect && null != (cItem = _aItems.FirstOrDefault(o => o.cEffect == cEffect)))  // манипуляции с эффектами безопасны только до create
					_aItems.Remove(cItem);
		}
        public bool TryAddText(string sText)
        {
            Item[] aTexts = _aItems.Where(o => o.cEffect is Text).ToArray();
            Text cTextLast, cText = null; ;
            if (null != aTexts && 0 < aTexts.Length)
            {
                cTextLast = (Text)aTexts.OrderByDescending(o => o.nOrder).ToArray()[0].cEffect;
                cText = new Text() { sText = sText };
                cText.cFont = new Font() { sName = cTextLast.cFont.sName, nSize = cTextLast.cFont.nSize, nFontStyle = cTextLast.cFont.nFontStyle, nWidth = cTextLast.cFont.nWidth };
                cText.cFont.cColor = new Color() { nAlpha = cTextLast.cFont.cColor.nAlpha, nBlue = cTextLast.cFont.cColor.nBlue, nGreen = cTextLast.cFont.cColor.nGreen, nRed = cTextLast.cFont.cColor.nRed };
                cText.cFont.cBorder = new Border() { nWidth = cTextLast.cFont.cBorder.nWidth, cColor = new Color() { nAlpha = cTextLast.cFont.cBorder.cColor.nAlpha, nBlue = cTextLast.cFont.cBorder.cColor.nBlue, nGreen = cTextLast.cFont.cBorder.cColor.nGreen, nRed = cTextLast.cFont.cBorder.cColor.nRed } };

                EffectAdd(cText);
                return true;
            }
            return false;
        }
    }

    public class Helper
    {
        public class EffectInfo
        {
            public int nHashCode;
            public string sName;
            public string sInfo;
            public string sType;
            public string sStatus;
            public EffectInfo()
            {
            }
        }
		static Helper()
		{
			_bInitialized = false;
			oLock = new object();
		}
		static private object oLock;
        static public void InitializeTCPChannel()
		{
			lock (oLock)
				if (!_bInitialized)
				{
					BinaryServerFormatterSinkProvider cBinaryServerFormatterSinkProvider = new BinaryServerFormatterSinkProvider();
					cBinaryServerFormatterSinkProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
					Dictionary<string, int> ahProperties = new Dictionary<string, int>();
					ahProperties.Add("port", 0);
					ChannelServices.RegisterChannel(new TcpChannel(ahProperties, new BinaryClientFormatterSinkProvider(), cBinaryServerFormatterSinkProvider), false);
					while (!_bInitialized)
						_bInitialized = true;
				}
        }
		static public bool _bInitialized;
        private object LockHelper = new object();
        private shared.Helper _cHelper;
        private shared.Helper cHelper
        {
            get
            {
                lock (LockHelper)
                    if (null == _cHelper)
                    {
                        Preferences.Reload();
                        Helper.InitializeTCPChannel();
                        (new Logger()).WriteNotice("will connect to server [" + Preferences.cUrlAttribute.UrlValue + "]");
                        return _cHelper = (shared.Helper)Activator.CreateInstance(typeof(shared.Helper), null, new object[] { Preferences.cUrlAttribute });
                    }
                    else
                        return _cHelper;
            }
        }
        public bool FileExists(string sFileName)
        {
            return cHelper.FileExist(sFileName);
        }
		public void FileCopyAsync(string sSource, string sTarget)
		{
			cHelper.FileCopyAsync(sSource, sTarget);
		}
		public bool FileMove(string sSource, string sTarget)
		{
			return cHelper.FileMove(sSource, sTarget);
		}
        public bool FileCreate(string sFile, string sText)
        {
            return cHelper.FileCreate(sFile, sText);
        }
        public void FileDelete(string sFileName)
        {
            cHelper.FileDelete(sFileName);
        }
		public string[] FileNamesGet(string sFolder, string[] aExtensions)
		{
			return cHelper.FileNamesGet(sFolder, aExtensions);
		}
        public string[] FileNamesGet(string sFolder, string[] aExtensions, bool bAddDate)
        {
            return cHelper.FileNamesGet(sFolder, aExtensions, bAddDate);
        }
        public string[] DirectoriesNamesGet(string sFolder)
		{
			return cHelper.DirectoriesNamesGet(sFolder);
		}


        public bool CopyFileExtendedCreate(string sSource, string sTarget, int nDelayMiliseconds, int nPeriodToDelayMiliseconds, long nFramesDur)
        {
            return cHelper.CopyFileExtendedCreate(sSource, sTarget, nDelayMiliseconds, nPeriodToDelayMiliseconds, nFramesDur);
        }
        public void CopyFileExtendedDoCopyAsync(bool bResetLastWriteTime)
        {
            cHelper.CopyFileExtendedDoCopyAsync(bResetLastWriteTime);
        }
        public float CopyFileExtendedProgressPercentGet()
        {
            return cHelper.CopyFileExtendedProgressPercentGet();
        }
        public bool CopyFileExtendedIsNull()
        {
            return cHelper.CopyFileExtendedIsNull();
        }
        public bool? ExCopyResult()
        {
            return cHelper.ExCopyResult();
        }


        public List<EffectInfo> BaetylusEffectsInfoGet()
        {
            List<shared.Helper.EffectInfo> aSEIs = cHelper.BaetylusEffectsInfoGet();
            List<EffectInfo> aRetVal = new List<EffectInfo>();
            foreach (shared.Helper.EffectInfo cSEI in aSEIs)
                aRetVal.Add(new EffectInfo() { nHashCode = cSEI.nHashCode, sName = cSEI.sName, sInfo = cSEI.sInfo, sType = cSEI.sType, sStatus = cSEI.sStatus });
            return aRetVal;
        }
        public List<int> BaetylusEffectStop(List<int> aHashes)
        {
            return cHelper.BaetylusEffectStop(aHashes);
        }
        public void DisComInit()
        {
            cHelper.DisComInit();
        }
    }
}
//internal interface IStorageCache
//{
//    void Cache();
//    void Release();
//}
//    #region IStorageCache
//    void IStorageCache.Cache()
//    {
//    }
//    void IStorageCache.Release()
//    {
//    }
//    #endregion
//private void FilesRelease(object cState)
//{
//    try
//    {
//        Atom[] aAtoms = (Atom[])cState;
//        Animation cAnimation = null;
//        string sCacheFolder = System.IO.Path.GetTempP	ath() + "/replica.cues.cache/"; //UNDONE //EMERGENCY нужно сделать нормальную обработку file cache, добавить интерфейс для классов поддерживающих кеширование и т.п.
//        foreach (Atom cAtom in aAtoms)
//        {
//            if (cAtom is Animation)
//            {
//                cAnimation = (Animation)cAtom;
//                for (int nFileIndx = 0; cAnimation.aFiles.Length > nFileIndx; nFileIndx++)
//                {
//                    try
//                    {
//                        if (cAnimation.aFiles[nFileIndx].sFile.Contains(sCacheFolder))
//                        {
//                            System.IO.File.Move(cAnimation.aFiles[nFileIndx].sFile, cAnimation.aFiles[nFileIndx].sFile + "_todel");
//                            System.IO.File.Delete(cAnimation.aFiles[nFileIndx].sFile + "_todel");
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        (new Logger()).WriteError(new Exception("ошибка удаления файла анимации из кэша [file:" + cAnimation.aFiles[nFileIndx].sFile + "]", ex)); //TODO LANG
//                    }
//                }
//            }
//        }
//    }
//    catch (Exception ex)
//    {
//        (new Logger()).WriteError(ex);
//    }
//}

