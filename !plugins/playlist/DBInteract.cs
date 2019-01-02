using System;
using System.Collections.Generic;
using hrc = helpers.replica.cues;
using hra = helpers.replica.adm;
using System.Linq;

namespace ingenie.plugins
{
	class DBInteract : helpers.replica.DBInteract
	{
		public DBInteract()
		{
			_cDB = new DB();
			_cDB.CredentialsSet(Preferences.DBCredentials);
		}
		static DateTime dtLast = DateTime.MinValue;
		public long TryToGetIDFromCommands()
		{
			hra.QueuedCommand[] aQCs = hra.QueuedCommand.Load("`sCommandName` = 'cues_plugin_playlist_start' AND 'proccessing'=`sCommandStatus`", "dt", "1");
			hra.QueuedCommand.Parameter cQCP;

			if (aQCs.Length > 0 && null != (cQCP = aQCs[0].aParameters.FirstOrDefault(o => o.sKey == "idPL")))
				return long.Parse(cQCP.sValue);
			(new Logger()).WriteNotice("commands parameter is null or no command");
            return -1;
        }
		public hrc.plugins.Playlist AdvancedPlaylistGet(long nID)
		{
			return hrc.plugins.Playlist.Load(this, nID);
		}

	}
}
