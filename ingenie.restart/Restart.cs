using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceProcess;

namespace ingenie.restart
{
	public class Logger : helpers.Logger
	{
		public Logger()
			: base("ingenie:restart")
		{ }
	}
	class Restart
	{
		static void Main(string[] args)
		{
			(new Logger()).WriteNotice("IIS restarting: begin");
			try
			{
				System.Diagnostics.Process cIISreset = new System.Diagnostics.Process();
				cIISreset.StartInfo.FileName = Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\iisreset.exe";
				cIISreset.Start();
				(new Logger()).WriteDebug("iisreset.exe started...");
				System.Diagnostics.Process[] aPPP;
				while (true)
				{
					System.Threading.Thread.Sleep(1000);
					aPPP = System.Diagnostics.Process.GetProcesses();
					if (null == aPPP.FirstOrDefault(o => o.ProcessName == "iisreset"))
						break;
				}
				(new Logger()).WriteNotice("IIS restarted");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}

			(new Logger()).WriteNotice("IG restarting: begin");
			try
			{
				ServiceController controller = new ServiceController();
				controller.ServiceName = "ingenie.initiator"; // i.e “w3svc”
				if (controller.Status != ServiceControllerStatus.Running)
				{
					controller.Start();
				}
				else
				{
					controller.Stop();
					(new Logger()).WriteDebug("InGenie.Initiator stopping...");
					controller.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 10));
					System.Threading.Thread.Sleep(1000);
					(new Logger()).WriteDebug("InGenie.Initiator stopped...");
					controller.Start();
				}
				(new Logger()).WriteDebug("InGenie.Initiator starting...");
				controller.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 10));
				(new Logger()).WriteNotice("InGenie.Initiator restarted");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			System.Threading.Thread.Sleep(1000);
		}
	}
}
