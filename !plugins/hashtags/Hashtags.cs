using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Linq;
using BTL.Play;
using btl = BTL.Play;
using System.Collections;
using System.Diagnostics;
using System.Xml;
using System.Timers;
using System.IO;
using System.Threading;
using ingenie;
using helpers;
using helpers.extensions;

namespace ingenie.plugins
{
	public class Hashtags : MarshalByRefObject, IPlugin
	{
		private Preferences _cPreferences;
		private EventDelegate Prepared;
		private EventDelegate Started;
		private EventDelegate Stopped;
		private object _oLock;
		private bool _bPrepared;
		private bool _bStopped;
        private bool _bStarted;
        private bool _bWorkerAborted;
        private Preferences.TextItem _cCurrentTI;
        private System.Threading.Thread _cThreadWorker;
        float _nNormBot, _nNormTop;
        float _nDifBot, _nDifTop;
        private enum Transition
        {
            Initial,
            Normal,
            Final,
        }
        public void Create(string sWorkFolder, string sData)
		{
			(new Logger()).WriteDebug("create");
			_oLock = new object();
			_bStopped = false;
            _bWorkerAborted = false;
            _cPreferences = new Preferences(sData);
		}
		public void Prepare()
		{
			try
			{
                //PixelsMap.DisComInit();
                lock (_oLock)
				{
					if (_bPrepared)
					{
						(new Logger()).WriteWarning("Hashtags has already prepared!");
						return;
					}
					_bPrepared = true;
				}
				(new Logger()).WriteDebug("prepare:in");
				_cPreferences.cRoll.Stopped += _cRoll_Stopped;
                _cPreferences.cRoll.EffectAdd(_cPreferences.cPlaylist, null, 0);
                _cCurrentTI = _cPreferences.aTextItems[0];
                
                _cPreferences.cRoll.Prepare(2);
                AddTransition(Transition.Initial);

                _cThreadWorker = new System.Threading.Thread(Worker);
                _cThreadWorker.IsBackground = true;
                _cThreadWorker.Priority = System.Threading.ThreadPriority.Normal;
                _cThreadWorker.Start();

                if (null != Prepared)
					Plugin.EventSend(Prepared, this);
				(new Logger()).WriteDebug("prepare:out");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
        private void Worker(object cState)
        {
            try
            {
                uint nLen;
                while (true)
                {
                    if (_bStopped)
                    {
                        if (!_bStarted)
                            break;
                        if (_cPreferences.cPlaylist.eStatus == BTL.EffectStatus.Stopped)
                            break;
                        AddTransition(Transition.Final);
                        while (_cPreferences.cPlaylist.eStatus != BTL.EffectStatus.Stopped && _cPreferences.cRoll.nEffectsQty > 0)
                        {
                            _cPreferences.cRoll.Prepare(1);
                            //System.Threading.Thread.Sleep(1);
                        }
                        break;
                    }

                    if (_cPreferences.cRoll.nPrerenderQueueCount >= _cPreferences.nRollPrerenderQueueMax)
                    {
                        //(new Logger()).WriteDebug("sleep 80");
                        System.Threading.Thread.Sleep(80);
                    }
                    else
                    {
                        nLen = _cPreferences.cRoll.nPrerenderQueueCount;
                        if (nLen < _cPreferences.nRollPrerenderQueueMax / 2)
                            (new Logger()).WriteDebug("roll queue = " + nLen);
                        _cPreferences.cRoll.Prepare(1);
                        if (_cCurrentTI.cBTLText.nFrameCurrent >= (ulong)_cCurrentTI.nDuration)
                        {
                            AddTransition(Transition.Normal);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
            finally
            {
                _bWorkerAborted = true;
            }
        }
        private void AddTransition(Transition eTrans)
        {
            switch (eTrans)
            {
                case Transition.Initial:
                    _cPreferences.cRoll.EffectAdd(_cCurrentTI.cBTLText, _cCurrentTI.cNode);
                    _nNormTop = 0;
                    _nDifTop = (_cCurrentTI.cBTLText.stArea.nHeight * 0.67F - _nNormTop) / _cCurrentTI.nTransDuration;
                    _nNormBot = _cCurrentTI.cBTLText.nPressBottom;
                    _nDifBot = (140 - _nNormBot) / _cCurrentTI.nTransDuration;
                    for (int nI = 0; nI <= _cCurrentTI.nTransDuration; nI++)
                    {
                        _cCurrentTI.cBTLText.nPressBottom = 140 - _nDifBot * nI;
                        _cPreferences.cRoll.Prepare(1);
                    }
                    while (_cPreferences.cRoll.nPrerenderQueueCount < (ulong)_cPreferences.nRollPrerenderQueueMax)
                    {
                        _cPreferences.cRoll.Prepare(1);
                    }
                    break;
                case Transition.Normal:
                    Preferences.TextItem cPrevious = _cCurrentTI;
                    _cCurrentTI = _cCurrentTI.cNext;
                    _cPreferences.cRoll.EffectAdd(_cCurrentTI.cBTLText, _cCurrentTI.cNode);

                    for (int nI = 0; nI < _cCurrentTI.nTransDuration; nI++)
                    {
                        cPrevious.cBTLText.nPressBottom = _nNormBot + _nDifBot * (nI + 1);
                        _cPreferences.cRoll.SetKeyframesToEffect(cPrevious.cBTLText, new Roll.Keyframes(null, new Roll.Keyframe[1] { new Roll.Keyframe() { eType = Roll.Keyframe.Type.hold, nFrame = 0, nPosition = _nNormTop + _nDifTop * (nI + 1) } }));
                        _cCurrentTI.cBTLText.nPressBottom = 140 - _nDifBot * nI;
                        _cPreferences.cRoll.Prepare(1);
                    }
                    _cCurrentTI.cBTLText.nPressBottom = _nNormBot;
                    cPrevious.cBTLText.Stop();
                    _cPreferences.cRoll.Prepare(1);
                    cPrevious.cBTLText.Idle();
                    cPrevious.cBTLText.nPressBottom = _nNormBot;
                    break;
                case Transition.Final:
                    for (int nI = 0; nI < _cCurrentTI.nTransDuration; nI++)
                    {
                        if (nI == 2)
                            _cPreferences.cPlaylist.Skip(true, 0);
                        _cCurrentTI.cBTLText.nPressBottom = _nNormBot + _nDifBot * (nI + 1);
                        //_cPreferences.cRoll.SetKeyframesToEffect(_cCurrentTI.cBTLText, new Roll.Keyframe[1] { new Roll.Keyframe() { eType = Roll.Keyframe.Type.hold, nFrame = 0, nPosition = _nNormTop + _nDifTop * nI } });
                        _cPreferences.cRoll.Prepare(1);
                    }
                    _cCurrentTI.cBTLText.Stop();
                    _cPreferences.cRoll.Prepare(1);
                    break;
                default:
                    break;
            }
        }

        private void _cRoll_Stopped(Effect cSender)
		{
			(new Logger()).WriteDebug("stopped");
			Stop();
		}

		public void Start()
		{
            lock (_oLock)
            {
                if (_bStopped || _bStarted)
                    return;
                _bStarted = true;
            }
            (new Logger()).WriteDebug("start:in");
			_cPreferences.cRoll.Start();
			if (null != Started)
				Plugin.EventSend(Started, this);
			(new Logger()).WriteDebug("start:out");
		}

		public void Stop()
		{
			lock (_oLock)
			{
				if (_bStopped)
					return;
				_bStopped = true;
			}
			try
			{
                (new Logger()).WriteDebug("stop:in");
                if (_bStarted)
                {
                    (new Logger()).WriteDebug("waiting for roll stopping");
                    _cPreferences.cRoll.nDuration = _cPreferences.cRoll.nFrameCurrent + 75;
                    DateTime dtKill = DateTime.Now.AddSeconds(10);
                    while (_cPreferences.cRoll.eStatus != BTL.EffectStatus.Stopped)
                    {
                        System.Threading.Thread.Sleep(40);
                        if (DateTime.Now > dtKill)
                        {
                            (new Logger()).WriteDebug("stop:mid: break waiting");
                            break;
                        }
                    }
                    (new Logger()).WriteDebug("stop:mid: roll stopped");
                }
                else
                    System.Threading.Thread.Sleep(200);

                if (_cPreferences.cRoll.eStatus == BTL.EffectStatus.Running || _cPreferences.cRoll.eStatus == BTL.EffectStatus.Preparing)
					_cPreferences.cRoll.Stop();
				_cPreferences.cRoll.Dispose();

                if (null!= _cThreadWorker && _cThreadWorker.IsAlive)
                {
                    _cThreadWorker.Abort();
                    //_cThreadWorker.Join();
                }
                (new Logger()).WriteDebug("stop:mid");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			if (null != Stopped)
				Plugin.EventSend(Stopped, this);
			(new Logger()).WriteDebug("stop:out");
		}
		public BTL.EffectStatus __eStatus
		{
			get
			{
				return ((IPlugin)this).eStatus;
			}
		}


		#region IPlugin
		event EventDelegate IPlugin.Prepared
		{
			add
			{
				this.Prepared += value;
			}
			remove
			{
				this.Prepared -= value;
			}
		}
		event EventDelegate IPlugin.Started
		{
			add
			{
				this.Started += value;
			}
			remove
			{
				this.Started -= value;
			}
		}
		event EventDelegate IPlugin.Stopped
		{
			add
			{
				this.Stopped += value;
			}
			remove
			{
				this.Stopped -= value;
			}
		}
		void IPlugin.Create(string sWorkFolder, string sData)
		{
			this.Create(sWorkFolder, sData);
		}
		BTL.EffectStatus IPlugin.eStatus
		{
			get
			{
				if (null != _cPreferences.cRoll)
					return _cPreferences.cRoll.eStatus;
				return BTL.EffectStatus.Idle;
			}
		}
		DateTime IPlugin.dtStatusChanged
		{
			get
			{
				if (null != _cPreferences.cRoll)
					return _cPreferences.cRoll.dtStatusChanged;
				return DateTime.Now;
			}
		}
		void IPlugin.Prepare()
		{
			this.Prepare();
		}
		void IPlugin.Start()
		{
			this.Start();
		}
		void IPlugin.Stop()
		{
			this.Stop();
		}
		#endregion
	}
}
