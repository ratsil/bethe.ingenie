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
    public class Voting : MarshalByRefObject, IPlugin
    {
        private Preferences _cPreferences;
        private EventDelegate Prepared;
        private EventDelegate Started;
        private EventDelegate Stopped;
        private BTL.EffectStatus _eStatus;
        private DateTime _dtStatusChanged;
        private object _oLock;
        private bool _bPrepared;
        private bool _bStopped;
        private bool _bStarted;
        private bool _bWorkerAborted;
        private bool _bWorkerUpdaterAborted;
        private System.Threading.Thread _cThreadWorker;
        private System.Threading.Thread _cThreadWorkerUpdater;
        private object _oLockUpdater;

        private Roll _cRollImages;
        private Roll _cRollTop;
        private Roll _cRollBot;
        private Roll _cRollMid;
        private BTL.IEffect[] _aEffects;
        private List<Bytes> _aBytesImages; // images in 1-23; loop 24; out 26-37;
        private List<Bytes> _aBytesTop; // top in1 0-13; loo1 14; sw1 16-28; loo2 29; sw2 31-43; in2 46-59;
        private List<Bytes> _aBytesBot; // bot in 0-26; loop 27;
        private List<Bytes> _aBytesMid;  // mid in 0-10; loo1 11; sw 13-21; loo2 22
        private List<Bytes> _aBytesMidUpdated;
        private List<Bytes> _aBytesMidOld;

        private int _nAddedFramesToEveryRoll;
        private int _nImagesLoopEnd;
        private int _nImagesNextStart;
        private int _nTopLoopEnd;
        private bool _bTopLoop1Now;
        private DateTime _dtMidNextUpdate;

        private enum Transition
        {
            Initial,
            Normal,
            Final,
        }
        public BTL.EffectStatus eStatus
        {
            get
            {
                return _eStatus;
            }
            set
            {
                _dtStatusChanged = DateTime.Now;
                _eStatus = value;
            }
        }
        public bool bMidUpdated
        {
            get
            {
                lock (_oLockUpdater)
                {
                    return _aBytesMidUpdated != null;
                }
            }
        }

        public Voting()
        {
            eStatus = BTL.EffectStatus.Idle;
            _nImagesNextStart = int.MaxValue;
            _oLockUpdater = new object();
            _dtMidNextUpdate = DateTime.MaxValue;
        }
        public void Create(string sWorkFolder, string sData)
        {
            (new Logger()).WriteDebug("create");
            _oLock = new object();
            _bStopped = false;
            _bWorkerAborted = false;
            _cPreferences = new Preferences(sData);
            _cRollImages = _cPreferences.cRollImages;
            _cRollTop = _cPreferences.cRollTop;
            _cRollBot = _cPreferences.cRollBot;
            _cRollMid = _cPreferences.cRollMid;
            _aBytesImages = new List<Bytes>();
            _aBytesTop = new List<Bytes>();
            _aBytesBot = new List<Bytes>();
            _aBytesMid = new List<Bytes>();
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
                        (new Logger()).WriteWarning("Voting has already prepared!");
                        return;
                    }
                    _bPrepared = true;
                }
                (new Logger()).WriteDebug("prepare:in");

                ulong nSimulID = (ulong)_cRollImages.nID;
                _cRollImages.SimultaneousSet(nSimulID, 4);
                _cRollTop.SimultaneousSet(nSimulID, 4);
                _cRollBot.SimultaneousSet(nSimulID, 4);
                _cRollMid.SimultaneousSet(nSimulID, 4);

                _cRollImages.Prepare(38);
                _aBytesImages = _cRollImages.PreRenderedFramesGet();
                ClearRoll(_cRollImages);
                _cRollTop.Prepare(61);
                _aBytesTop = _cRollTop.PreRenderedFramesGet();
                ClearRoll(_cRollTop);
                _cRollBot.Prepare(29);
                _aBytesBot = _cRollBot.PreRenderedFramesGet();
                ClearRoll(_cRollBot);
                _cRollMid.Prepare(23);
                _aBytesMid = _cRollMid.PreRenderedFramesGet();
                ClearRoll(_cRollMid);

                AddInitFrames(50);

                _cThreadWorker = new System.Threading.Thread(WorkerUpdater);
                _cThreadWorker.IsBackground = true;
                _cThreadWorker.Priority = System.Threading.ThreadPriority.Normal;
                _cThreadWorker.Start();

                _cThreadWorker = new System.Threading.Thread(Worker);
                _cThreadWorker.IsBackground = true;
                _cThreadWorker.Priority = System.Threading.ThreadPriority.Normal;
                _cThreadWorker.Start();

                eStatus = BTL.EffectStatus.Preparing;
                if (null != Prepared)
                    Plugin.EventSend(Prepared, this);
                (new Logger()).WriteDebug("prepare:out");
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
                eStatus = BTL.EffectStatus.Error;
            }
        }
        private void ClearRoll(Roll cRoll)
        {
            cRoll.ClearPreRenderedQueue();
            _aEffects = cRoll.EffectsGet();
            cRoll.RemoveAllEffects();
            foreach (BTL.IEffect iEff in _aEffects)
                if (iEff.eStatus == BTL.EffectStatus.Preparing || iEff.eStatus == BTL.EffectStatus.Running)
                    iEff.Stop();
        }
        private void AddInitFrames(int nQty)
        {
            int nI, nInitDur;
            nInitDur = 23;
            for (nI = 1; nI <= nInitDur; nI++)  
                _cRollImages.PreRenderedFrameAdd(_aBytesImages[nI]);
            for (nI = 1; nI <= nQty - nInitDur - (int)_cRollImages.nDelay; nI++)
                _cRollImages.PreRenderedFrameAdd(_aBytesImages[24]);
            _nImagesLoopEnd = nInitDur + (int)_cRollImages.nDelay + _cPreferences.nImagesLoopDur;
            _nImagesNextStart = 0;

            nInitDur = 14;
            for (nI = 0; nI <= nInitDur-1; nI++)  
                _cRollTop.PreRenderedFrameAdd(_aBytesTop[nI]);
            for (nI = 1; nI <= nQty - nInitDur - (int)_cRollTop.nDelay; nI++)
                _cRollTop.PreRenderedFrameAdd(_aBytesTop[14]);
            _nTopLoopEnd = nInitDur + (int)_cRollTop.nDelay + _cPreferences.nTopLoopDur;
            _bTopLoop1Now = true;

            for (nI = 0; nI <= 26; nI++)  
                _cRollBot.PreRenderedFrameAdd(_aBytesBot[nI]);
            for (nI = 1; nI <= nQty - 27 - (int)_cRollBot.nDelay; nI++)
                _cRollBot.PreRenderedFrameAdd(_aBytesBot[27]);

            for (nI = 0; nI <= 10; nI++)  
                _cRollMid.PreRenderedFrameAdd(_aBytesMid[nI]);
            for (nI = 1; nI <= nQty - 11 - (int)_cRollMid.nDelay; nI++)
                _cRollMid.PreRenderedFrameAdd(_aBytesMid[11]);

            _nAddedFramesToEveryRoll = nQty;
        }
        private void WorkerUpdater(object cState)
        {
            int nErrIndx = int.MaxValue;
            Roll cRollMid;
            List<Bytes> aBytesMid;
            while (!_bStopped)
                try
                {
                    if (null == _aBytesMidUpdated && DateTime.Now > _dtMidNextUpdate)
                    {
                        if (_cPreferences.PollUpdate())
                        {
                            cRollMid = _cPreferences.cPoll.NewRollMidGet();
                            cRollMid.Prepare(23);
                            aBytesMid = cRollMid.PreRenderedFramesGet();
                            cRollMid.ClearPreRenderedQueue();
                            cRollMid.PreRenderedFramesRegistrationsMoveTo(_cRollMid, aBytesMid);
                            lock (_oLockUpdater)
                            {
                                _aBytesMidUpdated = aBytesMid;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (nErrIndx++ > 40)
                    {
                        (new Logger()).WriteError(ex);
                        nErrIndx = 0;
                    }
                }
                finally
                {
                    Thread.Sleep(80);
                }
            _bWorkerUpdaterAborted = true;
            (new Logger()).WriteDebug("workerUpdater ended off");
        }
        private void Worker(object cState)
        {
            try
            {
                uint nLen;
                int nI, nInitDur, nDelta;
                while (true)
                {
                    if (_bStopped)
                    {
                        if (!_bStarted)
                            break;
                        if (AreAllRollsStopped())
                            break;

                        nDelta = -4;  // это если без images
                        if (_nImagesLoopEnd >= _nAddedFramesToEveryRoll)
                        {
                            nDelta = (int)_cRollImages.nPrerenderQueueCount - (int)_cRollTop.nPrerenderQueueCount;
                            while (nDelta < 0)
                            {
                                _cRollImages.PreRenderedFrameAdd(_aBytesImages[24]);
                                nDelta++;
                            }
                            for (nI = 26; nI <= 37; nI++)   //12   delay=20
                                _cRollImages.PreRenderedFrameAdd(_aBytesImages[nI]);
                        }
                        for (nI = 1; nI <= 5 + nDelta; nI++)
                            _cRollTop.PreRenderedFrameAdd(_bTopLoop1Now ? _aBytesTop[14] : _aBytesTop[29]);
                        for (nI = 13; nI >= 0; nI--)    //14  delay=13
                            _cRollTop.PreRenderedFrameAdd(_aBytesTop[nI + (_bTopLoop1Now ? 0 : 46)]);

                        for (nI = 1; nI <= 4 + nDelta; nI++)
                            _cRollBot.PreRenderedFrameAdd(_aBytesBot[27]);
                        for (nI = 26; nI >= 0; nI--)   // 27  delay=0
                            _cRollBot.PreRenderedFrameAdd(_aBytesBot[nI]);

                        for (nI = 1; nI <= 17 + nDelta; nI++)
                            _cRollMid.PreRenderedFrameAdd(_aBytesMid[11]);
                        for (nI = 10; nI >= 0; nI--)   // 11  delay=4
                            _cRollMid.PreRenderedFrameAdd(_aBytesMid[nI]);

                        while (!AreAllRollsStopped())
                        {
                            System.Threading.Thread.Sleep(80);
                        }
                        break;
                    }

                    if (_cRollTop.nPrerenderQueueCount >= _cPreferences.nRollPrerenderQueueMax && (_nImagesLoopEnd < _nAddedFramesToEveryRoll || _cRollImages.nPrerenderQueueCount >= _cPreferences.nRollPrerenderQueueMax))
                    {
                        System.Threading.Thread.Sleep(80);
                    }
                    else
                    {
                        nLen = _cRollTop.nPrerenderQueueCount;
                        if (nLen < _cPreferences.nRollPrerenderQueueMax / 2)
                            (new Logger()).WriteDebug("roll queue is less than a half = " + nLen);

                        if (_nImagesLoopEnd < _nAddedFramesToEveryRoll && _nImagesLoopEnd > _nImagesNextStart)
                        {
                            for (nI = 26; nI <= 37; nI++)
                            {
                                _cRollImages.PreRenderedFrameAdd(_aBytesImages[nI]);
                                _cRollTop.PreRenderedFrameAdd(_bTopLoop1Now ? _aBytesTop[14] : _aBytesTop[29]);
                                _cRollBot.PreRenderedFrameAdd(_aBytesBot[27]);
                                _cRollMid.PreRenderedFrameAdd(_aBytesMid[11]);
                                _nAddedFramesToEveryRoll++;
                            }
                            _nImagesNextStart = _nAddedFramesToEveryRoll + _cPreferences.nImagesInterval;
                        }
                        else if (_nImagesNextStart < _nAddedFramesToEveryRoll && _nImagesNextStart > _nImagesLoopEnd)
                        {
                            if (_cRollImages.eStatus != BTL.EffectStatus.Stopped)
                                _cRollImages.Stop();
                            _cRollImages.Idle();
                            _cRollImages.SimultaneousReset();
                            _cRollImages.nDelay = 0;
                            _cRollImages.Prepare();

                            nInitDur = 23;
                            for (nI = 1; nI <= nInitDur; nI++)
                                _cRollImages.PreRenderedFrameAdd(_aBytesImages[nI]);  // этот ролл только что стартонул, поэтому он нагоняет остальных
                            _nImagesLoopEnd = _nAddedFramesToEveryRoll + nInitDur + _cPreferences.nImagesLoopDur;
                            _cRollImages.Start();
                        }
                        else if (_nTopLoopEnd < _nAddedFramesToEveryRoll)
                        {
                            bool bImagesRun = _nImagesLoopEnd >= _nAddedFramesToEveryRoll;
                            for (nI = 16; nI <= 28; nI++) //12
                            {
                                if (bImagesRun)
                                    _cRollImages.PreRenderedFrameAdd(_aBytesImages[24]);
                                _cRollTop.PreRenderedFrameAdd(_aBytesTop[nI + (_bTopLoop1Now ? 0 : 15)]);
                                _cRollBot.PreRenderedFrameAdd(_aBytesBot[27]);
                                _cRollMid.PreRenderedFrameAdd(_aBytesMid[11]);
                                _nAddedFramesToEveryRoll++;
                            }
                            _nTopLoopEnd = _nAddedFramesToEveryRoll + _cPreferences.nTopLoopDur;
                            _bTopLoop1Now = !_bTopLoop1Now;
                        }
                        else if (bMidUpdated)
                        {
                            if (_aBytesMidOld != null)
                            {
                                _cRollMid.ForgetGotFrames(_aBytesMidOld);
                            }
                            lock (_oLockUpdater)
                            {
                                _dtMidNextUpdate = DateTime.Now.Add(_cPreferences.tsUpdateInterval);
                                _aBytesMidOld = _aBytesMid;
                                _aBytesMid = _aBytesMidUpdated;
                                _aBytesMidUpdated = null;
                            }
                            bool bImagesRun = _nImagesLoopEnd >= _nAddedFramesToEveryRoll;
                            for (nI = 13; nI <= 21; nI++)
                            {
                                if (bImagesRun)
                                    _cRollImages.PreRenderedFrameAdd(_aBytesImages[24]);
                                _cRollTop.PreRenderedFrameAdd(_bTopLoop1Now ? _aBytesTop[14] : _aBytesTop[29]);
                                _cRollBot.PreRenderedFrameAdd(_aBytesBot[27]);
                                _cRollMid.PreRenderedFrameAdd(_aBytesMid[nI]);
                                _nAddedFramesToEveryRoll++;
                            }
                        }
                        else
                        {
                            if (_nImagesLoopEnd >= _nAddedFramesToEveryRoll)
                                _cRollImages.PreRenderedFrameAdd(_aBytesImages[24]);
                            _cRollTop.PreRenderedFrameAdd(_bTopLoop1Now ? _aBytesTop[14] : _aBytesTop[29]);
                            _cRollBot.PreRenderedFrameAdd(_aBytesBot[27]);
                            _cRollMid.PreRenderedFrameAdd(_aBytesMid[11]);
                            _nAddedFramesToEveryRoll++;
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
                (new Logger()).WriteDebug("worker ended off");
            }
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
            _cRollImages.Start();
            _cRollTop.Start();
            _cRollBot.Start();
            _cRollMid.Start();
            _dtMidNextUpdate = DateTime.Now.Add(_cPreferences.tsUpdateInterval);
            eStatus = BTL.EffectStatus.Running;
            if (null != Started)
                Plugin.EventSend(Started, this);
            (new Logger()).WriteDebug("start:out");
        }
        private bool AreAllRollsStopped()
        {
            return _cRollImages.eStatus == BTL.EffectStatus.Stopped &&
                    _cRollTop.eStatus == BTL.EffectStatus.Stopped &&
                    _cRollBot.eStatus == BTL.EffectStatus.Stopped &&
                    _cRollMid.eStatus == BTL.EffectStatus.Stopped;
        }
        private void AddToAllRollsDurations(ulong nAdd)
        {
            _cRollImages.nDuration = _cRollImages.nFrameCurrent + nAdd;
            _cRollTop.nDuration = _cRollTop.nFrameCurrent + nAdd;
            _cRollBot.nDuration = _cRollBot.nFrameCurrent + nAdd;
            _cRollMid.nDuration = _cRollMid.nFrameCurrent + nAdd;
        }
        private void StopAndDisposeRolls()
        {
            if (_cRollImages.eStatus == BTL.EffectStatus.Running || _cRollImages.eStatus == BTL.EffectStatus.Preparing)
                _cRollImages.Stop();
            _cRollImages.Dispose();
            if (_cRollTop.eStatus == BTL.EffectStatus.Running || _cRollTop.eStatus == BTL.EffectStatus.Preparing)
                _cRollTop.Stop();
            _cRollTop.Dispose();
            if (_cRollBot.eStatus == BTL.EffectStatus.Running || _cRollBot.eStatus == BTL.EffectStatus.Preparing)
                _cRollBot.Stop();
            _cRollBot.Dispose();
            if (_cRollMid.eStatus == BTL.EffectStatus.Running || _cRollMid.eStatus == BTL.EffectStatus.Preparing)
                _cRollMid.Stop();
            _cRollMid.Dispose();
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
                    (new Logger()).WriteDebug("waiting for rolls stopping");
                    AddToAllRollsDurations(75);
                    DateTime dtKill = DateTime.Now.AddSeconds(10);
                    while (!AreAllRollsStopped())
                    {
                        System.Threading.Thread.Sleep(40);
                        if (DateTime.Now > dtKill)
                        {
                            (new Logger()).WriteDebug("stop:mid: break waiting");
                            break;
                        }
                    }
                    (new Logger()).WriteDebug("stop:mid: rolls stopped or must be");
                }
                else
                    System.Threading.Thread.Sleep(200);

                StopAndDisposeRolls();

                if (null!= _cThreadWorker && _cThreadWorker.IsAlive)
                {
                    _cThreadWorker.Abort();
                    //_cThreadWorker.Join();
                }
                if (null != _cThreadWorkerUpdater && _cThreadWorkerUpdater.IsAlive)
                {
                    _cThreadWorkerUpdater.Abort();
                    //_cThreadWorkerUpdater.Join();
                }
                (new Logger()).WriteDebug("stop:mid");
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
                //eStatus = BTL.EffectStatus.Error;
            }
            eStatus = BTL.EffectStatus.Stopped;
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
                return _eStatus;
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
