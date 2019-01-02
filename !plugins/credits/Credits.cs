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
	public class Credits : MarshalByRefObject, IPlugin
	{
		private Preferences _cPreferences;
		private EventDelegate Prepared;
		private EventDelegate Started;
		private EventDelegate Stopped;
		private object _oLock;
		private bool _bPrepared;
		private bool _bStopped;
        private bool _bStarted;
        private ulong _nSimultaneousID;
		private ushort _nSimultaneousTotalQty;

		public void Create(string sWorkFolder, string sData)
		{
			(new Logger()).WriteDebug("create");
			_nSimultaneousTotalQty = 0;
			_oLock = new object();
			_bStopped = false;
			_cPreferences = new Preferences(sData);
		}

		public void SetSimultaneousParams(ulong nSimultaneousID, ushort nSimultaneousTotalQty)
		{
			this._nSimultaneousID = nSimultaneousID;
			this._nSimultaneousTotalQty = nSimultaneousTotalQty;
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
						(new Logger()).WriteWarning("Credits has already prepared!");
						return;
					}
					_bPrepared = true;
				}
				(new Logger()).WriteDebug("prepare:in");
				_cPreferences.cRoll.Stopped += _cRoll_Stopped;
				_cPreferences.cRoll.SimultaneousSet(_nSimultaneousID, _nSimultaneousTotalQty); // сработает при количестве > 1
				_cPreferences.cRoll.Prepare();
				if (null != Prepared)
					Plugin.EventSend(Prepared, this);
				(new Logger()).WriteDebug("prepare:out");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
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
				if (_cPreferences.cRoll.eStatus == BTL.EffectStatus.Running || _cPreferences.cRoll.eStatus == BTL.EffectStatus.Preparing)
					_cPreferences.cRoll.Stop();
				_cPreferences.cRoll.Dispose();
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
