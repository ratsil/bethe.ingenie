//#define DEBUG1

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using helpers;
using helpers.extensions;
using helpers.replica.ia;
using helpers.replica.cues;
using helpers.replica.pl;

namespace ingenie.plugins
{
	class DBInteract : helpers.replica.DBInteract
    {
        public DBInteract()
        {
            _cDB = new DB();
            _cDB.CredentialsSet(Preferences.cDBCredentials);
		}
        static long nIDs = 0;
        static DateTime dtLast = DateTime.MinValue;
		public Queue<Message> MessagesQueuedGet(string sPrefix)
		{
			//Queue<Message> aq = new Queue<Message>();
			//if (5 > DateTime.Now.Subtract(dtLast).TotalMinutes)
			//    return aq;
			//dtLast = DateTime.Now;
			//aq.Enqueue(new Message(nIDs, nIDs++, new Gateway.IP("1.1.1.1"), 1, 66893782672, 5743, "тест1", null, DateTime.Now, DateTime.MaxValue));
			//aq.Enqueue(new Message(nIDs, nIDs++, new Gateway.IP("1.1.1.1"), 1, 66893782672, 5743, "тест2 МНОГОСТРОЧКА!! ЗНАЕТ СКОЛЬКО БУДЕТ СТРОК!!! sdhfjshgdfjhsf bsdfhsdfhsdf s dfhjhhjbsjhdfb sdfjhb sdhfjshgdfjhsf bsdfhsdfhsdf s dfhjhhjbsjhdfb sdfjhb sdhfjshgdfjhsf bsdfhsdfhsdf s dfhjhhjbsjhdfb sdfjhb", null, DateTime.Now, DateTime.MaxValue));
			//aq.Enqueue(new Message(nIDs, nIDs++, new Gateway.IP("1.1.1.1"), 1, 66893782672, 5743, "тест3 sjdfjk 324523 45 234 52345d fg sdfgfh", null, DateTime.Now, DateTime.MaxValue));
			//aq.Enqueue(new Message(nIDs, nIDs++, new Gateway.IP("1.1.1.1"), 1, 66893782672, 5743, "тест4", null, DateTime.Now, DateTime.MaxValue));
			//aq.Enqueue(new Message(nIDs, nIDs++, new Gateway.IP("1.1.1.1"), 1, 66893782672, 5743, "тест5", null, DateTime.Now, DateTime.MaxValue));
			//return aq;
			if (null == sPrefix)
				sPrefix = "";
			if (null != sPrefix && 0 < sPrefix.Length && sPrefix == "VIP")
				sPrefix = " AND `nTarget`=5743";
            return MessagesGet("`dtDisplay` IS NULL" + sPrefix, Preferences.nMessagesQty);  //"`dtDisplay` IS NULL"        
		}
		public Queue<Message> MessagesQueuedGet()
        {
            //return new Queue<Message>();
            return MessagesGet("`dtDisplay` IS NULL", Preferences.nMessagesQty);
        }
		public void MessagesDisplaySet(long[] aIDs)
        {
            string sSQL = "";
            foreach (long nID in aIDs)
                sSQL += "SELECT ia.`fMessageDTEventAdd`(" + nID + ", 'display');";
			if (0 < sSQL.Length)
			{
				_cDB.Perform(sSQL);
				(new Logger()).WriteDebug3(sSQL);
			}
        }
        public int MessagesUndisplayedCountGet()
        {
            return MessagesCountGet("`dtDisplay` IS NULL");
        }
    }
    public class Chat : MarshalByRefObject, IPlugin
    {
        #region Members
		private EventDelegate Prepared;
		private EventDelegate Started;
		private EventDelegate Stopped;
		private BTL.EffectStatus _eStatus;
		private DateTime _dtStatusChanged;
        private SMSChat _cSMSChat;
        private Preferences _cPreferences;
        private int _nPromoReleased = 0;  //-1 нет 1 да 0 - после перезагрузки сервиса
        private Preferences.Promo _cPromoLast   //выдаём последнее промо снабжая его временем выхода (из файла vip.dat)
        {
            get
            {
                Preferences.Promo cRetVal = null;
                if (null == _cPreferences || null == _cPreferences.cVIP.aPromos || 1 > _cPreferences.cVIP.aPromos.Count)
                    return cRetVal;
                if (-1 == _nPromoReleased)
                    return cRetVal;
                if (0 == _nPromoReleased)
                    _nPromoReleased = 1; //здесь можно бы реализовать отдачу предыдущего promo, а пока просто будем его пропускать. Но это только при сбоях...
                cRetVal = _cPreferences.cVIP.aPromos[0];
                int nID = 0;
                if (File.Exists(_cPreferences.cVIP.sFile))
                {
                    string[] aLines = File.ReadAllLines(_cPreferences.cVIP.sFile);
                    string sLine = aLines[0];
                    aLines = sLine.Split(('\t'));
                    nID = aLines[0].ToInt();
                    for (ushort nIndx = 0; _cPreferences.cVIP.aPromos.Count > nIndx; nIndx++)
                    {
                        if (_cPreferences.cVIP.aPromos[nIndx].nID == nID)
                        {
                            cRetVal = _cPreferences.cVIP.aPromos[nIndx];
                            cRetVal.dtLastShow = DateTime.Parse(aLines[1]);
                            break;
                        }
                    }
                }
                return cRetVal;
            }
            set
            {
                string sPromoShows = value.nID + "\t" + value.dtLastShow.ToString("yyyy-MM-dd HH:mm:ss") + "\t";
                File.WriteAllText(_cPreferences.cVIP.sFile, sPromoShows);
                _nPromoReleased = -1;
            }
        }
        private int GetIndexOfPromo(Preferences.Promo cPromo)
        {
            int nProCount = _cPreferences.cVIP.aPromos.Count;
            int nRetVal = -1;
            for (ushort nIndx = 0; nProCount > nIndx; nIndx++)
            {
                if (_cPreferences.cVIP.aPromos[nIndx].nID == cPromo.nID)
                {
                    nRetVal = nIndx;
                    break;
                }
            }
            return nRetVal;
        }
        private Preferences.Promo GetNextPromo(Preferences.Promo cPromo, DateTime dtNow)
        {
            Preferences.Promo cRetVal = null;
            int nProCount = _cPreferences.cVIP.aPromos.Count;
            int nIndx = GetIndexOfPromo(cPromo);
            for (ushort i = 0; nProCount > i; i++)
            {
                nIndx = (nIndx == nProCount - 1 ? 0 : nIndx + 1);
                bool bIsInRange = _cPreferences.cVIP.aPromos[nIndx].aWeeklyRange[0].IsDateInRange(dtNow);
                if (bIsInRange && _cPreferences.cVIP.aPromos[nIndx].bEnabled)
                {
                    cRetVal = _cPreferences.cVIP.aPromos[nIndx];
                    break;
                }
            }
            return cRetVal;
        }
		bool _bStopped, _bWorkerSMSEnd, _bWorkerEnd;
		Queue<SMS> _aqSMSs;
        #endregion
        public Chat()
        {
            _eStatus = BTL.EffectStatus.Idle;
			_dtStatusChanged = DateTime.Now;
			_bWorkerEnd = true;
			_bWorkerSMSEnd = true;
			_oLock = new object();
		}
        public void Create(string sWorkFolder, string sData) 
        {
            _cPreferences = new Preferences(sWorkFolder, sData);
        }
        public void Prepare()
        {
            try
            {
                _bStopped = true;
                _aqSMSs = new Queue<SMS>();
                (_cSMSChat = new SMSChat(_cPreferences)).Init(); 
                _eStatus = BTL.EffectStatus.Preparing;
				_dtStatusChanged = DateTime.Now;
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
            ThreadPool.QueueUserWorkItem(Worker);
			_eStatus = BTL.EffectStatus.Running;
			_dtStatusChanged = DateTime.Now;
            if (null != Started)
                Plugin.EventSend(Started, this);
		}
        private bool ChatMustGoOn()
		{
#if DEBUG1
			return !_cSMSChat.IsChatTerminating;
#endif
			switch (_cPreferences.eBroadcastType)
            {
                case Preferences.BroadcastType.linear:
                    return !_cSMSChat.IsChatTerminating;
                case Preferences.BroadcastType.live:
                    return !_bStopped;
            }
            return true;
        }
		DateTime dtNextTimeLog_Worker = DateTime.MinValue;
		DateTime dtNextTimeLog_WorkerSMS = DateTime.MinValue;
		private object _oLock;
        private void Worker(object cState)
        {
            _bStopped = false;
			_bWorkerEnd = false;
            try
            {
				(new Logger()).WriteDebug3("chat started");
				if (null == _cSMSChat)
				{
					_bWorkerEnd = true;
					return;
				}
				Logger.Timings cTimings = new helpers.Logger.Timings("chat:Worker");
				Queue<ChatInOut> aqChatInOuts = null;
                DateTime dtBase = DateTime.Now;
                DBInteract cDBI = null;
                PlaylistItem cCurrentPLI = null;
                Queue<PlaylistItem> aqCU = null;
                try
                {
                    cDBI = new DBInteract();

                    while (!_bStopped && null == cCurrentPLI)
                    {
						//if (DateTime.Now > dtNextTimeLog_Worker)
						//{
						//    (new Logger()).WriteNotice("Worker: I'm HERE! [hash:" + this.GetHashCode() + "]");
						//    dtNextTimeLog_Worker = DateTime.Now.AddMinutes(1);
						//}
                        aqCU = cDBI.ComingUpGet(0, 1);
                        if (0 < aqCU.Count)
                        {
                            cCurrentPLI = aqCU.Dequeue();
                            aqChatInOuts = cDBI.ChatInOutsGet(cCurrentPLI.cAsset);
//							aqChatInOuts = cDBI.ChatInOutsGet(helpers.replica.mam.Asset.Load(3261));
                            if (0 < aqChatInOuts.Count)
                                dtBase = cCurrentPLI.dtStartReal;
                            else
                                aqChatInOuts = null;
                        }
                        else
                            Thread.Sleep(300);
                    }
                }
                catch (Exception ex)
                {
                    (new Logger()).WriteError(ex);
                }


				// ==========================================================================================================
				//dtBase = DateTime.Now;
				//aqChatInOuts = new Queue<ChatInOut>();
				//aqChatInOuts.Enqueue(new ChatInOut(1176, new TimeRange(400, 900)));
				//aqChatInOuts.Enqueue(new ChatInOut(1176, new TimeRange(1300, 1600)));
				//aqChatInOuts.Enqueue(new ChatInOut(1176, new TimeRange(2200, 3500)));
				// ==========================================================================================================


                if (null == aqChatInOuts)
                {
                    aqChatInOuts = new Queue<ChatInOut>();
					aqChatInOuts.Enqueue(new ChatInOut(-1, 1, int.MaxValue));
                }

				ChatInOut cChatInOut;
                DateTime dtStop;
                int nMSStartDelay;

				ThreadPool.QueueUserWorkItem(WorkerSMS);

				while (!_bStopped && 0 < aqChatInOuts.Count)
                {
					//if (DateTime.Now > dtNextTimeLog_Worker)
					//{
					//    (new Logger()).WriteNotice("Worker: I'm HERE! [hash:" + this.GetHashCode() + "]");
					//    dtNextTimeLog_Worker = DateTime.Now.AddMinutes(1);
					//}
                    cChatInOut = aqChatInOuts.Dequeue();
                    if (TimeSpan.MaxValue > cChatInOut.cTimeRange.tsOut)
						dtStop = dtBase.Add(cChatInOut.cTimeRange.tsOut);
                    else
                        dtStop = DateTime.MaxValue;

                    if (DateTime.Now > dtStop.AddSeconds(-20))
                        continue;

					if (_cSMSChat.bReleased)
					{
						_cSMSChat.Init();
					}
					nMSStartDelay = (int)dtBase.Add(cChatInOut.cTimeRange.tsIn).Subtract(DateTime.Now).TotalMilliseconds;
                    if (0 < nMSStartDelay)
                        Thread.Sleep(nMSStartDelay);
                    _cSMSChat.Start();

					//#if UNLIMIT
					//while (!_bStopped)
					//#else
					//while (!_cSMSChat.IsChatTerminating)
					//#endif
					while (ChatMustGoOn()) 
                    {
						//if (DateTime.Now > dtNextTimeLog_Worker)
						//{
						//    (new Logger()).WriteNotice("Worker: I'm HERE! [hash:" + this.GetHashCode() + "]");
						//    dtNextTimeLog_Worker = DateTime.Now.AddMinutes(1);
						//}
                        if (_cPreferences.eBroadcastType == Preferences.BroadcastType.linear && DateTime.Now > dtStop)
                            break;
                        if (4 > _cSMSChat.QueueLength)   //  4>
						{
							lock (_aqSMSs)
							{
								if (0 < _aqSMSs.Count)
								{
									_cSMSChat.MessagesAdd(_aqSMSs);
									_aqSMSs.Clear();
								}
							}
						}

						//GC.Collect
						Thread.Sleep(500);
                    }
					(new Logger()).WriteDebug3("chat stopped" + (DateTime.MaxValue > dtStop ? ":" + dtStop.ToStr() : ""));
					lock (_oLock)
					{
						if (null != _cSMSChat)
						{
							_cSMSChat.Stop();
							_cSMSChat.Release();     // ушла в stop()
						}
					}
                    (new Logger()).WriteDebug4("return");
                }
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
                try
                {
                    _cSMSChat.Stop();
					//_cSMSChat.Release();    ушла в stop()
                }
                catch (Exception ex1)
                {
                    (new Logger()).WriteError(ex1);
                }
            }
			_bWorkerEnd = true;
			try
			{
				if (!_bStopped)
					Stop();
				//if (!_bStopped && null != Stopped)
				//    Stopped(this);                     ушла в stop()
				//_bStopped = true;                      ушла в stop()
				(new Logger()).WriteNotice("chat worker stopped");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
        private void WorkerSMS(object cState)
        {
			_bWorkerSMSEnd = false;
			try
			{
				int nCount;
				Queue<SMS> aqSMSs = null;
				Logger.Timings cTimings = new helpers.Logger.Timings("chat:WorkerSMS");
				while (!_bStopped)
				{
					//if (DateTime.Now > dtNextTimeLog_WorkerSMS)
					//{
					//    (new Logger()).WriteNotice("WorkerSMS: I'm HERE! [hash:" + this.GetHashCode() + "]");
					//    dtNextTimeLog_WorkerSMS = DateTime.Now.AddMinutes(1);
					//}
					try
					{
						lock (_aqSMSs)
							nCount = _aqSMSs.Count;
						if (1 > nCount)
						{
							aqSMSs = GetSMSs(10);
							if (null != aqSMSs && 0 < aqSMSs.Count)
							{
								lock (_aqSMSs)
								{
									while (0 < aqSMSs.Count)
										_aqSMSs.Enqueue(aqSMSs.Dequeue());
								}
							}
						}
						MessagesRelease();
					}
					catch (Exception ex)
					{
						(new Logger()).WriteError(ex);
					}

					//GC.Collect
					Thread.Sleep(500);
				}
				MessagesRelease();
				(new Logger()).WriteNotice("messages worker stopped");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			_bWorkerSMSEnd = true;
		}
        public void Stop()
        {
            try
            {
				if (_bStopped)
					return;
                _bStopped = true;
                _cSMSChat.IsChatTerminating = true;
				(new Logger()).WriteNotice("Stop: IsChatTerminating = true");
                DateTime dtNow = DateTime.Now.AddSeconds(5);
				while ((_cSMSChat.IsChatTerminating || _cSMSChat.IsInfoOnAir) && DateTime.Now < dtNow)
                    Thread.Sleep(50);

				while (!_bWorkerSMSEnd && !_bWorkerEnd)
					Thread.Sleep(50);

				_cSMSChat.Stop();
				_cSMSChat.Release();
				(new Logger()).WriteNotice("Stop: after _cSMSChat.Release();");
				lock (_oLock)
				{
					_cSMSChat = null;
				}
				_cPreferences = null;
			}
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
			}
			_eStatus = BTL.EffectStatus.Stopped;
			_dtStatusChanged = DateTime.Now;
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
				if (null != _cSMSChat && BTL.EffectStatus.Preparing < _cSMSChat.eStatus)
					_eStatus = _cSMSChat.eStatus;
				return _eStatus;
			}
		}
		DateTime IPlugin.dtStatusChanged
		{
			get
			{
				if (null != _cSMSChat)
					_dtStatusChanged = _cSMSChat.dtStatusChanged;
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

		#region SMS Proccessing

		private Queue<SMS> GetSMSs(int nQtty)
        {
			Queue<SMS> aRetVal = new Queue<SMS>();
			try
            {
				Queue<Message> aqMessages = null;
				Message cMessage = null;
                SMS cSMS = null;
                DBInteract cDBI = new DBInteract();
                try
                {
                    if (_cPreferences.eBroadcastType == Preferences.BroadcastType.linear && cDBI.IsThereAnyStartedLiveBroadcast())
                    {
                        cSMS = new SMS() { cPreferences = _cPreferences.cRoll.cSMSVIP };

                        //cSMS.Color = null;
                        //cSMS.Color = Color.FromArgb(0, 0, 0, 0);
                        cSMS.ID = -123456;
                        cSMS.sText = "По техническим причинам SMS-чат временно заблокирован! Приносим извинения за причиненные неудобства! :)";
                        cSMS.Phone = "+70000000003";
                        cSMS.eType = SMS.Type.Promo;
						aRetVal.Enqueue(cSMS);
                        Preferences.nUndisplayedMessages = 1;
                        return aRetVal;
                    }
                }
                catch (Exception ex)
                {
                    (new Logger()).WriteError(ex);
                }

                Preferences.nUndisplayedMessages = cDBI.MessagesUndisplayedCountGet();
                Preferences.Promo cPromo;
                Preferences.Promo cPromoLast = _cPromoLast;
                if (null != cPromoLast)
                {
					aqMessages = new Queue<Message>();
					DateTime dtNow = DateTime.Now;
                    if (dtNow >= cPromoLast.dtLastShow.Add(_cPreferences.cVIP.tsPromoPeriod) && null != (cPromo = GetNextPromo(cPromoLast, dtNow)))
					{
						cMessage = new Message(-109, "-109", null, 1, 70000000002, 0, "PROMO " + cPromo.sText, null, dtNow, dtNow);
						cPromo.dtLastShow = dtNow;
                        _cPromoLast = cPromo;
						aqMessages.Enqueue(cMessage);
					}
					if (1 > aqMessages.Count)
						aqMessages = null;
                }

				if (null == aqMessages)
				{
					aqMessages = cDBI.MessagesQueuedGet(_cPreferences.cVIP.sPrefix);
					if (null == aqMessages || 1 > aqMessages.Count)
						aqMessages = cDBI.MessagesQueuedGet();
				}

				while (null != aqMessages && 0 < aqMessages.Count)
				{
                    try
                    {
						cMessage = aqMessages.Dequeue();
						cSMS = new SMS();
						cSMS.ID = cMessage.nID;
						cSMS.sText = cMessage.sText.ToString();
                        if (cSMS.sText.StartsWith(_cPreferences.cVIP.sPrefix))
                        {
                            //if (bRemovePrefix)
                            cSMS.sText = cSMS.sText.Remove(0, _cPreferences.cVIP.sPrefix.Length);
                            cSMS.eType = SMS.Type.VIP;
                            cSMS.cPreferences = _cPreferences.cRoll.cSMSVIP;
							if (_cPreferences.cRoll.cSMSVIP.bToUpper)
								cSMS.sText = cSMS.sText.ToUpper();
						}
                        else if (cSMS.sText.StartsWith(_cPreferences.sPhotoPrefix))
                        {
                            cSMS.eType = SMS.Type.Photo;
                            cSMS.cPreferences = _cPreferences.cRoll.cSMSPhoto;
							if (_cPreferences.cRoll.cSMSPhoto.bToUpper)
								cSMS.sText = cSMS.sText.ToUpper();
                        }
                        else if (cSMS.sText.StartsWith("PROMO "))
                        {
                            cSMS.sText = cSMS.sText.Remove(0, ("PROMO ").Length);
                            cSMS.eType = SMS.Type.Promo;
							cSMS.cPreferences = _cPreferences.cRoll.cSMSPromo;
							if (_cPreferences.cRoll.cSMSPromo.bToUpper)
								cSMS.sText = cSMS.sText.ToUpper();
						}
                        else
                        {
                            cSMS.eType = SMS.Type.Common;
                            cSMS.cPreferences = _cPreferences.cRoll.cSMSCommon;
							if (_cPreferences.cRoll.cSMSCommon.bToUpper)
								cSMS.sText = cSMS.sText.ToUpper();
						}
						cSMS.Phone = "+" + cMessage.nSourceNumber;
						aRetVal.Enqueue(cSMS);
                    }
                    catch (Exception ex)
                    {
						if (null == _cSMSChat)
							break;
						(new Logger()).WriteWarning("[msgsc:" + aqMessages.Count + "]");
                        (new Logger()).WriteWarning("ERROR:" + ex.Message + ex.StackTrace.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
            return aRetVal;
        }
		private void MessagesRelease()
		{
			try
			{
				if (null != _cSMSChat)
				{
					long[] aMessageIDsDisplayed = _cSMSChat.MessageIDsDisplayed;
					if (null == aMessageIDsDisplayed || 1 > aMessageIDsDisplayed.Length)
						return;

					(new Logger()).WriteDebug3("MessagesRelease: " + aMessageIDsDisplayed.Length);
					List<long> aIDs = new List<long>();
					foreach (long nId in aMessageIDsDisplayed)
					{
						if (-109 == nId)
							_nPromoReleased = 1;
						else if (-123456 != nId)
							aIDs.Add(nId);
					}
					DBInteract cDBI = new DBInteract();
					if (_cPreferences.eBroadcastType == Preferences.BroadcastType.live && !cDBI.IsThereAnyStartedLiveBroadcast())
						return; 
                    if (_cPreferences.bMessagesRelease)
						cDBI.MessagesDisplaySet(aIDs.ToArray());
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		#endregion
    }
}
