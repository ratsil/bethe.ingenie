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
        static private Dictionary<long, Clip> _ahAllClipsAssetId_Clip = new Dictionary<long, Clip>();

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

        public class CopyJob
        {
            private class Logger : lib.Logger
            {
                public Logger()
                    : base("copying job")
                {
                }
            }
            static CopyJob()
            {
                System.Threading.ThreadPool.QueueUserWorkItem(Worker);
            }
            static private helpers.ThreadBufferQueue<CopyJob> _ahCopyJobs = new helpers.ThreadBufferQueue<CopyJob>(false, false);
            static private Dictionary<long, CopyJob> _ahQueuedClipsAssetId_Job = new Dictionary<long, CopyJob>();
            static private Dictionary<long, CopyJob> _ahQueuedPLIId_Job = new Dictionary<long, CopyJob>();
            static private Dictionary<long, CopyJob> _ahErrorClipsAssetId_Job = new Dictionary<long, CopyJob>();
            static private Dictionary<long, CopyJob> _ahErrorPLIId_Job = new Dictionary<long, CopyJob>();
            static private object _oLock = new object();
            static private CopyJob _cCurrentJob;

            static private CopyJob cCurrentJob
            {
                get
                {
                    userspace.Helper cHelper = new userspace.Helper();
                    lock (_oLock)
                    {
                        if (null != _cCurrentJob && !cHelper.CopyFileExtendedIsNull() && _cCurrentJob.bStarted)
                            _cCurrentJob.nPercentProgress = (byte)cHelper.CopyFileExtendedProgressPercentGet();
                        return _cCurrentJob;
                    }
                }
            }

            public string sSource;
            public string sTarget;
            public byte nPercentProgress;
            public string sPercentProgress
            {
                get
                {
                    if (!bStarted)
                        return "in queue";
                    if (bFinished)
                    {
                        if (bError)
                            return "err on " + nPercentProgress + "%";
                        return "ok 100%";
                    }
                    return "" + nPercentProgress + "%";
                }
            }
            public bool bFinished;
            public bool bStarted;
            public bool bError;
            public PlaylistItem cPLI;
            public System.Threading.ManualResetEvent mreCopyFinished;

            static private void JobDone(CopyJob cCJ)
            {
                if (null != cCJ && null!= cCJ.cPLI)
                {
                    lock (_oLock)
                    {
                        cCJ.bFinished = true;
                        if (null != cCJ.cPLI._cAdvertSCR)
                        {
                            if (_ahQueuedPLIId_Job.ContainsKey(cCJ.cPLI._cAdvertSCR.nPlaylistID))
                            {
                                _ahQueuedPLIId_Job.Remove(cCJ.cPLI._cAdvertSCR.nPlaylistID);
                                (new Logger()).WriteDebug2("JobDone: removed [pliid=" + cCJ.cPLI._cAdvertSCR.nPlaylistID + "][pli=" + cCJ.cPLI._cAdvertSCR.sFilename + "]");
                            }
                        }
                        else if (null != cCJ.cPLI._cClipSCR)
                        {
                            if (_ahQueuedClipsAssetId_Job.ContainsKey(cCJ.cPLI._cClipSCR.nID))
                            {
                                _ahQueuedClipsAssetId_Job.Remove(cCJ.cPLI._cClipSCR.nID);
                                (new Logger()).WriteDebug2("JobDone: removed [assetid=" + cCJ.cPLI._cClipSCR.nID + "][pli=" + cCJ.cPLI._cClipSCR.sFilename + "]");
                            }
                        }
                        if (cCJ.mreCopyFinished != null)
                            cCJ.mreCopyFinished.Set();
                    }
                }
                else
                    (new Logger()).WriteError("JobDone: [job=" + (cCJ == null ? "NULL" : "ok") + "][pli=" + (cCJ.cPLI == null ? "NULL" : "ok") + "]");
            }
            static private void JobAddToErrorList(CopyJob cCJ, Helper cHelper)
            {
                if (null != cCJ && null != cCJ.cPLI)
                {
                    lock (_oLock)
                    {
                        cCJ.nPercentProgress = cHelper.CopyFileExtendedIsNull() ? (byte)0 : (byte)cHelper.CopyFileExtendedProgressPercentGet();
                        cCJ.bError = true;
                        if (null != cCJ.cPLI._cAdvertSCR)
                        {
                            if (!_ahErrorPLIId_Job.ContainsKey(cCJ.cPLI._cAdvertSCR.nPlaylistID))
                            {
                                _ahErrorPLIId_Job.Add(cCJ.cPLI._cAdvertSCR.nPlaylistID, cCJ);
                                (new Logger()).WriteDebug2("JobToErrorList: added [pliid=" + cCJ.cPLI._cAdvertSCR.nPlaylistID + "][pli=" + cCJ.cPLI._cAdvertSCR.sFilename + "]");
                            }
                        }
                        else if (null != cCJ.cPLI._cClipSCR)
                        {
                            if (!_ahErrorClipsAssetId_Job.ContainsKey(cCJ.cPLI._cClipSCR.nID))
                            {
                                _ahErrorClipsAssetId_Job.Add(cCJ.cPLI._cClipSCR.nID, cCJ);
                                (new Logger()).WriteDebug2("JobToErrorList: added [assetid=" + cCJ.cPLI._cClipSCR.nID + "][pli=" + cCJ.cPLI._cClipSCR.sFilename + "]");
                            }
                        }
                        JobDone(cCJ);
                    }
                }
                else
                    (new Logger()).WriteError("JobToErrorList: [job=" + (cCJ == null ? "NULL" : "ok") + "][pli=" + (cCJ.cPLI == null ? "NULL" : "ok") + "]");
            }
            static private void JobRemoveFromErrorList(CopyJob cCJ)
            {
                if (null != cCJ && null != cCJ.cPLI)
                {
                    lock (_oLock)
                    {
                        if (null != cCJ.cPLI._cAdvertSCR)
                        {
                            if (_ahErrorPLIId_Job.ContainsKey(cCJ.cPLI._cAdvertSCR.nPlaylistID))
                            {
                                _ahErrorPLIId_Job.Remove(cCJ.cPLI._cAdvertSCR.nPlaylistID);
                                (new Logger()).WriteDebug2("JobRemoveFromErrorList: added [pliid=" + cCJ.cPLI._cAdvertSCR.nPlaylistID + "][pli=" + cCJ.cPLI._cAdvertSCR.sFilename + "]");
                            }
                        }
                        else if (null != cCJ.cPLI._cClipSCR)
                        {
                            if (_ahErrorClipsAssetId_Job.ContainsKey(cCJ.cPLI._cClipSCR.nID))
                            {
                                _ahErrorClipsAssetId_Job.Remove(cCJ.cPLI._cClipSCR.nID);
                                (new Logger()).WriteDebug2("JobRemoveFromErrorList: added [assetid=" + cCJ.cPLI._cClipSCR.nID + "][pli=" + cCJ.cPLI._cClipSCR.sFilename + "]");
                            }
                        }
                        JobDone(cCJ);
                    }
                }
                else
                    (new Logger()).WriteError("JobRemoveFromErrorList: [job=" + (cCJ == null ? "NULL" : "ok") + "][pli=" + (cCJ.cPLI == null ? "NULL" : "ok") + "]");
            }
            static public void JobToQueue(CopyJob cCJ, bool bFirst)
            {
                if (null != cCJ && null != cCJ.cPLI)
                {
                    lock (_oLock)
                    {
                        //JobRemoveFromErrorList(cCJ);  // т.к. работа всегда новая!

                        if (bFirst)
                            _ahCopyJobs.EnqueueFirst(cCJ);
                        else
                            _ahCopyJobs.Enqueue(cCJ);

                        if (null != cCJ.cPLI._cAdvertSCR)
                        {
                            if (!_ahQueuedPLIId_Job.ContainsKey(cCJ.cPLI._cAdvertSCR.nPlaylistID))
                            {
                                _ahQueuedPLIId_Job.Add(cCJ.cPLI._cAdvertSCR.nPlaylistID, cCJ);
                                (new Logger()).WriteDebug2("JobToQueue: added [pliid=" + cCJ.cPLI._cAdvertSCR.nPlaylistID + "][pli=" + cCJ.cPLI._cAdvertSCR.sFilename + "]");
                            }
                        }
                        else if (null != cCJ.cPLI._cClipSCR)
                        {
                            if (!_ahQueuedClipsAssetId_Job.ContainsKey(cCJ.cPLI._cClipSCR.nID))
                            {
                                _ahQueuedClipsAssetId_Job.Add(cCJ.cPLI._cClipSCR.nID, cCJ);
                                (new Logger()).WriteDebug2("JobToQueue: added [assetid=" + cCJ.cPLI._cClipSCR.nID + "][pli=" + cCJ.cPLI._cClipSCR.sFilename + "]");
                            }
                        }
                    }
                }
                else
                    (new Logger()).WriteError("JobToQueue: [job=" + (cCJ == null ? "NULL" : "ok") + "][pli=" + (cCJ.cPLI == null ? "NULL" : "ok") + "]");
            }
            static public CopyJob JobSearch(PlaylistItem cPLI)
            {
                CopyJob cRetVal = null;
                if (null != cPLI)
                {
                    lock (_oLock)
                    {
                        if (null != cPLI._cAdvertSCR)
                        {
                            if (_ahErrorPLIId_Job.ContainsKey(cPLI._cAdvertSCR.nPlaylistID))
                            {
                                cRetVal = _ahErrorPLIId_Job[cPLI._cAdvertSCR.nPlaylistID];
                                cRetVal.bError = true;
                            }
                            else if (_ahQueuedPLIId_Job.ContainsKey(cPLI._cAdvertSCR.nPlaylistID))
                            {
                                cRetVal = _ahQueuedPLIId_Job[cPLI._cAdvertSCR.nPlaylistID];
                            }
                        }
                        else if (null != cPLI._cClipSCR)
                        {
                            if (_ahErrorClipsAssetId_Job.ContainsKey(cPLI._cClipSCR.nID))
                            {
                                cRetVal = _ahErrorClipsAssetId_Job[cPLI._cClipSCR.nID];
                                cRetVal.bError = true;
                            }
                            else if (_ahQueuedClipsAssetId_Job.ContainsKey(cPLI._cClipSCR.nID))
                            {
                                cRetVal = _ahQueuedClipsAssetId_Job[cPLI._cClipSCR.nID];
                            }
                        }
                    }
                }
                else
                    (new Logger()).WriteError("JobSearch: [pli=" + (cPLI == null ? "NULL" : "ok") + "]");
                return cRetVal;
            }
            static public List<CopyJob> GetAllQueue()
            {
                List<CopyJob> aRetVal = new List<CopyJob>();
                List<string> aTargets = new List<string>();
                lock (_oLock)
                {
                    CopyJob cCJ = cCurrentJob;
                    if (null != cCJ)
                    {
                        aRetVal.Add(cCJ);
                        aTargets.Add(cCJ.sTarget);
                    }
                    foreach (CopyJob cJ in _ahCopyJobs)
                        if (!aTargets.Contains(cJ.sTarget))
                        {
                            aRetVal.Add(cJ);
                            aTargets.Add(cJ.sTarget);
                        }
                }
                return aRetVal;
            }
            static public bool IsFileTargetOfThisJob(CopyJob cCopyJob, string sFilename)
            {
                if (null == cCopyJob || null == sFilename)
                    return false;
                string sT = System.IO.Path.GetFileName(cCopyJob.sTarget).ToLower();
                string sF = System.IO.Path.GetFileName(sFilename).ToLower();
                return sT == sF || "_" + sT == sF;
            }

            static private void Worker(object cState)
            {
                CopyJob cJob = null;
                userspace.Helper cHelper = new userspace.Helper();
                string sFilePauseCopying = System.IO.Path.Combine(IW.Preferences.sCacheFolder, SyncConstants.sFilePauseCopying);
                bool bPauseCopyingExists;
                bool bDoCopyRes;
                (new Logger()).WriteNotice("Worker: started");
                while (true)
                {
                    try
                    {
                        if (_ahCopyJobs.nCount <= 0) // will sleep at next step
                        {
                            try
                            {
                                bPauseCopyingExists = false;
                                bPauseCopyingExists = cHelper.FileExists(sFilePauseCopying);
                            }
                            catch (Exception ex)   // cHelper is lost occasionally
                            {
                                (new Logger()).WriteError("catch-1", ex);
                                System.Threading.Thread.Sleep(100);
                                cHelper = new userspace.Helper();
                                bPauseCopyingExists = cHelper.FileExists(sFilePauseCopying);
                            }
                            if (bPauseCopyingExists)
                                cHelper.FileMove(sFilePauseCopying, sFilePauseCopying + "!");
                        }

                        while (_ahCopyJobs.nCount <= 0)  // sleeps here if no job
                            System.Threading.Thread.Sleep(100);

                        lock (_oLock)
                            cJob = _ahCopyJobs.Dequeue();

                        System.Threading.Thread.Sleep(100); // to avoid conflict with too fast jobdone and mreCopyFinished

                        try { cHelper.FileExists(sFilePauseCopying); } // cHelper is lost occasionally
                        catch (Exception ex) { cHelper = new userspace.Helper(); /* (new Logger()).WriteError("catch-2", ex); // yes, it's here!    */ }

#if DEBUG
                        cJob.sSource = cJob.sSource.Replace(@"\\airfs\clips01\", @"c:\storages\clips\");  // during debug \\-path is not acceptable  //DNF
#endif
                        (new Logger()).WriteNotice("Worker: получили новую работу по копированию: [src=" + cJob.sSource + "][trg=" + cJob.sTarget + "]");
                        if (cHelper.FileExists(cJob.sTarget))
                        {
                            (new Logger()).WriteError(new Exception("Worker: файл уже и так есть: [" + cJob.sTarget + "]"));
                            JobDone(cJob);
                            continue;
                        }
                        string sTargDir = System.IO.Path.GetDirectoryName(cJob.sTarget);
                        string sTargFN = System.IO.Path.GetFileName(cJob.sTarget);
                        if (cHelper.FileExists(System.IO.Path.Combine(sTargDir, "_" + sTargFN)))
                        {
                            (new Logger()).WriteError(new Exception("Worker: файл c '_' уже и так есть: [" + System.IO.Path.Combine(sTargDir, "_" + sTargFN) + "]"));
                            JobDone(cJob);
                            continue;
                        }

                        if (cHelper.FileExists(cJob.sTarget + "!"))
                        {
                            (new Logger()).WriteError(new Exception("Worker: временный файл уже есть - удаляем: [" + cJob.sTarget + "!]"));
                            cHelper.FileDelete(cJob.sTarget + "!");
                        }

                        if (cHelper.FileExists(sFilePauseCopying + "!"))
                            cHelper.FileMove(sFilePauseCopying + "!", sFilePauseCopying);

                        lock (_oLock)
                        {
                            _cCurrentJob = cJob;
                            cJob.bStarted = true;
                            cHelper.CopyFileExtendedCreate(cJob.sSource, cJob.sTarget + "!", IW.Preferences.nCopyDelayMiliseconds, IW.Preferences.nCopyPeriodToDelayMiliseconds, cJob.cPLI._nFramesQty);  // медленное копирование 
                        }

                        cHelper.CopyFileExtendedDoCopyAsync(true);
                        do
                        {
                            Thread.Sleep(100);
                        } while (cHelper.ExCopyResult() == null);
                        bDoCopyRes = cHelper.ExCopyResult().Value;

                        if (cHelper.FileExists(cJob.sTarget + "!"))
                        {
                            cHelper.FileMove(cJob.sTarget + "!", cJob.sTarget);
                            JobDone(cJob);
                            (new Logger()).WriteDebug("Worker: файл покопирован: [source=" + cJob.sSource + "][target=" + cJob.sTarget + "]");
                        }
                        else
                        {
                            JobAddToErrorList(cJob, cHelper);
                            (new Logger()).WriteError(new Exception("Не удалось загрузить файл в кэш! Возможно диск переполнен! [copy_res=" + bDoCopyRes + "]"));
                        }
                    }
                    catch (Exception ex)
                    {
                        if (null != cJob && !cJob.bFinished)
                            JobAddToErrorList(cJob, cHelper);
                        (new Logger()).WriteError(ex);
                    }
                }
            }
        }
        public class Playlist : Item
		{
            static private Dictionary<long, Advertisement> _ahLinearPLItemId_Item = new Dictionary<long, Advertisement>();
            static private Dictionary<long, List<Advertisement>> _ahLinearPLAssetId_Item = new Dictionary<long, List<Advertisement>>();
            static public object oLockPLFragment = new object();
            static private Advertisement[] _aLinearPLFragment = new Advertisement[0];
			static public Advertisement[] aLinearPLFragment
			{
                get
                {
                    return _aLinearPLFragment;
                }
				set
				{
                    lock (oLockPLFragment)
                    {
                        _aLinearPLFragment = value;
                        _ahLinearPLItemId_Item = value.ToDictionary(o => o.nPlaylistID, o => o);
                        _ahLinearPLAssetId_Item.Clear();
                        foreach (Advertisement cA in value)
                        {
                            if (!_ahLinearPLAssetId_Item.ContainsKey(cA.nAssetID))
                                _ahLinearPLAssetId_Item.Add(cA.nAssetID, new List<Advertisement>());
                            _ahLinearPLAssetId_Item[cA.nAssetID].Add(cA);
                        }
                    }
				}
			}

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
                if (IW.Preferences.cClientReplica == null)
                    (new Logger()).WriteNotice("[prefs in player = NULL]");
                else
                    (new Logger()).WriteNotice("[prefs in player = [name="+ IW.Preferences.cClientReplica.sChannelName + "][logfolder=" + IW.Preferences.sPlayerLogFolder + "][automation = " + IW.Preferences.sClipStartStopAutomationFile + "]]"); 

                 _cPlaylist = new userspace.Playlist();

				if (null != (cPL_PrefTemplate = IW.Preferences.cClientReplica.aTemplates.FirstOrDefault(o => o.sFile == "Player Playlist")) && cPL_PrefTemplate.stScaleVideo != Area.stEmpty)
				{
					//_cPlaylist.stArea = new Area(cPL_PrefTemplate.stScaleVideo.nLeft, cPL_PrefTemplate.stScaleVideo.nTop, 0, 0);
					_cPlaylist.cDock = new Dock(cPL_PrefTemplate.stScaleVideo.nLeft, cPL_PrefTemplate.stScaleVideo.nTop);
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
						(new Logger()).WriteNotice("effect prepared: [" + cPLI.sFilename + "][hc="+ cPLI.GetHashCode() + "]");
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
                                    aCurrentClasses = cPlaylistItem._cClipSCR.aClasses;
									tClipStop.Stop();
									AutomationWrite(cPlaylistItem._cClipSCR.nID + ", clip, started");
									sClipStop = cPlaylistItem._cClipSCR.nID + ", clip, stopped";
									tClipStop.Interval = (cPlaylistItem._cClipSCR.nFramesQty - IW.Preferences.nStopOffset) * IW.Preferences.nFrameDuration_ms;
									tClipStop.Start();
								}
								else if (cPlaylistItem._eType == PlaylistItem.PLIType.AdvBlockItem && null != cPlaylistItem._cAdvertSCR)
                                    aCurrentClasses = cPlaylistItem._cAdvertSCR.aClasses;
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
									aCurrentClasses = null;
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
            static public List<Advertisement> SearchPLFragmentItemsByCacheName(string sFileName, out long nAssetID)
            {
                sFileName = System.IO.Path.GetFileNameWithoutExtension(sFileName);
                if (sFileName.StartsWith("_"))
                    sFileName = sFileName.Substring(1);
                long nID;
                nAssetID = -1;
                lock (oLockPLFragment)
                {
                    if (sFileName.StartsWith("asset"))
                    {
                        sFileName = sFileName.Substring(6);
                        if (long.TryParse(sFileName, out nAssetID))
                            if (_ahLinearPLAssetId_Item.ContainsKey(nAssetID))
                                return _ahLinearPLAssetId_Item[nAssetID];
                    }
                    else
                    {
                        if (long.TryParse(sFileName, out nID))
                            if (_ahLinearPLItemId_Item.ContainsKey(nID))
                            {
                                nAssetID = _ahLinearPLItemId_Item[nID].nAssetID;
                                return new List<Advertisement>() { _ahLinearPLItemId_Item[nID] };
                            }
                    }
                }
                return null;
            }
            static public Advertisement SearchNearestPLIToAsset(long nAssetID)
			{
				Advertisement[] aPLIs = new Advertisement[0];
				lock (oLockPLFragment)
					aPLIs = _aLinearPLFragment.Where(o => o.nAssetID == nAssetID).ToArray();
				(new Logger()).WriteNotice("Search Asset in PL Fragment: [apli=" + aPLIs.Length + "]");
				Advertisement cRetVal = null;
				double nMinimum = long.MaxValue, nCurrent;
				DateTime dtNow = DateTime.Now;
				foreach (Advertisement cPLI in aPLIs)
				{
					if (nMinimum > (nCurrent = Math.Abs(dtNow.Subtract(cPLI.dtStartPlanned).TotalMinutes)))
					{
						nMinimum = nCurrent;
						cRetVal = cPLI;
					}
				}
				return cRetVal;
			}
            static public string IsItemInCache(PlaylistItem cItem)
            {
                string sFileAsset;
                string sFilePLI;
                if (!GetFileNamesForCache(cItem, out sFileAsset, out sFilePLI))
                    return null;

                userspace.Helper cHelper = new userspace.Helper();
                string sFile;
                if (null != sFileAsset)
                {
                    sFile = System.IO.Path.Combine(IW.Preferences.sCacheFolder, sFileAsset);
                    if (cHelper.FileExists(sFile))
                        return sFileAsset;
                    sFile = System.IO.Path.Combine(IW.Preferences.sCacheFolder, "_" + sFileAsset);
                    if (cHelper.FileExists(sFile))
                        return "_" + sFileAsset;
                }
                if (null != sFilePLI)
                {
                    sFile = System.IO.Path.Combine(IW.Preferences.sCacheFolder, sFilePLI);
                    if (cHelper.FileExists(sFile))
                        return sFilePLI;
                    sFile = System.IO.Path.Combine(IW.Preferences.sCacheFolder, "_" + sFilePLI);
                    if (cHelper.FileExists(sFile))
                        return "_" + sFilePLI;
                }
                return null;
            }
            static public bool GetFileNamesForCache(PlaylistItem cItem, out string sFileAsset, out string sFilePLI)
            {
                sFileAsset = null;
                sFilePLI = null;
                if (IW.Preferences.sCacheFolder.IsNullOrEmpty())
                    return false;
                long nPLIID = -1;
                string sExtension = System.IO.Path.GetExtension(cItem.sFilenameFull);
                if (cItem._cAdvertSCR != null)
                {
                    nPLIID = cItem._cAdvertSCR.nPlaylistID;
                    sFilePLI = "" + nPLIID + sExtension;
                    return true;
                }
                else if (cItem._cClipSCR != null)
                {
                    sFileAsset = "asset_" + cItem._cClipSCR.nID + sExtension;
                    Advertisement cClipFound = SearchNearestPLIToAsset(cItem._cClipSCR.nID);
                    if (cClipFound != null)
                    {
                        nPLIID = cClipFound.nPlaylistID;
                        sFilePLI = "" + nPLIID + sExtension;
                    }
                    return true;
                }
                else
                    return false;  // не надо кэшировать
            }
            static public string CacheItem(PlaylistItem cItem)
			{
                string sFileAsset;
                string sFilePLI;
                if (!GetFileNamesForCache(cItem, out sFileAsset, out sFilePLI))
                    return cItem.sFilenameFull;

                userspace.Helper cHelper = new userspace.Helper();
                Logger cLogger = new Logger();
                string sFile = IsItemInCache(cItem);
                string sFile_In_Cache;
                string sFileCached;
                string sRetVal = cItem.sFilenameFull;
                if (null == sFile) // файл не найден ни id ни _id ни ассет ни _ассет в кэше - надо копировать с банки 
                {
                    throw new Exception("File not found in cache! Add to PL cached files only! [" + cItem.sFilename + "]");
                    // теперь надо сначала закешировать, а потом уж добавлять...


                    sFile = cItem._cClipSCR == null ? sFilePLI : sFileAsset;
                    sFile_In_Cache = System.IO.Path.Combine(IW.Preferences.sCacheFolder, sFile);
                    sFileCached = System.IO.Path.Combine(IW.Preferences.sCacheFolder, "_" + sFile);

                    try
                    {
                        cLogger.WriteDebug("файла нет в кэше! попытка покопировать файл в кэш: [file = " + cItem.sFilenameFull + "][cashed = " + sFile_In_Cache + "]");
                        CopyJob cJob = new CopyJob() { sSource = cItem.sFilenameFull, sTarget = sFile_In_Cache, cPLI = cItem, mreCopyFinished = new ManualResetEvent(false) };
                        CopyJob.JobToQueue(cJob, true);
                        cJob.mreCopyFinished.WaitOne();

                        if (cJob.bError)
                            throw new Exception("copy failed");
                        cLogger.WriteNotice("файл покопирован в кэш: " + sFile_In_Cache);

                        return MoveToPlayerCache(sFile);
                    }
                    catch (Exception ex)
                    {
                        cLogger.WriteError(ex);
                    }
                    cLogger.WriteError("файл не найден в кэше и мы не смогли его скопировать - будем давать с банки [cached:" + sFile_In_Cache + "][original:" + (sRetVal = cItem.sFilenameFull) + "]");//TODO LANG
                }
                else if (sFile.StartsWith("_"))
                {
                    cLogger.WriteWarning("переименованный файл в кэше уже существует:" + (sRetVal = System.IO.Path.Combine(IW.Preferences.sCacheFolder, sFile)));   // после перезапуска, например
                    return sRetVal;
                }
                else // файл найден в кэше - надо добавлять "_" к имени
                {
                    try
                    {
                        return MoveToPlayerCache(sFile);
                    }
                    catch (Exception ex)
                    {
                        cLogger.WriteError(ex);
                    }
                }

                return sRetVal;
			}
            static private string MoveToPlayerCache(string sFile)
            {
                string sFile_In_Cache = System.IO.Path.Combine(IW.Preferences.sCacheFolder, sFile);
                string sFileCached = System.IO.Path.Combine(IW.Preferences.sCacheFolder, "_" + sFile);
                userspace.Helper cHelper = new userspace.Helper();
                Logger cLogger = new Logger();
                cLogger.WriteDebug("попытка переименовать файл в кэше: [file = " + sFile_In_Cache + "][cashed = " + sFileCached + "]" + sFileCached);
                if (!cHelper.FileMove(sFile_In_Cache, sFileCached))
                    throw new Exception("move failed. see igserver log");
                cLogger.WriteNotice("файл переименован в кэше: " + sFileCached);
                return sFileCached;
            }
            public void AddVideo(PlaylistItem cItem)
			{
				Video cVideo = new Video();
				cVideo.stArea = new Area(0, 0, cPL_PrefTemplate.stScaleVideo.nWidth, cPL_PrefTemplate.stScaleVideo.nHeight);

				(new Logger()).WriteNotice("пробуем добавить видео: [item=" + cItem.sFilenameFull + "][adv=" + (cItem._cAdvertSCR == null ? "NULL" : "" + cItem._cAdvertSCR.nPlaylistID) + "][clip=" + (cItem._cClipSCR == null ? "NULL" : "" + cItem._cClipSCR.nID) + "]");
				cVideo.sFile = CacheItem(cItem);

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
                    if (null == cPLI)
                        continue;
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
        static Player()
        {
            if (!IW.Preferences.bReLoaded)
                IW.Preferences.Reload();
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
            (new Logger()).WriteDebug("AutomationWrite: [inst = "+ IW.Preferences._cInstance + "][file = " + IW.Preferences.sClipStartStopAutomationFile + "]");

            if (IW.Preferences.sClipStartStopAutomationFile.IsNullOrEmpty())
                return;
			string sFileName = IW.Preferences.sClipStartStopAutomationFile;
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

                    //new Thread(delegate () {   // было актуально когда закачка начиналась при препаре
                    if (null == AddVideo(cRetVal, aItems))
                    {
                        (new Logger()).WriteWarning("player_create: deleting" + sInfo);
                        GarbageCollector.ItemDelete(cRetVal);
                    }
                    else
                    {
                        if (((Playlist)cRetVal).eStatus == Item.Status.Idle)
                            ((Playlist)cRetVal).Prepare();
                    }
                    //}).Start();
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
                (new Logger()).WriteNotice("player: add_video: begin [count=" + aItems.Length + "]");
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
            (new Logger()).WriteNotice("player: add_video: end");
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
                    {
                        aRetVal = ((Playlist)cItemLocal).aPlaylistItems;
                        if (cItemLocal.eStatus == Item.Status.Stopped && aRetVal != null && aRetVal[aRetVal.Length - 1].dtStopReal == DateTime.MinValue)
                            aRetVal[aRetVal.Length - 1].dtStopReal = DateTime.Now;
                    }
                    else
                        (new Logger()).WriteError("plis:get: указанный элемент не зарегистрирован [item:" + (null == cItem ? "NULL" : "" + cItem.GetHashCode()) + "]");
                }
				else
					(new Logger()).WriteError("plis:get: не инициирован механизм регистрации клиента");
			}
			catch (Exception ex)
            {
                (new Logger()).WriteError("[cli_id=" + Client.nID + "][item_status=" + (null == cItem ? "item is NULL" : cItem.eStatus + "]"), ex);
            }
            // logger
            string sLog = "EMPTY";
            if (null == aRetVal)
                sLog += "NULL";
            else
            {
                sLog = "";
                foreach (PlaylistItem cPLI in aRetVal)
                    sLog += "<br>\t\t" + cPLI.ToStringShort();
            }
            (new Logger()).WriteDebug2("player.PlaylistItemsGet.return: " + sLog);

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
            string sClipsPath = "NULL";
            try
            {
                (new Logger()).WriteDebug("ClipsSCRGet: Dont check files = " + IW.Preferences.bDontCheckFiles);
                (new Logger()).WriteDebug("ClipsSCRGet: [in_count:" + aClips.Length + "][_ahStoragesSCR = " + (_ahStoragesSCR == null ? "NULL" : "" + _ahStoragesSCR.Count) + "]");

                userspace.Helper cHelper = new userspace.Helper();
                sClipsPath = _ahStoragesSCR.Values.FirstOrDefault(o => o == null ? false : o.Contains("clips"));
                string sRot;
                if (sClipsPath != null)
                    _ahAllClipsAssetId_Clip = new Dictionary<long, Clip>();
                foreach (Clip cClip in aClips)
                {
                    if (cClip == null)
                        continue;
                    if (IW.Preferences.bDontCheckFiles || cHelper.FileExists(System.IO.Path.Combine(sClipsPath, cClip.sFilename)))
                    {
                        cClip.bCached = false;  // Playlist.IsItemInCache(new PlaylistItem() { _cClipSCR = cClip }) != null;   переделать на список кэшедов
                        cClip.sDuration = cClip.nFramesQty.ToFramesString();
                        sRot = cClip.sRotation.ToLower();
                        cClip.bLocked = (sRot == "стоп" || sRot == "stop") ? true : false; //PREFERENCES
                        cClip.sStoragePath = sClipsPath; //PREFERENCES
                        aRetVal.Add(cClip);
                        lock (_ahAllClipsAssetId_Clip)
                            _ahAllClipsAssetId_Clip.Add(cClip.nID, cClip);
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
			if (IW.Preferences.bDontCheckFiles)
				return aAdverts;
			List<Advertisement> aRetVal = new List<Advertisement>();
			try
			{
				userspace.Helper cHelper = new userspace.Helper();
				foreach (Advertisement cAdv in aAdverts)
				{
					if (cHelper.FileExists(cAdv.sStoragePath + cAdv.sFilename))
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
				aFilenames.AddRange(cHelper.FileNamesGet(sFilesFolder, aExtensions, true));
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

		[WebMethod(EnableSession = true)]
		public void LinearPLFragmentSet(Advertisement[] aPLIs)
		{
			if (null != aPLIs)
			{
				Player.Playlist.aLinearPLFragment = aPLIs;
				(new Logger()).WriteDebug("Linear PL got: [" + aPLIs.Length + "][begin=" + aPLIs[0].nPlaylistID + "][end=" + aPLIs[aPLIs.Length - 1].nPlaylistID + "]");
			}
		}
        [WebMethod(EnableSession = true)]
        public Advertisement[] ItemsCachedGet(bool bFromPlaylistOnly)
        {
            (new Logger()).WriteDebug("ItemsCachedGet: start");
            userspace.Helper cHelper = new userspace.Helper();
            List<string> aFilenames = cHelper.FileNamesGet(IW.Preferences.sCacheFolder, new string[5] {"mxf", "mp4", "avi", "mpg", "mov" }).ToList();
            List<Advertisement> aItems;
            List<Advertisement> aRetVal = new List<Advertisement>();
            Advertisement cClip;
            long nAssetID, nPLIID;
            Clip cClipTmp;
            string sPLIID = "";

            if (bFromPlaylistOnly)
            {
                foreach (string sS in aFilenames)
                {
                    try
                    {
                        aItems = Playlist.SearchPLFragmentItemsByCacheName(sS, out nAssetID);
                        if (null == aItems)  // pli id (and not clip)
                        {
                            sPLIID = System.IO.Path.GetFileNameWithoutExtension(sS);
                            sPLIID = sPLIID.StartsWith("_") ? sPLIID.Substring(1) : sPLIID;
                            if (long.TryParse(sPLIID, out nPLIID))
                            {
                                cClip = new Advertisement() { nPlaylistID = nPLIID, dtStartSoft = DateTime.Now };
                                cClip.bCached = true;
                                cClip.sCopyPercent = "ok";
                                aRetVal.Add(cClip);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        (new Logger()).WriteError("pli filename=" + sS, ex);
                    }
                }
                return aRetVal.ToArray();
            }

            List<CopyJob> aQueue = CopyJob.GetAllQueue();
            Dictionary<string, CopyJob> ahFilename_Job = aQueue.ToDictionary(o => System.IO.Path.GetFileName(o.sTarget), o => o);
            foreach (string sFN in ahFilename_Job.Keys)
            {
                if (!(aFilenames.Contains(sFN) || aFilenames.Contains("_" + sFN)))
                    aFilenames.Insert(0, sFN);
            }

            lock (Playlist.oLockPLFragment)
            {
                foreach (string sS in aFilenames)
                {
                    try
                    {
                        if (sS.EndsWith("!"))
                            continue;
                        aItems = Playlist.SearchPLFragmentItemsByCacheName(sS, out nAssetID);
                        DateTime dtIndex = DateTime.MinValue;
                        lock (_ahAllClipsAssetId_Clip)
                        {
                            if (null != aItems)
                            {
                                foreach (Advertisement cItem in aItems)
                                {
                                    cItem.bCached = ahFilename_Job.Keys.Contains(sS) ? false : true;
                                    cItem.sCopyPercent = ahFilename_Job.Keys.Contains(sS) ? ahFilename_Job[sS].sPercentProgress : "ok";
                                    if (_ahAllClipsAssetId_Clip.ContainsKey(cItem.nAssetID))
                                    {
                                        if (sS.StartsWith("asset") || sS.StartsWith("_asset"))
                                            cItem.sCopyPercent += " (asset)";
                                    }
                                }
                            }

                            if (sS.StartsWith("asset") || sS.StartsWith("_asset")) // только клипы качаются как ассеты!
                            {
                                if (_ahAllClipsAssetId_Clip.ContainsKey(nAssetID))
                                {
                                    cClipTmp = _ahAllClipsAssetId_Clip[nAssetID];
                                    cClip = new Advertisement() { nAssetID = nAssetID, dtStartSoft = DateTime.MaxValue, sDuration = cClipTmp.nFramesQty.ToFramesString(), sName = cClipTmp.sName, sStorageName = cClipTmp.sStorageName, sStoragePath = cClipTmp.sStoragePath };
                                    cClip.bCached = ahFilename_Job.Keys.Contains(sS) ? false : true;
                                    cClip.sFilename = cClipTmp.sFilename;
                                    cClip.nFramesQty = cClipTmp.nFramesQty;
                                    cClip.sCopyPercent = ahFilename_Job.Keys.Contains(sS) ? ahFilename_Job[sS].sPercentProgress + " (asset)" : "ok (asset)";
                                    cClip.dtStartPlanned = dtIndex;
                                    cClip.sStartPlanned = ""; // dtIndex.ToString("HH:mm:ss");
                                    dtIndex = dtIndex.AddMinutes(1); // for sorting in client
                                    aRetVal.Add(cClip);
                                }
                                else
                                    (new Logger()).WriteError("ItemsCachedGet: clip not found [file=" + sS + "][assetid=" + nAssetID + "]");
                            }
                            else if (null == aItems)  // pli id (and not clip)
                            {
                                sPLIID = System.IO.Path.GetFileNameWithoutExtension(sS);
                                sPLIID = sPLIID.StartsWith("_") ? sPLIID.Substring(1) : sPLIID;
                                cClip = new Advertisement() { nPlaylistID = long.Parse(sPLIID), dtStartSoft = DateTime.Now };
                                cClip.bCached = ahFilename_Job.Keys.Contains(sS) ? false : true;
                                cClip.sCopyPercent = ahFilename_Job.Keys.Contains(sS) ? ahFilename_Job[sS].sPercentProgress : "ok";
                                aRetVal.Add(cClip);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        (new Logger()).WriteError("filename=" + sS, ex);
                    }
                }
                (new Logger()).WriteNotice("ItemsCachedGet: got [assets_count=" + aRetVal.Count + "][plfragment_count=" + Playlist.aLinearPLFragment.Length + "]");
                aRetVal = aRetVal.OrderBy(o => o.dtStartPlanned).ToList();
                aRetVal.AddRange(Playlist.aLinearPLFragment);
            }
            return aRetVal.ToArray();
        }
        [WebMethod(EnableSession = true)]
        public void CacheClip(Clip cClip)
        {
            //            переделать 
            //                1) копирование файлов - на воркер и в очередь их всех
            //                2) прогресс копирования 
            //                3) стопить синкер на время копирования !!!   +  старт его после...  и при включении (после перезапуска)!!!!  как и при старте синка!!!!!
            //                 4) пусть синкер не грохает не его видосы сутки.
            // не забыть перенести sio.file.delete и fileextended в helper.....   см worker 340

            // NEW (28 апр)
            // +поотлаживай отображение разных якобы закешированных херей
            // добавить возможность по ПКМ ставить клип на закачку
            // ПРОВЕРИТЬ СКОРОСТЬ самого быстрого "медленного копирования"
            // поотлаживать это
            // перенести (уже отлаженную) медленное копирование в IG

            // добавь отработку - если препарим клип, а он уже в очереди, либо текущий джоб, либо уже готов - пусть ждёт или идёт 

            //  в синкер - пусть не свои айтемы (не 23434 или _4545454  не удаляет быстро, а только в полночь после срока хранения)


            // NEW 18 may
            // добавить параметр в ItemsCachedGet() чтобы брать только рекламу, а то долго чот. - долго из-за взятия "in_stock" для всех файлов БД!!
            (new Logger()).WriteDebug2("CacheClip: in");
            userspace.Helper cHelper = new userspace.Helper();
            Advertisement cPLI = Playlist.SearchNearestPLIToAsset(cClip.nID);

            string sFile = null, sAnother = null;
            if (cPLI != null)
            {
                string sFile1 = System.IO.Path.Combine(IW.Preferences.sCacheFolder, cPLI.nPlaylistID + System.IO.Path.GetExtension(cClip.sFilename));
                string sFile2 = System.IO.Path.Combine(IW.Preferences.sCacheFolder, "_" + cPLI.nPlaylistID + System.IO.Path.GetExtension(cClip.sFilename));
                if (cHelper.FileExists(sFile1))
                {
                    sFile = sFile1;
                    sAnother = sFile2;
                }
                else if (cHelper.FileExists(sFile2))
                {
                    sFile = sFile2;
                    sAnother = sFile1;
                }
            }
            (new Logger()).WriteDebug("CacheClip: [sFile=" + sFile + "][sAnother=" + sAnother + "]");

            string sDestination = System.IO.Path.Combine(IW.Preferences.sCacheFolder, "asset_" + cClip.nID + System.IO.Path.GetExtension(cClip.sFilename));
            (new Logger()).WriteDebug("CacheClip: [cPLI = " + (cPLI == null ? "NULL" : "" + cPLI.nPlaylistID) + "][sDestination=" + sDestination + "]");

            if (sFile == null)
            {
                CopyJob.JobToQueue(new CopyJob() { sSource = System.IO.Path.Combine(cClip.sStoragePath, cClip.sFilename), sTarget = sDestination, cPLI = new PlaylistItem() { _cClipSCR = cClip } }, false);
                (new Logger()).WriteDebug2("CacheClip: done add to job");
            }
            else
            {
                if (cHelper.FileExists(sDestination))
                {
                    (new Logger()).WriteError(new Exception("CacheClip: файл уже и так есть: [" + sDestination + "]"));
                    return;
                }
                cHelper.FileCreate(sAnother, "moved to [" + sDestination + "]");
                if (!cHelper.FileMove(sFile, sDestination))
                {
                    cHelper.FileDelete(sAnother);
                }
                (new Logger()).WriteDebug2("CacheClip: done move");
            }
        }
    }
}
