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
    public class Comingup : MarshalByRefObject, IPlugin
	{
		private Preferences _cPreferences;
		private EventDelegate Prepared;
		private EventDelegate Started;
		private EventDelegate Stopped;
		private bool bPrepared;
		private object _oLock;
		private object _oLockStatus;
		private bool _bStopped;
		private BTL.EffectStatus _estatus;
		private DateTime _dtStatusChanged;
		private ingenie.plugins.Credits _cCredits;
		public List<BTL.IEffect> _aEffects;

		public void Create(string sWorkFolder, string sData)
		{
			(new Logger()).WriteDebug("create");
			BTL.Baetylus cBTL = BTL.Baetylus.Helper.cBaetylus;
			_aEffects = new List<BTL.IEffect>();
			_estatus = BTL.EffectStatus.Idle;
			_dtStatusChanged = DateTime.Now;
			_oLock = new object();
			_oLockStatus = new object();
			_bStopped = false;
			_cPreferences = new Preferences(sData);
			_cCredits = new Credits();
			_cCredits.Create(sWorkFolder, "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n" + _cPreferences.sCredits);
		}

		public void Prepare()
		{
			try
			{
                //PixelsMap.DisComInit();
                lock (_oLock)
				{
					if (bPrepared)
					{
						(new Logger()).WriteWarning("Comingup has already prepared!");
						return;
					}
					bPrepared = true;
				}
				(new Logger()).WriteDebug("prepare:in");
				_cPreferences.cOverTop.Stopped += COverTop_Stopped;

				foreach (btl.Roll cR in _cPreferences.aRoll)
					_aEffects.Add(cR);
				_aEffects.Add(_cPreferences.cHashTag);
				_aEffects.Add(_cPreferences.cOverTop);
				_aEffects.Add(_cPreferences.cOverBot);

				ulong nSimulID = (ulong)DateTime.Now.Subtract(DateTime.MinValue).Ticks;
				ushort nEffectsQty = (ushort)(_aEffects.Count + 1);

				_cCredits.SetSimultaneousParams(nSimulID, nEffectsQty);
				_cCredits.Prepare();

                int nI = 0;
                foreach (BTL.IEffect cE in _aEffects)
                {
                    cE.SimultaneousSet(nSimulID, nEffectsQty);
                    cE.Prepare();
                    if (cE is btl.Roll && nI++ < 2)
                        System.Threading.Thread.Sleep(200); // паузы при заборе фрагментов клипов, а то один раз дало сбой - не хватило времени эфиру взять клипы.
                }
                lock (_oLockStatus)
                {
                    _estatus = BTL.EffectStatus.Preparing;
					_dtStatusChanged = DateTime.Now;
				}
				if (null != Prepared)
					Plugin.EventSend(Prepared, this);
				(new Logger()).WriteDebug("prepare:out");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		public void VideoFramesDelay(string sName)
		{

			if (sName == "rollvideo")
				Thread.Sleep(_cPreferences.nVideoFramesDelay);
		}
		private void COverTop_Stopped(Effect cSender)
		{
			Stop();  // т.к. овертоп заканчивается последний - после него ничего не должно быть
		}

		public void Start()
		{
			(new Logger()).WriteDebug("start:in");
			_cCredits.Start();
			foreach (BTL.IEffect cE in _aEffects)
				cE.Start();

			lock (_oLockStatus)
			{
				_estatus = BTL.EffectStatus.Running;
				_dtStatusChanged = DateTime.Now;
			}
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
				if (((IPlugin)_cCredits).eStatus == BTL.EffectStatus.Running)
					_cCredits.Stop();
				foreach (BTL.IEffect cE in _aEffects)
					cE.Dispose();
				(new Logger()).WriteDebug("stop:mid");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			lock (_oLockStatus)
			{
				_estatus = BTL.EffectStatus.Stopped;
				_dtStatusChanged = DateTime.Now;
			}
			if (null != Stopped)
				Plugin.EventSend(Stopped, this);
			(new Logger()).WriteDebug("stop:out");
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
				lock (_oLockStatus)
				{
					if (_estatus != BTL.EffectStatus.Idle)
					{
						BTL.EffectStatus eStatus = ((IPlugin)_cCredits).eStatus;
						foreach (BTL.IEffect cE in _aEffects)
						{
							if (eStatus != cE.eStatus)
								return _estatus;
						}
						if (_estatus != eStatus)
						{
							_estatus = eStatus;
							_dtStatusChanged = DateTime.Now;
						}
					}
					return _estatus;
				}
			}
		}
		DateTime IPlugin.dtStatusChanged
		{
			get
			{
				return _dtStatusChanged;
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
