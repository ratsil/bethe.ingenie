using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using helpers;
using helpers.extensions;
using helpers.replica.cues;
using System.Collections;
using helpers.replica.scr;

namespace ingenie.web.lib
{
    class DBInteract : helpers.replica.DBInteract //EMERGENCY ждем когда всё заработает, чтобы вообще этот файл убрать....
    {
		private class Logger : lib.Logger
		{
			public Logger()
				: base("dbi")
			{
			}
		}
		public DBInteract()
		{
			(new Logger()).WriteDebug("DBInteract-1");
			_cDB = new DB();
			(new Logger()).WriteDebug("DBInteract-2");
			_cDB.CredentialsLoad();
			(new Logger()).WriteDebug("DBInteract-3");
		}

		//public IdNamePair GetCurrentShiftTemplate()
		//{
		//    IdNamePair nRetVal = null;
		//    try
		//    {
		//        Queue<Hashtable> ahRes;
		//                           //SELECT "idTemplates", "sName" FROM scr."tShifts" LEFT JOIN scr."tTemplates" ON scr."tShifts"."idTemplates"=scr."tTemplates".id WHERE "dtStart" IS NOT NULL AND "dtStop" IS NULL AND "idTemplates">0 ORDER BY scr."tShifts".id DESC
		//        ahRes = _cDB.Select("SELECT `idTemplates`, `sName` FROM scr.`tShifts` LEFT JOIN scr.`tTemplates` ON scr.`tShifts`.`idTemplates`=scr.`tTemplates`.id WHERE `dtStart` IS NOT NULL AND `dtStop` IS NULL AND `idTemplates`>0 ORDER BY scr.`tShifts`.id DESC");
		//        if (null != ahRes && 0 < ahRes.Count)
		//        {
		//            nRetVal = new IdNamePair(ahRes.Dequeue(), "idTemplates", "sName");
		//        }
		//    }
		//    catch (Exception ex)
		//    {
		//        (new Logger()).WriteError(ex);
		//    }
		//    return nRetVal;
		//}
    }
}
