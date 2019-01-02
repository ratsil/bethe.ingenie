//#define DEBUG1
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Linq;
using BTL.Play;
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
    public class RssRoll : MarshalByRefObject, IPlugin
    {
        #region Members
        private Preferences _cPreferences;
        private EventDelegate Prepared;
        private EventDelegate Started;
        private EventDelegate Stopped;
        private Roll _cRoll;
        private System.Threading.Timer _cTimerRssRequest;
        private System.Threading.Timer _cTimerRollResume;
        private int _nBuildLast;
        private Dictionary<string, DateTime> _ahItems;
        private ushort _nWidthOfSpace;
        #endregion

        public RssRoll()
        {
        }
        public void Create(string sWorkFolder, string sData)
        {
            _nBuildLast = 0;
            _cPreferences = new Preferences(sData);
        }
        public void Prepare()
        {
            try
            {
                DisCom.Init();

                _cRoll = new Roll();
                _cRoll.eDirection = _cPreferences.eDirection;
                _cRoll.nSpeed = _cPreferences.nSpeed;
                _cRoll.stArea = _cPreferences.stArea;
                _cRoll.bCUDA = _cPreferences.bRollCuda;
                _cRoll.nLayer = _cPreferences.nLayer;
                _cRoll.bOpacity = false;
                if(0 < _cPreferences.nPause)
                    _cRoll.EffectIsOffScreen += _cRoll_EffectIsOffScreen;
                _cRoll.Prepare();
                
                _ahItems = new Dictionary<string, DateTime>();
                _cTimerRollResume = new System.Threading.Timer(TickRollResume);
                _cTimerRssRequest = new System.Threading.Timer(TickRssRequest);
                TickRssRequest(null);

                if (null != Prepared)
                    Plugin.EventSend(Prepared, this);
                (new Logger()).WriteDebug3("ok");
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
        }

        void _cRoll_EffectIsOffScreen(Effect cContainer, Effect cEffect)
        {
            try
            {
                if (null != cEffect && null != cEffect.oTag)
                {
                    if (cEffect.oTag is int)
                    {
                        _cRoll.nSpeed = 0;
                        _cTimerRollResume.Change((int)cEffect.oTag, Timeout.Infinite);
                    }
                }
            }
            catch (Exception ex)   // замена пустого кетча
            {
                (new Logger()).WriteError(ex);
            }
        }
        private void TickRollResume(object cState)
        {
            _cRoll.nSpeed = _cPreferences.nSpeed;
        }
        public void Start()
        {
            _cRoll.Start();
            if (null != Started)
                Plugin.EventSend(Started, this);
        }
        private void TickRssRequest(object cState)
        {
            if (null == _cTimerRssRequest)
                return;
            try
            {
                XmlNode cNews = Data.Get("yandex.news", 0);
                if(null != cNews)
                {
                    Dictionary<string, DateTime> ahItems = cNews.NodesGet("item").Select(o => o.InnerText).ToDictionary(k => k, v => DateTime.MinValue);
                    foreach (string sItem in _ahItems.Keys.ToArray())
                        if (ahItems.ContainsKey(sItem))
                            ahItems[sItem] = _ahItems[sItem];
                    _ahItems = ahItems;
                }
				if (_cPreferences.nQueueLength > _cRoll.nEffectsQty)
				{
					DateTime dtNow = DateTime.Now;
                    ushort nMargin;
					foreach (string sItem in _ahItems.OrderBy(o => o.Value).Select(o => o.Key).Take(_cPreferences.nQueueLength - _cRoll.nEffectsQty).ToArray())
					{
						_ahItems[sItem] = dtNow.AddSeconds(1);
                        if (0 < _cPreferences.nPause)
                        {
                            foreach (Composite cLine in MakeComposites(sItem, _cPreferences.stArea.nWidth))
                            {
                                _cRoll.EffectAdd(new Composite(1, (ushort)(_cPreferences.nSpeed / 25)) { bCUDA = false, oTag = _cPreferences.nPause }); //FPS
                                nMargin = (ushort)((_cRoll.stArea.nHeight - cLine.stArea.nHeight) / 2);
                                if (0 < nMargin )
                                    _cRoll.EffectAdd(new Composite(1, nMargin) { bCUDA = false });
                                _cRoll.EffectAdd(cLine);
                                if (0 < nMargin)
                                    _cRoll.EffectAdd(new Composite(1, nMargin) { bCUDA = false });
                            }
                        }
                        else
						    _cRoll.EffectAdd(new Text(sItem, _cPreferences.cFont, _cPreferences.nBorderWidth) { bCUDA = _cPreferences.bTextCuda, stColor = _cPreferences.stColor, stColorBorder = _cPreferences.stColorBorder });
					}
				}
                _cTimerRssRequest.Change(_cPreferences.nCheckInterval, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
        }
        public void Stop()
        {
            try
            {
                if (null == _cTimerRssRequest)
                    return;
                _cTimerRssRequest.Change(Timeout.Infinite, Timeout.Infinite);
                _cTimerRssRequest = null;
                _cRoll.Stop();
                (new Logger()).WriteDebug("stop");
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
            if (null != Stopped)
                Plugin.EventSend(Stopped, this);
        }

        private List<Composite> MakeComposites(string sText, ushort nMaxWidth)
        {
            sText = sText.RemoveNewLines();
            if (0 == _nWidthOfSpace)
                _nWidthOfSpace = (ushort)(BTL.Play.Text.Measure("SSS SSS", _cPreferences.cFont, 0).nWidth - BTL.Play.Text.Measure("SSSSSS", _cPreferences.cFont, 0).nWidth);
            List<Composite> aRetVal = new List<Composite>();
            List<EffectVideo> aEffects = new List<EffectVideo>();
            ushort nIdent = 0;
            int nIndx = 0;

            Text cText = new BTL.Play.Text(sText, _cPreferences.cFont, _cPreferences.nBorderWidth) { bCUDA = false, stColor = _cPreferences.stColor, stColorBorder = _cPreferences.stColorBorder };
            if (nMaxWidth < cText.stArea.nWidth)
            {
                List<string> aSplited = new List<string>();
                int nk = 0;
                bool bLastLetter = false, bLastDigit = false; //, bLastLetter = false;
                for (int ni = 0; sText.Length > ni; ni++)
                {
                    if (0 != ni && ((!bLastDigit && !bLastLetter && Char.IsLetterOrDigit(sText[ni])) || (bLastLetter && !(Char.IsLetter(sText[ni]))) || (bLastDigit && !(Char.IsDigit(sText[ni])))))
                    {
                        aSplited.Add(sText.Substring(nk, ni - nk));
                        nk = ni;
                    }
                    bLastLetter = Char.IsLetter(sText[ni]);
                    bLastDigit = Char.IsDigit(sText[ni]);
                }
                foreach (string sStr in aSplited)
                {
                    cText = new BTL.Play.Text(sStr, _cPreferences.cFont, _cPreferences.nBorderWidth) { bCUDA = false, stColor = _cPreferences.stColor, stColorBorder = _cPreferences.stColorBorder };
                    Text cTextPrev = null; ;
                    int ni = 0;
                    nk = 1;
                    if (nMaxWidth < cText.stArea.nWidth)
                    {
                        while (ni + nk < sStr.Length)
                        {
                            cText = new BTL.Play.Text(sStr.Substring(ni, nk), _cPreferences.cFont, _cPreferences.nBorderWidth) { bCUDA = false, stColor = _cPreferences.stColor, stColorBorder = _cPreferences.stColorBorder };
                            while (nMaxWidth > cText.stArea.nWidth && (ni + nk < sStr.Length))
                            {
                                nk++;
                                cTextPrev = cText;
                                cText = new BTL.Play.Text(sStr.Substring(ni, nk), _cPreferences.cFont, _cPreferences.nBorderWidth) { bCUDA = false, stColor = _cPreferences.stColor, stColorBorder = _cPreferences.stColorBorder };
                            }
                            ni += nk - 1;
                            nk = 1;
                            if (ni + nk == sStr.Length)
                                cTextPrev = cText;
                            aEffects.Add(cTextPrev);
                        }
                    }
                    else
                        aEffects.Add(cText);
                }
            }
            else
                aEffects.Add(cText);
            Composite cTemp;
            while (nIndx < aEffects.Count)
            {
                cTemp = new Composite(nMaxWidth, Composite.Type.Vertical);
                nIdent = 0;
                while (true)
                {
                    if (cTemp.stArea.nWidth + aEffects[nIndx].stArea.nWidth + nIdent > nMaxWidth)
                        break;
                    cTemp.EffectAdd(aEffects[nIndx], nIdent);
                    nIdent = _nWidthOfSpace;
                    nIndx++;
                    if (nIndx >= aEffects.Count)
                        break;
                }
                cTemp.bCUDA = false;
                aRetVal.Add(cTemp);
            }
            return aRetVal;
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
                if (null != _cRoll)
                    return _cRoll.eStatus;
                return BTL.EffectStatus.Idle;
            }
        }
        DateTime IPlugin.dtStatusChanged
        {
            get
            {
                if (null != _cRoll)
                    return _cRoll.dtStatusChanged;
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
