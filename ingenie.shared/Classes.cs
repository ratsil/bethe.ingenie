using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Runtime.Remoting.Lifetime;

namespace ingenie.shared
{
    public enum Status
    {
        Unknown = 0,
        Idle = 1,
        Prepared = 2,
        Started = 3,
        Stopped = 4,
        Error = -1
    }
	public class Helper : MarshalByRefObject
    {
        public class EffectInfo : MarshalByRefObject
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
        #region - File operations -
        //TODO this is ig.web side!! ))  move operations to ig.server side  )))  (and move try-catch blocks)
        public bool FileExist(string sFileName)
        {
            try
            {
                return System.IO.File.Exists(sFileName);
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
                return false;
            }
        }
        public bool FileDelete(string sFileName)
        {
            try
            {
                System.IO.File.Delete(sFileName);
                return true;
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError("[file=" + sFileName + "]", ex);
                return false;
            }
        }
        public string[] FileNamesGet(string sFolder, string[] aExtensions)
		{
            try
            {
                List<string> aResult = new List<string>();
                List<System.IO.FileInfo> aFiles = new List<System.IO.FileInfo>();
                System.IO.DirectoryInfo cDir = new System.IO.DirectoryInfo(sFolder);
                if (null == aExtensions)
                {
                    return cDir.GetFiles("*.*").OrderByDescending(o=>o.CreationTime).Select(o => o.Name).ToArray();
                }

                foreach (string sExt in aExtensions)
                {
                    aFiles.AddRange(cDir.GetFiles("*." + sExt));
                }
                return aFiles.OrderByDescending(o => o.CreationTime).Select(o => o.Name).ToArray();
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
                return new string[0];
            }
        }
        public string[] DirectoriesNamesGet(string sFolder)
        {
            try
            { 
            List<string> aResult = new List<string>();
            System.IO.DirectoryInfo[] aDirectories;
            if (System.IO.Directory.Exists(sFolder))
            {
                System.IO.DirectoryInfo cDir = new System.IO.DirectoryInfo(sFolder);
                aDirectories = cDir.GetDirectories();
                aResult.AddRange(from cFInfo in aDirectories select cFInfo.Name);
                return aResult.ToArray();
            }
            else
                return null;
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError("[folder=" + sFolder + "]", ex);
                return null;
            }
        }
        public bool FileMove(string sSource, string sTarget)
		{
			try
			{
				System.IO.File.Move(sSource, sTarget);
				return true;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError("[src="+ sSource + "][trg=" + sTarget + "]", ex);
				return false;
			}
		}
        public bool FileCreate(string sFile, string sText)
        {
            try
            {
                System.IO.File.WriteAllText(sFile, sText);
                return true;
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError("[file=" + sFile + "][txt=" + sText + "]", ex);
                return false;
            }
        }
        public bool FileCopy(string sSource, string sTarget)
		{
			try
			{
				System.IO.File.Copy(sSource, sTarget);
				return true;
			}
			catch (Exception ex)
			{
                (new Logger()).WriteError("[src=" + sSource + "][trg=" + sTarget + "]", ex);
                return false;
			}
		}

        static private helpers.CopyFileExtended _cCurrentCopying;
        public bool CopyFileExtendedCreate(string sSource, string sTarget, int nDelayMiliseconds, int nPeriodToDelayMiliseconds, long nFramesDur)
        {
            try
            {
                _cCurrentCopying = new helpers.CopyFileExtended(sSource, sTarget, nDelayMiliseconds, nPeriodToDelayMiliseconds, nFramesDur);  // медленное копирование 
                return true;
            }
            catch (Exception ex)  // although throw goes to Player.asmx.cs!
            {
                (new Logger()).WriteError(ex);
                return false;
            }
        }
        public bool CopyFileExtendedDoCopy(bool bResetLastWriteTime)
        {
            try
            {
                return _cCurrentCopying.DoCopy(bResetLastWriteTime);
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
                return false;
            }
        }
        public float CopyFileExtendedProgressPercentGet()
        {
            try
            {
                return _cCurrentCopying.nProgressPercent;
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
                return 0;
            }
        }
        public bool CopyFileExtendedIsNull()
        {
            try
            {
                return null == _cCurrentCopying;
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
                return true;
            }
        }
        #endregion
        #region - Management -
        #region . BaetylusEffectsInfoGet .
        public delegate List<EffectInfo> BaetylusEffectsInfoGetDelegate();
        static public event BaetylusEffectsInfoGetDelegate OnBaetylusEffectsInfoGet;
        public List<EffectInfo> BaetylusEffectsInfoGet()
        {
            if (null == OnBaetylusEffectsInfoGet)
                throw new Exception("shared:Helper:BaetylusEffectsInfoGet: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
            return OnBaetylusEffectsInfoGet();
        }

        #endregion
        #region . BaetylusEffectStop .
        public delegate List<int> BaetylusEffectStopDelegate(List<int> aHashes);
        static public event BaetylusEffectStopDelegate OnBaetylusEffectStop;
        public List<int> BaetylusEffectStop(List<int> aHashes)
        {
            if (null == OnBaetylusEffectStop)
                throw new Exception("shared:effect:create: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
            return OnBaetylusEffectStop(aHashes);
        }
        #endregion
        #region . DisComInit .
        public delegate void DisComInitDelegate();
        static public event DisComInitDelegate OnDisComInit;
        public void DisComInit()
        {
            if (null == OnDisComInit)
                throw new Exception("shared:effect:create: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
            OnDisComInit();
        }
        #endregion
        #endregion
    }
	public delegate void EventDelegate(EffectEventType eType, shared.Effect cEffect);
	public delegate void ContainerEventDelegate(ContainerEventType eType, shared.Effect cEffect);
	public enum EffectEventType
	{
		prepared,
		started,
		stopped,
		failed
	}
	public enum ContainerEventType
	{
		added,
		prepared,
		started,
		stopped,
		onscreen,
		offscreen,
		failed
	}
	abstract public class RemotelyDelegatableObject : MarshalByRefObject
	{
		public void Callback(EffectEventType eType, shared.Effect cEffect)
		{
			CallbackTransport(eType, cEffect);
		}
		public void ContainerCallback(ContainerEventType eType, shared.Effect cEffect)
		{
			ContainerCallbackTransport(eType, cEffect);
		}

		protected abstract void CallbackTransport(EffectEventType eType, shared.Effect cEffect);
		protected abstract void ContainerCallbackTransport(ContainerEventType eType, shared.Effect cEffect);
	}

	public class Device : MarshalByRefObject
    {
		public class DownStreamKeyer : MarshalByRefObject
		{
			public byte nLevel;
			public bool bInternal;
		}

		public Device()
        {
        }

		#region delegates
		public delegate DownStreamKeyer DownStreamKeyerGetDelegate();
		public delegate void DownStreamKeyerSetDelegate(DownStreamKeyer cDownStreamKeyer);
		#endregion

		static public event DownStreamKeyerGetDelegate OnDownStreamKeyerGet;
		static public event DownStreamKeyerSetDelegate OnDownStreamKeyerSet;
		public DownStreamKeyer cDownStreamKeyer
		{
			get
			{
				if (null == OnDownStreamKeyerGet)
					throw new Exception("shared:device:dsk:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				return OnDownStreamKeyerGet();
			}
			set
			{
				if (null == OnDownStreamKeyerSet)
					throw new Exception("shared:device:dsk:set: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				OnDownStreamKeyerSet(value);
			}
		}
    }
	abstract public class Effect : MarshalByRefObject, ISponsor
    {
		private bool _bDisposed;
		#region Events
		public event EventDelegate EventRaised;
		public void OnEffectEventRaised(EffectEventType eType)
		{
			if (null != EventRaised)
			{
				(new Logger()).WriteDebug4("shared:effect:event:" + eType.ToString() + ":before [hc:" + GetHashCode() + "]");
				try
				{
					EventRaised(eType, this);
				}
				catch (System.Net.Sockets.SocketException)
				{
					(new Logger()).WriteDebug4("shared:effect:client:lost:dispose:before [hc:" + GetHashCode() + "]");
					if (EffectEventType.stopped != eType)
						Stop();
					Dispose();
					(new Logger()).WriteDebug4("shared:effect:client:lost:dispose:after [hc:" + GetHashCode() + "]");
				}
				catch (Exception ex)
				{
					(new Logger()).WriteError(ex);
				}
				(new Logger()).WriteDebug4("shared:effect:event:" + eType.ToString() + ":after [hc:" + GetHashCode() + "]");
			}
			else
				(new Logger()).WriteDebug4("shared:effect:event:" + eType.ToString() + ":empty [hc:" + GetHashCode() + "]");
		}
		#endregion
        public Effect()
        {
			_bDisposed = false;
        }
		override public object InitializeLifetimeService()
		{
			ILease iLease = (ILease)base.InitializeLifetimeService();
			iLease.InitialLeaseTime = TimeSpan.FromSeconds(20);
			iLease.SponsorshipTimeout = TimeSpan.FromSeconds(20);
			iLease.RenewOnCallTime = TimeSpan.FromSeconds(20);
			iLease.Register(this);
			return iLease;
		}
		public TimeSpan Renewal(ILease lease)
		{
			(new Logger()).WriteDebug4("lease:renewal:" + (!_bDisposed ? "no" : "yes:" + this.GetHashCode()));
			if (!_bDisposed)
				return TimeSpan.FromSeconds(20);
			return TimeSpan.Zero;
		}

		#region delegates
		public delegate Status StatusGetDelegate(Effect cSender);
		public delegate object ObjectGetDelegate(Effect cSender);
		public delegate void ObjectSetDelegate(Effect cSender, object oValue);
		public delegate ushort UShortGetDelegate(Effect cSender);
		public delegate void UShortSetDelegate(Effect cSender, ushort nValue);
		public delegate ulong ULongGetDelegate(Effect cSender);
		public delegate void ULongSetDelegate(Effect cSender, ulong nValue);
		public delegate bool BoolGetDelegate(Effect cSender);
		public delegate void BoolSetDelegate(Effect cSender, bool bValue);
		public delegate byte[] ByteArrayGetDelegate(Effect cSender);
		public delegate void ByteArraySetDelegate(Effect cSender, byte[] aValue);
        #endregion

		static public event UShortGetDelegate OnLayerGet;
		static public event UShortSetDelegate OnLayerSet;
		public ushort nLayer
		{
			get
			{
				if (null == OnLayerGet)
					throw new Exception("shared:effect:layer:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				return OnLayerGet(this);
			}
			set
			{
				if (null == OnLayerSet)
					throw new Exception("shared:effect:layer:set: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				OnLayerSet(this, value);
			}
		}

		static public event ULongGetDelegate OnFrameStartGet;
		static public event ULongSetDelegate OnFrameStartSet;
		public ulong nFrameStart
		{
			get
			{
				if (null == OnFrameStartGet)
					throw new Exception("shared:effect:frame:start:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				return OnFrameStartGet(this);
			}
			set
			{
				if (null == OnFrameStartSet)
					throw new Exception("shared:effect:frame:start:set: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				OnFrameStartSet(this, value);
			}
		}

		static public event ULongGetDelegate OnDurationGet;
		static public event ULongSetDelegate OnDurationSet;
		public ulong nDuration
		{
			get
			{
				if (null == OnDurationGet)
					throw new Exception("shared:effect:duration:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				return OnDurationGet(this);
			}
			set
			{
				if (null == OnDurationSet)
					throw new Exception("shared:effect:duration:set: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				OnDurationSet(this, value);
			}
		}

		static public event ULongGetDelegate OnDelayGet;
		static public event ULongSetDelegate OnDelaySet;
		public ulong nDelay
		{
			get
			{
				if (null == OnDelayGet)
					throw new Exception("shared:effect:delay:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				return OnDelayGet(this);
			}
			set
			{
				if (null == OnDelaySet)
					throw new Exception("shared:effect:delay:set: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				OnDelaySet(this, value);
			}
		}

		static public event ULongGetDelegate OnFramesTotalGet;
		public ulong nFramesTotal
		{
			get
			{
				if (null == OnFramesTotalGet)
					throw new Exception("shared:effect:frames:total:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				return OnFramesTotalGet(this);
			}
		}

		static public event ULongGetDelegate OnFrameCurrentGet;
		public ulong nFrameCurrent
		{
			get
			{
				if (null == OnFrameCurrentGet)
					throw new Exception("shared:effect:frame:current:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				return OnFrameCurrentGet(this);
			}
		}

		static public event ObjectGetDelegate OnTagGet;
		static public event ObjectSetDelegate OnTagSet;
		public object oTag
		{
			get
			{
				if (null == OnTagGet)
					throw new Exception("shared:effect:tag:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				return OnTagGet(this);
			}
			set
			{
				if (null == OnTagSet)
					throw new Exception("shared:effect:tag:set: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				OnTagSet(this, value);
			}
		}

        static public event StatusGetDelegate OnStatusGet;
		virtual public Status eStatus
		{
			get
			{
				if (null != OnStatusGet)
					return OnStatusGet(this);
				return Status.Unknown;
			}
		}

		#region Create
		public delegate void CreateDelegate(Effect cSender);
		static public event CreateDelegate OnCreate;
		public void Create()
		{
			if (null == OnCreate)
				throw new Exception("shared:effect:create: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
			try
			{
				OnCreate(this);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
        public delegate void CreateFromXmlDelegate(Effect cSender, string sXML);
        static public event CreateFromXmlDelegate OnCreateFromXml;
        public void Create(string sXML)
        {
            if (null == OnCreateFromXml)
                throw new Exception("shared:text:create: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
            OnCreateFromXml(this, sXML);
        }
        #endregion

        #region Prepare
        public delegate bool PrepareDelegate(Effect cEffect);
        static public event PrepareDelegate OnPrepare;
        virtual public bool Prepare()
        {
            bool bRetVal = false;
			try
			{
				if (null != OnPrepare)
					bRetVal = OnPrepare(this);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
            return bRetVal;
        }
        #endregion
        #region Start
        public delegate bool StartDelegate(Effect cEffect);
        static public event StartDelegate OnStart;
        virtual public bool Start()
        {
            bool bRetVal = false;
			try
			{
				if (null != OnStart)
					bRetVal = OnStart(this);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return bRetVal;
        }
        #endregion         
        #region Stop
        public delegate bool StopDelegate(Effect cEffect);
        static public event StopDelegate OnStop;
        virtual public bool Stop()
        {
            bool bRetVal = false;
			try
			{
				if (null != OnStop)
					bRetVal = OnStop(this);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return bRetVal;
        }
        #endregion
        #region Dispose
        public delegate bool DisposeDelegate(Effect cEffect);
        static public event DisposeDelegate OnDispose;
        virtual public bool Dispose()
        {
            bool bRetVal = false;
			try
			{
				(new Logger()).WriteDebug3("shared:effect:dispose [hc:" + this.GetHashCode() + "]");//TODO LANG
				if (null != OnDispose)
					bRetVal = OnDispose(this);
				_bDisposed = true;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
            return bRetVal;
        }
        #endregion
    }
	abstract public class EffectVideo : Effect
    {
		public EffectVideo()
        {
        }

		#region delegates
		public delegate void DockSetDelegate(Effect cSender, helpers.Dock cDock);
		public delegate helpers.Area AreaGetDelegate(Effect cSender);
		public delegate void AreaSetDelegate(Effect cSender, helpers.Area stArea);
		#endregion

		static public event DockSetDelegate OnDockSet;
		public helpers.Dock cDock
		{
			set
			{
				if (null == OnDockSet)
					throw new Exception("shared:effect:dock:set: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				OnDockSet(this, value);
			}
		}

		static public event BoolGetDelegate OnOpacityGet;
		static public event BoolSetDelegate OnOpacitySet;
		public bool bOpacity
		{
			get
			{
				if (null == OnOpacityGet)
					throw new Exception("shared:effect:opacity:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				return OnOpacityGet(this);
			}
			set
			{
				if (null == OnOpacitySet)
					throw new Exception("shared:effect:opacity:set: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				OnOpacitySet(this, value);
			}
		}

		static public event BoolGetDelegate OnCUDAGet;
		static public event BoolSetDelegate OnCUDASet;
		public bool bCUDA
		{
			get
			{
				if (null == OnCUDAGet)
					throw new Exception("shared:effect:cuda:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				return OnCUDAGet(this);
			}
			set
			{
				if (null == OnCUDASet)
					throw new Exception("shared:effect:cuda:set: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				OnCUDASet(this, value);
			}
		}

		static public event AreaGetDelegate OnAreaGet;
		static public event AreaSetDelegate OnAreaSet;
		public helpers.Area stArea
		{
			get
			{
				if (null == OnAreaGet)
					throw new Exception("shared:effect:area:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				return OnAreaGet(this);
			}
			set
			{
				if (null == OnAreaSet)
					throw new Exception("shared:effect:area:set: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				OnAreaSet(this, value);
			}
		}
    }
	abstract public class EffectAudio : Effect
    {
		public EffectAudio()
        {
        }

		static public event ByteArrayGetDelegate OnChannelsGet;
		static public event ByteArraySetDelegate OnChannelsSet;
		public byte[] aChannels
		{
			get
			{
				if (null == OnChannelsGet)
					throw new Exception("shared:effect:channels:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				return OnChannelsGet(this);
			}
			set
			{
				if (null == OnChannelsSet)
					throw new Exception("shared:effect:channels:set: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				OnChannelsSet(this, value);
			}
		}
    }
	abstract public class EffectVideoAudio : EffectVideo
    {
		public EffectVideoAudio()
        {
        }

		static public event ByteArrayGetDelegate OnChannelsGet;
		static public event ByteArraySetDelegate OnChannelsSet;
		public byte[] aChannels
		{
			get
			{
				if (null == OnChannelsGet)
					throw new Exception("shared:effect:channels:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				return OnChannelsGet(this);
			}
			set
			{
				if (null == OnChannelsSet)
					throw new Exception("shared:effect:channels:set: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				OnChannelsSet(this, value);
			}
		}
	}
	abstract public class Container : EffectVideoAudio
	{
		#region Events
		public event ContainerEventDelegate ContainerEventRaised;
		public void OnContainerEventRaised(ContainerEventType eType, Effect cEffect)
		{
			if (null != ContainerEventRaised)
			{
				(new Logger()).WriteDebug4("shared:container:event:" + eType.ToString() + ":before [effect hc:" + cEffect.GetHashCode() + "][container hc:" + GetHashCode() + "]");
				try
				{
					ContainerEventRaised(eType, cEffect);
				}
				catch (System.Net.Sockets.SocketException)
				{
					(new Logger()).WriteDebug4("shared:container:client:lost:dispose:before [effect hc:" + cEffect.GetHashCode() + "][container hc:" + GetHashCode() + "]");
					Stop();
					Dispose();
					(new Logger()).WriteDebug4("shared:container:client:lost:dispose:after [effect hc:" + cEffect.GetHashCode() + "][container hc:" + GetHashCode() + "]");
				}
				catch (Exception ex)
				{
					(new Logger()).WriteError(ex);
				}
                (new Logger()).WriteDebug4("shared:container:event:" + eType.ToString() + ":after [effect hc:" + cEffect.GetHashCode() + "][container hc:" + GetHashCode() + "]");
			}
			else
				(new Logger()).WriteDebug4("shared:container:event:" + eType.ToString() + ":empty [effect hc:" + cEffect.GetHashCode() + "][container hc:" + GetHashCode() + "]");
		}
        static public event UShortGetDelegate OnEffectsQtyGet;
        public ushort nEffectsQty
        {
            get
            {
                if (null == OnEffectsQtyGet)
                    throw new Exception("shared:container:effectsqty:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
                return OnEffectsQtyGet(this);
            }
        }
		static public event ULongGetDelegate OnSumDurationGet;
		public ulong nSumDuration
		{
			get
			{
				if (null == OnSumDurationGet)
					throw new Exception("shared:container:sumduration:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				return OnSumDurationGet(this);
			}
		}
        #endregion
	}
	public class Video : EffectVideoAudio
    {
        #region Create
		new public delegate void CreateDelegate(shared.Video cVideo, string sFilename, helpers.Dock cDock, ushort nZ, ulong nFrameStart, ulong nDuration, bool bOpacity, ulong nDelay);
        new static public event CreateDelegate OnCreate;
		public void Create(string sFilename, helpers.Dock cDock, ushort nZ, ulong nFrameStart, ulong nDuration, bool bOpacity, ulong nDelay)
        {
			if (null == OnCreate)
				throw new Exception("shared:video:create: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
			OnCreate(this, sFilename, cDock, nZ, nFrameStart, nDuration, bOpacity, nDelay);
        }
        #endregion
    }
	public class Audio : EffectAudio
    {
        #region Create
		new public delegate void CreateDelegate(shared.Audio cAudio, string sFilename);
        new static public event CreateDelegate OnCreate;
		public void Create(string sFilename)
        {
			if (null == OnCreate)
				throw new Exception("shared:audio:create: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
			OnCreate(this, sFilename);
        }
        #endregion
    }
    public class Animation : EffectVideo
    {
        #region Create
		new public delegate void CreateDelegate(shared.Animation cAnimation, string sFolder, ushort nLoopsQty, bool bKeepAlive, helpers.Dock cDock, ushort nZ, bool bOpacity, ulong nDelay, float nPixelAspectRatio, bool bTurnOffQueue);
		new static public event CreateDelegate OnCreate;
		public void Create(string sFolder, ushort nLoopsQty, bool bKeepAlive, helpers.Dock cDock, ushort nZ, bool bOpacity, ulong nDelay, float nPixelAspectRatio, bool bTurnOffQueue)
        {
			if (null == OnCreate)
				throw new Exception("shared:animation:create: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
			try
			{
				OnCreate(this, sFolder, nLoopsQty, bKeepAlive, cDock, nZ, bOpacity, nDelay, nPixelAspectRatio, bTurnOffQueue);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
        }
        #endregion
    }
    public class Text : EffectVideo
    {
        #region Create
		new public delegate void CreateDelegate(Text cText, object[] aArgs);
		new static public event CreateDelegate OnCreate;
		public void Create(object[] aArgs) 
        {
			if (null == OnCreate)
				throw new Exception("shared:text:create: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
			OnCreate(this, aArgs);
        }
        #endregion
    }
    public class Clock : EffectVideo
    {
        #region Create
		new public delegate void CreateDelegate(Clock cClock, object[] aArgs);
		new static public event CreateDelegate OnCreate;
		public void Create(object[] aArgs)
        {
			if (null == OnCreate)
				throw new Exception("shared:clock:create: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
			OnCreate(this, aArgs);
        }
        #endregion
    }
	public class Playlist : Container
    {
		#region Create
		new public delegate void CreateDelegate(shared.Playlist cPlaylist, helpers.Dock cDock, ushort nZ, bool bStopOnEmpty, bool bOpacity, ulong nDelay);
		new static public event CreateDelegate OnCreate;
        public bool bStopOnEmpty;
		public void Create(helpers.Dock cDock, ushort nZ, bool bStopOnEmpty, bool bOpacity, ulong nDelay)
        {
			(new Logger()).WriteDebug3("shared:playlist:create:in [hc:" + GetHashCode() + "]");
			this.bStopOnEmpty = bStopOnEmpty;
			if (null == OnCreate)
				throw new Exception("shared:playlist:create: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
			(new Logger()).WriteDebug3("shared:playlist:create: before callback [hc:" + GetHashCode() + "]");
			OnCreate(this, cDock, nZ, bStopOnEmpty, bOpacity, nDelay);
			(new Logger()).WriteDebug3("shared:playlist:create:return [hc:" + GetHashCode() + "]");
		}
        #endregion
        #region Add
		public delegate void EffectAddDelegate(Playlist cPL, Effect cEffect, ushort nTransDur);
		static public event EffectAddDelegate OnAddEffect;

		public void EffectAdd(Effect cEffect, ushort nTransDur)
        {
			if (null == OnAddEffect)
				throw new Exception("shared:playlist:effect:add: не удалось добавить эффект в плейлист [hc:" + GetHashCode() + "]");
			OnAddEffect(this, cEffect, nTransDur);
		}
        #endregion
        #region Skip
		public delegate void SkipDelegate(Playlist cPL, bool bLast, ushort nNewTransDur, Effect cEffect);
		static public event SkipDelegate OnSkip;
		public void Skip(bool bLast, ushort nNewTransDur, Effect cEffect)
		{
			if (null == OnSkip)
				throw new Exception("shared:playlist:skip: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
			OnSkip(this, bLast, nNewTransDur, cEffect);
		}
		#endregion
        #region Delete
		public delegate void PLItemDeleteDelegate(Playlist cPL, Effect[] aEffectIDs);
        static public event PLItemDeleteDelegate OnPLItemDelete;
        public void PLItemDelete(Effect[] aEffectIDs)
        {
            if (null == OnPLItemDelete)
                throw new Exception("shared:playlist:items_delete: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
            OnPLItemDelete(this, aEffectIDs);
        }
		#endregion
		#region EndTransDurationSet
		public delegate void EndTransDurationSetDelegate(Playlist cPL, ushort nEndTransDuration);
		static public event EndTransDurationSetDelegate OnEndTransDurationSet;
		public void EndTransDurationSet(ushort nEndTransDuration)
		{
			if (null == OnEndTransDurationSet)
				throw new Exception("shared:playlist:EndTransDurationSet: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
			OnEndTransDurationSet(this, nEndTransDuration);
		}
		#endregion
	}
    public class Composite : Container
    {

    }

    public class Roll : Container
	{
		public class Keyframe : MarshalByRefObject
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
			}
		}
		public enum Direction
		{
			LeftToRight,
			RightToLeft,
			UpToDown,
			DownToUp
		}
		public Keyframe KeyframeMake(int nType, float nPosition, long nFrame, float сBesierControlPointFrames, float сBesierControlPointPixels)
		{
			return new Keyframe() { eType = (Keyframe.Type)nType, nFrame = nFrame, nPosition = nPosition, nBesierControlPointFrames = сBesierControlPointFrames, nBesierControlPointPixels = сBesierControlPointPixels };
		}

		public delegate Direction DirectionGetDelegate(Roll cSender);
		static public event DirectionGetDelegate OnDirectionGet;
		public delegate void DirectionSetDelegate(Roll cSender, Direction eDirection);
		static public event DirectionSetDelegate OnDirectionSet;
		public Direction eDirection
		{
			get
			{
				if (null == OnDirectionGet)
					throw new Exception("shared:roll:direction:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				return OnDirectionGet(this);
			}
			set
			{
				if (null == OnDirectionSet)
					throw new Exception("shared:roll:direction:set: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				OnDirectionSet(this, value);
			}
		}

		public delegate float SpeedGetDelegate(Roll cSender);
		static public event SpeedGetDelegate OnSpeedGet;
		public delegate void SpeedSetDelegate(Roll cSender, float nSpeed);
		static public event SpeedSetDelegate OnSpeedSet;
		public float nSpeed //кол-во пикселей в секунду.  "byte" стало мало поменял на short.
		{
			get
			{
				if (null == OnSpeedGet)
					throw new Exception("shared:roll:speed:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				return OnSpeedGet(this);
			}
			set
			{
				if (null == OnSpeedSet)
					throw new Exception("shared:roll:speed:set: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				OnSpeedSet(this, value);
			}
		}

		public delegate bool StopOnEmptyGetDelegate(Roll cSender);
		static public event StopOnEmptyGetDelegate OnStopOnEmptyGet;
		public delegate void StopOnEmptySetDelegate(Roll cSender, bool bStopOnEmpty);
		static public event StopOnEmptySetDelegate OnStopOnEmptySet;
		public bool bStopOnEmpty
		{
			get
			{
				if (null == OnStopOnEmptyGet)
					throw new Exception("shared:roll:StopOnEmpty:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				return OnStopOnEmptyGet(this);
			}
			set
			{
				if (null == OnStopOnEmptySet)
					throw new Exception("shared:roll:StopOnEmpty:set: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				OnStopOnEmptySet(this, value);
			}
		}
	
		public delegate int EffectsQtyGetDelegate(Roll cSender);
		static public event EffectsQtyGetDelegate OnEffectsQtyGet;
		public int nEffectsQty
		{
			get
			{
				if (null == OnEffectsQtyGet)
					throw new Exception("shared:roll:effectsqty:get: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
				return OnEffectsQtyGet(this);
			}
		}

		public delegate void EffectAddDelegate(Roll cSender, Effect cEffect, float nSpeed, Keyframe[] aKeyframes, bool bWaitForEmptySpace, bool bIsMaskForAllUpper, ulong nDelay);
		static public event EffectAddDelegate OnEffectAdd;
		public void EffectAdd(Effect cEffect)
		{
			EffectAdd(cEffect, float.MaxValue, null, false, false, 0);
		}
		public void EffectAdd(Effect cEffect, float nSpeed, Keyframe[] aKeyframes, bool bWaitForEmptySpace, bool bIsMaskForAllUpper, ulong nDelay)
		{
			if (null == OnEffectAdd)
				throw new Exception("shared:roll:effect:add: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
			try
			{
				OnEffectAdd(this, cEffect, nSpeed, aKeyframes, bWaitForEmptySpace, bIsMaskForAllUpper, nDelay);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
	}

    public class Plugin : Effect
    {
        #region Create
        new public delegate void CreateDelegate(shared.Plugin cPlugin, string sFile, string sClass, string sData);
		new static public event CreateDelegate OnCreate;
        public void Create(string sFile, string sClass, string sData)
		{
			if (null == OnCreate)
				throw new Exception("shared:plugin:create: отсутствует привязка к серверу объектов [hc:" + GetHashCode() + "]");
			OnCreate(this, sFile, sClass, sData);
        }
        #endregion
    }
}
