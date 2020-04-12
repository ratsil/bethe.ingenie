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
	public class Weather : MarshalByRefObject, IPlugin
	{
		private Preferences _cPreferences;
		private EventDelegate Prepared;
		private EventDelegate Started;
		private EventDelegate Stopped;
		private object _oLock;
		private bool _bPrepared;
		private bool _bStopped;

		public void Create(string sWorkFolder, string sData)
		{
			_oLock = new object();
			_bStopped = false;
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
						(new Logger()).WriteWarning("Credits has already prepared!");
						return;
					}
					_bPrepared = true;
				}
				(new Logger()).WriteDebug("prepare:in");
				btl.Roll cRoll = _cPreferences.cRoll;
				
				cRoll.Stopped += _cRoll_Stopped;
				btl.Text cText;
				btl.Animation cAnim;
				//btl.Roll.Keyframe[] aKFHold = new btl.Roll.Keyframe[1] { new btl.Roll.Keyframe() { eType = Roll.Keyframe.Type.hold, nFrame = 0, nPosition = 0 } };



				bool bFirstTime = true;
				bool bLastTime = false;
				int nOdd = 1;
				//cRoll.nDuration = 200;

				foreach (helpers.data.Data.WeatherItem cWI in _cPreferences.aWeatherItems)
				{
					if (cWI == _cPreferences.aWeatherItems[_cPreferences.aWeatherItems.Count - 1])
						bLastTime = true;
					if (bFirstTime)
					{
						cRoll.EffectAdd(_cPreferences.ahItems["backgr_intro"].cVideo, null, false, false);
						cRoll.Prepare(53);
					}
					// city
					cText = (btl.Text)_cPreferences.ahItems["text_city_" + nOdd].cVideo;
                    cRoll.RemoveEffect(cText);
                    IdleEffect(cText);
					cText.sText = cWI.sCity;
					cText.iMask = _cPreferences.ahItems["mask_city_loop_" + nOdd].cVideo;
					IdleEffect(cText.iMask);
                    cRoll.EffectAdd(cText, _cPreferences.ahItems["text_city_" + nOdd].aKFs[0], float.MinValue, false, 0, false, false);
                    cRoll.Prepare(3);
					// time  81 
					cAnim = (btl.Animation)_cPreferences.ahItems["backgr_pink"].cVideo;
                    cRoll.RemoveEffect(cAnim);
					IdleEffect(cAnim);
					cRoll.EffectAdd(_cPreferences.ahItems["backgr_pink"].cVideo, null, false, false);

					cText = (btl.Text)_cPreferences.ahItems["text_time"].cVideo;
                    cRoll.RemoveEffect(cText);
                    IdleEffect(cText);
					cText.sText = cWI.sTime;
					cText.iMask = _cPreferences.ahItems["mask_time"].cVideo;
					IdleEffect(cText.iMask);
					cRoll.EffectAdd(cText, _cPreferences.ahItems["text_time"].aKFs[0], float.MinValue, false, 0, false, false);
					cRoll.Prepare(2);
					// temperature
					if (bFirstTime)
					{
						cRoll.EffectAdd(_cPreferences.ahItems["backgr_black_in"].cVideo, null, false, false);
					}

					cText = (btl.Text)_cPreferences.ahItems["text_temperature"].cVideo;
                    cRoll.RemoveEffect(cText);
                    cText.bWaitForOutDissolve = false;
					IdleEffect(cText);
					cText.sText = cWI.sTemperature.StartsWith("-") || cWI.sTemperature.StartsWith("+") ? cWI.sTemperature.Substring(0, 1) + " " + cWI.sTemperature.Substring(1) : cWI.sTemperature;
					if (bFirstTime)
					{
						cText.iMask = _cPreferences.ahItems["mask_tempr_in"].cVideo;
						IdleEffect(cText.iMask);
					}
					if (bFirstTime)
						cRoll.EffectAdd(cText, _cPreferences.ahItems["text_temperature"].aKFs[0], float.MinValue, false, 0, false, false);
					else if (bLastTime)
						cRoll.EffectAdd(cText, _cPreferences.ahItems["text_temperature"].aKFs[1], float.MinValue, false, 0, false, false);
					else
						cRoll.EffectAdd(cText, _cPreferences.ahItems["text_temperature"].aKFs[2], float.MinValue, false, 0, false, false);
					cRoll.Prepare(2);
					// icon
					try
					{
						cAnim = (btl.Animation)btl.Effect.EffectGet(_cPreferences.ahItems["animation_icon"].XMLReplace("{%FOLDER%}", cWI.sIcon));
						cRoll.EffectAdd(cAnim, _cPreferences.ahItems["animation_icon"].aKFs[0], float.MinValue, false, 0, false, false);
					}
					catch (Exception ex)
					{
						(new Logger()).WriteError("icon problem [icon="+cWI.sIcon+"]<br>" + ex);
					}
					cRoll.Prepare(6);
					// ---
					if (bFirstTime)
					{
						cRoll.EffectAdd(_cPreferences.ahItems["backgr_black_loop"].cVideo, null, false, false);
						cRoll.EffectAdd(_cPreferences.ahItems["backgr_final_loop"].cVideo, null, false, false);
						bFirstTime = false;
					}
					cRoll.Prepare(68);

					nOdd = nOdd == 1 ? 2 : 1;
				}
				//cRoll.Prepare(5);
				StopEffect(_cPreferences.ahItems["backgr_black_loop"].cVideo);
				cAnim = (btl.Animation)_cPreferences.ahItems["backgr_black_out"].cVideo;
				StopEffect(cAnim);
				cRoll.EffectAdd(_cPreferences.ahItems["backgr_black_out"].cVideo, null, false, false);
				cText = (btl.Text)_cPreferences.ahItems["text_yandex"].cVideo;
				cRoll.EffectAdd(cText, null, false, false);
				cRoll.Prepare(110);
				cAnim = (btl.Animation)_cPreferences.ahItems["backgr_final_pink"].cVideo;
				StopEffect(cAnim);
				cRoll.EffectAdd(cAnim, null, false, false);
				cRoll.Prepare(8);
				StopEffect(_cPreferences.ahItems["backgr_final_loop"].cVideo);
				StopEffect(cText);
				cRoll.Prepare(20);

				if (null != Prepared)
					Plugin.EventSend(Prepared, this);
				(new Logger()).WriteDebug("prepare:out");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		private void IdleEffect(BTL.IVideo cEffect)
		{
			if (((BTL.IEffect)cEffect).eStatus == BTL.EffectStatus.Running)
				((BTL.IEffect)cEffect).Stop();
			if (((BTL.IEffect)cEffect).eStatus == BTL.EffectStatus.Stopped)
				((BTL.IEffect)cEffect).Idle();
		}
		private void StopEffect(BTL.IVideo cEffect)
		{
			if (((BTL.IEffect)cEffect).eStatus == BTL.EffectStatus.Running)
				((BTL.IEffect)cEffect).Stop();
		}
		private void _cRoll_Stopped(Effect cSender)
		{
			Stop();
		}

		public void Start()
		{
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
				if (_cPreferences.cRoll.eStatus == BTL.EffectStatus.Running)
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
				if (null != _cPreferences && null != _cPreferences.cRoll)
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
