using System;
using System.Collections.Generic;
using helpers.replica.cues;

namespace ingenie.plugins
{
	class DBInteract : helpers.replica.DBInteract
	{
		public DBInteract()
		{
			_cDB = new DB();
			_cDB.CredentialsSet(Preferences.DBCredentials);
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
	}
}
