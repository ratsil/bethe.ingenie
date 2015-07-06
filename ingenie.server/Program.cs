using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Drawing;

using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
//using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Tcp;
using helpers.extensions;
using ingenie.plugins;

namespace ingenie.server
{
    class Program
    {
		static Dictionary<shared.Effect, EffectCover> _ahEffects;
		static Dictionary<shared.Effect, EffectCover> _ahEffectsRemoved;

		static void Main(string[] args)
        {
			try
			{
				int nPID = System.Diagnostics.Process.GetCurrentProcess().Id;
				if (0 < args.Length)
					BTL.Preferences.sFile = AppDomain.CurrentDomain.BaseDirectory + args[0];
				(new Logger()).WriteNotice("сервер объектов запущен [pid:" + nPID + "]");
				if (!System.IO.File.Exists(BTL.Preferences.sFile))
					throw new System.IO.FileNotFoundException("файл конфигурации не найден [pid:" + nPID + "][" + BTL.Preferences.sFile + "]");
				(new Logger()).WriteNotice("файл конфигурации: [pid:" + nPID + "][" + BTL.Preferences.sFile + "]");
				Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
				BinaryServerFormatterSinkProvider cBinaryServerFormatterSinkProvider = new BinaryServerFormatterSinkProvider();
				cBinaryServerFormatterSinkProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
				Dictionary<string, int> ahProperties = new Dictionary<string, int>();
				ahProperties.Add("port", shared.Preferences.nPort);
				ChannelServices.RegisterChannel(new TcpChannel(ahProperties, new BinaryClientFormatterSinkProvider(), cBinaryServerFormatterSinkProvider), false);

				// Pass the properties for the port setting and the server provider in the server chain argument. (Client remains null here.)
				//System.Runtime.Remoting.Channels.ChannelServices.RegisterChannel(new System.Runtime.Remoting.Channels.Tcp.TcpChannel(properties, null, provider), false);

                (new Logger()).WriteNotice("CUDA:" + (BTL.Preferences.bCUDA ? helpers.PixelsMap.Preferences.nCUDAVersion.ToString() : "NO"));

				_ahEffects = new Dictionary<shared.Effect, EffectCover>();
				_ahEffectsRemoved = new Dictionary<shared.Effect, EffectCover>();
				//RemotingConfiguration.RegisterWellKnownServiceType(typeof(shared.Animation), "Animation.soap", WellKnownObjectMode.Singleton);

				RemotingConfiguration.RegisterActivatedServiceType(typeof(shared.Device));
				RemotingConfiguration.RegisterActivatedServiceType(typeof(shared.Video));
				RemotingConfiguration.RegisterActivatedServiceType(typeof(shared.Audio));
				RemotingConfiguration.RegisterActivatedServiceType(typeof(shared.Animation));
				RemotingConfiguration.RegisterActivatedServiceType(typeof(shared.Clock));
				RemotingConfiguration.RegisterActivatedServiceType(typeof(shared.Playlist));
				RemotingConfiguration.RegisterActivatedServiceType(typeof(shared.Plugin));
				RemotingConfiguration.RegisterActivatedServiceType(typeof(shared.Text));
				RemotingConfiguration.RegisterActivatedServiceType(typeof(shared.Roll));
				RemotingConfiguration.RegisterActivatedServiceType(typeof(shared.Helper));

				#region device
				shared.Device.OnDownStreamKeyerGet += Device_OnDownStreamKeyerGet;
				shared.Device.OnDownStreamKeyerSet += Device_OnDownStreamKeyerSet;
				#endregion
				#region effect
				shared.Effect.OnLayerGet += new shared.Effect.UShortGetDelegate(Effect_OnLayerGet);
				shared.Effect.OnLayerSet += new shared.Effect.UShortSetDelegate(Effect_OnLayerSet);
				shared.Effect.OnFrameStartGet += new shared.Effect.ULongGetDelegate(Effect_OnFrameStartGet);
				shared.Effect.OnFrameStartSet += new shared.Effect.ULongSetDelegate(Effect_OnFrameStartSet);
				shared.Effect.OnDurationGet += new shared.Effect.ULongGetDelegate(Effect_OnDurationGet);
				shared.Effect.OnDurationSet += new shared.Effect.ULongSetDelegate(Effect_OnDurationSet);
				shared.Effect.OnDelayGet += new shared.Effect.ULongGetDelegate(Effect_OnDelayGet);
				shared.Effect.OnDelaySet += new shared.Effect.ULongSetDelegate(Effect_OnDelaySet);
				shared.Effect.OnFramesTotalGet += new shared.Effect.ULongGetDelegate(Effect_OnFramesTotalGet);
				shared.Effect.OnFrameCurrentGet += new shared.Effect.ULongGetDelegate(Effect_OnFrameCurrentGet);
				shared.Effect.OnTagGet += new shared.Effect.ObjectGetDelegate(Effect_OnTagGet);
				shared.Effect.OnTagSet += new shared.Effect.ObjectSetDelegate(Effect_OnTagSet);
				shared.Effect.OnStatusGet += new shared.Effect.StatusGetDelegate(StatusGet);

				shared.Effect.OnPrepare += new shared.Effect.PrepareDelegate(Prepare);
				shared.Effect.OnStart += new shared.Effect.StartDelegate(Start);
				shared.Effect.OnStop += new shared.Effect.StopDelegate(Stop);
				shared.Effect.OnDispose += new shared.Effect.DisposeDelegate(Dispose);
				#endregion
				#region container
                shared.Container.OnEffectsQtyGet += Container_OnEffectsQtyGet;
				shared.Container.OnSumDurationGet += Container_OnSumDurationGet;
				#endregion
				#region effect video
				shared.EffectVideo.OnDockSet += new shared.EffectVideo.DockSetDelegate(EffectVideo_OnDockSet);
				shared.EffectVideo.OnOpacityGet += new shared.Effect.BoolGetDelegate(EffectVideo_OnOpacityGet);
				shared.EffectVideo.OnOpacitySet += new shared.Effect.BoolSetDelegate(EffectVideo_OnOpacitySet);
				shared.EffectVideo.OnCUDAGet += new shared.Effect.BoolGetDelegate(EffectVideo_OnCUDAGet);
				shared.EffectVideo.OnCUDASet += new shared.Effect.BoolSetDelegate(EffectVideo_OnCUDASet);
				shared.EffectVideo.OnAreaGet += new shared.EffectVideo.AreaGetDelegate(EffectVideo_OnAreaGet);
				shared.EffectVideo.OnAreaSet += new shared.EffectVideo.AreaSetDelegate(EffectVideo_OnAreaSet);
				#endregion
				#region effect audio
				shared.EffectAudio.OnChannelsGet += new shared.Effect.ByteArrayGetDelegate(EffectAudio_OnChannelGet);
				shared.EffectAudio.OnChannelsSet += new shared.Effect.ByteArraySetDelegate(EffectAudio_OnChannelSet);
				#endregion
				#region effect video&audio
				shared.EffectVideoAudio.OnChannelsGet += new shared.Effect.ByteArrayGetDelegate(EffectVideoAudio_OnChannelGet);
				shared.EffectVideoAudio.OnChannelsSet += new shared.Effect.ByteArraySetDelegate(EffectVideoAudio_OnChannelSet);
				#endregion
				shared.Video.OnCreate += new shared.Video.CreateDelegate(VideoCreate);
				shared.Audio.OnCreate += new shared.Audio.CreateDelegate(AudioCreate);
				shared.Animation.OnCreate += new shared.Animation.CreateDelegate(AnimationCreate);
				shared.Text.OnCreate += new shared.Text.CreateDelegate(TextCreate);
				shared.Clock.OnCreate += new shared.Clock.CreateDelegate(ClockCreate);
				#region playlist
				shared.Playlist.OnCreate += new shared.Playlist.CreateDelegate(PlaylistCreate);
				shared.Playlist.OnAddEffect += new shared.Playlist.EffectAddDelegate(PlaylistAddEffect);
				shared.Playlist.OnSkip += new shared.Playlist.SkipDelegate(PlaylistSkip);
                shared.Playlist.OnEndTransDurationSet += new shared.Playlist.EndTransDurationSetDelegate(Playlist_OnEndTransDurationSet);
                shared.Playlist.OnPLItemDelete += new shared.Playlist.PLItemDeleteDelegate(Playlist_OnPLItemsDelete);
				#endregion
				#region roll
				shared.Roll.OnCreate += new shared.Effect.CreateDelegate(RollCreate);
				shared.Roll.OnEffectsQtyGet += new shared.Roll.EffectsQtyGetDelegate(Roll_OnEffectsQtyGet);
				shared.Roll.OnDirectionGet += new shared.Roll.DirectionGetDelegate(Roll_OnDirectionGet);
				shared.Roll.OnDirectionSet += new shared.Roll.DirectionSetDelegate(Roll_OnDirectionSet);
				shared.Roll.OnSpeedGet += new shared.Roll.SpeedGetDelegate(Roll_OnSpeedGet);
				shared.Roll.OnSpeedSet += new shared.Roll.SpeedSetDelegate(Roll_OnSpeedSet);
				shared.Roll.OnStopOnEmptyGet += new shared.Roll.StopOnEmptyGetDelegate(Roll_OnStopOnEmptyGet);
				shared.Roll.OnStopOnEmptySet += new shared.Roll.StopOnEmptySetDelegate(Roll_OnStopOnEmptySet);

				shared.Roll.OnEffectAdd += new shared.Roll.EffectAddDelegate(Roll_OnEffectAdd);
				#endregion

				shared.Plugin.OnCreate += new shared.Plugin.CreateDelegate(PluginCreate);

				shared.Helper.OnBaetylusEffectsInfoGet += OnBaetylusEffectsInfoGet;
                shared.Helper.OnBaetylusEffectStop += OnBaetylusEffectStop;
                shared.Helper.OnDisComInit += Helper_OnDisComInit;

				#region . GC .
				DateTime dtReportNext = DateTime.Now.AddMinutes(10);
                int nDeletingIdleDelay = 6000;  //в секундах
                int nDeletingStopDelay = 30;  //в секундах
				while (true)
				{
					try
					{
						(new Logger()).WriteDebug4("before lock");   // типа debu4 - это когда спам вообще на ровном месте ))... что же будет 9? )))
						lock (_ahEffects)
						{
							(new Logger()).WriteDebug4("inside lock");
							shared.Effect[] aEffectsShared = _ahEffects.Keys.ToArray();
							if (DateTime.Now > dtReportNext)
							{
								string sMessage = "";
								foreach (shared.Effect cEffectShared in aEffectsShared)
									if (_ahEffects[cEffectShared].StatusIsOlderThen(3600))
										sMessage += "<br>\t[ec:" + _ahEffects[cEffectShared].GetHashCode() + "][es:" + cEffectShared.GetHashCode() + "][name:" + _ahEffects[cEffectShared].sType + "][info:" + _ahEffects[cEffectShared].sInfo + "][status:" + _ahEffects[cEffectShared].eStatus + "]";
								(new Logger()).WriteNotice("gc:effects:count:" + aEffectsShared.Length + "; timeworns:" + sMessage);
								dtReportNext = DateTime.Now.AddMinutes(10);
							}
							foreach (shared.Effect cEffectShared in aEffectsShared)
							{
                                if (null == _ahEffects[cEffectShared]) // это если эффект добавлен только что, то он еще может быть нул. //EMERGENCY и это проблема!!! т.к. данная строчка - это единственное место, где мы ожидаем null... а во всех остальных местах мы поимеем эксепшн (и уже поимели и I.S грохнулся с анхелдед эксепшн) есть ли какая-то причина почему у тебя все EffectCreate идут перед созданием эффекта BTL'ного эффекта? не проще ли EffectCreate перенеси в лок и проблемы такой в принципе не будет?
                                    continue;
                                if (
                                    _ahEffects[cEffectShared].StatusIsOlderThen(BTL.EffectStatus.Stopped, nDeletingStopDelay) ||
                                    _ahEffects[cEffectShared].StatusIsOlderThen(BTL.EffectStatus.Error, nDeletingStopDelay) ||
									_ahEffects[cEffectShared].StatusIsOlderThen(BTL.EffectStatus.Preparing, nDeletingIdleDelay) ||
									_ahEffects[cEffectShared].StatusIsOlderThen(BTL.EffectStatus.Idle, nDeletingIdleDelay) ||
                                    _ahEffects[cEffectShared].StatusIsOlderThen(BTL.EffectStatus.Unknown, nDeletingIdleDelay)
								)
								{
									(new Logger()).WriteNotice("gc:remove: [hc:" + _ahEffects[cEffectShared].GetHashCode() + "][name:" + _ahEffects[cEffectShared].sType + "][info:" + _ahEffects[cEffectShared].sInfo + "][status:" + _ahEffects[cEffectShared].eStatus + "]");
									_ahEffectsRemoved.Add(cEffectShared, _ahEffects[cEffectShared]);
									if (BTL.EffectStatus.Running == _ahEffects[cEffectShared].eStatus)  // ввёл лог сюда только чтобы понять бывает ли вообще такое
									{
										(new Logger()).WriteWarning("gc:effect: it have happened in inner foreach!!!!!!" + cEffectShared.GetHashCode());
										continue;
									}
									_ahEffects.Remove(cEffectShared);
								}
							}
//							GC.Collect();
						}
						foreach (shared.Effect cEffectShared in _ahEffectsRemoved.Keys.ToArray())
						{
							if (shared.Status.Started == cEffectShared.eStatus) // ввёл лог сюда только чтобы понять бывает ли вообще такое
							{
								(new Logger()).WriteWarning("gc:effect: it have happened in outer foreach!!!!!!" + cEffectShared.GetHashCode());
								continue;
							}
							(new Logger()).WriteDebug3("gc:effect:dispose:" + cEffectShared.GetHashCode());
							_ahEffectsRemoved[cEffectShared].Dispose();
							cEffectShared.Dispose();
							_ahEffectsRemoved.Remove(cEffectShared);
							(new Logger()).WriteDebug3("gc:effect:remove:" + cEffectShared.GetHashCode());
						}
					}
					catch (Exception ex)
					{
						(new Logger()).WriteError(ex);
					}
					Thread.Sleep(3000);
				}
				#endregion
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			(new Logger()).WriteNotice("сервер объектов остановлен");
		}

        static void Helper_OnDisComInit()
        {
            try
            {
                helpers.DisCom.Init();
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
        }
        public static EffectCover EffectCoverGet(BTL.Play.Effect cEffect)
        {
            lock (_ahEffects)
                return _ahEffects.Values.FirstOrDefault(row => row.oEffect == cEffect);
        }
        public static EffectCover EffectCoverGet(shared.Effect cEffect)
        {
			lock (_ahEffects)
			{
				if (_ahEffects.ContainsKey(cEffect))
					return _ahEffects[cEffect];
			}
			return null;
        }
        static List<int> OnBaetylusEffectStop(List<int> aHashes)
        {
			(new Logger()).WriteDebug3("in");
            List<int> aRetVal = new List<int>();
            List<BTL.Play.Effect> aStopping = new List<BTL.Play.Effect>();
            List<BTL.Play.Effect> aRemoveFromStopping = new List<BTL.Play.Effect>();
            try
            {
                List<BTL.Play.Effect> aBEffects = BTL.Baetylus.Helper.cBaetylus.BaetylusEffectsInfoGet();
                foreach (BTL.Play.Effect cBEI in aBEffects) // выкидываем которых нет
                {
                    int nHC = cBEI.GetHashCode();
                    if (!aHashes.Contains(nHC))
                    {
                        aRetVal.Add(nHC);
                        aHashes.Remove(nHC);
                        (new Logger()).WriteNotice("попытка остановки эффекта [" + nHC + "] не удалась. Эффект не найден в байтилусе");
                    }
                    else
                    {
                        cBEI.Stop();
                        aStopping.Add(cBEI);
                    }
                }
                DateTime dtWait = DateTime.Now.AddSeconds(2); // не залипнет ли чего-нибудь??
                System.Threading.Thread.Sleep(20);
                while (DateTime.Now < dtWait)
                {
                    foreach (BTL.Play.Effect cBEI in aStopping)
                    {
                        if (BTL.EffectStatus.Stopped == cBEI.eStatus)
                            aRemoveFromStopping.Add(cBEI);
                    }
                    foreach (BTL.Play.Effect cStopped in aRemoveFromStopping)
                    {
                        aStopping.Remove(cStopped);
                    }
                    System.Threading.Thread.Sleep(100);
                }
                foreach (BTL.Play.Effect cBEI in aStopping)
                {
                    int nHC = cBEI.GetHashCode();
                    aRetVal.Add(nHC);
                    (new Logger()).WriteNotice("попытка остановки эффекта [" + nHC + "] не удалась. Не получен статус 'Stopped' из-за таймаута");
                }
				(new Logger()).WriteDebug3("return");
                return aRetVal;
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
                return null;
            }
        }
        static List<shared.Helper.EffectInfo> OnBaetylusEffectsInfoGet()
        {
			(new Logger()).WriteDebug3("in");
            List<shared.Helper.EffectInfo> aRetVal = new List<shared.Helper.EffectInfo>();
            try
            {
                List<BTL.Play.Effect> aEffects = BTL.Baetylus.Helper.cBaetylus.BaetylusEffectsInfoGet();
                EffectCover cEffectCover;
                foreach (BTL.Play.Effect cEffect in aEffects)
                {
                    cEffectCover = EffectCoverGet(cEffect);
                    if (null == cEffectCover)
                        aRetVal.Add(new shared.Helper.EffectInfo() { nHashCode = cEffect.GetHashCode(), sInfo = "NULL", sStatus = cEffect.eStatus.ToString() });
                    else
                        aRetVal.Add(new shared.Helper.EffectInfo() { nHashCode = cEffectCover.oEffect.GetHashCode(), sInfo = cEffectCover.sInfo, sType = cEffectCover.sType, sStatus = ((BTL.Play.Effect)cEffectCover.oEffect).eStatus.ToString() });
                }
				(new Logger()).WriteDebug3("return");
                return aRetVal;
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
                return null;
            }
        }
        #region device
		static shared.Device.DownStreamKeyer Device_OnDownStreamKeyerGet()
		{
			shared.Device.DownStreamKeyer cRetVal = null;
			try
			{
				if(null != BTL.Preferences.cDownStreamKeyer)
					cRetVal = new shared.Device.DownStreamKeyer() { nLevel = BTL.Preferences.cDownStreamKeyer.nLevel, bInternal = BTL.Preferences.cDownStreamKeyer.bInternal };
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return cRetVal;
		}
		static void Device_OnDownStreamKeyerSet(shared.Device.DownStreamKeyer cDownStreamKeyer)
		{
			try
			{
				if (null != cDownStreamKeyer)
				{
					BTL.Preferences.cDownStreamKeyer = new BTL.Preferences.DownStreamKeyer();
					BTL.Preferences.cDownStreamKeyer.nLevel = cDownStreamKeyer.nLevel;
					BTL.Preferences.cDownStreamKeyer.bInternal = cDownStreamKeyer.bInternal;
				}
				else
					BTL.Preferences.cDownStreamKeyer = null;
				BTL.Baetylus.Helper.cBoard.DownStreamKeyer();
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		#endregion
        #region effect
		public static void OnEffectEvent(shared.EffectEventType eEventType, EffectCover cEffectCover)
		{
			Logger cLogger = new Logger();
            cLogger.WriteDebug3("in [" + eEventType.ToString() + "]");
			try
			{
				if (null != cEffectCover)
				{
					shared.Effect cEffectShared = null;
					cLogger.WriteDebug4("effect:event:" + eEventType.ToString() + ":lock:before [ec hc:" + cEffectCover.GetHashCode() + " info: " + cEffectCover.sInfo + "]");
					lock (_ahEffects)
						cEffectShared = _ahEffects.FirstOrDefault(row => row.Value == cEffectCover).Key;
					cLogger.WriteDebug4("effect:event:" + eEventType.ToString() + ":lock:after [ec hc:" + cEffectCover.GetHashCode() + "]");
					if (null != cEffectShared)
					{
						cLogger.WriteDebug4("effect:event:" + eEventType.ToString() + ":raise:before [ec hc:" + cEffectCover.GetHashCode() + "][shared hc:" + cEffectShared.GetHashCode() + "]");
						cEffectShared.OnEffectEventRaised(eEventType);
						cLogger.WriteDebug4("effect:event:" + eEventType.ToString() + ":raise:after [ec hc:" + cEffectCover.GetHashCode() + "][shared hc:" + cEffectShared.GetHashCode() + "]");
					}
					else
						throw new Exception("effect:event:" + eEventType.ToString() + ": указанный экземпляр эффекта не зарегистрирован на сервере [ec hc:" + cEffectCover.GetHashCode() + "]");
				}
				else
					throw new Exception("effect:event:" + eEventType.ToString() + ": экземпляр эффекта не может быть null");
			}
			catch (Exception ex)
			{
				cLogger.WriteError(ex);
			}
			cLogger.WriteDebug4("return [" + eEventType.ToString() + "]");
		}

		static ushort Effect_OnLayerGet(shared.Effect cSender)
		{
			ushort nRetVal = ushort.MaxValue;
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:layer:get: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				nRetVal = ((BTL.Play.Effect)cEffectCover.oEffect).nLayer;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return nRetVal;
		}
		static void Effect_OnLayerSet(shared.Effect cSender, ushort nValue)
		{
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:layer:set: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				((BTL.Play.Effect)cEffectCover.oEffect).nLayer = nValue;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		static ulong Effect_OnFrameStartGet(shared.Effect cSender)
		{
			ulong nRetVal = ulong.MaxValue;
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:frame:start:get: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				nRetVal = ((BTL.Play.Effect)cEffectCover.oEffect).nFrameStart;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return nRetVal;
		}
		static void Effect_OnFrameStartSet(shared.Effect cSender, ulong nValue)
		{
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:frame:start:set: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				((BTL.Play.Effect)cEffectCover.oEffect).nFrameStart = nValue;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		static ulong Effect_OnDurationGet(shared.Effect cSender)
		{
			ulong nRetVal = ulong.MaxValue;
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:duration:get: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				nRetVal = ((BTL.Play.Effect)cEffectCover.oEffect).nDuration;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return nRetVal;
		}
		static void Effect_OnDurationSet(shared.Effect cSender, ulong nValue)
		{
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:duration:set: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				((BTL.Play.Effect)cEffectCover.oEffect).nDuration = nValue;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		static ulong Effect_OnDelayGet(shared.Effect cSender)
		{
			ulong nRetVal = ulong.MaxValue;
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:delay:get: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				nRetVal = ((BTL.Play.Effect)cEffectCover.oEffect).nDelay;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return nRetVal;
		}
		static void Effect_OnDelaySet(shared.Effect cSender, ulong nValue)
		{
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:delay:set: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				((BTL.Play.Effect)cEffectCover.oEffect).nDelay = nValue;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		static ulong Effect_OnFramesTotalGet(shared.Effect cSender)
		{
			ulong nRetVal = ulong.MaxValue;
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:frames:total:get: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				nRetVal = ((BTL.Play.EffectVideo)cEffectCover.oEffect).nFramesTotal;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return nRetVal;
		}
		static ulong Effect_OnFrameCurrentGet(shared.Effect cSender)
		{
			ulong nRetVal = ulong.MaxValue;
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:frame:current:get: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				nRetVal = ((BTL.Play.EffectVideo)cEffectCover.oEffect).nFrameCurrent;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return nRetVal;
		}
		static object Effect_OnTagGet(shared.Effect cSender)
		{
			object oRetVal = null;
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:tag:get: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				oRetVal = ((BTL.Play.Effect)cEffectCover.oEffect).oTag;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return oRetVal;
		}
		static void Effect_OnTagSet(shared.Effect cSender, object oValue)
		{
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:tag:set: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				((BTL.Play.Effect)cEffectCover.oEffect).oTag = oValue;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		static shared.Status StatusGet(shared.Effect cEffect)
		{
			shared.Status eRetVal = shared.Status.Idle;
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cEffect);
				if (null != cEffectCover)
					eRetVal = StatusTranslate(cEffectCover.eStatus);
				else
					eRetVal = shared.Status.Error;
			}
			catch (Exception ex)
			{
				eRetVal = shared.Status.Error;
				(new Logger()).WriteError(ex);
			}
			return eRetVal;
		}
		static bool Prepare(shared.Effect cEffect)
		{
			bool bRetVal = false;
			try
			{
				EffectCover cEffectCover = null;
				lock (_ahEffects)
				{
					if (_ahEffects.ContainsKey(cEffect))
						cEffectCover = _ahEffects[cEffect];
				}
				if (null != cEffectCover)
				{
					if (BTL.EffectStatus.Stopped == cEffectCover.eStatus)
						cEffectCover.Idle();
					cEffectCover.Prepare();
					bRetVal = true;
				}
				else
					throw new Exception("prepare: указанный эффект не зарегистрирован [hc:" + cEffect.GetHashCode() + "]"); //TODO LANG
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return bRetVal;
		}
        static bool Start(shared.Effect cEffect)
		{
			bool bRetVal = false;
			try
			{
				EffectCover cEffectCover = null;
				lock (_ahEffects)
				{
					if (_ahEffects.ContainsKey(cEffect))
						cEffectCover = _ahEffects[cEffect];
				}
				if (null != cEffectCover)
				{
                    cEffectCover.Start();
                    bRetVal = true;
				}
				else
					throw new Exception("start: указанный эффект не зарегистрирован [hc:" + cEffect.GetHashCode() + "]"); //TODO LANG
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return bRetVal;
		}
		static bool Stop(shared.Effect cEffect)
		{
			bool bRetVal = false;
			try
			{
				EffectCover cEffectCover = null;
				lock (_ahEffects)
				{
					if (_ahEffects.ContainsKey(cEffect))
						cEffectCover = _ahEffects[cEffect];
				}
				if (null != cEffectCover)
				{
					if (BTL.EffectStatus.Stopped != cEffectCover.eStatus)
					{
						cEffectCover.Stop();
						bRetVal = true;
					}
					else
						(new Logger()).WriteDebug2("stop: указанный эффект уже остановлен [hc:" + cEffect.GetHashCode() + "]");
				}
				else
					throw new Exception("stop: указанный эффект не зарегистрирован [hc:" + cEffect.GetHashCode() + "]"); //TODO LANG
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return bRetVal;
		}
		static bool Dispose(shared.Effect cEffect)
		{
			bool bRetVal = false;
			try
			{
				EffectCover cEffectCover = null;
				lock (_ahEffects)
				{
					if (_ahEffects.ContainsKey(cEffect))
					{
						cEffectCover = _ahEffects[cEffect];
						if (null == cEffectCover)
							(new Logger()).WriteDebug2("effect:dispose: is null [hc:" + cEffect.GetHashCode() + "]");
					}
					else if(_ahEffectsRemoved.ContainsKey(cEffect))
						bRetVal = true;
				}
				if (null != cEffectCover)
				{
					cEffectCover.Dispose();
					bRetVal = true;
				}
				else if (!bRetVal)
					(new Logger()).WriteNotice("dispose: указанный эффект не зарегистрирован [hc:" + cEffect.GetHashCode() + "]");   //TODO LANG    // не ошибка, т.к. он просто удален в GC
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return bRetVal;
		}
		#endregion
        #region container
        public static ushort Container_OnEffectsQtyGet(shared.Effect cSender)
        {
            ushort nRetVal = ushort.MaxValue;
            try
            {
                EffectCover cEffectCover = EffectCoverGet(cSender);
                if (null == cEffectCover)
                    throw new Exception("effect:qty:get: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
                nRetVal = ((BTL.IContainer)cEffectCover.oEffect).nEffectsQty;
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
            return nRetVal;
        }
		public static ulong Container_OnSumDurationGet(shared.Effect cSender)
		{
			ulong nRetVal = ulong.MaxValue;
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:sumduration:get: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				nRetVal = ((BTL.IContainer)cEffectCover.oEffect).nSumDuration;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return nRetVal;
		}
		#endregion
        #region effect video
		static void EffectVideo_OnDockSet(shared.Effect cSender, helpers.Dock cDock)
		{
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:dock:set: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				BTL.Play.EffectVideo cEffectVideoBTL;
				if (cEffectCover.oEffect is BTL.Play.EffectVideoAudio)
					cEffectVideoBTL = (BTL.Play.EffectVideo)(BTL.Play.EffectVideoAudio)cEffectCover.oEffect;
				else
					cEffectVideoBTL = (BTL.Play.EffectVideo)cEffectCover.oEffect;
				cEffectVideoBTL.cDock = cDock;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		static bool EffectVideo_OnOpacityGet(shared.Effect cSender)
		{
			bool bRetVal = false;
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:opacity:get: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				bRetVal = ((BTL.Play.EffectVideo)cEffectCover.oEffect).bOpacity;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return bRetVal;
		}
		static void EffectVideo_OnOpacitySet(shared.Effect cSender, bool bValue)
		{
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:opacity:set: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				((BTL.Play.EffectVideo)cEffectCover.oEffect).bOpacity = bValue;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		static bool EffectVideo_OnCUDAGet(shared.Effect cSender)
		{
			bool bRetVal = false;
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:layer:get: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				bRetVal = ((BTL.Play.EffectVideo)cEffectCover.oEffect).bCUDA;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return bRetVal;
		}
		static void EffectVideo_OnCUDASet(shared.Effect cSender, bool nValue)
		{
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:layer:set: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				((BTL.Play.EffectVideo)cEffectCover.oEffect).bCUDA = nValue;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		static helpers.Area EffectVideo_OnAreaGet(shared.Effect cSender)
		{
			helpers.Area stRetVal = helpers.Area.stEmpty;
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:area:get: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				stRetVal = ((BTL.Play.EffectVideo)cEffectCover.oEffect).stArea;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return stRetVal;
		}
		static void EffectVideo_OnAreaSet(shared.Effect cSender, helpers.Area stArea)
		{
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effect:area:set: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				if (helpers.Area.stEmpty != stArea)
				{
					BTL.Play.EffectVideo cEffectVideoBTL;
					if (cEffectCover.oEffect is BTL.Play.EffectVideoAudio)
						cEffectVideoBTL = (BTL.Play.EffectVideo)(BTL.Play.EffectVideoAudio)cEffectCover.oEffect;
					else
						cEffectVideoBTL = (BTL.Play.EffectVideo)cEffectCover.oEffect;
					cEffectVideoBTL.stArea = stArea;
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		#endregion
        #region effect audio
		static byte[] EffectAudio_OnChannelGet(shared.Effect cSender)
		{
			byte[] aRetVal = null;
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effectaudio:channels:get: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				aRetVal = ((BTL.Play.EffectAudio)cEffectCover.oEffect).aChannels;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return aRetVal;
		}
		static void EffectAudio_OnChannelSet(shared.Effect cSender, byte[] aValue)
		{
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effectaudio:channels:set: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				
				((BTL.Play.EffectAudio)cEffectCover.oEffect).aChannels = aValue;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		#endregion
        #region effect video&audio
		static byte[] EffectVideoAudio_OnChannelGet(shared.Effect cSender)
		{
			byte[] aRetVal = null;
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effectvideoaudio:channels:get: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				aRetVal = ((BTL.Play.EffectVideoAudio)cEffectCover.oEffect).aChannelsAudio;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return aRetVal;
		}
		static void EffectVideoAudio_OnChannelSet(shared.Effect cSender, byte[] aValue)
		{
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("effectvideoaudio:channels:set: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				((BTL.Play.EffectVideoAudio)cEffectCover.oEffect).aChannelsAudio = aValue;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		#endregion
        #region create
		static void EffectCreate(shared.Effect cEffect)
        {
			(new Logger()).WriteDebug3("in [hc:" + cEffect.GetHashCode() + "]");
			EffectCover cEffectCover = null;
			lock (_ahEffects)
			{
				if (_ahEffects.ContainsKey(cEffect))
					cEffectCover = _ahEffects[cEffect];
			}
			if (null != cEffectCover)
			{
				(new Logger()).WriteDebug2("effect:create: effect exists [hc:" + cEffect.GetHashCode() + "]");
				BTL.EffectStatus eES = (BTL.EffectStatus)cEffectCover.eStatus;
				if (BTL.EffectStatus.Stopped == eES || BTL.EffectStatus.Idle == eES)
					cEffectCover.Idle();              //Dispose();    надо бы его убивать вообще-то........
				else
					throw new Exception("create: указанный эффект уже создан [hc:" + cEffect.GetHashCode() + "]"); //TODO LANG
			}
			else
			{
				lock (_ahEffects)
				{
					_ahEffects.Add(cEffect, null);
				}
			}
			(new Logger()).WriteDebug4("return [hc:" + cEffect.GetHashCode() + "]");
		}
		static void VideoCreate(shared.Video cVideoShared, string sFilename, helpers.Dock cDock, ushort nZ, ulong nFrameStart, ulong nDuration, bool bOpacity, ulong nDelay) //TODO убрать dock - его инициализация уже дублируется в userspace
        {
			BTL.Play.Video cVideoBTL = new BTL.Play.Video(sFilename);
			EffectCover cEffectCover = null;
			lock (_ahEffects)
			{
				EffectCreate(cVideoShared);
				_ahEffects[cVideoShared] = new EffectCover(cVideoBTL);
				cEffectCover = _ahEffects[cVideoShared];
			}
			cEffectCover.sType = "Video";
			cEffectCover.sInfo = sFilename;
            cVideoBTL.cDock = cDock;
			cVideoBTL.nLayer = nZ;
			cVideoBTL.nFrameStart = nFrameStart;
			cVideoBTL.nDuration = nDuration;
			cVideoBTL.bOpacity = bOpacity;
			cVideoBTL.nDelay = nDelay;
        }
		static void AudioCreate(shared.Audio cAudioShared, string sFilename)
        {
			BTL.Play.Audio cAudioBTL = new BTL.Play.Audio(sFilename);
			EffectCover cEffectCover = null;
			lock (_ahEffects)
			{
				EffectCreate(cAudioShared);
				_ahEffects[cAudioShared] = new EffectCover(cAudioBTL);
				cEffectCover = _ahEffects[cAudioShared];
			}
			cEffectCover.sType = "Audio";
			cEffectCover.sInfo = sFilename;
        }
		static void AnimationCreate(shared.Animation cAnimationShared, string sFolder, ushort nLoopsQty, bool bKeepAlive, helpers.Dock cDock, ushort nZ, bool bOpacity, ulong nDelay, float nPixelAspectRatio)
        {
			(new Logger()).WriteDebug3("in [bKeepAlive:" + bKeepAlive + "]");
			BTL.Play.Animation cAnimationBTL = new BTL.Play.Animation(sFolder, nLoopsQty, bKeepAlive);
			EffectCover cEffectCover = null;
			lock (_ahEffects)
			{
				EffectCreate(cAnimationShared);
				_ahEffects[cAnimationShared] = new EffectCover(cAnimationBTL);
				cEffectCover = _ahEffects[cAnimationShared];
			}
			cEffectCover.sType = "Animation";
			cEffectCover.sInfo = sFolder;
            cAnimationBTL.cDock = cDock;
            cAnimationBTL.nLayer = nZ;
			cAnimationBTL.bOpacity = bOpacity;
			cAnimationBTL.nDelay = nDelay;
			cAnimationBTL.nPixelAspectRatio = nPixelAspectRatio;
        }
		static void TextCreate(shared.Text cTextShared, object[] aArgs) //EMERGENCY раскрыть массив в нормальные параметры!!!! 
        {
			BTL.Play.Text cTextBTL = new BTL.Play.Text((string)aArgs[0], new Font((string)aArgs[1], (int)aArgs[2], (FontStyle)aArgs[3]), (float)aArgs[7]);
			EffectCover cEffectCover = null;
			lock (_ahEffects)
			{
				EffectCreate(cTextShared);
				_ahEffects[cTextShared] = new EffectCover(cTextBTL);
				cEffectCover = _ahEffects[cTextShared];
			}
			cEffectCover.sType = "Text";
			cEffectCover.sInfo = (string)aArgs[0];

			cTextBTL.stColor = Color.FromArgb((int)(byte)aArgs[19], (int)(byte)aArgs[4], (int)(byte)aArgs[5], (int)(byte)aArgs[6]);
			cTextBTL.stColorBorder = Color.FromArgb((int)(byte)aArgs[20], (int)(byte)aArgs[8], (int)(byte)aArgs[9], (int)(byte)aArgs[10]);
			cTextBTL.cDock = new helpers.Dock((helpers.Dock.Corner)aArgs[21], (short)aArgs[11], (short)aArgs[12]);
			cTextBTL.nLayer = aArgs[13].ToUShort();
            cTextBTL.nDelay = aArgs[14].ToULong();
			cTextBTL.nDuration = aArgs[15].ToULong();
            cTextBTL.bOpacity = (bool)aArgs[16];
			cTextBTL.nInDissolve = (byte)aArgs[17];
			cTextBTL.nOutDissolve = (byte)aArgs[18];
			cTextBTL.nMaxOpacity = (byte)aArgs[19];
        }
		static void ClockCreate(shared.Clock cClockShared, object[] aArgs) //EMERGENCY раскрыть массив в нормальные параметры!!!!
        {
			BTL.Play.Clock cClockBTL = new BTL.Play.Clock((string)aArgs[0], new Font((string)aArgs[1], (int)aArgs[2], (FontStyle)aArgs[3]), (float)aArgs[7]);
			EffectCover cEffectCover = null;
			lock (_ahEffects)
			{
				EffectCreate(cClockShared);
				_ahEffects[cClockShared] = new EffectCover(cClockBTL);
				cEffectCover = _ahEffects[cClockShared];
			}
			cEffectCover.sType = "Clock";
			cEffectCover.sInfo = (string)aArgs[0];

			cClockBTL.stColor = Color.FromArgb((int)(byte)aArgs[18], (int)(byte)aArgs[4], (int)(byte)aArgs[5], (int)(byte)aArgs[6]);
			cClockBTL.stColorBorder = Color.FromArgb((int)(byte)aArgs[19], (int)(byte)aArgs[8], (int)(byte)aArgs[9], (int)(byte)aArgs[10]);
			cClockBTL.cDock = new helpers.Dock((helpers.Dock.Corner)aArgs[20], (short)aArgs[11], (short)aArgs[12]);
			cClockBTL.nLayer = aArgs[13].ToUShort();
			cClockBTL.nDelay = aArgs[14].ToULong();
			cClockBTL.nDuration = aArgs[15].ToULong();
			cClockBTL.sSuffix = (string)aArgs[16];
			cClockBTL.bOpacity = (bool)aArgs[17];
        }
        static void PluginCreate(shared.Plugin cPlugin, string sFile, string sClass, string sData)
        {
			Plugin cPluginBTL = new Plugin(sFile, sClass, sData);
			EffectCover cEffectCover = null;
			lock (_ahEffects)
			{
				EffectCreate(cPlugin);
				_ahEffects[cPlugin] = new EffectCover(cPluginBTL);
				cEffectCover = _ahEffects[cPlugin];
			}
			cEffectCover.sType = "Plugin";
			cEffectCover.sInfo = sFile;
        }
		#endregion
        public static void OnContainerEvent(shared.ContainerEventType eEventType, EffectCover cContainer, EffectCover cEffect)
		{
			Logger cLogger = new Logger();
			cLogger.WriteDebug3("in [" + eEventType.ToString() + "]");
			try
			{
				if (null != cContainer)
				{
					if (null != cEffect)
					{
						shared.Container cContainerShared = null;
						shared.Effect cEffectShared = null;
						cLogger.WriteDebug4("container:event:" + eEventType.ToString() + ":lock:before [container ec hc:" + cContainer.GetHashCode() + " info: " + cContainer.sInfo + "][effect ec hc:" + cEffect.GetHashCode() + "]");
						lock (_ahEffects)
						{
							cContainerShared = (shared.Container)_ahEffects.FirstOrDefault(row => row.Value == cContainer).Key;
							cEffectShared = (shared.Effect)_ahEffects.FirstOrDefault(row => row.Value == cEffect).Key;
						}
						cLogger.WriteDebug4("container:event:" + eEventType.ToString() + ":lock:after [container ec hc:" + cContainer.GetHashCode() + "][effect ec hc:" + cEffect.GetHashCode() + "]");
						if (null != cContainerShared)
						{
							if (null != cEffectShared)
							{
								cLogger.WriteDebug4("container:event:" + eEventType.ToString() + ":raise:before [container ec hc:" + cContainer.GetHashCode() + "][effect ec hc:" + cEffect.GetHashCode() + "][container shared hc:" + cContainerShared.GetHashCode() + "][effect shared hc:" + cEffectShared.GetHashCode() + "]");
								cContainerShared.OnContainerEventRaised(eEventType, cEffectShared);
								cLogger.WriteDebug4("container:event:" + eEventType.ToString() + ":raise:after [container ec hc:" + cContainer.GetHashCode() + "][effect ec hc:" + cEffect.GetHashCode() + "][container shared hc:" + cContainerShared.GetHashCode() + "][effect shared hc:" + cEffectShared.GetHashCode() + "]");
							}
							else
								throw new Exception("container:event:" + eEventType.ToString() + ": указанный экземпляр эффекта не зарегистрирован на сервере [container ec hc:" + cContainer.GetHashCode() + "][effect ec hc:" + cEffect.GetHashCode() + "]");
						}
						else
							throw new Exception("container:event:" + eEventType.ToString() + ": указанный экземпляр контейнера не зарегистрирован на сервере [container ec hc:" + cContainer.GetHashCode() + "][effect ec hc:" + cEffect.GetHashCode() + "]");
					}
					else
						throw new Exception("container:event:" + eEventType.ToString() + ": экземпляр эффекта не может быть null");
				}
				else
					throw new Exception("container:event:" + eEventType.ToString() + ": экземпляр контейнера не может быть null");
			}
			catch (Exception ex)
			{
				cLogger.WriteError(ex);
			}
			cLogger.WriteDebug4("return [" + eEventType.ToString() + "]");
		}
		#region playlist

		static void PlaylistCreate(shared.Playlist cPlaylistShared, helpers.Dock cDock, ushort nZ, bool bStopOnEmpty, bool bOpacity, ulong nDelay)
        {
			(new Logger()).WriteDebug3("in [hc:" + cPlaylistShared.GetHashCode() + "]");
			EffectCreate(cPlaylistShared);
            try
            {
				BTL.Play.Playlist cPlaylist = new BTL.Play.Playlist();
				EffectCover cEffectCover = null;
				lock (_ahEffects)
				{
					_ahEffects[cPlaylistShared] = new EffectCover(cPlaylist);
					cEffectCover = _ahEffects[cPlaylistShared];
				}
				cEffectCover.sType = "Playlist";
				cEffectCover.sInfo = "x=" + cDock.cOffset.nLeft + ", y=" + cDock.cOffset.nTop;

				cPlaylist.cDock = cDock;
				cPlaylist.nLayer = nZ;
				cPlaylist.bStopOnEmpty = bStopOnEmpty;
				cPlaylist.bOpacity = bOpacity;
				cPlaylist.nDelay = nDelay;
				cPlaylist.oTag = cPlaylistShared;  //EMERGENCY это что за хрень???????? Tag не использовать нигде!!!! кроме пользовательского фронтенда!!!!!
				cPlaylist.OnPlaylistIsEmpty += new BTL.Play.Playlist.PlaylistIsEmpty(OnPlaylistIsEmpty);
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
			(new Logger()).WriteDebug4("return [hc:" + cPlaylistShared.GetHashCode() + "]");
		}

		static void OnPlaylistIsEmpty(BTL.Play.Playlist cPlaylist)
        {// Плейлист сам стопится, если он bStopOnEmpty....
            //KeyValuePair<shared.Effect, EffectCover> ahCurrent;
            //try
            //{
            //    lock (_ahEffects)
            //        ahCurrent = _ahEffects.First(o => o.Value.oEffect is BTL.Play.Effect && ((BTL.Play.Effect)o.Value.oEffect) == cEffect);
            //    ahCurrent.Value.Stop();
            //}
            //catch (Exception ex)
            //{
            //    (new Logger()).WriteError(ex);
            //}
		}

		static void PlaylistAddEffect(shared.Playlist cPL, shared.Effect cEffect, ushort nTransDur)
		{
			EffectCover cEffectCover = null;
			EffectCover cEffectToAdd = null;
			try
			{
				lock (_ahEffects)
				{
					if (_ahEffects.ContainsKey(cPL))
						cEffectCover = _ahEffects[cPL];
					else
						throw new Exception("playlist:add:effect: указанный объект плейлиста не зарегистрирован на сервере [hcPL:" + cPL.GetHashCode() + "]");
					if (_ahEffects.ContainsKey(cEffect))
						cEffectToAdd = _ahEffects[cEffect];
					else
						throw new Exception("playlist:add:effect: добавляемый эффект не зарегистрирован на сервере  [hcPL:" + cPL.GetHashCode() + "] [hceff:" + cEffect.GetHashCode() + "]");
				}
				((BTL.Play.Playlist)cEffectCover.oEffect).EffectAdd((BTL.Play.Effect)cEffectToAdd.oEffect, nTransDur);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
        }
		static void PlaylistSkip(shared.Playlist cPL, bool bLast, ushort nNewTransDur, shared.Effect cEffect)
        {
			try
			{
				(new Logger()).WriteDebug("playlist:skip: [e:" + (null == cEffect ? "null" : cEffect.nDuration.ToString()) + "]");
				EffectCover cEffectCover = null;
				BTL.Play.Effect cEffectToSkip = null;
				lock (_ahEffects)
				{
					if (_ahEffects.ContainsKey(cPL))
						cEffectCover = _ahEffects[cPL];
					else
						throw new Exception("playlist:skip: отсутствует указанный объект плейлиста [hc:" + cPL.GetHashCode() + "]");
					if (null != cEffect)
					{
						if (_ahEffects.ContainsKey(cEffect))
							cEffectToSkip = (BTL.Play.Effect)_ahEffects[cEffect].oEffect;
						else
							throw new Exception("playlist:skip:effect: пропускаемый эффект не зарегистрирован на сервере  [hcPL:" + cPL.GetHashCode() + "] [hceff:" + cEffect.GetHashCode() + "]");
					}
				}
				((BTL.Play.Playlist)cEffectCover.oEffect).Skip(bLast, nNewTransDur, cEffectToSkip);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
        static void Playlist_OnEndTransDurationSet(shared.Playlist cPL, ushort nEndTransDuration)
        {
            try
            {
				EffectCover cEffectCover = null;
				lock (_ahEffects)
				{
					if (_ahEffects.ContainsKey(cPL))
						cEffectCover = _ahEffects[cPL];
					else
						throw new Exception("playlist:EndTransDurationSet: отсутствует указанный объект плейлиста [hc:" + cPL.GetHashCode() + "]");
				}
				((BTL.Play.Playlist)cEffectCover.oEffect).EndTransDurationSet(nEndTransDuration);
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
        }
        static void Playlist_OnPLItemsDelete(shared.Playlist cPL, shared.Effect[] aEffects)
        {
            try
            {
				EffectCover cEffectCover = null;
				lock (_ahEffects)
				{
					if (_ahEffects.ContainsKey(cPL))
						cEffectCover = _ahEffects[cPL];
					else
						throw new Exception("playlist:items_delete: отсутствует указанный объект плейлиста [hc:" + cPL.GetHashCode() + "]");
				}
				int[] aIDs = _ahEffects.Where(o => aEffects.Contains(o.Key)).Select(o => o.Value.oEffect.GetHashCode()).ToArray();
				((BTL.Play.Playlist)cEffectCover.oEffect).PLItemsDelete(aIDs);
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
        }
        #endregion
        #region roll

		static void RollCreate(shared.Effect cSender)
        {
			(new Logger()).WriteDebug3("in [hc:" + cSender.GetHashCode() + "]");
			EffectCreate(cSender);
            try
            {
				BTL.Play.Roll cRoll = new BTL.Play.Roll();
				EffectCover cEffectCover = null;
				lock (_ahEffects)
				{
					_ahEffects[cSender] = new EffectCover(cRoll);
					cEffectCover = _ahEffects[cSender];
				}
				cEffectCover.sType = "Roll";
				cEffectCover.sInfo = "x=" + cRoll.stArea.nLeft + ", y=" + cRoll.stArea.nTop;

				//cRoll.cDock = cDock;
				//cRoll.nLayer = nZ;
				//cRoll.bOpacity = bOpacity;
				//cRoll.nDelay = nDelay;
				//cRoll.cTag = cSender; //EMERGENCY это что за хрень???????? Tag не использовать нигде!!!! кроме пользовательского фронтенда!!!!!
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
			(new Logger()).WriteDebug4("return [hc:" + cSender.GetHashCode() + "]");
		}

		static int Roll_OnEffectsQtyGet(shared.Roll cSender)
		{
			int nRetVal = -1;
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if(null == cEffectCover)
					throw new Exception("roll:effectsqty:get: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				nRetVal = ((BTL.Play.Roll)cEffectCover.oEffect).nEffectsQty;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return nRetVal;
		}
		static shared.Roll.Direction Roll_OnDirectionGet(shared.Roll cSender)
		{
			shared.Roll.Direction eRetVal = shared.Roll.Direction.DownToUp;
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("roll:direction:get: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				eRetVal = (shared.Roll.Direction) Enum.Parse(typeof(shared.Roll.Direction), ((BTL.Play.Roll)cEffectCover.oEffect).eDirection.ToString(), true);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return eRetVal;
		}
		static void Roll_OnDirectionSet(shared.Roll cSender, shared.Roll.Direction eDirection)
		{
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("roll:direction:set: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				((BTL.Play.Roll)cEffectCover.oEffect).eDirection = (BTL.Play.Roll.Direction)Enum.Parse(typeof(BTL.Play.Roll.Direction), eDirection.ToString(), true);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		static float Roll_OnSpeedGet(shared.Roll cSender)
		{
			float nRetVal = short.MaxValue;
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("roll:speed:get: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				nRetVal = ((BTL.Play.Roll)cEffectCover.oEffect).nSpeed;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return nRetVal;
		}
		static void Roll_OnSpeedSet(shared.Roll cSender, float nSpeed)
		{
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("roll:speed:set: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				((BTL.Play.Roll)cEffectCover.oEffect).nSpeed = nSpeed;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		static bool Roll_OnStopOnEmptyGet(shared.Roll cSender)
		{
			bool bRetVal = false;
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("roll:StopOnEmpty:get: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				bRetVal = ((BTL.Play.Roll)cEffectCover.oEffect).bStopOnEmpty;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return bRetVal;
		}
		static void Roll_OnStopOnEmptySet(shared.Roll cSender, bool bStopOnEmpty)
		{
			try
			{
				EffectCover cEffectCover = EffectCoverGet(cSender);
				if (null == cEffectCover)
					throw new Exception("roll:StopOnEmpty:set: указанный объект не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				((BTL.Play.Roll)cEffectCover.oEffect).bStopOnEmpty = bStopOnEmpty;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		static void Roll_OnEffectAdd(shared.Roll cSender, shared.Effect cEffect, float nSpeed)
		{
			try
			{
				EffectCover cEffectCoverSender = EffectCoverGet(cSender);
				if (null == cEffectCoverSender)
					throw new Exception("roll:effect:add: указанный объект [roll] не зарегистрирован на сервере [hc:" + cSender.GetHashCode() + "]");
				EffectCover cEffectCover = EffectCoverGet(cEffect);
				if (null == cEffectCover)
					throw new Exception("roll:effect:add: указанный объект [effect] не зарегистрирован на сервере [hc:" + cEffect.GetHashCode() + "]");
				((BTL.Play.Roll)cEffectCoverSender.oEffect).EffectAdd((BTL.IVideo)cEffectCover.oEffect, nSpeed);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		#endregion

		static private shared.Status StatusTranslate(BTL.EffectStatus eStatus)
		{
			switch (eStatus)
			{
				case BTL.EffectStatus.Error:
					return shared.Status.Error;
				case BTL.EffectStatus.Idle:
					return shared.Status.Idle;
				case BTL.EffectStatus.Preparing:
					return shared.Status.Prepared;
				case BTL.EffectStatus.Running:
					return shared.Status.Started;
				case BTL.EffectStatus.Stopped:
					return shared.Status.Stopped;
			}
			return shared.Status.Unknown;
		}
    }
}
