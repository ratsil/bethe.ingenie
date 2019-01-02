using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Xml;
using System.Xml.Serialization;

namespace ingenie.web.services
{
	/// <summary>
	/// Summary description for preferences
	/// </summary>
	[WebService(Namespace = "http://replica/ig/services/Preferences.asmx")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
	// [System.Web.Script.Services.ScriptService]
	public class Preferences : System.Web.Services.WebService
	{
        private class Logger : lib.Logger
        {
            public Logger()
                : base("preferences")
            {
            }
        }
        public DateTime dtReload
		{
			get
			{
				if (null == Session["dtReload"])
					dtReload = DateTime.MinValue;
				return (DateTime)Session["dtReload"];
			}
			set
			{
				Session["dtReload"] = value;
			}
		}

		[WebMethod(EnableSession = true)]
        public ingenie.web.Preferences.Clients.SCR SCRGet()
        {
            try
            {
                if (1 < DateTime.Now.Subtract(dtReload).TotalMinutes)
                {
                    ingenie.web.Preferences.Reload();
                    dtReload = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
            return ingenie.web.Preferences.cClientReplica;
        }

        [WebMethod(EnableSession = true)]
        public ingenie.web.Preferences.Clients.Presentation PresentationGet()
        {
            try
            {
                if (1 < DateTime.Now.Subtract(dtReload).TotalMinutes)
                {
                    ingenie.web.Preferences.Reload();
                    dtReload = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
            return ingenie.web.Preferences.cClientPresentation;
        }
    }
}
