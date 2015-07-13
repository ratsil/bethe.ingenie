using System;
using System.Collections.Generic;
using System.Linq;
using helpers;
using BTL;
using BTL.Play;
using System.IO;
using System.Threading;

namespace ingenie.plugins
{
	public class SMSChat
	{
		public EffectStatus eStatus
		{
			get
			{
				EffectStatus eRetVal = EffectStatus.Stopped;
				EffectStatus eCurrent = eRetVal;
				if (null != _cSMSRoll)
				{
					eCurrent = _cSMSRoll.eStatus;
					if (eCurrent < eRetVal)
						eRetVal = eCurrent;
				}
				if (null != _cInfoCrawl)
				{
					eCurrent = _cInfoCrawl.eStatus;
					if (eCurrent < eRetVal)
						eRetVal = eCurrent;
				}
				if (null != _cMat)
				{
					eCurrent = _cMat.eStatus;
					if (eCurrent < eRetVal)
						eRetVal = eCurrent;
				}
				return eRetVal;
			}
		}
		public DateTime dtStatusChanged
		{
			get
			{
				DateTime dtRetVal = DateTime.MinValue;
				DateTime dtCurrent = dtRetVal;
				if (null != _cSMSRoll)
				{
					dtCurrent = _cSMSRoll.dtStatusChanged;
					if (dtCurrent > dtRetVal)
						dtRetVal = dtCurrent;
				}
				if (null != _cInfoCrawl)
				{
					dtCurrent = _cInfoCrawl.dtStatusChanged;
					if (dtCurrent > dtRetVal)
						dtRetVal = dtCurrent;
				}
				if (null != _cMat)
				{
					dtCurrent = _cMat.dtStatusChanged;
					if (dtCurrent > dtRetVal)
						dtRetVal = dtCurrent;
				}
				return dtRetVal;
			}
		}
		#region Барабан
		private SMSRoll _cSMSRoll;
		private class SMSRoll
		{
			public delegate void PhotoSetupCallback(string sFile);
			public Action OnRollStandby;
			public Action OnRollShow;
			public PhotoSetupCallback OnPhotoSetup;
			public Action OnPhotoShow;
			public Action OnPhotoHide;
			public EffectStatus eStatus
			{
				get
				{
					if (null != _cRoll)
						return _cRoll.eStatus;
					return EffectStatus.Idle; //когда класс уже есть, но ролла пока нет, видимо это Idle ?  похоже да, но может это preparing?
				}
			}
			public DateTime dtStatusChanged
			{
				get
				{
					if (null != _cRoll)
						return _cRoll.dtStatusChanged;
					return DateTime.MinValue;
				}
			}

			#region Синхронизация
			private bool _bVisible;
			public bool bVisible
			{
				set
				{
					_bVisible = value;
				}
			}
			#endregion

			private Preferences.Roll _cPreferences;
			private Queue<SMS> _aqMessages;
			private Queue<SMS> _aqMessagesVIP;
			private Queue<long> _aqMessageIDsDisplayed;
			public long[] MessageIDsDisplayed
			{
				get
				{
					long[] aRetVal = null;
					try
					{
						lock (_aqMessageIDsDisplayed)
						{
							lock (_aQueuedIDs)
							{
								aRetVal = new long[_aqMessageIDsDisplayed.Count];
								int nIndx = 0;
								while (0 < _aqMessageIDsDisplayed.Count)
								{
									aRetVal[nIndx] = _aqMessageIDsDisplayed.Dequeue();
									if (_aQueuedIDs.Contains(aRetVal[nIndx]))
										if (_ahQueuedIDsDates.ContainsKey(aRetVal[nIndx]))
											(new Logger()).WriteError(new Exception("В очереди на удаление уже есть такое SMS: " + aRetVal[nIndx]));
										else
											_ahQueuedIDsDates.Add(aRetVal[nIndx], DateTime.Now.AddSeconds(20));
									nIndx++;
								}
								List<long> aToDel = new List<long>();
								foreach (long nID in _ahQueuedIDsDates.Keys)
									if (_ahQueuedIDsDates[nID] < DateTime.Now)
									{
										_aQueuedIDs.Remove(nID);
										aToDel.Add(nID);
									}
								foreach (long nID in aToDel)
									_ahQueuedIDsDates.Remove(nID);
							}
						}
					}
					catch (Exception ex)   // замена пустого кетча
					{
						(new Logger()).WriteError(ex);
					}
					return aRetVal;
				}
			}
			private Dictionary<long, DateTime> _ahQueuedIDsDates;
			private List<long> _aQueuedIDs;
			private Animation _cMaskSingle;
			private Animation _cMaskMulti;
			private Roll _cRoll;
			private Thread _cRollThread;

			public SMSRoll(Preferences.Roll cPreferences)
			{
				_cPreferences = cPreferences;
				//_cPreferences.nSpeed = 0;
				_bVisible = false;
			}
			public void Init()
			{
				_aqMessages = new Queue<SMS>();
				_aqMessagesVIP = new Queue<SMS>();
				_aqMessageIDsDisplayed = new Queue<long>();
				_aQueuedIDs = new List<long>();
				_ahQueuedIDsDates = new Dictionary<long, DateTime>();

				_cMaskSingle = new Animation(_cPreferences.sMaskPageSingle, 0, true) { stArea = new Area(_cPreferences.stSize) { nTop = 0, nLeft = 0 }, bCUDA = false };
				_cMaskMulti = new Animation(_cPreferences.sMaskPageMulti, 0, true) { stArea = new Area(_cPreferences.stSize) { nTop = 0, nLeft = 0 }, bCUDA = false };

				_cRoll = new Roll();
				_cRoll.eDirection = Roll.Direction.Up;
				_cRoll.nSpeed = _cPreferences.nSpeed;
				_cRoll.stArea = new Area(_cPreferences.stSize);
				_cRoll.bCUDA = true;
				_cRoll.nLayer = _cPreferences.nLayer;
				_cRoll.EffectIsOffScreen += _cRoll_EffectIsOffScreen;
				_cRoll.EffectIsOnScreen += _cRoll_EffectIsOnScreen;
				_bVisible = false;

			}

			public int MessagesAdd(Queue<SMS> aMessages)
			{
				int nRetVal = 0;
				try
				{
					lock (_aqMessages)
						lock (_aqMessagesVIP)
							lock (_aQueuedIDs)
								try
								{
									SMS cSMS = null;
									while (0 < aMessages.Count)
									{
										cSMS = aMessages.Dequeue();
										if (_aQueuedIDs.Contains(cSMS.ID) && (cSMS.eType != SMS.Type.Promo || cSMS.ID == -123456))
											continue;
										if (SMS.Type.Common == cSMS.eType)
											_aqMessages.Enqueue(cSMS);
										else
											_aqMessagesVIP.Enqueue(cSMS);
										nRetVal++;
										if (cSMS.eType != SMS.Type.Promo)
											_aQueuedIDs.Add(cSMS.ID);
									}
								}
								catch (Exception ex)   // замена пустого кетча
								{
									(new Logger()).WriteError(ex);
								}
				}
				catch (Exception ex)   // замена пустого кетча
				{
					(new Logger()).WriteError(ex);
				}
				return nRetVal;
			}
			public void Start()
			{
				if (null == _cRollThread)
				{
					_cRollThread = new Thread(new ThreadStart(Run));
					_cRollThread.Name = "Chat-Roll";
					_cRollThread.IsBackground = true;
					_cRollThread.Start();
				}
			}
			public void Stop()
			{
				if (null != _cRollThread)
				{
					_cRollThread.Abort();
					_cRollThread.Join();
				}
			}
			private void Run()
			{
				try
				{
					bool bStandby = true;
					_cRoll.nSpeed = _cPreferences.nSpeed;
					_cMaskSingle.Start(null);
					_cMaskMulti.Start(null);
					_cRoll.Start();
					double nStandbyMin = 10000;
					DateTime dtLastRollShow = DateTime.Now;
					while (true)
					{
						if (_bVisible)
						{
							if (!bStandby && 1 > _cRoll.nEffectsQty && 1 > _aqMessages.Count && 1 > _aqMessagesVIP.Count)
							{
								//_cRoll.nSpeed = 0;
								if (null != OnRollStandby)
									OnRollStandby();
								bStandby = true;
								dtLastRollShow = DateTime.Now;
							}
							else if (bStandby && (5 < _aqMessages.Count || 0 < _aqMessagesVIP.Count) && DateTime.Now.Subtract(dtLastRollShow).TotalMilliseconds > nStandbyMin)
							{
								if (null != OnRollShow)
									OnRollShow();
								bStandby = false;
							}
							if (!bStandby && 7 > _cRoll.nEffectsQty)
								MessagesAddToRoll();
						}
						Thread.Sleep(1000);
					}
				}
				catch (Exception exx)
				{
					try
					{
						_cRoll.Stop();
						_cRoll.Dispose();
						_cRoll = null;
					}
					catch (Exception ex)   // замена пустого кетча
					{
						(new Logger()).WriteError(ex);
					}
					if (!(exx is ThreadAbortException))
						(new Logger()).WriteWarning("roll worker stopped exx=" + exx.Message + "<br>" + exx.StackTrace);
				}
			}
			private void MessagesAddToRoll()
			{
				try
				{
					SMS cSMS = null;
					Queue<SMS> aqMessages = null;
					if (0 < _aqMessagesVIP.Count)
						aqMessages = _aqMessagesVIP;
					else
						aqMessages = _aqMessages;

					Queue<Composite> aqLines;
					Composite cLine;
					ushort nHeight, nTargetHeight, nLineHeight, nLineNextHeight = 0, nLinePreviousHeight, nPauseDuration = 0, nPreviousPauseDuration = 0;
					float nSpeed = _cPreferences.nSpeed / 25;
					int nLinesQty;
					int nIndex;
					uint nFramesIn = 0, nFrameTr = 0, nFrameOut = 0;
					int nDelay;
					float nMiddleRounded;
					float nMiddlePreviousRounded;
					int nCorrectionPosition = _cPreferences.nCorrectPosition;
					int nCorrectionDelay = _cPreferences.nCorrectDelay;
					ushort nPauseDurMax = (ushort)(25 * _cPreferences.nSecondsPerLine * 2 - 1);
					long nDeltaFrames;
					Animation cMask = _cMaskSingle;
					Roll.Keyframe[] aKeyframes = null;
					long nKeyFrame;

					lock (aqMessages)
					{
						while (0 < aqMessages.Count)
						{
							cSMS = aqMessages.Dequeue();
							if (SMS.Type.Photo == cSMS.eType && null != OnPhotoSetup)
								OnPhotoSetup("photos/" + cSMS.Phone + ".jpg");

							aqLines = new Queue<Composite>(cSMS.RollItemsGet((ushort)_cPreferences.stSize.Width));
							if (0 < (nLinesQty = aqLines.Count))
							{
								nPreviousPauseDuration = nPauseDuration;
								nDelay = _cRoll.nEffectsQty == 0 ? 0 : 1;
								nIndex = 1;
								nTargetHeight = _cRoll.stArea.nHeight;
								cMask = (2 < nLinesQty ? _cMaskMulti : _cMaskSingle);
								cLine = aqLines.Dequeue();
								cLine.iMask = cMask;
								cLine.nMaxOpacity = cSMS.cPreferences.nOpacity;
								cLine.nInDissolve = cSMS.cPreferences.nInDissolve;
								aKeyframes = new Roll.Keyframe[4];

								nHeight = nLineHeight = cLine.stArea.nHeight;

								nPauseDuration = (ushort)(25 * _cPreferences.nSecondsPerLine * (2 == nLinesQty ? 2 : 1));
								if (1 < nLinesQty)
									nHeight += (nLineNextHeight = aqLines.Peek().stArea.nHeight);

								// первая строка
								nMiddleRounded = (float)Math.Round((nTargetHeight + nHeight) / 2F);
								nFramesIn = (uint)Math.Round(nMiddleRounded / nSpeed);
								aKeyframes[0] = new Roll.Keyframe()
								{
									eType = Roll.Keyframe.Type.linear,
									nFrame = 0,
									nPosition = nTargetHeight
								};
								aKeyframes[1] = new Roll.Keyframe()
								{
									eType = Roll.Keyframe.Type.linear,
									nFrame = nFramesIn,
									nPosition = nTargetHeight - nMiddleRounded + nCorrectionPosition
								};
								aKeyframes[2] = new Roll.Keyframe()
								{
									eType = Roll.Keyframe.Type.linear,
									nFrame = nFramesIn + nPauseDuration,
									nPosition = nTargetHeight - nMiddleRounded + nCorrectionPosition
								};
								aKeyframes[3] = new Roll.Keyframe()
								{
									eType = Roll.Keyframe.Type.linear,
									nFrame = 2 * nFramesIn + nPauseDuration,
									nPosition = nTargetHeight - 2 * nMiddleRounded - 1
								};

								_cRoll.EffectAdd(cLine, aKeyframes, (uint)(nDelay * (nFramesIn + (nPreviousPauseDuration == 0 ? nPauseDurMax : nPreviousPauseDuration) + nCorrectionDelay)));

								while (0 < aqLines.Count)
								{
									nIndex++;
									cLine = aqLines.Dequeue();
									cLine.iMask = cMask;
									cLine.nMaxOpacity = cSMS.cPreferences.nOpacity;
									if (2 < nLinesQty && 0 < aqLines.Count)
									{
										nLinePreviousHeight = nLineHeight;
										nLineHeight = nLineNextHeight;
										nLineNextHeight = aqLines.Peek().stArea.nHeight;

										nMiddlePreviousRounded = nMiddleRounded;
										nMiddleRounded = (float)Math.Round((nTargetHeight + nLineHeight + nLineNextHeight) / 2f);
										nFramesIn = (uint)Math.Round((nMiddlePreviousRounded - nLinePreviousHeight) / nSpeed);
										nFrameTr = (uint)Math.Round((nMiddleRounded - (nMiddlePreviousRounded - nLinePreviousHeight)) / nSpeed);
										nFrameOut = (uint)Math.Round((nMiddleRounded + nCorrectionPosition) / nSpeed);

										if (2 == nIndex)
										{ // вторая строка, когда их больше двух
											nDeltaFrames = GapAppearingFrameGet(aKeyframes, nLinePreviousHeight) - 1;
											Roll.Keyframe[] aCurve = Roll.Keyframe.CopyCurve(aKeyframes);
											uint nFrameInFirst = (uint)(aCurve[3].nFrame - nPauseDuration) / 2;
											Roll.Keyframe.ChangeFramesInCurve(aCurve, -nDeltaFrames);
											Roll.Keyframe.ChangePointsInCurve(aCurve, nLineHeight);
											aCurve[3].nPosition = nTargetHeight - nMiddleRounded + nCorrectionPosition;
											aCurve[3].nFrame = aCurve[2].nFrame + nFrameTr;
											Array.Resize<Roll.Keyframe>(ref aCurve, 6);
											aCurve[4] = new Roll.Keyframe()
											{
												eType = Roll.Keyframe.Type.linear,
												nFrame = nKeyFrame = aCurve[3].nFrame + nPauseDuration,
												nPosition = nTargetHeight - nMiddleRounded + nCorrectionPosition
											};
											aCurve[5] = new Roll.Keyframe()
											{
												eType = Roll.Keyframe.Type.linear,
												nFrame = nKeyFrame + nFrameOut,
												nPosition = nTargetHeight - 2 * nMiddleRounded
											};
											_cRoll.EffectAdd(cLine, aCurve);
											aKeyframes = aCurve;
										}
										else
										{ // начиная с третьей строки, но кроме последней
											nDeltaFrames = GapAppearingFrameGet(aKeyframes, nLinePreviousHeight) - 1;
											aKeyframes = new Roll.Keyframe[] {
												new Roll.Keyframe()
												{
													eType = Roll.Keyframe.Type.linear,
													nFrame = 0,
													nPosition = nTargetHeight
												},
												new Roll.Keyframe() {
													eType = Roll.Keyframe.Type.linear,
													nFrame = nKeyFrame = nFramesIn,
													nPosition = nTargetHeight - nMiddlePreviousRounded + nLinePreviousHeight + nCorrectionPosition
												},
												new Roll.Keyframe()
												{
													eType = Roll.Keyframe.Type.linear,
													nFrame = (nKeyFrame += nPauseDuration),
													nPosition = nTargetHeight - nMiddlePreviousRounded + nLinePreviousHeight + nCorrectionPosition
												},
												new Roll.Keyframe()
												{
													eType = Roll.Keyframe.Type.linear,
													nFrame = (nKeyFrame += nFrameTr),
													nPosition = nTargetHeight - nMiddleRounded + nCorrectionPosition
												},
												new Roll.Keyframe()
												{
													eType = Roll.Keyframe.Type.linear,
													nFrame = (nKeyFrame += nPauseDuration),
													nPosition = nTargetHeight - nMiddleRounded + nCorrectionPosition
												},
												new Roll.Keyframe()
												{
													eType = Roll.Keyframe.Type.linear,
													nFrame = nKeyFrame + nFrameOut,
													nPosition = nTargetHeight - 2 * nMiddleRounded
												}
											};
											_cRoll.EffectAdd(cLine, aKeyframes, (uint)(nFramesIn - nDeltaFrames + nPauseDuration - (nFramesIn - nFrameTr)));
										}
									}

									if (0 == aqLines.Count)
									{
										if (2 == nIndex)
										{ // вторая и она же последняя
											long nDelta = GapAppearingFrameGet(aKeyframes, nLineHeight) - 1;
											Roll.Keyframe[] aCurve = Roll.Keyframe.CopyCurve(aKeyframes);
											Roll.Keyframe.ChangeFramesInCurve(aCurve, -nDelta);
											Roll.Keyframe.ChangePointsInCurve(aCurve, nLineHeight);
											_cRoll.EffectAdd(cLine, aCurve);
										}
										else
										{ // последняя, когда их больше двух
											Roll.Keyframe cKF4 = aKeyframes[4];
											Roll.Keyframe cKF5 = aKeyframes[5];

											nDeltaFrames = GapAppearingFrameGet(aKeyframes, nLineHeight) - 1;
											aKeyframes = new Roll.Keyframe[] {
												new Roll.Keyframe()
												{
													eType = Roll.Keyframe.Type.linear,
													nFrame = 0,
													nPosition = nTargetHeight
												},
												new Roll.Keyframe()
												{
													eType = Roll.Keyframe.Type.linear,
													nFrame = nFramesIn,
													nPosition = nTargetHeight - nMiddleRounded + nLineHeight + nCorrectionPosition
												},
												new Roll.Keyframe()
												{
													eType = Roll.Keyframe.Type.linear,
													nFrame = nFramesIn + nPauseDuration,
													nPosition = nTargetHeight - nMiddleRounded + nLineHeight + nCorrectionPosition
												},
												new Roll.Keyframe()
												{
													eType = Roll.Keyframe.Type.linear,
													nFrame = nFramesIn + nPauseDuration + (cKF5.nFrame - cKF4.nFrame),
													nPosition = nTargetHeight - nMiddleRounded + nLineHeight + nCorrectionPosition + (cKF5.nPosition - cKF4.nPosition)
												}
											};
											_cRoll.EffectAdd(cLine, aKeyframes, (uint)(nFramesIn - nDeltaFrames + nPauseDuration - (nFramesIn - nFrameTr)));
										}
									}
								}
								Composite cSpace = new Composite(1, 1)
								{
									bCUDA = false,
									oTag = cSMS
								};
								_cRoll.EffectAdd(cSpace, true);
							}
							cSMS = null;
						}
					}
				}
				catch (Exception ex)
				{
					(new Logger()).WriteError(ex);
				}
			}
			private long GapAppearingFrameGet(Roll.Keyframe[] aCurveFirstLine, int nHeightFirstLine)
			{ // отдаст кадр момента появления зазора при выезде строки в роле
				for (long ni = aCurveFirstLine[0].nFrame; ni <= aCurveFirstLine[aCurveFirstLine.Length - 1].nFrame; ni++)
				{
					if (Roll.Keyframe.CalculateCurvesPoint(aCurveFirstLine, ni) < _cRoll.stArea.nHeight - nHeightFirstLine - 1)
						return ni;
				}
				return 0;
			}
			private void DoneHandler()
			{
			}
			void _cRoll_EffectIsOffScreen(Effect cContainer, Effect cEffect)
			{
				try
				{
					if (null != cEffect && null != cEffect.oTag)
					{
						SMS cSMS = null;
						if (cEffect.oTag is SMS)
						{
							cSMS = (SMS)cEffect.oTag;
							lock (_aqMessageIDsDisplayed)
							{
								(new Logger()).WriteDebug3("SMS displayed: " + cSMS.ID);
								_aqMessageIDsDisplayed.Enqueue(cSMS.ID);
								cSMS.Dispose();
							}
							if (SMS.Type.Photo == cSMS.eType && null != OnPhotoHide)
								OnPhotoHide();
						}
						if (cEffect is Composite)
						{
							Composite cC = (Composite)cEffect;
							if (cC.eStatus == EffectStatus.Running)
								cC.Stop();
							cC.Dispose();
							(new Logger()).WriteDebug3("composite disposing: " + cEffect.GetHashCode());
						}
					}
				}
				catch (Exception ex)   // замена пустого кетча
				{
					(new Logger()).WriteError(ex);
				}
			}
			void _cRoll_EffectIsOnScreen(Effect cContainer, Effect cEffect)
			{
			}
		}
		public long[] MessageIDsDisplayed
		{
			get
			{
				if (null != _cSMSRoll)
				{
					long[] aMID = _cSMSRoll.MessageIDsDisplayed;
					if (null != aMID)
						QueueLength -= aMID.Length;
					return aMID;
				}
				else
					return null;
			}
		}
		#endregion

		#region Бегущая строка
		private Crawl _cInfoCrawl;
		private class Crawl
		{
			private Roll _cInfoCrawl;
			private Text _cCrawlText;
			private int _nTodayTimes;
			private Preferences.BroadcastType _eBroadcastType;
			public EffectStatus eStatus
			{
				get
				{
					if (null != _cInfoCrawl)
						return _cInfoCrawl.eStatus;
					return EffectStatus.Idle; //когда класс уже есть, но ролла пока нет, видимо это Idle ?  похоже да, но может это preparing?
				}
			}
			public DateTime dtStatusChanged
			{
				get
				{
					if (null != _cInfoCrawl)
						return _cInfoCrawl.dtStatusChanged;
					return DateTime.MinValue;
				}
			}
			private Preferences.Crawl _cPreferences;

			public Crawl(Preferences.Crawl cPreferences, Preferences.BroadcastType eBroadcastType)
			{
				_eBroadcastType = eBroadcastType;
				_cPreferences = cPreferences;
			}

			public void Init()
			{
				string sString = GetInfoSMSs();
				if (1 > sString.Length)
				{
					(new Logger()).WriteError("Init:GetInfoSMSs = ''");
					return;
				}
				lock (_cInfoCrawl = new Roll())
				{
					_cInfoCrawl.eDirection = Roll.Direction.Left;
					_cInfoCrawl.stArea = new helpers.Area(_cPreferences.stSize);
					_cCrawlText = new Text(sString, _cPreferences.cFont, _cPreferences.nBorderWidth);
					_cCrawlText.stColor = _cPreferences.stColor;
					_cCrawlText.stColorBorder = _cPreferences.stBorderColor;
					_cCrawlText.bCUDA = false;
					_cInfoCrawl.nSpeed = _cPreferences.nSpeed;
					_cInfoCrawl.nLayer = _cPreferences.nLayer;

					_cInfoCrawl.EffectIsOffScreen += new ContainerVideoAudio.EventDelegate(_cInfoCrawl_EffectIsOffScreen);
					_cInfoCrawl.EffectAdd(_cCrawlText);
					_cInfoCrawl.Prepare();
				}
			}

			void _cInfoCrawl_EffectIsOffScreen(Effect cSender, Effect cEffect)
			{
				if (cEffect is Text && ((Text)cEffect).sText.StartsWith(_cPreferences.sRegistryInfo))
				{
					if (_eBroadcastType == Preferences.BroadcastType.live && !(new DBInteract()).IsThereAnyStartedLiveBroadcast())
						return;
					(new Logger()).WriteNotice("reg_info finished:" + _cPreferences.sRegistryInfo);
					File.WriteAllText(_cPreferences.sInfoStringNight, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " \t" + _nTodayTimes);
				}
				File.WriteAllText(_cPreferences.sCrowlLastStart, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
				(new Logger()).WriteNotice("info_crawl finished.");
				if (null == _cPreferences.aStartPoints || 1 > _cPreferences.aStartPoints.Length)
				{
					_cCrawlText.Stop();
					_cCrawlText.Idle();
					//_cInfoCrawl.EffectAdd(_cCrawlText);
				}
			}
			public void Start()
			{
#if DEBUG
				//return;  //DNF
#endif
				if (null != _cInfoCrawl)
					lock (_cInfoCrawl)
						_cInfoCrawl.Start();
			}
			public void Stop()
			{
				if (null != _cInfoCrawl)
				{
					lock (_cInfoCrawl)
					{
						if (_cInfoCrawl.eStatus == EffectStatus.Preparing || _cInfoCrawl.eStatus == EffectStatus.Running)
							_cInfoCrawl.Stop();
						_cInfoCrawl = null;
					}
				}
			}
			private string GetInfoSMSs()
			{
				DateTime dtRegistryLastPlayed = DateTime.MinValue;
				DateTime dtCrawlLastPlayed = DateTime.MinValue;
				DateTime dtNow = DateTime.Now;
				_nTodayTimes = 0;
				string[] aValues = { "", "" };
				string sInfo = "";
				try
				{
					if (File.Exists(_cPreferences.sCrowlLastStart))   // реализация правила, что строка идет в часе только после минут, указанных в префах (starts)
					{
						string[] aLines = File.ReadAllLines(_cPreferences.sCrowlLastStart);
						dtCrawlLastPlayed = DateTime.Parse(aLines[0]);
					}
					if (null == _cPreferences.aStartPoints || 1 > _cPreferences.aStartPoints.Length || dtCrawlLastPlayed == DateTime.MinValue || IsItFirstStart(dtCrawlLastPlayed, dtNow))
					{
						(new Logger()).WriteDebug3("crawl can start now. [last_start=" + dtCrawlLastPlayed.ToString("yyyy-MM-dd HH:mm:ss") + "]");
					}
					else
						return sInfo;

					sInfo = _cPreferences.sText ?? "";

					if (File.Exists(_cPreferences.sInfoStringNight))   // реализация правила, что строка с лицензией канала идет только раз в 5 часов.
					{
						string[] aLines = File.ReadAllLines(_cPreferences.sInfoStringNight);
						aValues = aLines[0].Split(('\t'));
						dtRegistryLastPlayed = DateTime.Parse(aValues[0]);
						_nTodayTimes = int.Parse(aValues[1]);
					}
					if (dtRegistryLastPlayed == DateTime.MinValue || 5 <= dtNow.Subtract(dtRegistryLastPlayed).TotalHours)
					{
						string sRegistryInfo = _cPreferences.sRegistryInfo + "     ";
						sInfo = sRegistryInfo + sInfo;
						if (dtRegistryLastPlayed.Date != dtNow.Date)
							_nTodayTimes = 0;
						_nTodayTimes++;
						(new Logger()).WriteNotice("reg_info prepared");
					}
				}
				catch (Exception ex)
				{
					(new Logger()).WriteError(ex);
				}
				return sInfo;
			}
			private bool IsItFirstStart(DateTime dtLast, DateTime dtCurrent)
			{
				if (1 > _cPreferences.aStartPoints.Length)
					return false;
				DateTime dtMin = dtCurrent;
				while (true)
				{
					if (dtMin < dtLast)
						return false;
					if (_cPreferences.aStartPoints.Contains((byte)dtMin.Minute))
						return true;
					dtMin = dtMin.Subtract(new TimeSpan(0, 1, 0));
				}
			}
		}
		#endregion

		#region Подложка
		private Mat _cMat;
		private class Mat
		{
			private Preferences.Mat _cPreferences;
			private Animation _cShowLoop;
			private Animation _cShowOut;
			private Animation _cShowTransition;
			private Animation _cStandbyLoop;
			private Animation _cStandbyOut;
			private Animation _cStandbyTransition;
			private Animation _cMatOut;
			private Playlist _cPlaylistBadge;
			private Playlist _cPlaylistShow;
			public bool bStopped;

			public EffectStatus eStatus
			{
				get
				{
					if (null == _cPlaylistBadge)
						return EffectStatus.Idle;
					return _cPlaylistBadge.eStatus;
				}
			}
			public DateTime dtStatusChanged
			{
				get
				{
					if (null == _cPlaylistBadge)
						return DateTime.MinValue;
					return _cPlaylistBadge.dtStatusChanged;
				}
			}
			public Action OnStop;

			public Mat(Preferences.Mat cPreferences)
			{
				_cPreferences = cPreferences;
			}

			public void Init()
			{
				Animation cBadgeIn = new Animation(_cPreferences.sBadgeIn, 1, false);
				if (0 == _cPreferences.stBadgeSize.Width)
					_cPreferences.stBadgeSize.Width = (int)cBadgeIn.stArea.nWidth;
				if (0 == _cPreferences.stBadgeSize.Height)
					_cPreferences.stBadgeSize.Height = (int)cBadgeIn.stArea.nHeight;

				_cPlaylistBadge = new Playlist();
				_cPlaylistBadge.nLayer = _cPreferences.nBadgeLayer;
				_cPlaylistBadge.bStopOnEmpty = true;
				_cPlaylistBadge.OnPlaylistIsEmpty += OnPlaylistIsEmpty;
				_cPlaylistBadge.stArea = new Area(_cPreferences.stBadgeSize);
				_cPlaylistBadge.EffectAdd(cBadgeIn, 0);
				_cPlaylistBadge.EffectAdd(new Animation(_cPreferences.sBadgeLoop, 0, true), 0);
				_cPlaylistBadge.EffectAdd(new Animation(_cPreferences.sBadgeOut, 1, false), 0);
				_cPlaylistBadge.Prepare();


				_cShowLoop = new Animation(_cPreferences.sShowLoop, 0, true);
				if (0 == _cPreferences.stShowSize.Width)
					_cPreferences.stShowSize.Width = (int)_cShowLoop.stArea.nWidth;
				if (0 == _cPreferences.stShowSize.Height)
					_cPreferences.stShowSize.Height = (int)_cShowLoop.stArea.nHeight;
				_cShowOut = new Animation(_cPreferences.sShowOut, 1, false);
				_cShowTransition = new Animation(_cPreferences.sShowTransition, 1, true);

				_cStandbyLoop = new Animation(_cPreferences.sStandbyLoop, 0, true);
				_cStandbyTransition = new Animation(_cPreferences.sStandbyTransition, 1, true);
				_cStandbyOut = new Animation(_cPreferences.sStandbyOut, 1, false);

				_cMatOut = new Animation(_cPreferences.sOut, 1, false);
				_cMatOut.stArea = new Area(_cPreferences.stOutSize);
				_cMatOut.nLayer = _cPreferences.nOutLayer;
				_cMatOut.Prepare();

				Animation cStandbyIn = new Animation(_cPreferences.sStandbyIn, 1, false);
				cStandbyIn.Prepare();
				_cStandbyLoop.Prepare();
				_cPlaylistShow = new Playlist();
				_cPlaylistShow.nLayer = _cPreferences.nShowLayer;
				_cPlaylistShow.bStopOnEmpty = true;
				_cPlaylistShow.OnPlaylistIsEmpty += OnPlaylistIsEmpty;
				_cPlaylistShow.stArea = new Area(_cPreferences.stShowSize);
				_cPlaylistShow.EffectAdd(cStandbyIn, 0);
				_cPlaylistShow.EffectAdd(_cStandbyLoop, 0);
				_cPlaylistShow.Prepare();
				_cStandbyOut.Prepare();
				_cShowOut.Prepare();
				_cStandbyTransition.Prepare();
				_cShowTransition.Prepare();
			}
			public void Start()
			{
				(new Logger()).WriteDebug3("in");
				if (EffectStatus.Running == _cPlaylistShow.eStatus)
				{
					lock (_cMatOut)
					{
						if (bStopped)
							return;

						while (EffectStatus.Running != _cStandbyLoop.eStatus)
							Thread.Sleep(10);

						if (_cStandbyTransition.eStatus != EffectStatus.Idle)
						{
							if (_cStandbyTransition.eStatus != EffectStatus.Stopped)
								_cStandbyTransition.Stop();
							_cStandbyTransition.Idle();
						}
						_cStandbyTransition.Prepare();
						if (_cShowLoop.eStatus != EffectStatus.Idle)
						{
							if (_cShowLoop.eStatus != EffectStatus.Stopped)
								_cShowLoop.Stop();
							_cShowLoop.Idle();
						}
						_cShowLoop.Prepare();

						_cPlaylistShow.EffectAdd(_cStandbyTransition, 0);
						_cPlaylistShow.EffectAdd(_cShowLoop, 0);

						_cPlaylistShow.Skip(false, 0);
						while (EffectStatus.Running != _cShowLoop.eStatus)
							Thread.Sleep(10);
					}
				}
				else
				{
					_cPlaylistBadge.Start();
					_cPlaylistShow.Start();
				}
			}
			public void Standby()
			{
				(new Logger()).WriteDebug3("in");
				while (EffectStatus.Running != _cShowLoop.eStatus)
					Thread.Sleep(10);
				lock (_cMatOut)
				{
					if (bStopped)
						return;

					if (_cShowTransition.eStatus != EffectStatus.Idle)
					{
						if (_cShowTransition.eStatus != EffectStatus.Stopped)
							_cShowTransition.Stop();
						_cShowTransition.Idle();
					}
					_cShowTransition.Prepare();
					if (_cStandbyLoop.eStatus != EffectStatus.Idle)
					{
						if (_cStandbyLoop.eStatus != EffectStatus.Stopped)
							_cStandbyLoop.Stop();
						_cStandbyLoop.Idle();
					}
					_cStandbyLoop.Prepare();

					_cPlaylistShow.EffectAdd(_cShowTransition, 0);
					_cPlaylistShow.EffectAdd(_cStandbyLoop, 0);

					_cPlaylistShow.Skip(false, 0);
					while (EffectStatus.Running != _cStandbyLoop.eStatus)
						Thread.Sleep(10);
				}
			}
			public void Stop()
			{
				(new Logger()).WriteDebug3("in");
				int nIndx = 0;
				while (true)
				{
					if (EffectStatus.Running == _cStandbyLoop.eStatus && EffectStatus.Running != _cShowLoop.eStatus)
						_cPlaylistShow.EffectAdd(_cStandbyOut, 0);
					else if (EffectStatus.Running != _cStandbyLoop.eStatus && EffectStatus.Running == _cShowLoop.eStatus)
						_cPlaylistShow.EffectAdd(_cShowOut, 0);
					else
					{
						Thread.Sleep(10);
						nIndx++;
						if (nIndx > 200)
						{
							(new Logger()).WriteNotice("breake by timeout");
							break;
						}
						continue;
					}
					break;
				}
				try
				{
					lock (_cMatOut)
					{
						(new Logger()).WriteDebug3("mat_out_begin: [show_qty=" + _cPlaylistShow.nEffectsQty + "][badge_qty=" + _cPlaylistBadge.nEffectsQty + "]");
						bStopped = true;
						_cMatOut.Start();
						_cPlaylistShow.nDuration = _cPlaylistShow.nFrameCurrent + _cShowOut.nDuration + _cStandbyOut.nDuration; // гарантия ухода чата до конца
						_cPlaylistShow.Skip(true, 0);
						_cPlaylistBadge.nDuration = _cPlaylistBadge.nFrameCurrent + 20;  // гарантия ухода чата до конца
						_cPlaylistBadge.Skip(true, 0, 18);
					}
				}
				catch (Exception ex)
				{
					(new Logger()).WriteError(ex);
				}
			}
			void OnPlaylistIsEmpty(Playlist cPlaylist)
			{
				(new Logger()).WriteDebug3("in");
				if (null != OnStop)
					OnStop();
				(new Logger()).WriteDebug4("return");
			}
		}
		#endregion


		ManualResetEvent _mreInfoOnAir;
		ManualResetEvent _mreChatOnAir;
		ManualResetEvent _mreChatSetuping;

		private int _nQueueLength;
		public int QueueLength
		{
			set
			{
				if (0 > value)
					value = 0;
				if (value != _nQueueLength)
				{
					_nQueueLength = value;
					//if (-1 < _nQueueLength)
					//    QueueCountShow();
					//else
					//    QueueCountHide();
				}
			}
			get
			{
				return _nQueueLength;
			}
		}

		public bool bReleased { get; private set; }
		public bool IsChatOnAir { get; private set; }
		public bool IsInfoOnAir
		{
			get
			{
				return !_mreInfoOnAir.WaitOne(0, false);
				//return _bIsChatOnAir;
			}
		}
		public bool IsChatSetuping
		{
			get
			{
				return !_mreChatSetuping.WaitOne(0, false);
			}
		}
		public bool IsChatTerminating { get; set; }

		private Transition _cPhotoTrans = null;
		private Preferences _cPreferences;

		public SMSChat(Preferences cPreferences)
		{
			_cPreferences = cPreferences;
		}

		public void Init()
		{
			(new Logger()).WriteDebug3("in");
			_cMat = new Mat(_cPreferences.cMat) { OnStop = MatStopped };
			_cSMSRoll = new SMSRoll(_cPreferences.cRoll) { OnRollStandby = _cMat.Standby, OnRollShow = _cMat.Start };
			_cInfoCrawl = new Crawl(_cPreferences.cCrawl, _cPreferences.eBroadcastType);
			bReleased = false;
			_mreChatOnAir = new ManualResetEvent(true);
			_mreInfoOnAir = new ManualResetEvent(true);
			_mreChatSetuping = new ManualResetEvent(true);
			_cSMSRoll.Init();
			QueueLength = 0;
			_cInfoCrawl.Init();
			_cMat.Init();

			(new Logger()).WriteDebug4("return");
		}
		public void Release()
		{
			try
			{
				if (null != _cSMSRoll)
				{
					_cSMSRoll.Stop();
					_cSMSRoll = null;
				}
			}
			catch (Exception ex)   // замена пустого кетча
			{
				(new Logger()).WriteError(ex);
			}
			bReleased = true;
			(new Logger()).WriteDebug4("return [bReleased = true]");
		}

		public void MessagesAdd(Queue<SMS> aMessages)
		{
			QueueLength += _cSMSRoll.MessagesAdd(aMessages);
		}

		public void Start()
		{
			_cSMSRoll.Start();
			_mreInfoOnAir.Reset();

			_cMat.Start();
			if (null != _cInfoCrawl)
				_cInfoCrawl.Start();

			_cSMSRoll.bVisible = true;
			(new Logger()).WriteDebug2("SMSChat: ShowInfo: ok");
		}
		public void Pause()
		{

		}
		public void Stop()
		{
			(new Logger()).WriteDebug3("in");
			IsChatTerminating = true;
			_cMat.Stop();
			try
			{
				_cSMSRoll.Stop();
				if (IsChatOnAir)
					_mreChatOnAir.Set();
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			try
			{
				_cInfoCrawl.Stop();
				_cInfoCrawl = null;
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			GC.Collect();
			(new Logger()).WriteDebug4("return");
		}

		private void MatStopped()
		{
			(new Logger()).WriteDebug3("in");
			_mreInfoOnAir.Set();
			IsChatTerminating = false;
			IsChatOnAir = false;
			(new Logger()).WriteDebug4("return");
		}


		private void PhotoSetup(string sFile)
		{
			//if (null == sFile || !File.Exists(_sWorkFolder + sFile))
			//    return;
			//_cPhotoTrans = new Transition();
			//_cPhotoTrans.Source = _sWorkFolder + sFile;
			//_cPhotoTrans.FrameBuffer = new FrameBuffer();
			//_cPhotoTrans.FrameBuffer.SetBounds(520, 360, 200, 200);
			//_cPhotoTrans.FrameBuffer.Z = 55;
			//_cPhotoTrans.FrameBuffer.Setup(_cAnimationContext);
			//_cPhotoTrans.Type = TransitionType.Push_trans;
			//_cPhotoTrans.Direction = TransitionDirection.Up_td;
			//_cPhotoTrans.Duration = 500;
			//_cPhotoTrans.Setup(_cAnimationContext);
		}
		private void PhotoShow()
		{
			//if (null != _cPhotoTrans)
			//    _cPhotoTrans.Take();
		}
		private void PhotoHide()
		{
			//try
			//{
			//    _cPhotoTrans.Source = null;
			//    _cPhotoTrans.Type = TransitionType.Push_trans;
			//    _cPhotoTrans.Direction = TransitionDirection.Down_td;
			//    _cPhotoTrans.Duration = 500;
			//    _cPhotoTrans.Setup(_cAnimationContext);
			//    _cPhotoTrans.Take();
			//    _cPhotoTrans = null;
			//}
			//catch { }
		}
	};
}
