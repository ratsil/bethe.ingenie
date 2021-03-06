﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using helpers;
using System.IO;
using helpers.extensions;

namespace ingenie.plugins
{
    public class Blender : MarshalByRefObject, IPlugin
    {
        #region Members
		//static private byte _nQueueLength = 0;
        private Preferences _cPreferences;
        private EventDelegate Prepared;
        private EventDelegate Started;
        private EventDelegate Stopped;
        private string _sWorkFolder;
		private BTL.IEffect _iEffect;
        private BTL.EffectStatus _eStatus;
        private DateTime _dtStatusChanged;
		private bool _bPrepared;
		private object _oLock;
		#endregion

        public Blender()
        {
        }
        public void Create(string sWorkFolder, string sData)
        {
			_iEffect = null;
            _eStatus = BTL.EffectStatus.Idle;
            _dtStatusChanged = DateTime.Now;
            _sWorkFolder = sWorkFolder;
            _cPreferences = new Preferences(sWorkFolder, sData);
			_oLock = new object();
		}
        public void Prepare()
        {
			lock (_oLock)
			{
				if (_bPrepared)
				{
					(new Logger()).WriteWarning("Blender has already prepared!");
					return;
				}
				_bPrepared = true;
			}
			try
            {
				if (null != _cPreferences.iVideo)
				{
					if (!_cPreferences.bExists)
					{
						(new Logger()).WriteDebug2("render from prepare [pf="+ _cPreferences.sPythonFile + "]");
						Render(_cPreferences.sBlendFile, _cPreferences.sPythonFile, _cPreferences.sOutputTarget, "0", _cPreferences.bUseOutput, _cPreferences.bZip);
					}
					_cPreferences.EffectVideoInit();
					(_iEffect = (BTL.IEffect)_cPreferences.iVideo).Prepare();
				}
				else
					_eStatus = BTL.EffectStatus.Preparing;

				if (null != Prepared)
                    Plugin.EventSend(Prepared, this);
                (new Logger()).WriteDebug3("prepared");
            }
            catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
        }
		public static  void CopyAll(string sSource, string sTarget)
		{
			CopyAll(new DirectoryInfo(sSource), new DirectoryInfo(sTarget));
		}
		public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
		{
			// Check if the target directory exists, if not, create it.
			if (Directory.Exists(target.FullName) == false)
			{
				Directory.CreateDirectory(target.FullName);
			}

			// Copy each file into it’s new directory.
			foreach (FileInfo fi in source.GetFiles())
				fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);

			// если мозг ебёт при копировании файлов, то копировать вот этой штукой:
			//new CopyFileExtended(sFile, Path.Combine(_cPreferences._sOutputTarget, Path.GetFileName(sFile)), 0, 1000);

			// Copy each subdirectory using recursion.
			foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
			{
				DirectoryInfo nextTargetSubDir =
					target.CreateSubdirectory(diSourceSubDir.Name);
				CopyAll(diSourceSubDir, nextTargetSubDir);
			}
		}
		private void Render(string sBlendFile, string sPythonFile, string sOutput, string sPrefix, bool bUseOutput, bool bZip)//(System.Diagnostics.ProcessStartInfo cProcessStartInfo)
        {
			//int nPF = Convert.ToInt32(Path.GetFileName(sPythonFile).Substring(3, 4), 16) - 1;
			//string sPF = Path.Combine(Path.GetTempPath(), ("tmp" + nPF.ToString("X") + ".tmp"));
			//if (File.Exists(sPF))
			//	(new Logger()).WriteNotice("render task start [file is there! " + sPF + "]");
			//else
			//	(new Logger()).WriteNotice("render task start [no file! " + sPF + "]");







			string sTaskFoldername, sTaskFolder, sPythonFilename;
			while (Directory.Exists(sTaskFolder = Path.Combine(_cPreferences.sPath, sTaskFoldername = Path.GetRandomFileName()))) ;
			Directory.CreateDirectory(sTaskFolder);
			Encoding cEncoding = Encoding.GetEncoding(1251);
			File.WriteAllText(sPythonFile, File.ReadAllText(sPythonFile, cEncoding).Replace("{%_RENDER_FOLDER_%}", sTaskFoldername), cEncoding);
			(new Logger()).WriteNotice("render task start [" + sTaskFolder + "][" + sBlendFile + "][" + sPythonFile + "][" + sOutput + "]");
			File.Copy(sPythonFile, Path.Combine(sTaskFolder, sPythonFilename = Path.GetFileName(sPythonFile)));
			File.WriteAllLines(Path.Combine(sTaskFolder, "task"), new string[] { sBlendFile, sPythonFilename, sPrefix, bUseOutput.ToString(), bZip ? "zip_yes" : "zip_no" });







			//File.Copy(sPythonFile, Path.Combine(Path.GetTempPath(), sPythonFilename + ".py"));

			//if (File.Exists(sPF))
			//	(new Logger()).WriteNotice("render task start2 [file is there! " + sPF + "]");
			//else
			//	(new Logger()).WriteNotice("render task start2 [no file! " + sPF + "]");






			string sResultFolder = Path.Combine(sTaskFolder, "result");
			DateTime dtTimeout = DateTime.Now.AddMinutes(30);  // каминап теперь 17 минут где-то... иногда 24  (на normal priority на фоне эфира)
			while(!Directory.Exists(sResultFolder))
			{
				if(DateTime.Now > dtTimeout)
				{
					(new Logger()).WriteError("render timeout [" + sTaskFolder + "][" + sBlendFile + "][" + sPythonFile + "][" + sOutput + "]");
					_eStatus = BTL.EffectStatus.Error;
					if (!Directory.EnumerateFileSystemEntries(sOutput).Any())
					{
						Directory.Delete(sOutput);
						(new Logger()).WriteNotice("пустая директория удалена после таймаута: [" + sOutput + "]");

						if (null != _cPreferences.sPythonFile && File.Exists(_cPreferences.sPythonFile))
						{
							File.Delete(_cPreferences.sPythonFile);
							(new Logger()).WriteDebug("deleted on timeout: [pythonf=" + _cPreferences.sPythonFile + "]");
						}
					}
					return;
				}
				//что-нить и как-нить логировать и отваливаться по таймауту
				System.Threading.Thread.Sleep(200);
			}

			CopyAll(sResultFolder, sOutput);
			try
			{
				(new Logger()).WriteNotice("попытка удалить [" + sTaskFolder + "]");
				Directory.Delete(sTaskFolder, true);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
			(new Logger()).WriteNotice("render task stop [" + sTaskFolder + "][" + sBlendFile + "][" + sPythonFile + "][" + sOutput + "]");
			_cPreferences.RenderFinished();

			//bool bLogged = false;
			//while (_nQueueLength > Preferences.nQueueLengthMax)
			//{
			//    if (!bLogged)
			//        (new Logger()).WriteNotice("rendering: waiting [c:" + _nQueueLength + "][m:" + Preferences.nQueueLengthMax + "]");
			//    bLogged = true;
			//    System.Threading.Thread.Sleep(500);
			//}
			//_nQueueLength++;
			//(new Logger()).WriteNotice("rendering [" + cProcessStartInfo.Arguments + "]");
			//System.Diagnostics.Process cProcess = System.Diagnostics.Process.Start(cProcessStartInfo);
			////helpers.Logger cLogger = new helpers.Logger("render:" + GetHashCode(), "blender_render_last");
			////cLogger.WriteNotice("***************************************************************<br>Arguments:" + cProcess.StartInfo.Arguments);
			//_sMessage = "<br>";
			//cProcess.OutputDataReceived += (sender, args) => _sMessage += "[" + DateTime.Now.ToStr() + "][notice][" + args.Data + "]<br>";// cLogger.WriteNotice(args.Data);
			//cProcess.ErrorDataReceived += (sender, args) => _sMessage += "[" + DateTime.Now.ToStr() + "][error][" + args.Data + "]<br>";// cLogger.WriteNotice("error<br>", args.Data);
			//cProcess.BeginOutputReadLine();
			//cProcess.BeginErrorReadLine();
			//cProcess.WaitForExit();
			//_cPreferences.RenderFinished();
			////cLogger.WriteNotice(_sMessage + "Exit code:" + cProcess.ExitCode);
			//_nQueueLength--;
		}
		public void Start()
        {
			if (null == _iEffect)
			{
				System.Threading.ThreadPool.QueueUserWorkItem(delegate(object o)
				{
					try
					{
						if (null != _cPreferences.cData)
							_cPreferences.cData.Request(Render);
						else
						{
							(new Logger()).WriteDebug2("render from start [pf=" + _cPreferences.sPythonFile + "]");
							Render(_cPreferences.sBlendFile, _cPreferences.sPythonFile, _cPreferences.sOutputTarget, "0", _cPreferences.bUseOutput, _cPreferences.bZip);
						}
						Stop();
					}
					catch (Exception ex)
					{
						(new Logger()).WriteError(ex);
					}
				});
			}
			else
			{
				_iEffect.Stopped += iEffect_Stopped;
				_iEffect.Start();
			}

			(new Logger()).WriteDebug("started: [blendf=" + _cPreferences.sBlendFile+"][pythonf="+_cPreferences.sPythonFile+"]");
			if (null != Started)
				Plugin.EventSend(Started, this);
        }

		void iEffect_Stopped(BTL.Play.Effect cSender)
		{
			try
			{
				(new Logger()).WriteDebug("self stopped: [blendf=" + _cPreferences.sBlendFile + "][pythonf=" + _cPreferences.sPythonFile + "]");
				if (null != Stopped)
					Plugin.EventSend(Stopped, this);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
        public void Stop()
        {
            try
            {
				if (null != _iEffect)
				{
					if (BTL.EffectStatus.Running == _iEffect.eStatus)
						_iEffect.Stop();
				}
				(new Logger()).WriteDebug("stopped: [blendf=" + _cPreferences.sBlendFile + "][pythonf=" + _cPreferences.sPythonFile + "]");
				if (null != _cPreferences.sPythonFile && File.Exists(_cPreferences.sPythonFile))
				{
					File.Delete(_cPreferences.sPythonFile);
					(new Logger()).WriteDebug("deleted: [pythonf=" + _cPreferences.sPythonFile + "]");
				}
			}
			catch (Exception ex)
            {
                (new Logger()).WriteError(ex);
            }
            if (null != Stopped)
                Plugin.EventSend(Stopped, this);
        }

        #region IPlugin
        event EventDelegate IPlugin.Prepared
        {
            add
            {
                this.Prepared += value;
            }
            remove
            {
                this.Prepared -= value;
            }
        }
        event EventDelegate IPlugin.Started
        {
            add
            {
                this.Started += value;
            }
            remove
            {
                this.Started -= value;
            }
        }
        event EventDelegate IPlugin.Stopped
        {
            add
            {
                this.Stopped += value;
            }
            remove
            {
                this.Stopped -= value;
            }
        }
        void IPlugin.Create(string sWorkFolder, string sData)
        {
            this.Create(sWorkFolder, sData);
        }
        BTL.EffectStatus IPlugin.eStatus
        {
            get
            {
				if (null != _iEffect)
					return _iEffect.eStatus;
                return _eStatus;
            }
        }
        DateTime IPlugin.dtStatusChanged
        {
            get
            {
				if (null != _iEffect)
					return _iEffect.dtStatusChanged;
                return _dtStatusChanged;
            }
        }
        void IPlugin.Prepare()
        {
            this.Prepare();
        }
        void IPlugin.Start()
        {
            this.Start();
        }
        void IPlugin.Stop()
        {
            this.Stop();
        }
        #endregion
    }
}
