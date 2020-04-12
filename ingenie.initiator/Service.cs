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
using System.IO.Compression;

namespace ingenie.initiator
{
	public partial class Service : ServiceBase
	{
		public class ProcessTarget : Preferences.Process
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
				sConfig = cProcess.sConfig;
				bHideConsole = cProcess.bHideConsole;
				enPriority = cProcess.enPriority;
			}
			public override string ToString()
            {
                return "--[id:" + nID + "; name:" + sName + "; owner:" + sOwner + "; arguments:" + (sArguments ?? "") + "; config:" + (sConfig ?? "") + "][priority=" + enPriority + "][hide=" + bHideConsole + "]";
            }
			private void ReplaceConfigs() // run one exe with different configs
            {
                (new helpers.Logger()).WriteNotice("try to replace configs:  [new=" + sConfig + "][main=" + sConfigMain + "] [exists:" + (sConfig == null ? " NULL" : sio.File.Exists(sConfig).ToString()) + "]  ");
                if (sConfig != null && sio.File.Exists(sConfig) && sConfig.ToLower() != sConfigMain.ToLower())
                {
                    (new helpers.Logger()).WriteNotice("replacing configs [new=" + sConfig + "][main=" + sConfigMain + "]");
					if (sio.File.Exists(sConfigMain))
					{
						if (sio.File.Exists(sConfigBKP))
						{
							(new helpers.Logger()).WriteNotice("Внимание! файл _bkp уже есть! Странно ["+ sConfigBKP + "]");
							sio.File.Delete(sConfigMain);
						}
						else
							sio.File.Move(sConfigMain, sConfigBKP);
					}
					sio.File.Copy(sConfig, sConfigMain);
				}
			}
			private void PlaceConfigBack()
			{
				if (sio.File.Exists(sConfigBKP))
				{
					(new helpers.Logger()).WriteNotice("placing config back [back=" + sConfigBKP + "][main=" + sConfigMain + "]");
					if (sio.File.Exists(sConfigMain))
						sio.File.Delete(sConfigMain);
					sio.File.Move(sConfigBKP, sConfigMain);
				}
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
						ReplaceConfigs();
						(new helpers.Logger()).WriteNotice("запуск целевого процесса");
						System.Threading.Thread.Sleep(500);
						nID = ProcessAsUser.Launch("\"" + sName + ".exe\" " + sArguments, cExplorer.Id, bHideConsole);
						System.Threading.Thread.Sleep(500);
						PlaceConfigBack();
						break;
					}
				}
			}
			public void Stop()
			{
				System.Diagnostics.Process cProcess = System.Diagnostics.Process.GetProcessById(nID);
				cProcess.Kill();
				try
				{
					while (true)
					{
						if (null == System.Diagnostics.Process.GetProcessById(nID))
						{
							(new helpers.Logger()).WriteNotice("процесс остановлен по команде извне");
							return;
						}
						System.Threading.Thread.Sleep(10);
					}
				}
				catch { }
			}
			static public ProcessOwner GetProcessOwner(int nProcessId)
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
		public class ProcessOwner
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
        bool bAbort;

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
            bAbort = false;
            if (!Preferences.aProcesses.IsNullOrEmpty() || null != Preferences.sRestartFile)
                System.Threading.ThreadPool.QueueUserWorkItem(Worker);
			if (null != Preferences.cBlender)
				System.Threading.ThreadPool.QueueUserWorkItem(WorkerBlender);
            System.Threading.Thread.Sleep(3000);
        }
		void Worker(object cState)
		{
			(new helpers.Logger()).WriteWarning("сервер инициации запущен");
			string sProcesses = "целевые процессы:";
			_aProcesses = new ProcessTarget[Preferences.aProcesses.Length];
			for (int nIndx = 0; Preferences.aProcesses.Length > nIndx; nIndx++)
			{
				_aProcesses[nIndx] = new ProcessTarget(Preferences.aProcesses[nIndx]);
				sProcesses += "<br>\t\t" + _aProcesses[nIndx].ToString();

			}
			(new helpers.Logger()).WriteNotice(sProcesses);
            try
            {
                System.Diagnostics.Process cProcess = null;
                while (!bAbort)
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
                                    if (cProcess.PriorityClass != cProcessTarget.enPriority)
                                    {
                                        cProcess.PriorityClass = cProcessTarget.enPriority;
                                        (new helpers.Logger()).WriteNotice("приоритет целевого процесса изменен:" + cProcessTarget.ToString());
                                    }
                                    if (cProcessTarget.bFail)
                                    {
                                        cProcessTarget.bFail = false;
                                        (new helpers.Logger()).WriteNotice("целевой процесс запущен " + cProcessTarget.ToString());
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
                    if (!Preferences.sRestartFile.IsNullOrEmpty())
                    {
                        string sRestartFNWE = sio.Path.GetFileNameWithoutExtension(Preferences.sRestartFile);
                        string sFirstRestartFN = sio.Directory.GetFiles(sio.Path.GetDirectoryName(Preferences.sRestartFile)).FirstOrDefault(o => sio.Path.GetFileName(o).StartsWith(sRestartFNWE) && !o.EndsWith("!"));
                        string sFirstRestartName = sio.Path.GetFileName(sFirstRestartFN);
                        if (System.IO.File.Exists(sFirstRestartFN))
                        {
                            (new Logger()).WriteNotice("try to restart something - see answer file in [" + System.IO.Path.GetDirectoryName(sFirstRestartFN) + "]");
                            string sAnswer = "processes detected:\n";
                            foreach (ProcessTarget cP in _aProcesses)
                                sAnswer += "[arg=" + cP.sArguments + "][name=" + cP.sName + "]\n";
                            Exception cCurrentException = null;
                            try
                            {
                                string[] aLines = System.IO.File.ReadLines(sFirstRestartFN).ToArray();
                                foreach (string sRestart in aLines)
                                {
                                    if (sRestart.IsNullOrEmpty() || sRestart.StartsWith("#"))
                                        continue;
                                    sAnswer += "read lines: [" + sRestart + "]\n";
                                    if (sRestart.ToLower() == "iis")
                                    {
                                        RestartIIS();
                                        sAnswer += "restarted IIS\n";
                                        continue;
                                    }
                                    ProcessTarget cPT = _aProcesses.FirstOrDefault(o => o.sArguments == sRestart);
                                    if (null != cPT)
                                    {
                                        (new helpers.Logger()).WriteDebug("try_to_stop_process = [" + cPT.sArguments + "]");
                                        cPT.Stop();
                                        sAnswer += "restarted " + sRestart + "\n";
                                    }
                                }
                                //System.Diagnostics.Process.Start(@"c:\Program Files\replica\ingenie\server\restart\ingenie.restart.exe");
                            }
                            catch (Exception ex)
                            {
                                cCurrentException = ex;
                                (new helpers.Logger()).WriteError(ex);
                            }
                            string sFileAnswer = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Preferences.sRestartFile), "answer_" + sFirstRestartName);

                            if (cCurrentException == null)
                                sAnswer += "done successfully\n";
                            else
                                sAnswer += "ERRORS! \n" + cCurrentException.Message + "\n" + (null == cCurrentException.InnerException ? "" : cCurrentException.InnerException.Message);
                            System.IO.File.WriteAllText(sFileAnswer, sAnswer);

                            System.IO.File.Move(sFirstRestartFN, sFirstRestartFN + "!");
                        }
                    }
                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                (new helpers.Logger()).WriteError(ex);
            }
            finally
            {
                (new helpers.Logger()).WriteNotice("сервер инициации остановлен");
            }
		}
		void RestartIIS()
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
        void WorkerBlender(object cState)
		{
			(new helpers.Logger()).WriteNotice("модуль управления рендером запущен");

			bool bLogged = false;
			string sTaskFile, sOutputFolder;
			string[] aLines;
            try
            {
                while (!bAbort)
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
                                    (new helpers.Logger()).WriteNotice("try to delete dir: " + sOutputFolder);
                                    sio.Directory.Delete(sOutputFolder, true);
                                }
                                (new helpers.Logger()).WriteNotice("try to create dir: " + sOutputFolder + "[parallel=" + _nBlenderQueueLength + "]");
                                sio.Directory.CreateDirectory(sOutputFolder);
                            }
                            catch (Exception ex)
                            {
                                (new helpers.Logger()).WriteError(ex);
                                continue;
                            }




                            System.Threading.Tasks.Task.Run(() =>
                            {
                                StartRender(new string[] { aLines[0], sio.Path.Combine(sTask, aLines[1]), sio.Path.Combine(sTask, "result"), sOutputFolder, aLines[2], aLines[3], aLines.Length > 4 ? aLines[4] : null });
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
            finally
            {
                (new helpers.Logger()).WriteNotice("модуль управления рендером остановлен");
            }
		}

		private void StartRender(string[] aValues)
		{
			try
			{
				Render(BlenderStartInfoGet(aValues[5].ToBool() ? sio.Path.Combine(aValues[3], aValues[4]) : null, aValues[0], aValues[1], null, null));
				if (null != aValues[6] && "zip_yes" == aValues[6])
				{
					string[] aFiles = sio.Directory.GetFiles(aValues[3]);
					string sFilename = sio.Path.Combine(aValues[3], "arch.zip");
					System.IO.FileStream sStream = System.IO.File.OpenWrite(sFilename);
                    using (ZipArchive cZip = new ZipArchive(sStream, ZipArchiveMode.Create, false))
					{
						foreach (string sFile in aFiles)
						{
							cZip.CreateEntryFromFile(sFile, sio.Path.GetFileName(sFile), CompressionLevel.NoCompression);
							sio.File.Delete(sFile);
						}
						cZip.Dispose();
					}
					sStream.Close();
                }
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
            bAbort = true;
            System.Threading.Thread.Sleep(1000);
            if (!Preferences.aProcesses.IsNullOrEmpty())
			{
				System.Diagnostics.Process cProcess;
                (new helpers.Logger()).WriteNotice("will kill [" + _aProcesses.Length + "] processes");
                foreach (ProcessTarget cProcessTarget in _aProcesses)
				{
					if (0 < cProcessTarget.nID)
					{
						try
						{
							cProcess = System.Diagnostics.Process.GetProcessById(cProcessTarget.nID);
                            (new helpers.Logger()).WriteNotice("will kill " + cProcessTarget.nID + " " + cProcessTarget.sName);
                            ProcessAsUser.KillProcess(cProcess, cProcessTarget.sOwner); // иначе прервать процесс можно только forced, а тогда не отрабатывается закрытие приложения (в приложении)
                        }
                        catch (Exception ex)
                        {
                            (new helpers.Logger()).WriteError(ex);
                        }
                    }
                }
                System.Threading.Thread.Sleep(4000);
                foreach (ProcessTarget cProcessTarget in _aProcesses)  // force kill if needed
                {
                    if (0 < cProcessTarget.nID)
                    {
                        try
                        {
                            cProcess = System.Diagnostics.Process.GetProcessById(cProcessTarget.nID);
                            if (cProcess != null)
                            {
                                (new helpers.Logger()).WriteWarning("will force kill " + cProcessTarget.nID + " " + cProcessTarget.sName);
                                cProcess.Kill();
                            }
                        }
                        catch (Exception ex)
                        {
                            (new helpers.Logger()).WriteError(ex);
                        }
                    }
                }
                System.Threading.Thread.Sleep(400);
            }
        }
        private ProcessStartInfo BlenderStartInfoGet(string sOutputTarget, string sBlendFile, string sPythonFile, string sEngine, string sThreads)
		{
			char[] chars = { 'p', 'a', 's', 's', 'w', 'o', 'd' };
			System.Security.SecureString ssPWD = new System.Security.SecureString();
			foreach (char ch in chars)
				ssPWD.AppendChar(ch);
			return new ProcessStartInfo()
			{
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				//UserName = "replica",  
				//Password = ssPWD,
				//Domain = "domain",
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
			cProcess.PriorityClass = ProcessPriorityClass.Normal;
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
