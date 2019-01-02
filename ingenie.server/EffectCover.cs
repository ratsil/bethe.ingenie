using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ingenie.plugins;

namespace ingenie.server
{
    class EffectCover  // чтобы приравнять обработку плагина к остальным BTL эффектам
    {
        private object oEventsLocker;
        private object oContainerEventsLocker;
        public object oEffect;
        private object oLock;
        private bool bDisposed;
		public EffectCover cEffectParrent;
		public string sType;
        public string sInfo;
        public BTL.EffectStatus eStatus
        {
            get
            {
                BTL.EffectStatus eRetVal;
                if (oEffect is BTL.Play.Effect)
                {
                    eRetVal = ((BTL.Play.Effect)oEffect).eStatus;
                }
                else if (oEffect is Plugin)
                {
                    eRetVal = ((Plugin)oEffect).eStatus;
                }
                else
                    eRetVal = BTL.EffectStatus.Idle;
                return eRetVal;
            }
        }
        public DateTime dtStatusChanged
        {
            get
            {
                DateTime dtRetVal;
                if (oEffect is BTL.Play.Effect)
                {
                    dtRetVal = ((BTL.Play.Effect)oEffect).dtStatusChanged;
                }
                else if (oEffect is Plugin)
                {
                    dtRetVal = ((Plugin)oEffect).dtStatusChanged;
                }
                else
                    dtRetVal = DateTime.MinValue;
                return dtRetVal;
            }
        }
        public int nEffectHashCode
        {
            get
            {
                if (null == oEffect)
                    return -1;
                if (oEffect is BTL.Play.Effect)
                    return ((BTL.Play.Effect)oEffect).nID;
                return oEffect.GetHashCode();
            }
        }
        public EffectCover(object cEffect)
        {
            oEffect = cEffect;
            (new Logger()).WriteDebug2("effect:cover:create [hc:" + GetHashCode() + "][effect:" + nEffectHashCode + "][type:" + cEffect.GetType() + "]");
            oEventsLocker = new object();
            oContainerEventsLocker = new object();
            oLock = new object();
            bDisposed = false;
            if (cEffect is BTL.Play.Effect)
            {
                if (cEffect is BTL.Play.Animation)
                {
                    this.sType = "animation";
                    this.sInfo = ((BTL.Play.Animation)cEffect).sFolder;
                }
                else if (cEffect is BTL.Play.Audio)
                {
                    this.sType = "audio";
                    this.sInfo = ((BTL.Play.Audio)cEffect).sFile;
                }
                else if (cEffect is BTL.Play.Clock)
                {
                    this.sType = "clock";
                    this.sInfo = ((BTL.Play.Clock)cEffect).sText;
                }
                else if (cEffect is BTL.Play.Text)
                {
                    this.sType = "text";
                    this.sInfo = ((BTL.Play.Text)cEffect).sText;
                }
                else if (cEffect is BTL.Play.Video)
                {
                    this.sType = "video";
                    this.sInfo = ((BTL.Play.Video)cEffect).sFile;
                }
                else if (cEffect is BTL.Play.Composite)
                {
                    this.sType = "composite";
                    this.sInfo = "x=" + ((BTL.Play.Composite)cEffect).stArea.nLeft + ", y=" + ((BTL.Play.Composite)cEffect).stArea.nTop;
                }
                else if (cEffect is BTL.Play.Playlist)
                {
                    this.sType = "playlist";
                    this.sInfo = "x=" + ((BTL.Play.Playlist)cEffect).cDock.cOffset.nLeft + ", y=" + ((BTL.Play.Playlist)cEffect).cDock.cOffset.nTop;
                }
                else if (cEffect is BTL.Play.Roll)
                {
                    this.sType = "roll";
                    this.sInfo = "x=" + ((BTL.Play.Roll)cEffect).stArea.nLeft + ", y=" + ((BTL.Play.Roll)cEffect).stArea.nTop;
                }
                BTL.Play.Effect cEffectBTL = (BTL.Play.Effect)cEffect;
                cEffectBTL.Prepared += new BTL.Play.Effect.EventDelegate(OnEffectPrepared);
                cEffectBTL.Started += new BTL.Play.Effect.EventDelegate(OnEffectStarted);
                cEffectBTL.Stopped += new BTL.Play.Effect.EventDelegate(OnEffectStopped);
                cEffectBTL.Failed += new BTL.Play.Effect.EventDelegate(OnEffectFailed);
                if (cEffectBTL.bContainer)
                {
                    if (cEffectBTL is BTL.Play.Roll) //UNDONE временное условие - нужно roll наследовать от container, но только после того, как уберем разделение на IAudio и IVideo
                    {
                        BTL.Play.Roll cContainer = (BTL.Play.Roll)cEffectBTL;
                        cContainer.EffectAdded += OnContainerEffectAdded;
                        cContainer.EffectPrepared += OnContainerEffectPrepared;
                        cContainer.EffectStarted += OnContainerEffectStarted;
                        cContainer.EffectStopped += OnContainerEffectStopped;
                        cContainer.EffectIsOnScreen += OnContainerEffectIsOnScreen;
                        cContainer.EffectIsOffScreen += OnContainerEffectIsOffScreen;
                        cContainer.EffectFailed += OnContainerEffectFailed;
                    }
                    else
                    {
                        BTL.Play.ContainerVideoAudio cContainer = (BTL.Play.ContainerVideoAudio)cEffectBTL;
                        cContainer.EffectAdded += OnContainerEffectAdded;
                        cContainer.EffectPrepared += OnContainerEffectPrepared;
                        cContainer.EffectStarted += OnContainerEffectStarted;
                        cContainer.EffectStopped += OnContainerEffectStopped;
                        cContainer.EffectIsOnScreen += OnContainerEffectIsOnScreen;
                        cContainer.EffectIsOffScreen += OnContainerEffectIsOffScreen;
                        cContainer.EffectFailed += OnContainerEffectFailed;
                    }
                }
            }
            else if (cEffect is Plugin)
            {
                Plugin cPlugin = (Plugin)cEffect;
                this.sType = "Plugin";
                this.sInfo = cPlugin.sFile;
                cPlugin.Prepared += new plugins.EventDelegate(OnEffectPrepared);
                cPlugin.Started += new plugins.EventDelegate(OnEffectStarted);
                cPlugin.Stopped += new plugins.EventDelegate(OnEffectStopped);
            }
            else throw new Exception("ec: неизвестный тип эффекта [hc:" + cEffect.GetHashCode() + "]"); //TODO LANG
            (new Logger()).WriteNotice("effect created: [hc:" + this.GetHashCode() + "][name:" + this.sType + "][info:" + this.sInfo + "][status:" + this.eStatus + "]");
        }

        public bool StatusIsOlderThen(int nSeconds)
        {
            lock (oEffect)
                if (dtStatusChanged.AddSeconds(nSeconds) < DateTime.Now)
                    return true;
            return false;
        }
		public bool StatusIsOlderThen(BTL.EffectStatus eStat, int nSeconds)
		{
			lock (oEffect)
				if (cEffectParrent != null && (eStat == BTL.EffectStatus.Preparing || eStat == BTL.EffectStatus.Idle) && (cEffectParrent.eStatus == BTL.EffectStatus.Preparing || cEffectParrent.eStatus == BTL.EffectStatus.Running))
					return false;
			if (eStatus == eStat && dtStatusChanged.AddSeconds(nSeconds) < DateTime.Now)
				return true;
			return false;
		}
		private void OnEffectPrepared(object cSender)
        {
            lock (oEventsLocker)
            {
                Program.OnEffectEvent(shared.EffectEventType.prepared, this);
            }
        }
        private void OnEffectStarted(object cSender)
        {
            lock (oEventsLocker)
            {
                Program.OnEffectEvent(shared.EffectEventType.started, this);
            }
        }
        private void OnEffectStopped(object cSender)
        {
            lock (oEventsLocker)
            {
                (new Logger()).WriteDebug4("effectcover: OnEffectStopped: point #1 [hc:" + GetHashCode() + "]");
                Program.OnEffectEvent(shared.EffectEventType.stopped, this);
                (new Logger()).WriteDebug4("effectcover: OnEffectStopped: point #3 [hc:" + GetHashCode() + "]");
            }
        }
        private void OnEffectFailed(object cSender)
        {
            lock (oEventsLocker)
            {
                (new Logger()).WriteDebug4("effectcover: OnEffectFailed: point #1 [hc:" + GetHashCode() + "]");
                Program.OnEffectEvent(shared.EffectEventType.failed, this);
                (new Logger()).WriteDebug4("effectcover: OnEffectFailed: point #3 [hc:" + GetHashCode() + "]");
            }
        }
        private void OnContainerEffectPrepared(BTL.Play.Effect cContainer, BTL.Play.Effect cEffect)
        {
            lock (oContainerEventsLocker)
                Program.OnContainerEvent(shared.ContainerEventType.prepared, this, Program.EffectCoverGet(cEffect));
        }
        private void OnContainerEffectStarted(BTL.Play.Effect cContainer, BTL.Play.Effect cEffect)
        {
            lock (oContainerEventsLocker)
                Program.OnContainerEvent(shared.ContainerEventType.started, this, Program.EffectCoverGet(cEffect));
        }
        private void OnContainerEffectStopped(BTL.Play.Effect cContainer, BTL.Play.Effect cEffect)
        {
            lock (oContainerEventsLocker)
                Program.OnContainerEvent(shared.ContainerEventType.stopped, this, Program.EffectCoverGet(cEffect));
        }
        private void OnContainerEffectAdded(BTL.Play.Effect cContainer, BTL.Play.Effect cEffect)
        {
            lock (oContainerEventsLocker)
                Program.OnContainerEvent(shared.ContainerEventType.added, this, Program.EffectCoverGet(cEffect));
        }
        private void OnContainerEffectIsOnScreen(BTL.Play.Effect cContainer, BTL.Play.Effect cEffect)
        {
            lock (oContainerEventsLocker)
                Program.OnContainerEvent(shared.ContainerEventType.onscreen, this, Program.EffectCoverGet(cEffect));
        }
        private void OnContainerEffectIsOffScreen(BTL.Play.Effect cContainer, BTL.Play.Effect cEffect)
        {
            lock (oContainerEventsLocker)
                Program.OnContainerEvent(shared.ContainerEventType.offscreen, this, Program.EffectCoverGet(cEffect));
        }
        private void OnContainerEffectFailed(BTL.Play.Effect cContainer, BTL.Play.Effect cEffect)
        {
            lock (oContainerEventsLocker)
                Program.OnContainerEvent(shared.ContainerEventType.failed, this, Program.EffectCoverGet(cEffect));
        }

        public void Prepare()
        {
            (new Logger()).WriteNotice("effect preparing: [hc:" + this.GetHashCode() + "][name:" + this.sType + "][info:" + this.sInfo + "][status:" + this.eStatus + "]");
            if (oEffect is BTL.Play.Effect)
            {
                ((BTL.Play.Effect)oEffect).Prepare();
            }
            if (oEffect is Plugin)
            {
                ((Plugin)oEffect).Prepare();
            }
            (new Logger()).WriteNotice("effect prepared: [hc:" + this.GetHashCode() + "][name:" + this.sType + "][info:" + this.sInfo + "][status:" + this.eStatus + "]");
        }
        public void Start()
        {
            if (oEffect is BTL.Play.Effect)
            {
                ((BTL.Play.Effect)oEffect).Start();
            }
            if (oEffect is Plugin)
            {
                ((Plugin)oEffect).Start();
            }
            (new Logger()).WriteNotice("effect started: [hc:" + this.GetHashCode() + "][name:" + this.sType + "][info:" + this.sInfo + "][status:" + this.eStatus + "]");
        }
        public void Stop()
        {
            if (oEffect is BTL.Play.Effect)
            {
                ((BTL.Play.Effect)oEffect).Stop();
            }
            if (oEffect is Plugin)
            {
                ((Plugin)oEffect).Stop();
            }
            (new Logger()).WriteNotice("effect stopped: [hc:" + this.GetHashCode() + "][name:" + this.sType + "][info:" + this.sInfo + "][status:" + this.eStatus + "]");
        }
        public void Idle()
        {
            if (oEffect is BTL.Play.Effect)
            {
                ((BTL.Play.Effect)oEffect).Idle();
            }
            if (oEffect is Plugin)
            {
                // ????
            }
            (new Logger()).WriteNotice("effect idled: [hc:" + this.GetHashCode() + "][name:" + this.sType + "][info:" + this.sInfo + "][status:" + this.eStatus + "]");
        }
        public void Dispose()
        {
            lock (oLock)
            {
                if (bDisposed)
                    return;
                bDisposed = true;
            }
            if (null == oEffect)
            {
                (new Logger()).WriteNotice("program.cs:EffectCover:Dispose [hash this: " + this.GetHashCode() + "]");
                return;
            }
            //(new Logger()).WriteNotice("program.cs:EffectCover:Dispose: part1 [hash this: " + nHash + "]");
            if (oEffect is BTL.Play.Effect)
            {
                //(new Logger()).WriteNotice("program.cs:EffectCover:Dispose: part2 [hash this: " + nHash + "]");
                BTL.Play.Effect cEffectBTL = (BTL.Play.Effect)oEffect;
                //(new Logger()).WriteNotice("program.cs:EffectCover:Dispose: part3 [hash this: " + nHash + "]");
                if (cEffectBTL.bContainer)
                {
                    //(new Logger()).WriteNotice("program.cs:EffectCover:Dispose: part4 [hash this: " + nHash + "]");
                    if (cEffectBTL is BTL.Play.Roll) //UNDONE временное условие - нужно roll наследовать от container, но только после того, как уберем разделение на IAudio и IVideo
                    {
                        //(new Logger()).WriteNotice("program.cs:EffectCover:Dispose: part5 [hash this: " + nHash + "]");
                        BTL.Play.Roll cContainer = (BTL.Play.Roll)cEffectBTL;
                        cContainer.EffectAdded -= OnContainerEffectAdded;
                        cContainer.EffectPrepared -= OnContainerEffectPrepared;
                        cContainer.EffectStarted -= OnContainerEffectStarted;
                        cContainer.EffectStopped -= OnContainerEffectStopped;
                        cContainer.EffectIsOnScreen -= OnContainerEffectIsOnScreen;
                        cContainer.EffectIsOffScreen -= OnContainerEffectIsOffScreen;
                        cContainer.EffectFailed -= OnContainerEffectFailed;
                    }
                    else
                    {
                        //(new Logger()).WriteNotice("program.cs:EffectCover:Dispose: part6 [hash this: " + nHash + "]");
                        BTL.Play.ContainerVideoAudio cContainer = (BTL.Play.ContainerVideoAudio)cEffectBTL;
                        cContainer.EffectAdded -= OnContainerEffectAdded;
                        cContainer.EffectPrepared -= OnContainerEffectPrepared;
                        cContainer.EffectStarted -= OnContainerEffectStarted;
                        cContainer.EffectStopped -= OnContainerEffectStopped;
                        cContainer.EffectIsOnScreen -= OnContainerEffectIsOnScreen;
                        cContainer.EffectIsOffScreen -= OnContainerEffectIsOffScreen;
                        cContainer.EffectFailed -= OnContainerEffectFailed;
                    }
                }
                cEffectBTL.Prepared -= OnEffectPrepared;
                cEffectBTL.Started -= OnEffectStarted;
                cEffectBTL.Stopped -= OnEffectStopped;
                cEffectBTL.Failed -= OnEffectFailed;
                cEffectBTL.Dispose();
            }
            //(new Logger()).WriteNotice("program.cs:EffectCover:Dispose: part7 [hash this: " + nHash + "]");
            if (oEffect is Plugin)
            {
                //(new Logger()).WriteNotice("program.cs:EffectCover:Dispose: part8 [hash this: " + nHash + "]");
                Plugin cPlugin = (Plugin)oEffect;
                //(new Logger()).WriteNotice("program.cs:EffectCover:Dispose: part81 [hash this: " + nHash + "]");
                cPlugin.Prepared -= new plugins.EventDelegate(OnEffectPrepared);
                //(new Logger()).WriteNotice("program.cs:EffectCover:Dispose: part82 [hash this: " + nHash + "]");
                cPlugin.Started -= new plugins.EventDelegate(OnEffectStarted);
                //(new Logger()).WriteNotice("program.cs:EffectCover:Dispose: part83 [hash this: " + nHash + "]");
                cPlugin.Stopped -= new plugins.EventDelegate(OnEffectStopped);
                //(new Logger()).WriteNotice("program.cs:EffectCover:Dispose: part84 [hash this: " + nHash + "]");
                cPlugin.Dispose();
            }
            //(new Logger()).WriteNotice("program.cs:EffectCover:Dispose: part9 [hash this: " + nHash + "]");
            (new Logger()).WriteNotice("effect disposed: [hc:" + this.GetHashCode() + "][name:" + this.sType + "][info:" + this.sInfo + "][status:" + this.eStatus + "]");
        }
    }
}
