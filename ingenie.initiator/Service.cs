using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.ServiceProcess;
using System.Management;
using System.Text;

using helpers;
using helpers.extensions;
using sio = System.IO;
using System.Diagnostics;

namespace ingenie.initiator
{
	public partial class Service : ServiceBase
	{
		private class ProcessTarget : Preferences.Process
		{
			public int nID;
			public DateTime dtMessageLast;
			public double nMessageLastAge;
			public bool bFail;

			public ProcessTarget(Preferences.Process cProcess)
			{
				dtMessageLast = DateTime.MinValue;
				nMessageLastAge = 0;
				bFail = false;

				sName = cProcess.sName;
				sOwner = cProcess.sOwner;
				sArguments = cProcess.sArguments;
			}
			public override string ToString()
			{
				return "[id:" + nID + "; name:" + sName + "; owner:" + sOwner + "; arguments:" + sArguments ?? "" + "]";
			}
			public void Start()
			{
				System.Diagnostics.Process[] aExplorers = System.Diagnostics.Process.GetProcessesByName("explorer");
				ProcessOwner cProcessOwner = null;
				foreach (System.Diagnostics.Process cExplorer in aExplorers)
				{
					cProcessOwner = GetProcessOwner(cExplorer.Id);
					(new helpers.Logger()).WriteDebug2(cExplorer.Id + ":" + cProcessOwner.sUsername);
					if (sOwner == cProcessOwner.sUsername)
					{
						(new helpers.Logger()).WriteNotice("запуск целевого процесса");
						nID = ProcessAsUser.Launch("\"" + sName + ".exe\" " + sArguments, cExplorer.Id);
						break;
					}
				}
			}
			private ProcessOwner GetProcessOwner(int nProcessId)
			{
				ManagementObjectCollection aMOC = (new ManagementObjectSearcher("Select * From Win32_Process Where ProcessID = " + nProcessId)).Get();
				foreach (ManagementObject cMO in aMOC)
				{
					string[] aArguments = new string[] { string.Empty, string.Empty };
					int nStatus = cMO.InvokeMethod("GetOwner", aArguments).ToInt32();
					if (0 == nStatus)
						return new ProcessOwner(aArguments[1], aArguments[0]);
				}
				throw new Exception("не могу определить владельца процесса");
			}
		}
		private class ProcessOwner
		{
			public string sDomain { get; set; }
			public string sUsername { get; set; }

			public ProcessOwner(string sDomain, string sUsername)
			{
				this.sDomain = sDomain;
				this.sUsername = sUsername;
			}
		}

		ProcessTarget[] _aProcesses;
		byte _nBlenderQueueLength = 0;

		public Service()
		{
			InitializeComponent();
		}
		public void Start()
		{
			OnStart(null);
		}
		protected override void OnStart(string[] args)
		{
			if (!Preferences.aProcesses.IsNullOrEmpty())
				System.Threading.ThreadPool.QueueUserWorkItem(Worker);
			if (null != Preferences.cBlender)
				System.Threading.ThreadPool.QueueUserWorkItem(WorkerBlender);
		}
		void Worker(object cState)
		{
			(new helpers.Logger()).WriteNotice("сервер инициации запущен");
			string sProcesses = "целевые процессы:";
			_aProcesses = new ProcessTarget[Preferences.aProcesses.Length];
			for (int nIndx = 0; Preferences.aProcesses.Length > nIndx; nIndx++)
			{
				_aProcesses[nIndx] = new ProcessTarget(Preferences.aProcesses[nIndx]);
				sProcesses += "<br>" + _aProcesses[nIndx].ToString();

			}
			(new helpers.Logger()).WriteNotice(sProcesses);
			try
			{
				System.Diagnostics.Process cProcess = null;
				while (true)
				{
					foreach (ProcessTarget cProcessTarget in _aProcesses)
					{
						try
						{
							if (0 < cProcessTarget.nID)
							{
								try
								{
									cProcess = System.Diagnostics.Process.GetProcessById(cProcessTarget.nID);
									if (cProcessTarget.bFail)
									{
										(new helpers.Logger()).WriteNotice("целевой процесс запущен " + cProcessTarget.ToString());
										cProcessTarget.bFail = false;
									}
								}
								catch
								{
									cProcessTarget.nMessageLastAge = DateTime.Now.Subtract(cProcessTarget.dtMessageLast).TotalMinutes;
									if (5 < cProcessTarget.nMessageLastAge)
									{
										if (!cProcessTarget.bFail)
										{
											(new helpers.Logger()).WriteError(new Exception("не найден целевой процесс " + cProcessTarget.ToString()));
											cProcessTarget.bFail = true;
										}
										else
										{
											(new helpers.Logger()).WriteError(new Exception("ошибка запуска целевого процесса " + cProcessTarget.ToString()));
											cProcessTarget.dtMessageLast = DateTime.Now;
										}
									}
									cProcessTarget.Start();
								}
							}
							else
								cProcessTarget.Start();
						}
						catch (Exception ex)
						{
							(new helpers.Logger()).WriteError(ex);
						}
					}
					if (null != Preferences.sRestartFile && System.IO.File.Exists(Preferences.sRestartFile)) //EMERGENCY:l это зачем такое?
					{
						try
						{
							System.IO.File.Delete(Preferences.sRestartFile);
							System.Diagnostics.Process.Start(@"c:\Program Files\replica\ingenie\server\restart\ingenie.restart.exe");
						}
						catch (Exception ex)
						{
							(new helpers.Logger()).WriteError(ex);
						}

					}
					System.Threading.Thread.Sleep(3000);
				}
			}
			catch (Exception ex)
			{
				(new helpers.Logger()).WriteError(ex);
			}
			(new helpers.Logger()).WriteNotice("сервер инициации остановлен");
		}
		void WorkerBlender(object cState)
		{
			(new helpers.Logger()).WriteNotice("модуль управления рендером запущен");
			bool bLogged = false;
			string sTaskFile, sOutputFolder;
			string[] aLines;
			try
			{
				while (true)
				{
					foreach (string sTask in sio.Directory.EnumerateDirectories(Preferences.cBlender.sTasks))
					{
						if (sio.File.Exists(sTaskFile = sio.Path.Combine(sTask, "task")))
						{
							while (true)
							{
								try
								{
									(new helpers.Logger()).WriteDebug("try to read file: " + sTaskFile);
									aLines = sio.File.ReadAllLines(sTaskFile);
									break;
								}
								catch (Exception ex)  // реально бывает такое
								{
									(new helpers.Logger()).WriteError(ex);
								}
								System.Threading.Thread.Sleep(1);
							}
							while (true)
							{
								try
								{
									(new helpers.Logger()).WriteDebug("try to delete file: " + sTaskFile);
									sio.File.Delete(sTaskFile);
									break;
								}
								catch (Exception ex)  // реально бывает такое
								{
									(new helpers.Logger()).WriteError(ex);
								}
								System.Threading.Thread.Sleep(1);
							}
							while (_nBlenderQueueLength > Preferences.cBlender.nQueue)
							{
								if (!bLogged)
									(new Logger("queue", "blender", true)).WriteNotice("rendering: waiting [t:" + sTask + "][c:" + _nBlenderQueueLength + "][m:" + Preferences.cBlender.nQueue + "]");
								bLogged = true;
								System.Threading.Thread.Sleep(500);
							}
							_nBlenderQueueLength++;
							bLogged = false;
							try
							{
								if (sio.Directory.Exists(sOutputFolder = sio.Path.Combine(sTask, "!result")))
								{
									(new helpers.Logger()).WriteDebug("try to delete dir: " + sOutputFolder);
									sio.Directory.Delete(sOutputFolder, true);
								}
								(new helpers.Logger()).WriteDebug("try to create dir: " + sOutputFolder);
								sio.Directory.CreateDirectory(sOutputFolder);
							}
							catch (Exception ex)
							{
								(new helpers.Logger()).WriteError(ex);
								continue;
							}




							System.Threading.Tasks.Task.Run(() =>
							{
								StartRender(new string[] { aLines[0], sio.Path.Combine(sTask, aLines[1]), sio.Path.Combine(sTask, "result"), sOutputFolder, aLines[2], aLines[3] });
							});


							//System.Threading.Tasks.Task.Factory.StartNew(() => 
							//{
							//	StartRender(new string[] { aLines[0], sio.Path.Combine(sTask, aLines[1]), sio.Path.Combine(sTask, "result"), sOutputFolder, aLines[2], aLines[3] });
							//}, System.Threading.Tasks.TaskCreationOptions.LongRunning);


							//System.Threading.ThreadPool.QueueUserWorkItem(delegate(object oState){
							//    try
							//    {
							//        string[] aValues = (string[])oState;
							//        Render(BlenderStartInfoGet(aValues[5].ToBool() ? sio.Path.Combine(aValues[3], aValues[4]) : null, aValues[0], aValues[1], null, null));
							//        sio.Directory.Move(aValues[3], aValues[2]);
							//        _nBlenderQueueLength--;
							//    }
							//    catch (Exception ex)
							//    {
							//        (new Logger("queue", "blender", true)).WriteError(ex);
							//    }
							//}, new string[] { aLines[0], sio.Path.Combine(sTask, aLines[1]), sio.Path.Combine(sTask, "result"), sOutputFolder, aLines[2], aLines[3] });
						}
						else if (24 < DateTime.Now.Subtract(sio.Directory.GetLastWriteTime(sTask)).TotalHours)
						{
							try
							{
								(new helpers.Logger()).WriteDebug("try to delete dir after 24 hours waiting: " + sTask);
								sio.Directory.Delete(sTask, true);
							}
							catch (Exception ex)
							{
								(new helpers.Logger()).WriteError(ex);
							}
						}
					}
					System.Threading.Thread.Sleep(1500);
				}
			}
			catch (Exception ex)
			{
				(new helpers.Logger()).WriteError(ex);
			}
			(new helpers.Logger()).WriteNotice("модуль управления рендером остановлен");
		}

		private void StartRender(string[] aValues)
		{
			try
			{
				Render(BlenderStartInfoGet(aValues[5].ToBool() ? sio.Path.Combine(aValues[3], aValues[4]) : null, aValues[0], aValues[1], null, null));
				sio.Directory.Move(aValues[3], aValues[2]);
				_nBlenderQueueLength--;
			}
			catch (Exception ex)
			{
				(new Logger("queue", "blender", true)).WriteError(ex);
			}
		}


		protected override void OnStop()
		{
			if (!Preferences.aProcesses.IsNullOrEmpty())
			{
				System.Diagnostics.Process cProcess;
				foreach (ProcessTarget cProcessTarget in _aProcesses)
				{
					if (0 < cProcessTarget.nID)
					{
						try
						{
							cProcess = System.Diagnostics.Process.GetProcessById(cProcessTarget.nID);
							cProcess.Kill();
						}
						catch { }
					}
				}
			}
		}
		private ProcessStartInfo BlenderStartInfoGet(string sOutputTarget, string sBlendFile, string sPythonFile, string sEngine, string sThreads)
		{
			return new ProcessStartInfo()
			{
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				//LoadUserProfile = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				FileName = Preferences.cBlender.sPath,
				Arguments = "-b \"" + sBlendFile
					+ "\"" + (null == sEngine ? "" : " -E " + sEngine)
					+ " -t " + (null == sThreads ? "0" : sThreads)
					+ (null == sPythonFile ? "" : " -P \"" + sPythonFile + "\"")
					+ (null == sOutputTarget ? "" : " -o \"" + sOutputTarget + "\"") + " -x 1 -a"
			};
		}
		private void Render(System.Diagnostics.ProcessStartInfo cProcessStartInfo)
		{
			(new Logger("render", "blender", true)).WriteNotice("render start [" + cProcessStartInfo.Arguments + "]");
			System.Diagnostics.Process cProcess = System.Diagnostics.Process.Start(cProcessStartInfo);
			cProcess.PriorityClass = ProcessPriorityClass.RealTime;
			//helpers.Logger cLogger = new helpers.Logger("render:" + GetHashCode(), "blender_render_last");
			//cLogger.WriteNotice("***************************************************************<br>Arguments:" + cProcess.StartInfo.Arguments);
			string sErrorMessage = "";
			//cProcess.OutputDataReceived += (sender, args) => sMessage += "[" + DateTime.Now.ToStr() + "][notice][" + args.Data + "]<br>"; // засирает поток и в итоге сильно замедляет рендер
			cProcess.ErrorDataReceived += (sender, args) => sErrorMessage += "[" + DateTime.Now.ToStr() + "][error][" + args.Data + "]<br>";// cLogger.WriteNotice("error<br>", args.Data);
			cProcess.BeginErrorReadLine();
			string sTMP = cProcess.StandardOutput.ReadToEnd();
			//cProcess.BeginOutputReadLine();
			cProcess.WaitForExit();
			if (37 < sErrorMessage.Length) // на 37 там порожняк какой-то генерится, типа "[2015-06-14 01:36:37.61][error][]		"
				(new Logger("render", "blender", true)).WriteError("<br> !!! ERRRORS !!!<br>" + sErrorMessage);
			(new Logger("render", "blender", true)).WriteNotice("render stop [" + cProcessStartInfo.Arguments + "]");
		}

	}
}
