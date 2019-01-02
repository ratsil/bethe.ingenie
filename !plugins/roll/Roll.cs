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
		private btl.Playlist _cPLBackground;
		private btl.Animation _cAnimIn;
		private btl.Animation _cAnimLoop;
		private btl.Animation _cAnimOut;
		private btl.Playlist _cPLMask;
		private btl.Animation _cMaskIn;
		private btl.Animation _cMaskLoop;
		private btl.Animation _cMaskOut;
		private btl.Animation _cMaskAllOff;
		private btl.Effect _cLastAddedEffect;
		private int _nWaitAndStop;
		private bool _RollFeedStop = false;
        private Area _stAreaComposite;
		private System.Threading.Timer _cTimerRequest;
		private System.Threading.Timer _cTimerStop;
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
                //PixelsMap.DisComInit();

                _cRoll = new btl.Roll();
                _cRoll.eDirection = _cPreferences.eDirection;
                _cRoll.nSpeed = _cPreferences.nSpeed;
                _cRoll.stArea = _cPreferences.stArea;
                _cRoll.stMergingMethod = _cPreferences.stRollMerging;
                _cRoll.nLayer = _cPreferences.nLayer;
                _cRoll.bOpacity = false;
				_cRoll.EffectIsOnScreen += _cRoll_EffectIsOnScreen;
				_nWaitAndStop = 0;

				if (_cPreferences.cBackground != null)
				{
					_stAreaComposite = new Area((short)(_cPreferences.stArea.nLeft - _cPreferences.cBackground.stArea.nLeft), (short)(_cPreferences.stArea.nTop - _cPreferences.cBackground.stArea.nTop), _cPreferences.stArea.nWidth, _cPreferences.stArea.nHeight);
					_cRoll.stArea = _cPreferences.cBackground.stArea;

					_cAnimIn = new Animation(_cPreferences.cBackground.sIn, 1, false);
					_cAnimIn.Prepare();
					_cAnimLoop = new Animation(_cPreferences.cBackground.sLoop, 0, true);
					_cAnimLoop.Prepare();
					_cAnimOut = new Animation(_cPreferences.cBackground.sOut, 1, false);
					_cAnimOut.Prepare();
					_cPLBackground = new Playlist();
					_cPLBackground.bStopOnEmpty = true;
                    _cPLBackground.EffectAdd(_cAnimIn);
					_cPLBackground.EffectAdd(_cAnimLoop);
					_cPLBackground.EffectAdd(_cAnimOut);
					_cPLBackground.Prepare();

					if (_cPreferences.cBackground.sMaskIn != null && _cPreferences.cBackground.sMaskIn.Length > 0)
					{
						_cMaskIn = new Animation(_cPreferences.cBackground.sMaskIn, 1, false);
						_cMaskIn.Prepare();
						_cMaskLoop = new Animation(_cPreferences.cBackground.sMaskLoop, 0, true);
						_cMaskLoop.Prepare();
						_cMaskOut = new Animation(_cPreferences.cBackground.sMaskOut, 1, false);
						_cMaskOut.Prepare();
						_cMaskAllOff = new Animation(_cPreferences.cBackground.sMaskAllOff, 0, true);
						_cMaskAllOff.Prepare();
						_cPLMask = new Playlist();
						_cPLMask.bStopOnEmpty = false;	
						_cPLMask.EffectAdd(_cMaskIn);
						_cPLMask.EffectAdd(_cMaskLoop);
						_cPLMask.EffectAdd(_cMaskOut);
						_cPLMask.EffectAdd(_cMaskAllOff);
						_cPLMask.Prepare();
					}
				}
				else
				{
					_stAreaComposite = new Area(0, 0, _cPreferences.stArea.nWidth, _cPreferences.stArea.nHeight);
                }
				_cRoll.Prepare();


				_aItems = new List<Item>();
                _cTimerRequest = new System.Threading.Timer(TickRequest);
				TickRequest(null);
				_cRollFeed = new Thread(RollFeed);
				_cRollFeed.IsBackground = true;
				_cRollFeed.Start();
				_cTimerStop = new System.Threading.Timer(AsyncStop);
                if (null != Prepared)
                    Plugin.EventSend(Prepared, this);
                (new Logger()).WriteDebug3("ok");
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
        }

		private void _cRoll_EffectIsOnScreen(Effect cSender, Effect cEffect)
		{
			if (_RollFeedStop && cEffect == _cLastAddedEffect)
			{
				_cTimerStop.Change(_cPreferences.nPause, Timeout.Infinite);
            }
		}
		private void AsyncStop(object cState)
		{
			Stop();
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
				DateTime dtFirsItem = DateTime.MinValue;
				int nLoop = 0;
				bool bFirsTime = true;
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
						int nDelay, nTransDelay = 0;
						float nMiddleRounded;
						int nCorrectionPosition = 1;     //UNDONE   Через параметры!!
						btl.Roll.Keyframe[] aKeyframes = null;
						nTransDelay = _cPreferences.nDelay / 40;

						foreach (Item cItem in aItems.OrderBy(o => o.dt).Take(_cPreferences.nQueueLength - _cRoll.nEffectsQty).ToArray())
						{
							dtNow = DateTime.Now;

							if (_cPreferences.nLoops > 0)  // т.е. тормозимся сами по окончании проигрывания набора элементов столько-то раз
							{
								if (dtFirsItem == DateTime.MinValue && aItems.Count(o => o.dt == dtFirsItem) == 1) // 1-й луп был - это последний элемент лупа
								{
									dtFirsItem = dtNow;
									nLoop = 1;
								}
								else if (dtFirsItem > DateTime.MinValue && dtFirsItem == cItem.dt)    // очередной луп был  - это последний элемент лупа
								{
									dtFirsItem = dtNow;
									nLoop++;
								}
								if (nLoop == _cPreferences.nLoops)
								{
									_RollFeedStop = true;
								}
							}

							cItem.dt = dtNow.AddSeconds(1);
							cComposite = ItemParse(cItem);
							cComposite.stArea = new Area(_stAreaComposite.nLeft, _stAreaComposite.nTop, cComposite.stArea.nWidth, cComposite.stArea.nHeight);
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
							if (bFirsTime)
							{
								bFirsTime = false;
								if (_cPLBackground != null)
								{
									btl.Roll.Keyframe[] aBackKeyframes = new BTL.Play.Roll.Keyframe[1] { new btl.Roll.Keyframe() { eType = btl.Roll.Keyframe.Type.hold, nFrame = 0, nPosition = 0 } };
									_cRoll.EffectAdd(_cPLBackground, aBackKeyframes, 0, false);
								}
								if (_cPLMask != null)
								{
									btl.Roll.Keyframe[] aMaskKeyframes = new BTL.Play.Roll.Keyframe[1] { new btl.Roll.Keyframe() { eType = btl.Roll.Keyframe.Type.hold, nFrame = 0, nPosition = 0 } };
                                    _cPLMask.cMask = new Mask() { eMaskType = DisCom.Alpha.mask_all_upper };
                                    _cRoll.EffectAdd(_cPLMask, aMaskKeyframes, 0, false);
								}
							}
							if (_cPreferences.nLoops > 0)
								_cLastAddedEffect = cComposite;
							_cRoll.EffectAdd(cComposite, aKeyframes, (ushort)((nTransDelay + nFramesIn / 2) > 0 ? (nTransDelay + nFramesIn / 2) : 0));
							if (_RollFeedStop)
								return;
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
				_RollFeedStop = true;
                _cTimerRequest.Change(Timeout.Infinite, Timeout.Infinite);
				_cTimerRequest = null;
				if (_cPLBackground != null)
					_cPLBackground.Skip(false, 0);
				if (_cPLMask != null)
					_cPLMask.Skip(false, 0);
				if (_cPLBackground != null)
					while (_cPLBackground.eStatus!= BTL.EffectStatus.Stopped)
						Thread.Sleep(5);

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
			Composite cRetVal = new Composite(_stAreaComposite.nWidth, Composite.Type.Vertical);
			cRetVal.stMergingMethod = _cPreferences[null].stMerging;
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
				_nWidthOfSpace = BTL.Play.Text.SizeOfSpaceGet(cPreferencesItem.cFont, 0).nWidth;
            List<EffectVideo> aEffects = new List<EffectVideo>();
            ushort nIdent = 0;

			Text cText;
			Text cTextSource = new BTL.Play.Text(sText, cPreferencesItem.cFont, cPreferencesItem.nBorderWidth, cPreferencesItem.stColor, cPreferencesItem.stColorBorder, cPreferencesItem.nWidthMax) { stMergingMethod = new MergingMethod() };
			ushort nTextWidth = cTextSource.stArea.nWidth;
			ushort nMaxWidth = _cPreferences.stArea.nWidth;
            if (nMaxWidth < nTextWidth)
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
                    cText = new BTL.Play.Text(sStr, cPreferencesItem.cFont, cPreferencesItem.nBorderWidth, cPreferencesItem.stColor, cPreferencesItem.stColorBorder, cPreferencesItem.nWidthMax) { stMergingMethod = new MergingMethod() };
					Text cTextPrev = null; ;
                    int ni = 0;
                    nk = 1;
                    if (nMaxWidth < nTextWidth)
                    {
                        while (ni + nk < sStr.Length)
                        {
                            cText = new BTL.Play.Text(sStr.Substring(ni, nk), cPreferencesItem.cFont, cPreferencesItem.nBorderWidth, cPreferencesItem.stColor, cPreferencesItem.stColorBorder, cPreferencesItem.nWidthMax) { stMergingMethod = new MergingMethod() };
                            while (nMaxWidth > nTextWidth && (ni + nk < sStr.Length))
                            {
                                nk++;
                                cText = new BTL.Play.Text(sStr.Substring(ni, nk), cPreferencesItem.cFont, cPreferencesItem.nBorderWidth, cPreferencesItem.stColor, cPreferencesItem.stColorBorder, cPreferencesItem.nWidthMax) { stMergingMethod = new MergingMethod() };
                            }
							if (nk == 1) // т.е. оочень слишком длинное слово - циклом не исправить уже. можно ужать пытаться в будущем, если проблема будет.
							{
								(new Logger()).WriteWarning("попалось слишком длинное слово: " + sText);
								break;
							}
                            ni += nk - 1;
                            nk = 1;
                            if (ni + nk == sStr.Length)
                                cTextPrev = cTextSource;
                            aEffects.Add(cTextPrev);
                        }
                    }
                    else
                        aEffects.Add(cTextSource);
                }
            }
            else
				aEffects.Add(cTextSource);

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
