using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using helpers;
using System.Threading;

namespace ingenie.web.lib
{
	abstract public class Item
	{
		public enum Status
		{
			Unknown = 0,
			Idle = 1,
			Prepared = 2,
			Started = 3,
			Stopped = 4,
			Error = -1
		}

		private Status _eStatus;
		
		public ulong nID;

		private object cSyncRoot;
		public string sPreset { get; set; }
		public string sCurrentClass { get; set; }
		public DateTime dtStatusChanged { get; set; }

		virtual public Status eStatus
		{
			get
			{
				lock (cSyncRoot)
					return _eStatus;
			}
			set
			{
				lock (cSyncRoot)
				{
					if (_eStatus != value)
						dtStatusChanged = DateTime.Now;
					_eStatus = value;
				}
			}
		}
		virtual public string sInfo { get; set; }

		public Item()
		{
			cSyncRoot = new object();
			eStatus = Status.Idle;
			nID = 0;
		}

		abstract public void Prepare();
		abstract public void Start();
		abstract public void Stop();

		public override string ToString()
		{
			return "[id:" + nID + "][preset:" + sPreset + "][status:" + _eStatus + "][dts:" + dtStatusChanged + "][info:" + sInfo + "][hc:" + GetHashCode() + "]";
		}
	}
	static public class GarbageCollector
	{
		private class Logger : lib.Logger
		{
			public Logger()
				: base("gc")
			{
			}
			public Logger(helpers.Logger.Level eLevel)
				: base(eLevel, "gc")
			{
			}
		}
		static private ulong nMaxID = 1;
		static private Thread _cGC = null;
		static private List<Item> _aItems = new List<Item>();
		static private List<Item> _aItemsDeleted = new List<Item>();
		static private object _cSyncRoot = new object();

		static public bool IsRunning()
		{
			lock(_cSyncRoot)
				return !(null == _cGC);
		}
		static public void Run()
		{
			lock (_cSyncRoot)
			{
				if (null == _cGC)
				{
					_cGC = new Thread(new ThreadStart(Worker));
					_cGC.Start();
				}
			}
		}

		static private void Worker()
		{
			(new Logger()).WriteDebug3("in");
			Item.Status eStatus = Item.Status.Unknown;
			DateTime dtLastChanged = DateTime.MaxValue;
			DateTime dtServersNow = DateTime.MaxValue;
			List<Item> aToDelete = new List<Item>();
			Item[] aItems;
			try
			{
				while (true)
				{
					aToDelete.Clear();
					dtServersNow = DateTime.Now;
					lock (_aItems)
					{
						aItems = _aItems.Where(o => Item.Status.Stopped == (eStatus = o.eStatus) 
												|| Item.Status.Error == eStatus 
												|| (Item.Status.Started != eStatus 
												&& o.dtStatusChanged.AddSeconds(Preferences.nRemovingDelayInSeconds) < dtServersNow)).ToArray();
						foreach (Item cItem in aItems)
						{
							if (Item.Status.Prepared != cItem.eStatus)
							{
								(new Logger()).WriteNotice("deleting: " + cItem.ToString());
								ItemDelete(cItem, true, (Item.Status.Stopped == cItem.eStatus ? Item.Status.Stopped : Item.Status.Error));
							}
							else
							{
								(new Logger()).WriteNotice("timeworn: item prepared more than " + Preferences.nRemovingDelayInSeconds + " seconds ago" + cItem.ToString());
								ItemDelete(cItem, true, Item.Status.Error);
							}
						}
					}
					lock (_aItemsDeleted)
					{
						aItems = _aItemsDeleted.Where(o => true).ToArray();
						DateTime dtTimeLineForDeleted = DateTime.Now.Subtract(new TimeSpan(1, 0, 0));
						foreach (Item cItem in aItems)
						{
							if (cItem.dtStatusChanged < dtTimeLineForDeleted)
							{
								_aItemsDeleted.Remove(cItem);
								(new Logger()).WriteNotice("worker: vanished: " + cItem.ToString());
							}
						}
					}
					System.Threading.Thread.Sleep(3000);
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		static public void ItemAdd(Item cItem)
		{
			lock (_aItems)  // а то гарбаж коллектор удаляет иногда пустой элемент ))
			{
				if (null != _aItems.FirstOrDefault(o => cItem.nID == o.nID))
					throw new Exception("указанный элемент уже зарегистрирован");
				cItem.nID = nMaxID++;
				_aItems.Add(cItem);
			}
		}
		static public void ItemDelete(Item cItem)
		{
			ItemDelete(cItem, false, Item.Status.Error);
		}
		static private void ItemDelete(Item cItem, Item.Status eStatus)
		{
			ItemDelete(cItem, false, eStatus);
		}
		static private void ItemDelete(Item cItem, bool bAddToDeleted, Item.Status eStatus)
		{
			string slog = "deleted: status = {" + cItem.eStatus + "}, item = {" + cItem.GetHashCode() + "}, info = {" + cItem.sInfo + "}";
			try
			{
				lock (_aItems)
				{
					lock (_aItemsDeleted)
					{
						try
						{
							if (_aItems.Contains(cItem))
							{
								_aItems.Remove(cItem);
								if (bAddToDeleted)
								{
									cItem.eStatus = eStatus;
									_aItemsDeleted.Add(cItem);
								}
							}
							else if (_aItemsDeleted.Contains(cItem))
							{
								_aItemsDeleted.Remove(cItem);
								(new Logger()).WriteNotice("item:delete:vanished: " + cItem.ToString());
							}
						}
						catch
						{
							throw new Exception("указанный элемент не зарегистрирован [item:" + cItem.GetHashCode() + "]");
						}
					}
				}
				(new Logger()).WriteNotice(slog);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		static public Item[] ItemsStartedGet()
		{
			Item[] aRetVal = null;
			ulong nClientID = Client.nID;
			lock (_aItems)
			{
				aRetVal = _aItems.Where(row => Item.Status.Prepared == row.eStatus).ToArray();
				foreach (Item cI in aRetVal)
					ItemDelete(cI, false, Item.Status.Prepared);
				aRetVal = _aItems.Where(row => Item.Status.Started == row.eStatus).ToArray();
			}
			return aRetVal;
		}
		static public Item ItemGet(Item cItem)
		{
			Item cRetVal = null;
			if (0 < Client.nID && null != cItem)
			{
				lock (_aItems)
				{
					if (null == (cRetVal = _aItems.FirstOrDefault(o => cItem.nID == o.nID)))
						cRetVal = _aItemsDeleted.FirstOrDefault(o => cItem.nID == o.nID);
				}
			}
			return cRetVal;
		}
		static public Item ItemGet(string sInfo)
		{
			Item cRetVal = null;
			lock (_aItems)
			{
				if (null == (cRetVal = _aItems.FirstOrDefault(o => sInfo == o.sInfo)))
					cRetVal = _aItemsDeleted.FirstOrDefault(o => sInfo == o.sInfo);
			}
			return cRetVal;
		}
	}
}