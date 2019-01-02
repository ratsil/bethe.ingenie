using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using helpers;
using ingenie.web.lib;

namespace ingenie.web.services
{
	public class Common : System.Web.Services.WebService
	{
		private class Logger : lib.Logger
		{
			public Logger()
				: base("common")
			{
			}
		}
		static protected object _cSyncRoot = new object();
        static private string _sVersionOfXapScr;


        public Common()
		{
		}

		[WebMethod(EnableSession = true)]
		virtual public string Init(ulong nClientBrowserID, string sVersionOfSCR)
		{
			(new Logger()).WriteWarning("________________INIT   begin");
            if (_sVersionOfXapScr == null)
                _sVersionOfXapScr = helpers.replica.scr.XAP.GetVersionOfDll(@"ClientBin\scr.xap", @"ClientBin\scr.dll");
            if (sVersionOfSCR != _sVersionOfXapScr)
            {
                (new Logger()).WriteError("init. client's version doesn't match [client=" + sVersionOfSCR + "][server=" + _sVersionOfXapScr + "]");
                return "не совпадают версии SCR [client=" + sVersionOfSCR + "][server=" + _sVersionOfXapScr + "]";
            }

            string sRetVal = "";
			(new Logger()).WriteDebug2("init [browser:" + nClientBrowserID + "][client:" + Client.nID + "][last:" + Client.dtPing + "][client_ver=" + sVersionOfSCR + "]");
#if DEBUG
			if (true)
#else
			if (DateTime.Now > Client.dtPing.AddSeconds(20) || IsClientCurrent(nClientBrowserID))
#endif
			{
				ClientInit(nClientBrowserID);
			}
			//else if (null == Session["ClientID"])
			//    Session["ClientID"] = Client.nID;
			//else if (!IsClientCurrent(nClientBrowserID))
			else
			{
				return (Session["ClientID"] ?? "null") + ":" + Client.nCurrentClientBrowserID + ":" + nClientBrowserID + ":" + " клиент управления автоматизацией аппаратно-студийного блока уже запущен в другом окне браузера или на другой рабочей станции";
			}

			if (!GarbageCollector.IsRunning())
			{
				try
				{
					(new Logger()).WriteDebug2("init:text");
					Template cText = new Template(""); // взмолаживание девайса
					cText.TextCreate(".");
					cText.Prepare();
					cText.Start();
					System.Threading.Thread.Sleep(1000);
					cText.Stop();

					(new Logger()).WriteDebug2("init:discom");
					//(new userspace.Helper()).DisComInit(); // взмолаживание сридов для просчета чата.  // чат считается в устройстве из префов, так что это не всегда DisCom
				}
				catch (Exception ex)
				{
					sRetVal = "не пройдена инициализация сервиса";
					(new Logger()).WriteError(ex);
				}
				if ("" == sRetVal)
				{
					GarbageCollector.Run();
				}
			}
			(new Logger()).WriteWarning("________________INIT   end   " + sRetVal);
			return sRetVal;
		}

		protected bool IsClientCurrent(ulong nClientBrowserID)
		{
			//if (Client.nCurrentClientBrowserID == nClientBrowserID && null != Session && null != Session["ClientID"] && Client.nID == (ulong)Session["ClientID"])
			if (Client.nCurrentClientBrowserID == nClientBrowserID)
				return true;
			return false;
		}
		protected void ClientInit(ulong nClientBrowserID)
		{
			Client.Init(nClientBrowserID);
			if (null != Session)
				Session["ClientID"] = Client.nID;
		}

		[WebMethod(EnableSession = true)]
		public void Ping()
		{
			if (null != Session["ClientID"])
				Client.dtPing = DateTime.Now;
			else
				(new Logger()).WriteError(new Exception("Ping(): Попытка пинговать с непроинициализированного клиента"));
			return;
		}
		[WebMethod(EnableSession = true)]
		public Item[] ItemsUpdate(Item[] aItems)
		{
			if (null == Session["ClientID"])
			{
				(new Logger()).WriteError(new Exception("ItemsUpdate(): Попытка обращения с непроинициализированного клиента"));
				return null;
			}
			Client.dtPing = DateTime.Now;
			List<Item> aRetVal = new List<Item>();
			try
			{
				//if (DateTime.Now > _dtStatusGetLast.AddMinutes(1))
				//{
				//    _dtStatusGetLast = DateTime.Now;
				//    int nTemplCount = 0;
				//    lock (_aItems)
				//        nTemplCount = _aItems.Count(o => Client.nID == o.nClientID);
				//    (new Logger()).WriteDebug2("ingenie.asmx.cs: MyPair[] EffectStatusGet: Вошли в StatusGet, aItemIDs.Length = {" + aItemIDs.Length + "}, _ahItems[nClientID].Keys.Count = {" + nTemplCount + "}");
				//}
				if (null != aItems)
				{
					Item cItem;
					bool bGCRunning = GarbageCollector.IsRunning();
					for (int nIndx = 0; aItems.Length > nIndx; nIndx++)
					{
						if (!bGCRunning || null == (cItem = GarbageCollector.ItemGet(aItems[nIndx])))
						{
							(new Logger()).WriteError("items:update: указанный элемент не зарегистрирован [item:" + aItems[nIndx].GetHashCode() + "]");
							cItem = aItems[nIndx];
							cItem.eStatus = Item.Status.Error;
						}
						aRetVal.Add(cItem);
					}
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return aRetVal.ToArray();
		}
		//[WebMethod(EnableSession = true)]
		//public Item[] ItemsRunningGet()
		//{
		//    return GarbageCollector.ItemsStartedGet();
		//}
		[WebMethod(EnableSession = true)]
		public bool ItemDelete(Item cItem)
		{
			bool bRetVal = false;
			try
			{
				if (0 < Client.nID)
				{
					Item cItemLocal = GarbageCollector.ItemGet(cItem);
					if (null != cItemLocal)
					{
						(new Logger()).WriteDebug2("delete: " + cItemLocal.ToString());
						GarbageCollector.ItemDelete(cItemLocal);
						bRetVal = true;
					}
					else
						(new Logger()).WriteError("item:delete: указанный элемент не зарегистрирован [item:" + cItem.GetHashCode() + "]");
				}
				else
					(new Logger()).WriteError("item:delete: не инициирован механизм регистрации клиента");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			return bRetVal;
		}

		[WebMethod(EnableSession = true)]
		public bool ItemPrepare(Item cItem)
		{
			bool bRetVal = false;
			Item cItemLocal = null;
			try
			{
				if (0 < Client.nID)
				{
					if (null != (cItemLocal = GarbageCollector.ItemGet(cItem)))
					{
						(new Logger()).WriteDebug2("prepare: " + cItemLocal.ToString());
						cItemLocal.Prepare();
						bRetVal = true;
					}
					else
						(new Logger()).WriteError("item:prepare: указанный элемент не зарегистрирован [item:" + cItem.GetHashCode() + "]");
				}
				else
					(new Logger()).WriteError("item:prepare: не инициирован механизм регистрации клиента");
			}
			catch (Exception ex)
			{
				if (null != cItemLocal)
					cItemLocal.eStatus = Item.Status.Error;
				(new Logger()).WriteError(ex);

			}
			return bRetVal;
		}
		[WebMethod(EnableSession = true)]
		public bool ItemStart(Item cItem)
		{
			bool bRetVal = false;
			Item cItemLocal = null;
			try
			{
				if (0 < Client.nID)
				{
					if (null != (cItemLocal = GarbageCollector.ItemGet(cItem)))
					{
						(new Logger()).WriteDebug2("start: " + cItemLocal.ToString());
						cItemLocal.Start();
						bRetVal = true;
					}
					else
						(new Logger()).WriteError("item:start: указанный элемент не зарегистрирован [item:" + cItem.GetHashCode() + "]");
				}
				else
					(new Logger()).WriteError("item:start: не инициирован механизм регистрации клиента");
			}
			catch (Exception ex)
			{
				if (null != cItemLocal)
					cItemLocal.eStatus = Item.Status.Error;
				(new Logger()).WriteError(ex);
			}
			return bRetVal;
		}
		[WebMethod(EnableSession = true)]
		public bool ItemStop(Item cItem)
		{
			bool bRetVal = false;
			Item cItemLocal = null;
			try
			{
				if (0 < Client.nID)
				{
					if (null != (cItemLocal = GarbageCollector.ItemGet(cItem)))
					{
						(new Logger()).WriteDebug2("stop: " + cItemLocal.ToString());
						cItemLocal.Stop();
						bRetVal = true;
					}
					else
						(new Logger()).WriteError("item:stop: указанный элемент не зарегистрирован [item:" + cItem.GetHashCode() + "]");
				}
				else
					(new Logger()).WriteError("item:stop: не инициирован механизм регистрации клиента");
			}
			catch (Exception ex)
			{
				if (null != cItemLocal)
					cItemLocal.eStatus = Item.Status.Error;
				(new Logger()).WriteError(ex);
			}
			return bRetVal;
		}
		[WebMethod(EnableSession = true)]
		public void WriteError(string sEx)
		{
			(new helpers.Logger(helpers.Logger.Level.debug2, "scr", "client.sl")).WriteError(new Exception(sEx));
		}
		[WebMethod(EnableSession = true)]
		public void WriteNotice(string sMsg)
		{
			(new helpers.Logger(helpers.Logger.Level.debug2, "scr","client.sl")).WriteNotice(sMsg);
		}

		[WebMethod(EnableSession = true)]
		public void DeviceDownStreamKeyerDisable()
		{
			try
			{
				(new Logger()).WriteNotice("keyer disabling");
				userspace.Device.cDownStreamKeyer = null;
				(new Logger()).WriteNotice("keyer disabled");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
				throw;
			}
		}
		[WebMethod(EnableSession = true)]
		public void DeviceDownStreamKeyerEnable(byte nLevel, bool bInternal)
		{
			try
			{
				(new Logger()).WriteNotice("keyer enabling [" + bInternal + "][" + nLevel + "]");
				userspace.Device.cDownStreamKeyer = new userspace.Device.DownStreamKeyer(nLevel, bInternal);
				(new Logger()).WriteNotice("keyer enabled");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
				throw;
			}
		}
	}
}
