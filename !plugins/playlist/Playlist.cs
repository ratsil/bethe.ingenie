//#define DEBUG1
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Linq;
using BTL.Play;
using btl=BTL.Play;
using System.Collections;
using System.Diagnostics;
using System.Xml;
using System.Timers;
using System.IO;
using System.Threading;
using ingenie;
using helpers;
using helpers.extensions;
using hrc = helpers.replica.cues;

namespace ingenie.plugins
{
    public class Playlist : MarshalByRefObject, IPlugin
    {
        #region Members
        private Preferences _cPreferences;
        private EventDelegate Prepared;
        private EventDelegate Started;
        private EventDelegate Stopped;
        private btl.Playlist _cPlaylist;
		private DBInteract _cDBI;
		private long _nPLID;
		private hrc.plugins.Playlist _cPLCurrent;
		private Dictionary<Video, hrc.plugins.PlaylistItem> _ahVideoBinds;
		private static IdNamePair[] _aStatuses;
		private bool bIsStopped;
		#endregion

		public Playlist()
        {
			bIsStopped = false;
        }
        public void Create(string sWorkFolder, string sData)
		{
			(new Logger()).WriteDebug2("create_in: data <br>" + sData);
			try
			{
				_cPreferences = new Preferences(sData);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
				throw;
			}
			(new Logger()).WriteDebug2("create_ok");
		}
		public void Prepare()
        {
            try
            {
				(new Logger()).WriteDebug2("prepare_in");
				_cDBI = new DBInteract();
				_cPLCurrent = null;
				_nPLID = _cDBI.TryToGetIDFromCommands();
				(new Logger()).WriteDebug2("pl_id=" + _nPLID);
				_ahVideoBinds = new Dictionary<Video, helpers.replica.cues.plugins.PlaylistItem>();

				_cPlaylist = new btl.Playlist();
                _cPlaylist.stArea = _cPreferences.stArea;
                _cPlaylist.stMergingMethod = _cPreferences.stMerging;
                _cPlaylist.nLayer = _cPreferences.nLayer;
                _cPlaylist.bOpacity = false;
				_cPlaylist.bStopOnEmpty = true;
				_cPlaylist.aChannelsAudio = new byte[] { 0, 1 };
				_cPlaylist.EffectStarted += _cPlaylist_EffectStarted;
				_cPlaylist.EffectStopped += _cPlaylist_EffectStopped;
				_cPlaylist.EffectFailed += _cPlaylist_EffectFailed;
				_cPlaylist.Stopped += _cPlaylist_Stopped;
                _cPlaylist.Prepare();

				
				Video cVideo;
				_cPLCurrent = _cDBI.AdvancedPlaylistGet(_nPLID > 0 ? _nPLID : _cPreferences.nPlaylistID);
				(new Logger()).WriteDebug2("pl_id=" + (_nPLID > 0 ? _nPLID : _cPreferences.nPlaylistID));
				helpers.replica.cues.plugins.PlaylistItem cPLI = _cPLCurrent.aItems[0];
                cVideo = new Video(cPLI.oAsset.cFile.sFile);
				cVideo.nDuration = (ulong)(cPLI.nFramesQty == long.MaxValue || cPLI.nFramesQty < 1 ? cPLI.oAsset.nFramesQty : cPLI.nFramesQty);
				cVideo.Prepare();
				_cPlaylist.VideoAdd(cVideo);
				_ahVideoBinds.Add(cVideo, cPLI);

				if (null != Prepared)
                    Plugin.EventSend(Prepared, this);
                (new Logger()).WriteDebug2("prepare_ok");
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
				Stop();
			}
        }

		#region effects events
		private void _cPlaylist_EffectFailed(Effect cSender, Effect cEffect)
		{
			try
			{
				(new Logger()).WriteDebug("eff_failed_in");
				hrc.plugins.PlaylistItem cPLI = _ahVideoBinds[(Video)cEffect];
				cPLI.oStatus = StatusGet("failed");
				cPLI.Save(_cDBI);
				(new Logger()).WriteDebug("status 'failed' saved:[id=" + cPLI.nID + "][name=" + cPLI.oAsset.sName + "]");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}

		private void _cPlaylist_EffectStopped(Effect cSender, Effect cEffect)
		{
			try
			{
				(new Logger()).WriteDebug("eff_stopped_in");
				hrc.plugins.PlaylistItem cPLI = _ahVideoBinds[(Video)cEffect];
				cPLI.oStatus = StatusGet("played");
				cPLI.Save(_cDBI);
				(new Logger()).WriteDebug("status 'played' saved:[id=" + cPLI.nID + "][name=" + cPLI.oAsset.sName + "]");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}

		private void _cPlaylist_EffectStarted(Effect cSender, Effect cEffect)
		{
			try
			{
				(new Logger()).WriteDebug("eff_started_in");
				hrc.plugins.PlaylistItem cPLI = _ahVideoBinds[(Video)cEffect];
				cPLI.oStatus = StatusGet("onair");
				cPLI.dtStarted = DateTime.Now;
				cPLI.Save(_cDBI);
				(new Logger()).WriteDebug("status 'onair' saved:[id=" + cPLI.nID + "][name=" + cPLI.oAsset.sName + "]");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		private void _cPlaylist_Stopped(Effect cSender)
		{
			(new Logger()).WriteDebug("pl_stopped");
			Stop();
		}
		#endregion

		public void Start()
		{
			try
			{
				(new Logger()).WriteDebug("start_in");
				bool bFirst = true;
				foreach (hrc.plugins.PlaylistItem cPLI in _cPLCurrent.aItems)
				{
					if (bFirst)
					{
						bFirst = false;
						_cPlaylist.Start();  // first pli was added in prepare!
						continue;
					}
					Video cVideo;
					cVideo = new Video(cPLI.oAsset.cFile.sFile);
					cVideo.nDuration = (ulong)(cPLI.nFramesQty == long.MaxValue || cPLI.nFramesQty < 1 ? cPLI.oAsset.nFramesQty : cPLI.nFramesQty);
					_cPlaylist.VideoAdd(cVideo);
					_ahVideoBinds.Add(cVideo, cPLI);
				}
				if (null != Started)
					Plugin.EventSend(Started, this);
				(new Logger()).WriteDebug("started");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
				Stop();
			}
		}
		public void Stop()
        {
			if (bIsStopped)
				return;
			bIsStopped = true;

            try
			{
				(new Logger()).WriteDebug("stop_in");
				if (null != _cPlaylist && _cPlaylist.eStatus != BTL.EffectStatus.Stopped)
					_cPlaylist.Stop();
			}
			catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
            if (null != Stopped)
                Plugin.EventSend(Stopped, this);
			(new Logger()).WriteDebug("stopped");
		}
		private IdNamePair StatusGet(string sName)
		{
			IdNamePair cRetVal;
			if (null == _aStatuses)
				_aStatuses = _cDBI.PlaylistItemsStatusesGet();
			if (null == (cRetVal= _aStatuses.FirstOrDefault(o=>o.sName==sName)))
				throw new Exception("there is no status name like [name=" + sName + "] in DB");
			return cRetVal;
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
                if (null != _cPlaylist)
                    return _cPlaylist.eStatus;
                return BTL.EffectStatus.Idle;
            }
        }
        DateTime IPlugin.dtStatusChanged
        {
            get
            {
                if (null != _cPlaylist)
                    return _cPlaylist.dtStatusChanged;
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
