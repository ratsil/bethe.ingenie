#define chatdebug
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Activation;
using System.Threading;
using System.Web.Services;
using System.Drawing;
using helpers;
using helpers.extensions;
//using helpers.replica;
using ingenie.web.lib;
using IW = ingenie.web;
using ingenie.userspace;

namespace ingenie.web.services
{
	/// <summary>
	/// Summary description for ingenie
	/// </summary>
	[WebService(Namespace = "http://replica/ig/services/Player.asmx")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	// To allow this web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
	// [System.web.Script.Services.ScriptService]
	public class Player : Common
	{
		private class Logger : lib.Logger
		{
			public Logger()
				: base("player")
			{
			}
		}
		//public class ServerSideCues :  replica.Cues.IInteract
		//{
		//    static public List<helpers.replica.pl.PlaylistItem> _aPlaylist;
		//    static public object _cSyncRoot;
		//    static private Dictionary<helpers.replica.pl.Class, helpers.replica.cues.TemplateBind[]> _ahClasses;
		//    static public ServerSideCues()
		//    {
		//        _cSyncRoot = new object();
		//    }
		//    static public void Init()
		//    {
		//        _aPlaylist = new List<helpers.replica.pl.PlaylistItem>();
		//        _ahClasses = new Dictionary<helpers.replica.pl.Class, helpers.replica.cues.TemplateBind[]>();
				
		//    }
		//    static public helpers.replica.pl.PlaylistItem PlaylistItemOnAirGet()
		//    {
		//        lock (_cSyncRoot)
		//            return null == _aPlaylist || 1 > _aPlaylist.Count ? null : _aPlaylist[0];
		//    }
		//    static public Queue<helpers.replica.pl.PlaylistItem> PlaylistItemsPreparedGet()
		//    {
		//        Queue<helpers.replica.pl.PlaylistItem> aqRetVal = new Queue<helpers.replica.pl.PlaylistItem>();
		//        lock (_cSyncRoot)
		//            if (null == _aPlaylist || 1 >= _aPlaylist.Count)
		//                return null;
		//            else
		//            {
		//                aqRetVal.Enqueue(_aPlaylist[1]);
		//                return aqRetVal;
		//            }
		//    }
		//    static public helpers.replica.cues.TemplateBind[] TemplateBindsGet(helpers.replica.pl.PlaylistItem cPLI)
		//    {
		//        lock (_cSyncRoot)
		//            return (_ahClasses.ContainsKey(cPLI.cClass) ? _ahClasses[cPLI.cClass] : null);
		//    }
		//    #region реализация Cues.IInteract
		//    replica.Cues.IInteract replica.Cues.IInteract.Init()
		//    {
		//        return _cSSCues;
		//    }
		//    helpers.replica.pl.PlaylistItem replica.Cues.IInteract.PlaylistItemOnAirGet() { return PlaylistItemOnAirGet(); }
		//    Queue<helpers.replica.pl.PlaylistItem> replica.Cues.IInteract.PlaylistItemsPreparedGet() { return PlaylistItemsPreparedGet(); }
		//    helpers.replica.cues.TemplateBind[] replica.Cues.IInteract.TemplateBindsGet(helpers.replica.pl.PlaylistItem cPLI) { return TemplateBindsGet(cPLI); }
		//    #endregion реализация Cues.IInteract
		//}
		public class Playlist : Item
		{
			private userspace.Playlist _cPlaylist;
			private List<PlaylistItem> _aPlaylistItems;
			private System.Timers.Timer tClipStop;
			private string sClipStop;
			private IW.Preferences.Clients.SCR.Template cPL_PrefTemplate;
			public PlaylistItem[] aPlaylistItems
			{
				get
				{
					lock (_aPlaylistItems)
						return _aPlaylistItems.ToArray();
				}
			}
			public DateTime dtPlaylistStopPlanned
			{
				get
				{
					DateTime dtRetVal = DateTime.MinValue;
					if (eStatus == Status.Started)
					{
						foreach (PlaylistItem cPLI in _aPlaylistItems)
							if (cPLI.dtStartReal > DateTime.MinValue)
								dtRetVal = cPLI.dtStartReal.AddMilliseconds(cPLI._nFramesQty * 40);
							else
								dtRetVal = dtRetVal.AddMilliseconds(cPLI._nFramesQty * 40);
					}
					return dtRetVal; //.AddSeconds(2);
				}
			}
			override public Status eStatus
			{
				get
				{
					if (base.eStatus != Status.Error && base.eStatus != Status.Stopped)
						if (null != _cPlaylist)
							base.eStatus = (Status)_cPlaylist.eStatus;
					return base.eStatus;
				}
				set
				{
					base.eStatus = value;
				}
			}

			public Playlist() {}
			public Playlist(ushort nLayer, bool bStopOnEmpty)
			{
				_cPlaylist = new userspace.Playlist();

				if (null != (cPL_PrefTemplate = IW.Preferences.cClientReplica.aTemplates.FirstOrDefault(o => o.sFile == "Player Playlist")) && cPL_PrefTemplate.stScaleVideo != Area.stEmpty)
				{
					_cPlaylist.stArea = new Area(cPL_PrefTemplate.stScaleVideo.nLeft, cPL_PrefTemplate.stScaleVideo.nTop, 0, 0);
				}
				else
					cPL_PrefTemplate = null;

				_aPlaylistItems = new List<PlaylistItem>();
				_cPlaylist.EffectStarted += PlaylistItemStarted;
				_cPlaylist.EffectPrepared += PlaylistItemPrepared;
				_cPlaylist.EffectStopped += PlaylistItemStopped;
				_cPlaylist.bStopOnEmpty = bStopOnEmpty;
				_cPlaylist.nLayer = nLayer;
				_cPlaylist.aChannels = null;
				tClipStop = new System.Timers.Timer();
				tClipStop.Elapsed += new System.Timers.ElapsedEventHandler(tClipStop_Elapsed);
				//ServerSideCues.Init();
			}
			override public void Prepare()
			{
				_cPlaylist.Prepare();
				Player.dtPlaylistStopPlanned = DateTime.MinValue;
			}
			override public void Start()
			{
				_cPlaylist.Start();
			}
			override public void Stop()
			{
				_cPlaylist.Stop();
				Player.dtPlaylistStopPlanned = DateTime.MinValue;
			}
			

			void PlaylistItemPrepared(Container cSender, Effect cEffect)
			{
				PlaylistItem cPLI = null;
				lock (_aPlaylistItems)
					if (null != (cPLI = _aPlaylistItems.FirstOrDefault(o => o.nAtomHashCode == cEffect.GetHashCode())))
						(new Logger()).WriteNotice("effect prepared: [" + cPLI.sFilename + "]");
			}
			void PlaylistItemStarted(Container cSender, Effect cEffect)
			{
				PlaylistItem cPLI = null;
				lock (_aPlaylistItems)
				{
					if (null != (cPLI = _aPlaylistItems.FirstOrDefault(o => o.nAtomHashCode == cEffect.GetHashCode())))
						(new Logger()).WriteNotice("effect started: [" + cPLI.sFilename + "]");
					if (0 < _aPlaylistItems.Count)
					{
						PlaylistItem cPLIPrevious = null;
						foreach (PlaylistItem cPlaylistItem in _aPlaylistItems)
						{
							if (cPlaylistItem.nAtomHashCode == cEffect.GetHashCode())
							{
								DateTime dtNow = DateTime.Now;
								cPlaylistItem.dtStartReal = dtNow.AddSeconds(IW.Preferences.nQueuesCompensation);
								Player.dtPlaylistStopPlanned = dtPlaylistStopPlanned;
								
								if (null != cPLIPrevious)
									cPLIPrevious.dtStopReal = cPlaylistItem.dtStartReal;
								if (cPlaylistItem._eType == PlaylistItem.PLIType.Clip && null != cPlaylistItem._cClipSCR)
								{
									sCurrentClass = cPlaylistItem._cClipSCR.sClassName;
									tClipStop.Stop();
									AutomationWrite(cPlaylistItem._cClipSCR.nID + ", clip, started");
									sClipStop = cPlaylistItem._cClipSCR.nID + ", clip, stopped";
									tClipStop.Interval = (cPlaylistItem._cClipSCR.nFramesQty - IW.Preferences.nStopOffset) * IW.Preferences.nFrameDuration_ms;
									tClipStop.Start();
								}
								else if (cPlaylistItem._eType == PlaylistItem.PLIType.AdvBlockItem && null != cPlaylistItem._cAdvertSCR)
									sCurrentClass = cPlaylistItem._cAdvertSCR.sClassName;
								return;
							}
							cPLIPrevious = cPlaylistItem;
						}
					}
				}
				throw (new Exception("Template.cs:cPL_PLIPrepared(Effect cEffect):Получен сигнал о старте элемента, которого нет в плейлисте."));    //TODO LANG
			}
			void PlaylistItemStopped(Container cSender, Effect cEffect)
			{
				PlaylistItem cPLI = null;
				string sMessage = null;
				DateTime dtDuration;
				lock (_aPlaylistItems)
				{
					if (null != (cPLI = _aPlaylistItems.FirstOrDefault(o => o.nAtomHashCode == cEffect.GetHashCode())))
						(new Logger()).WriteNotice("effect stopped: [" + cPLI.sFilename + "]");
					if (0 < _aPlaylistItems.Count)
					{
						int ni = 0;
						foreach (PlaylistItem cPlaylistItem in _aPlaylistItems)
						{
							if (cPlaylistItem.nAtomHashCode == cEffect.GetHashCode())
							{
								if (_aPlaylistItems.Count - 1 == _aPlaylistItems.IndexOf(cPlaylistItem))
								{
									sCurrentClass = null;
									//Player.dtPlaylistStopPlanned = DateTime.MinValue;
								}
								if (cPlaylistItem.dtStopReal == DateTime.MinValue)
								{
									cPlaylistItem.dtStopReal = DateTime.Now.AddSeconds(IW.Preferences.nQueuesCompensation);
									for (int nj = ni + 1; _aPlaylistItems.Count > nj; nj++)
										if (nj == ni + 1)
											_aPlaylistItems[nj].dtStartReal = cPlaylistItem.dtStopReal;
										//else
											//_aPlaylistItems[nj].dtStartReal = _aPlaylistItems[nj - 1].dtStartReal + new TimeSpan(0, 0, 0, (int)(_aPlaylistItems[nj - 1]._nFramesQty * 40)); //FPS
								}
								if (cPlaylistItem._eType == PlaylistItem.PLIType.Clip && null != cPlaylistItem._cClipSCR)
								{
									AutomationWrite(cPlaylistItem._cClipSCR.nID + ", clip, stopped");
									dtDuration = cPlaylistItem.dtStopReal.AddTicks(-cPlaylistItem.dtStartReal.Ticks);
									sMessage = cPlaylistItem.dtStartReal + "\t" + cPlaylistItem._cClipSCR.sArtist + "\t" + cPlaylistItem._cClipSCR.sSong + "\t" + cPlaylistItem._nFramesQty.ToFramesString(false, false, false, false, true) + "\t" + dtDuration.ToString("HH:mm:ss");      // yyyy.MM.dd hh:mm:ss
									lock (_cSyncRoot)
										sMessage += "\t" + sPreset;
								}
								if (cPlaylistItem._eType == PlaylistItem.PLIType.AdvBlockItem && null != cPlaylistItem._cAdvertSCR)
									_aAdvertsStoppedPLIsIDs.Add(cPlaylistItem._cAdvertSCR.nPlaylistID);
								if (null == sMessage)
								{
									dtDuration = cPlaylistItem.dtStopReal.AddTicks(-cPlaylistItem.dtStartReal.Ticks);
									sMessage = cPlaylistItem.dtStartReal + "\t\t\t\t\t" + cPlaylistItem.sName + "\t" + cPlaylistItem._nFramesQty.ToFramesString(false, false, false, false, true) + "\t" + dtDuration.ToString("HH:mm:ss");
								}
								break;
							}
							ni++;
						}
					}
				}
				if (null != sMessage)
					LogWrite(sMessage);
				else
					throw (new Exception("Template.cs:cPL_PLIStopped(Effect cEffect):Получен сигнал об остановке элемента, которого нет в плейлисте."));  //TODO LANG
			}
			private void tClipStop_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
			{
				tClipStop.Stop();
				AutomationWrite(sClipStop);
			}
			public void AddVideo(PlaylistItem cItem)
			{
				Video cVideo = new Video();
				cVideo.stArea = new Area(0, 0, cPL_PrefTemplate.stScaleVideo.nWidth, cPL_PrefTemplate.stScaleVideo.nHeight);
				cVideo.sFile = cItem.sFilenameFull;

				cItem.nAtomHashCode = cVideo.GetHashCode();
				_cPlaylist.EffectAdd(cVideo, cItem.nTransDuration);
				_cPlaylist.EndTransDurationSet(cItem.nEndTransDuration);
				lock (_aPlaylistItems)
					_aPlaylistItems.Add(cItem);
				cItem.nEffectID = cVideo.GetHashCode();
				Player.dtPlaylistStopPlanned = dtPlaylistStopPlanned;
			}
			public void AddAnimation(PlaylistItem cItem)
			{
				Animation cAnimation = new Animation();
				cAnimation.bKeepAlive = true;
				cAnimation.nLoopsQty = (ushort)cItem._nFramesQty;
				cAnimation.sFolder = cItem.sFilenameFull;
				cAnimation.stArea = new Area(0, 0, cPL_PrefTemplate.stScaleVideo.nWidth, cPL_PrefTemplate.stScaleVideo.nHeight);
				cAnimation.nPixelAspectRatio = cPL_PrefTemplate.nPixelAspectRatio;

				cItem.nAtomHashCode = cAnimation.GetHashCode();
				_cPlaylist.EffectAdd(cAnimation, cItem.nTransDuration);
				_cPlaylist.EndTransDurationSet(cItem.nEndTransDuration);
				lock (_aPlaylistItems)
					_aPlaylistItems.Add(cItem);
				cItem.nEffectID = cAnimation.GetHashCode();
				Player.dtPlaylistStopPlanned = dtPlaylistStopPlanned;
			}
			public void Skip(bool bSkipLastEffect, ushort nSkipTransitionDuration)
			{
				_cPlaylist.bSkipLastEffect = bSkipLastEffect;
				_cPlaylist.nSkipTransitionDuration = nSkipTransitionDuration;
				_cPlaylist.Skip();
				Player.dtPlaylistStopPlanned = dtPlaylistStopPlanned;
			}
			public void EndTransitionDurationSet(ushort nEndTransitionDuration)
			{
				_cPlaylist.EndTransDurationSet(nEndTransitionDuration);
			}
			public void PlaylistItemsDelete(PlaylistItem[] aItems)
			{
				List<int> aEffectIDs = new List<int>();
				List<PlaylistItem> aToDel = new List<PlaylistItem>();
				PlaylistItem cPLI2del = null;
				foreach (PlaylistItem cPLI in aItems)
				{
					lock (_aPlaylistItems)
						if (null == _aPlaylistItems.FirstOrDefault(o => o.nID == cPLI.nID))
							continue;
					if (null != cPLI)
					{
						aEffectIDs.Add(cPLI.nEffectID);
						cPLI2del = _aPlaylistItems.FirstOrDefault(o => o.nID == cPLI.nID);
						if (null != cPLI2del)
							aToDel.Add(cPLI2del);
					}
				}
				_cPlaylist.PLItemsDelete(aEffectIDs);
				lock (_aPlaylistItems)
				{
					foreach (PlaylistItem cPLI in aToDel)
						_aPlaylistItems.Remove(cPLI);
				}
			}
		}
		static void LogWrite(string sMessage)
		{
			string sFileName = IW.Preferences.sPlayerLogFolder + DateTime.Now.ToString("yyyy_MM") + ".txt";
			if (!System.IO.Directory.Exists(IW.Preferences.sPlayerLogFolder))
				System.IO.Directory.CreateDirectory(IW.Preferences.sPlayerLogFolder);
			if (!System.IO.File.Exists(sFileName))
				System.IO.File.Create(sFileName);
			try
			{
				System.IO.File.AppendAllText(sFileName, sMessage + Environment.NewLine);
			}
			catch { }
		}
		static void AutomationWrite(string sMessage)
		{
			string sFileName = IW.Preferences.sClipStartStopAutomationFile;
			if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(sFileName)))
				System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(sFileName));
			try
			{
				System.IO.File.WriteAllText(sFileName, "id, type, status" + Environment.NewLine + sMessage);
				(new Logger()).WriteNotice("AutomationWrite: [" + sMessage + "]");
			}
			catch(Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		static Dictionary<long, string> _ahStoragesSCR = new Dictionary<long, string>();
		static public List<long> _aAdvertsStoppedPLIsIDs = new List<long>();
		static public DateTime dtPlaylistStopPlanned = DateTime.MinValue;

		public Player()
		{
		}

		[WebMethod(EnableSession = true)]
		public void StoragesSet(IdNamePair[] aStorages)
		{
			if (!aStorages.IsNullOrEmpty())
			{
				_ahStoragesSCR.Clear();
				string sLog="";
				foreach (IdNamePair cINP in aStorages)
				{
					_ahStoragesSCR.Add(cINP.nID, cINP.sName);
					sLog += "[id:" + cINP.nID + "][name:" + cINP.sName + "]";
				}
				(new Logger()).WriteWarning("player_storages: " + sLog);
			}
			else
				(new Logger()).WriteWarning("player_storages: Is NULL or EMPTY");
		}
		[WebMethod(EnableSession = true)]
		public void ShiftStart(long nID, string sSubject, long nPresetID, string sPresetName)
		{
			LogWrite(DateTime.Now + "\t********** запущен эфир [shift:" + nID + ":" + sSubject + "][preset:" + nPresetID + ":" + sPresetName + "]");
		}
		[WebMethod(EnableSession = true)]
		public void ShiftStop()
		{
			LogWrite(DateTime.Now + "\t********** эфир остановлен");
		}
		[WebMethod(EnableSession = true)]
		[System.Xml.Serialization.XmlInclude(typeof(Playlist))]
		public Item ItemCreate(string sPreset, PlaylistItem[] aItems, ushort nLayer, bool bStopOnEmpty)
		{
			Item cRetVal = null;
			try
			{
				if (0 < Client.nID)
				{
					string sInfo = "Player Playlist";
					if (null != (cRetVal = (Item)GarbageCollector.ItemGet(sInfo)) && cRetVal.eStatus != Item.Status.Stopped && cRetVal.eStatus != Item.Status.Error)
						return cRetVal;
					cRetVal = new Playlist(nLayer, bStopOnEmpty);
					cRetVal.sPreset = sPreset;
					cRetVal.sInfo = sInfo;
					GarbageCollector.ItemAdd(cRetVal);
					if (null == AddVideo(cRetVal, aItems))
					{
						(new Logger()).WriteWarning("player_create: deleting" + sInfo);
						GarbageCollector.ItemDelete(cRetVal);
						cRetVal = null;
					}
				}
				else
					(new Logger()).WriteError("create: не инициирован механизм регистрации клиента");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return cRetVal;
		}

		[WebMethod(EnableSession = true)]
		public PlaylistItem[] AddVideo(Item cItem, PlaylistItem[] aItems)
		{
			bool bRetVal = false;
			try
			{
				if (0 < Client.nID)
				{
					Item cItemLocal = GarbageCollector.ItemGet(cItem);
					if (null != cItemLocal)
					{
						Playlist cPlaylist = (Playlist)cItemLocal;
						foreach (PlaylistItem cPLI in aItems)
							if (cPLI.bFileIsImage)
								cPlaylist.AddAnimation(cPLI);
							else
								cPlaylist.AddVideo(cPLI);
						bRetVal = true;
					}
					else
						(new Logger()).WriteError("add:video: указанный элемент не зарегистрирован [item:" + cItem.GetHashCode() + "]");
				}
				else
					(new Logger()).WriteError("add:video: не инициирован механизм регистрации клиента");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			if (bRetVal)
				return aItems;
			else
				return null;
		}
		//[WebMethod(EnableSession = true)]
		//public bool PlaylistAddAnimation(ulong nEffectID, string sFolder, ushort nLoopsQty, bool bKeepAlive)
		//{
		//    try
		//    {
		//        if (0 < nClientID && 0 < nEffectID && _ahTemplates.ContainsKey(nClientID) && _ahTemplates[nClientID].ContainsKey(nEffectID))
		//            _ahTemplates[nClientID][nEffectID].PlaylistAnimationAdd(sFolder, nLoopsQty, bKeepAlive);
		//    }
		//    catch (Exception ex)
		//    {
		//        _cLog.WriteError(ex);
		//    }
		//    return false;
		//}
		[WebMethod(EnableSession = true)]
		public bool Skip(Item cItem, bool bSkipLastEffect, ushort nTransitionDuration)  //TODO nTransitionDuration пока никуда не ведёт ))
		{
			bool bRetVal = false;
			try
			{
				if (0 < Client.nID)
				{
					Item cItemLocal = GarbageCollector.ItemGet(cItem);
					if (null != cItemLocal)
					{
						((Playlist)cItemLocal).Skip(bSkipLastEffect, nTransitionDuration);
						bRetVal = true;
					}
					else
						(new Logger()).WriteError("skip: указанный элемент не зарегистрирован [item:" + cItem.GetHashCode() + "]");
				}
				else
					(new Logger()).WriteError("skip: не инициирован механизм регистрации клиента");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return bRetVal;
		}
		[WebMethod(EnableSession = true)]
		public PlaylistItem[] PlaylistItemsGet(Item cItem)
		{
			PlaylistItem[] aRetVal = null;
			try
			{
				if (0 < Client.nID)
				{
					Item cItemLocal = GarbageCollector.ItemGet(cItem);
					if (null != cItemLocal)
						aRetVal = ((Playlist)cItemLocal).aPlaylistItems;
					else
						(new Logger()).WriteError("plis:get: указанный элемент не зарегистрирован [item:" + cItem.GetHashCode() + "]");
				}
				else
					(new Logger()).WriteError("plis:get: не инициирован механизм регистрации клиента");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return aRetVal;
		}
		[WebMethod(EnableSession = true)]
		public bool PlaylistItemDelete(Item cItem, PlaylistItem[] aItems)
		{
			bool bRetVal = false;
			try
			{
				if (0 < Client.nID)
				{
					Item cItemLocal = GarbageCollector.ItemGet(cItem);
					if (null != cItemLocal)
					{
						((Playlist)cItemLocal).PlaylistItemsDelete(aItems);
						bRetVal = true;
					}
					else
						(new Logger()).WriteError("skip: указанный элемент не зарегистрирован [item:" + cItem.GetHashCode() + "]");
				}
				else
					(new Logger()).WriteError("skip: не инициирован механизм регистрации клиента");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return bRetVal;
		}
	
		//public bool EffectPrepare(int nObjectID)
		//{
		//    try
		//    {
		//        if (0 < nClientID && 0 < nObjectID && _ahTemplates.ContainsKey(nClientID) && _ahTemplates[nClientID].ContainsKey(nObjectID))
		//        {
		//            _ahTemplates[nClientID][nObjectID].Prepare();
		//            return true;
		//        }
		//    }
		//    catch (Exception ex)
		//    {
		//        _cLog.WriteError(ex);
		//    }
		//    return false;
		//}

		[WebMethod(EnableSession = true)]
		public Clip[] ClipsSCRGet(Clip[] aClips)
		{
			List<Clip> aRetVal = new List<Clip>();
			DBInteract cDBI = new DBInteract();
			string sClipsPath = "NULL";
			(new Logger()).WriteDebug("ClipsSCRGet: [in_count:" + aClips.Length + "]");
			try
			{
				//List<Clip> aSource = cDBI.ClipsSCRGet();
				userspace.Helper cHelper = new userspace.Helper();
				sClipsPath = _ahStoragesSCR.Values.FirstOrDefault(o => o.Contains("clips"));
				if (sClipsPath != null)
					foreach (Clip cClip in aClips)
					{
						if (cHelper.FileExist(sClipsPath + cClip.sFilename))
						{
							cClip.sDuration = cClip.nFramesQty.ToFramesString();
							cClip.bLocked = cClip.sRotation == "Стоп" ? true : false; //PREFERENCES
							cClip.sStoragePath = sClipsPath; //PREFERENCES
							aRetVal.Add(cClip);
						}
					}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			(new Logger()).WriteDebug("ClipsSCRGet: [out_count:" + aRetVal.Count + "][path:" + sClipsPath + "]");
			return aRetVal.ToArray();
		}
		[WebMethod(EnableSession = true)]
		public Advertisement[] AdvertsSCRGet(Advertisement[] aAdverts)
		{
			List<Advertisement> aRetVal = new List<Advertisement>();
			try
			{
				userspace.Helper cHelper = new userspace.Helper();
				foreach (Advertisement cAdv in aAdverts)
				{
					if (cHelper.FileExist(cAdv.sStoragePath + cAdv.sFilename))
						cAdv.bFileExist = true;
					aRetVal.Add(cAdv);
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return aRetVal.ToArray();
		}
		[WebMethod(EnableSession = true)]
		public long[] AdvertsStoppedGet()
		{
			return _aAdvertsStoppedPLIsIDs.ToArray();
		}
		[WebMethod(EnableSession = true)]
		public string[] FilesSCRGet(string sFilesFolder, string[] aExtensions)
		{
			List<string> aFilenames = new List<string>();
			try
			{
				userspace.Helper cHelper = new userspace.Helper();
				aFilenames.AddRange(cHelper.FileNamesGet(sFilesFolder, aExtensions));
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return aFilenames.ToArray();
		}
		[WebMethod(EnableSession = true)]
		public int VideoFramesQtyGet(string sFilenameFull)
		{
			try
			{
				ffmpeg.net.File.Input cFile = new ffmpeg.net.File.Input(sFilenameFull);
				ulong nFQ = cFile.nFramesQty;
				cFile.Dispose();
				return (int)nFQ;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return -1;
		}

		[WebMethod(EnableSession = true)]
		public Item[] ItemsRunningGet()
		{
			return GarbageCollector.ItemsStartedGet().Where(row => row is Playlist).ToArray();
		}
	}
}
