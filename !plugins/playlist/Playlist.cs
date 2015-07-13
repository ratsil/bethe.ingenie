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
        #endregion

        public Playlist()
        {
        }
        public void Create(string sWorkFolder, string sData)
        {
            _cPreferences = new Preferences(sData);
        }
        public void Prepare()
        {
            try
            {
                DisCom.Init();

                _cPlaylist = new btl.Playlist();
                _cPlaylist.stArea = _cPreferences.stArea;
                _cPlaylist.bCUDA = _cPreferences.bCuda;
                _cPlaylist.nLayer = _cPreferences.nLayer;
                _cPlaylist.bOpacity = false;
                _cPlaylist.Prepare();
                
                if (null != Prepared)
                    Plugin.EventSend(Prepared, this);
                (new Logger()).WriteDebug3("ok");
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
        }

        public void Start()
        {
            _cPlaylist.Start();
            if (null != Started)
                Plugin.EventSend(Started, this);
        }
        public void Stop()
        {
            try
            {
                _cPlaylist.Stop();
                (new Logger()).WriteDebug("stop");
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
            if (null != Stopped)
                Plugin.EventSend(Stopped, this);
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
