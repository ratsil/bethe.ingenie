using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using helpers;

namespace ingenie.plugins
{
    public class Plugin
    {
        private object _oSyncRoot = new object();
        private IPlugin _cRemoteInstance;
        #region events processing
        public event EventDelegate Prepared
        {
            add
            {
                lock (_oSyncRoot)
                    if (null != _cRemoteInstance)
                        _cRemoteInstance.Prepared += value;
            }
            remove
            {
                lock (_oSyncRoot)
                    if (null != _cRemoteInstance)
                        _cRemoteInstance.Prepared -= value;
            }
        }
        public event EventDelegate Started
        {
            add
            {
                lock (_oSyncRoot)
                    if (null != _cRemoteInstance)
                        _cRemoteInstance.Started += value;
            }
            remove
            {
                lock (_oSyncRoot)
                    if (null != _cRemoteInstance)
                        _cRemoteInstance.Started -= value;
            }
        }
        public event EventDelegate Stopped
        {
            add
            {
                lock (_oSyncRoot)
                    if (null != _cRemoteInstance)
                        _cRemoteInstance.Stopped += value;
            }
            remove
            {
                lock (_oSyncRoot)
                    if (null != _cRemoteInstance)
                        _cRemoteInstance.Stopped -= value;
            }
        }
        static private ThreadBufferQueue<Tuple<EventDelegate, IPlugin>> _aqEvents;
        static private System.Threading.Thread _cThreadEvents;
        static Plugin()
        {
            _aqEvents = new ThreadBufferQueue<Tuple<EventDelegate, IPlugin>>(false, true);
            _cThreadEvents = new System.Threading.Thread(WorkerEvents);
            _cThreadEvents.IsBackground = true;
            _cThreadEvents.Start();
        }
        static private void WorkerEvents()
        {
            Tuple<EventDelegate, IPlugin> cEvent;
			System.Diagnostics.Stopwatch cWatch = new System.Diagnostics.Stopwatch();
            while (true)
            {
                try
                {
                    cEvent = _aqEvents.Dequeue();

					cWatch.Reset();
					cWatch.Restart();
					cEvent.Item1(cEvent.Item2);
					cWatch.Stop();
					if (40 < cWatch.ElapsedMilliseconds)
						(new Logger()).WriteDebug3("duration: " + cWatch.ElapsedMilliseconds + " queue: " + _aqEvents.nCount);
					if (0 < _aqEvents.nCount)
						(new Logger()).WriteDebug3(" queue: " + _aqEvents.nCount);
                }
                catch (System.Threading.ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    (new Logger()).WriteError(ex);
                }
            }
        }
        static public void EventSend(EventDelegate dEvent, IPlugin cSender)
        {
            _aqEvents.Enqueue(new Tuple<EventDelegate, IPlugin>(dEvent, cSender));
        }
        #endregion

        public BTL.EffectStatus eStatus
        {
            get
            {
                lock (_oSyncRoot)
                    if (null != _cRemoteInstance)
                        return _cRemoteInstance.eStatus;
                return BTL.EffectStatus.Stopped;
            }
        }
        private DateTime _dtStatusChanged;
        public DateTime dtStatusChanged
        {
            get
            {
                lock (_oSyncRoot)
                    if (null != _cRemoteInstance)
                        return _cRemoteInstance.dtStatusChanged;
                return _dtStatusChanged;
            }
        }
        public string sFile;
        static string _sPluginFileLast = "";
        private ulong _nFrameReference;
        public Plugin(string sFile, string sClass, string sData)
        {
            try
            {
                this.sFile = sFile;
                if (_sPluginFileLast != sFile)
                    (new Logger()).WriteDebug4("plugin: constructor: before CreateInstanceFrom: [sFile = " + (_sPluginFileLast = sFile) + "]");
                System.Runtime.Remoting.ObjectHandle cHandle = Activator.CreateInstanceFrom(AppDomain.CurrentDomain, sFile, "ingenie.plugins." + sClass);
                _cRemoteInstance = (plugins.IPlugin)cHandle.Unwrap();
                _cRemoteInstance.Create(System.IO.Path.GetDirectoryName(sFile), sData);
                _dtStatusChanged = DateTime.Now;
            }
            catch (Exception ex)
            {
				throw new Exception("указанный тип [" + sClass + "] не реализует API плагина [ingenie.plugins.IPlugin]", ex); //TODO LANG
            }
        }
        ~Plugin()
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
        public void Dispose()
        {
            try
            {
                lock (_oSyncRoot)
                    if (_cRemoteInstance != null)
                        Stop();
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
        }

        public void Prepare()
        {
			Action dAction;
			lock (_oSyncRoot)
			{
				if (null != _cRemoteInstance)
					dAction = _cRemoteInstance.Prepare;
				else
					throw new Exception("необходимо сначала создать плагин"); //TODO LANG
			}
			dAction();
        }
        public void Start()
        {
			Action dAction;
			lock (_oSyncRoot)
			{
				if (null != _cRemoteInstance)
					dAction = _cRemoteInstance.Start;
				else
					throw new Exception("необходимо сначала создать плагин"); //TODO LANG
			}
			dAction();
        }
        public void Stop()
        {
            (new Logger()).WriteDebug3("in");
			Action dAction;
			lock (_oSyncRoot)
			{
				if (null != _cRemoteInstance)
					dAction = _cRemoteInstance.Stop;
				else
					throw new Exception("необходимо сначала создать плагин"); //TODO LANG
			}
            try
            {
				dAction();
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
            _dtStatusChanged = DateTime.Now;
            _cRemoteInstance = null;
            (new Logger()).WriteDebug4("return");
        }
    }
}
