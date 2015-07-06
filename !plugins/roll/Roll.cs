//#define DEBUG1
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
    public class Roll : MarshalByRefObject, IPlugin
    {
        private class Item
        {
            public int nID
            {
                get
                {
                    return cXmlNode.OuterXml.GetHashCode();
                }
            }
            public DateTime dt;
            public XmlNode cXmlNode;
        }
        #region Members
        private Preferences _cPreferences;
        private EventDelegate Prepared;
        private EventDelegate Started;
        private EventDelegate Stopped;
        private btl.Roll _cRoll;
        private System.Threading.Timer _cTimerRequest;
        private List<Item> _aItems;
        private ushort _nWidthOfSpace;
		private Thread _cRollFeed;
        #endregion

        public Roll()
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

                _cRoll = new btl.Roll();
                _cRoll.eDirection = _cPreferences.eDirection;
                _cRoll.nSpeed = _cPreferences.nSpeed;
                _cRoll.stArea = _cPreferences.stArea;
                _cRoll.bCUDA = _cPreferences.bRollCuda;
                _cRoll.nLayer = _cPreferences.nLayer;
                _cRoll.bOpacity = false;
                _cRoll.Prepare();

                _aItems = new List<Item>();
                _cTimerRequest = new System.Threading.Timer(TickRequest);
                TickRequest(null);
				_cRollFeed = new Thread(RollFeed);
				_cRollFeed.IsBackground = true;
				_cRollFeed.Start();

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
            _cRoll.Start();
            if (null != Started)
                Plugin.EventSend(Started, this);
        }
        private void TickRequest(object cState)
        {
            if (null == _cTimerRequest)
                return;
            try
            {
                XmlNode cData = Data.Get(_cPreferences.sRequest, _cPreferences.nTemplate, _cPreferences.sValue);
                if (null != cData)
                {
                    List<Item> aItems = cData.NodesGet("item").Select(o => new Item() { dt = DateTime.MinValue, cXmlNode = o }).ToList();
                    Item cItemNew;
                    foreach (Item cItem in _aItems)
                    {
                        if (null != (cItemNew = aItems.FirstOrDefault(o => cItem.nID == o.nID)))
                        {
                            cItemNew.dt = cItem.dt;
                            cItemNew.cXmlNode = cItem.cXmlNode;
                        }
                    }
					lock (_aItems)
						_aItems = aItems;
                }
                _cTimerRequest.Change(_cPreferences.nCheckInterval, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
        }
		private void RollFeed(object cState)
		{
			try
			{
				while (true)
				{
					if (_cPreferences.nQueueLength > _cRoll.nEffectsQty)
					{
						Item[] aItems;
						lock (_aItems)
							aItems = _aItems.ToArray();
						DateTime dtNow = DateTime.Now;
						Composite cComposite;
						ushort nHeight, nTargetHeight, nLineHeight, nPauseDuration = 0, nPreviousPauseDuration = 0;
						float nSpeed = _cPreferences.nSpeed / 25;
						uint nFramesIn = 0;
						int nDelay;
						float nMiddleRounded;
						int nCorrectionPosition = 1;     //UNDONE   Через параметры!!
						btl.Roll.Keyframe[] aKeyframes = null;

						foreach (Item cItem in aItems.OrderBy(o => o.dt).Take(_cPreferences.nQueueLength - _cRoll.nEffectsQty).ToArray())
						{
							cItem.dt = dtNow.AddSeconds(1);
							cComposite = ItemParse(cItem);
							aKeyframes = null;
							if (0 < _cPreferences.nPause)
							{

								nPreviousPauseDuration = nPauseDuration;
								nDelay = _cRoll.nEffectsQty == 0 ? 0 : 1;
								nTargetHeight = _cRoll.stArea.nHeight;

								nHeight = nLineHeight = cComposite.stArea.nHeight;
								nPauseDuration = (ushort)(_cPreferences.nPause / 40);

								nMiddleRounded = (float)Math.Round((nTargetHeight + nHeight) / 2F);
								nFramesIn = (uint)Math.Round(nMiddleRounded / nSpeed);
								aKeyframes = new btl.Roll.Keyframe[] {
									new btl.Roll.Keyframe()
									{
										eType = btl.Roll.Keyframe.Type.linear,
										nFrame = 0,
										nPosition = nTargetHeight
									},
									new btl.Roll.Keyframe()
									{
										eType = btl.Roll.Keyframe.Type.linear,
										nFrame = nFramesIn,
										nPosition = nTargetHeight - nMiddleRounded + nCorrectionPosition
									},
									new btl.Roll.Keyframe()
									{
										eType = btl.Roll.Keyframe.Type.linear,
										nFrame = nFramesIn + nPauseDuration,
										nPosition = nTargetHeight - nMiddleRounded + nCorrectionPosition
									},
									new btl.Roll.Keyframe()
									{
										eType = btl.Roll.Keyframe.Type.linear,
										nFrame = 2 * nFramesIn + nPauseDuration,
										nPosition = nTargetHeight - 2 * nMiddleRounded - 1
									}
								};
							}
							_cRoll.EffectAdd(cComposite, aKeyframes, nPauseDuration + nFramesIn / 2);
						}
					}
					Thread.Sleep(1000);
				}
			}
			catch (ThreadAbortException)
			{ }
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
        public void Stop()
        {
            try
            {
                if (null == _cTimerRequest)
                    return;
				_cRollFeed.Abort();
                _cTimerRequest.Change(Timeout.Infinite, Timeout.Infinite);
                _cTimerRequest = null;
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

        private Composite ItemParse(Item cItem)
        {
			Composite cRetVal = new Composite(_cPreferences.stArea.nWidth, Composite.Type.Vertical);
			cRetVal.bCUDA = _cPreferences[null].bCuda;
			MakeComposites(cItem.cXmlNode, null, cRetVal);
			return cRetVal;
        }
        private void MakeComposites(XmlNode cXmlNode, string sDefaultID, Composite cRetVal)
        {
            XmlNode[] aNodes = cXmlNode.NodesGet("item", false);
            string sID = cXmlNode.AttributeValueGet("id", false);
            if (null == sID)
                sID = sDefaultID;
            if (!aNodes.IsNullOrEmpty())
            {
                foreach (XmlNode cXN in aNodes)
                    MakeComposites(cXN, sID, cRetVal);
                return;
            }
            Preferences.Item.Text cPreferencesItem = (Preferences.Item.Text)_cPreferences[sID];
            string sText = cXmlNode.InnerText;
            sText = sText.RemoveNewLines();
            if (0 == _nWidthOfSpace)
                _nWidthOfSpace = (ushort)(BTL.Play.Text.Measure("SSS SSS", cPreferencesItem.cFont, 0).nWidth - BTL.Play.Text.Measure("SSSSSS", cPreferencesItem.cFont, 0).nWidth);
            List<EffectVideo> aEffects = new List<EffectVideo>();
            ushort nIdent = 0;

            Text cText = new BTL.Play.Text(sText, cPreferencesItem.cFont, cPreferencesItem.nBorderWidth) { bCUDA = false, stColor = cPreferencesItem.stColor, stColorBorder = cPreferencesItem.stColorBorder };
            ushort nMaxWidth = _cPreferences.stArea.nWidth;
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
                    cText = new BTL.Play.Text(sStr, cPreferencesItem.cFont, cPreferencesItem.nBorderWidth) { bCUDA = false, stColor = cPreferencesItem.stColor, stColorBorder = cPreferencesItem.stColorBorder };
                    Text cTextPrev = null; ;
                    int ni = 0;
                    nk = 1;
                    if (nMaxWidth < cText.stArea.nWidth)
                    {
                        while (ni + nk < sStr.Length)
                        {
                            cText = new BTL.Play.Text(sStr.Substring(ni, nk), cPreferencesItem.cFont, cPreferencesItem.nBorderWidth) { bCUDA = false, stColor = cPreferencesItem.stColor, stColorBorder = cPreferencesItem.stColorBorder };
                            while (nMaxWidth > cText.stArea.nWidth && (ni + nk < sStr.Length))
                            {
                                nk++;
                                cTextPrev = cText;
                                cText = new BTL.Play.Text(sStr.Substring(ni, nk), cPreferencesItem.cFont, cPreferencesItem.nBorderWidth) { bCUDA = false, stColor = cPreferencesItem.stColor, stColorBorder = cPreferencesItem.stColorBorder };
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

			nIdent = 0;
			for(int nIndx = 0; aEffects.Count > nIndx; nIndx++)
			{
				if (cRetVal.stArea.nWidth + aEffects[nIndx].stArea.nWidth + nIdent > nMaxWidth)
					break;
				aEffects[nIndx].stArea = new Area(aEffects[nIndx].stArea.nLeft, (short)(aEffects[nIndx].stArea.nTop + cPreferencesItem.nTopOffset), aEffects[nIndx].stArea.nWidth, aEffects[nIndx].stArea.nHeight); 
				cRetVal.EffectAdd(aEffects[nIndx], nIdent);
				nIdent = _nWidthOfSpace;
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
