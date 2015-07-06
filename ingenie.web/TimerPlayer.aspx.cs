using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ingenie.web
{
	public partial class TimerPlayer : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
            String sXML = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine + "<PlaylistStopPlanned>";
			sXML += ingenie.web.services.Player.dtPlaylistStopPlanned.ToString("dd.MM.yyyy HH:mm:ss") + "</PlaylistStopPlanned>" + Environment.NewLine;
			Response.Write(sXML);
		}
	}
}