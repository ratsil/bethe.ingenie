using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using BTL;
using helpers;
using System.Xml;
using helpers.extensions;
using System.Diagnostics;
using System.IO;

namespace ingenie.plugins
{
    class Preferences
    {
		class Animation : BTL.Play.Animation
		{
			private bool _bInited;
			public Animation()
			{
				_bInited = false;
			}
			public void Init(string sFolder)
			{
				if (!_bInited)
				{
					_bInited = true;
					base.Init(sFolder);
				}
			}
		}
		class Video : BTL.Play.Video
		{
			private bool _bInited;
			public Video()
			{
				_bInited = false;
			}
			public void Init(string sFolder)
			{
				if (!_bInited)
				{
					string sFile = (new DirectoryInfo(sFolder)).GetFileSystemInfos().Where(o => !o.Attributes.HasFlag(FileAttributes.Directory) && !o.Attributes.HasFlag(FileAttributes.Hidden)).Select(o => o.FullName).FirstOrDefault();
					if (sFile.IsNullOrEmpty())
						throw new Exception("can't find any video file in folder " + sFolder);
					_bInited = true;
					base.Init(sFile);
				}
			}
		}
        public class Data
        {
            class Cache
            {
                public string sRequest;
                public byte nTemplate;
                public string sOutput;
                public int[] aHashcodes;
            }
			public delegate void Render(string sBlendFile, string sPythonFile, string sOutput, string sPrefix, bool bUseOutput);
            static private List<Cache> _aCache = new List<Cache>();
            private string _sRequest;
			private byte _nTemplate;
			private string _sValue;
			private Preferences _cPreferences;

            public Data(Preferences cPreferences, string sValue)
            {
                _cPreferences = cPreferences;
                string[] aValues = sValue.ToLower().Split(':');
                _sRequest = aValues[0];
                _nTemplate = (1 < aValues.Length ? aValues[1].ToByte() : (byte)0);
				_sValue = (2 < aValues.Length ? aValues[2] : null );
            }

            public void Request(Render fRender)
            {
                _cPreferences._bExists = true;

                XmlNode cXNData = ingenie.plugins.Data.Get(_sRequest, _nTemplate, _sValue);
                XmlNode[] aItems = cXNData.NodesGet("item");
                Cache cCached;
				bool bClear = false;
				lock (_aCache)
					if ((bClear = (1 > _aCache.Count)) || null == (cCached = _aCache.FirstOrDefault(o => _cPreferences._sOutputTarget == o.sOutput && _sRequest == o.sRequest && _nTemplate == o.nTemplate)))
						_aCache.Add(cCached = new Cache() { sOutput = _cPreferences._sOutputTarget, sRequest = _sRequest, nTemplate = _nTemplate, aHashcodes = new int[aItems.Length + 1] });
                lock (cCached)
                {
                    int nHash = cXNData.OuterXml.GetHashCode();
                    if (Directory.Exists(_cPreferences._sOutputTarget) && nHash == cCached.aHashcodes[0])
                        return;
                    if (cCached.aHashcodes.Length != aItems.Length)
                        Array.Resize<int>(ref cCached.aHashcodes, aItems.Length + 1);
                    cCached.aHashcodes[0] = nHash;
                    string sPython, sPythonFile, sFolder;
                    sPython = sPythonFile = sFolder = null;
                    for (int nID = 0; aItems.Length > nID; nID++)
                    {
                        if (cCached.aHashcodes[nID + 1] == (nHash = aItems[nID].OuterXml.GetHashCode()))
                            continue;
                        if (null == sFolder)
                        {
                            sFolder = Path.Combine(_cPreferences._sOutputTarget, ".render");
                            if (Directory.Exists(sFolder))
                                Directory.Delete(sFolder, true);
                            Directory.CreateDirectory(sFolder);
                            sPythonFile = Path.Combine(sFolder, "python.py");
                        }
                        sPython = _cPreferences._sPython;
                        foreach (XmlNode cXNMacro in aItems[nID].NodesGet("item"))
                            sPython = sPython.Replace("{%_" + cXNMacro.AttributeValueGet("id") + "_%}", cXNMacro.InnerText.FromXML());
                        File.WriteAllText(sPythonFile, sPython, System.Text.Encoding.GetEncoding(1251));
                        fRender(_cPreferences._sBlendFile, sPythonFile, sFolder, aItems[nID].AttributeValueGet("output"), _cPreferences._bUseOutput);//, _cPreferences._sEngine, _cPreferences._sThreads));
                        cCached.aHashcodes[nID + 1] = nHash;
                    }
					if (bClear)
					{
						foreach (FileInfo cFI in (new DirectoryInfo(_cPreferences._sOutputTarget)).GetFiles())
						{
							try
							{
								cFI.Delete();
							}
							catch (Exception ex)
							{
								(new Logger()).WriteWarning(ex);
							}
						}
					}
                    if (null != sFolder)
                    {
                        File.Delete(sPythonFile);
                        foreach (string sFile in Directory.GetFiles(sFolder))
                        {
							for (int nI = 0; nI < 10; nI++)
							{
								try
								{
									//File.Copy(sFile, Path.Combine(_cPreferences._sOutputTarget, Path.GetFileName(sFile)), true);
									new CopyFileExtended(sFile, Path.Combine(_cPreferences._sOutputTarget, Path.GetFileName(sFile)), 0, 1000);
									break;
								}
								catch (Exception ex)
								{
									(new Logger()).WriteError(ex);
									System.Threading.Thread.Sleep(300);
								}
							}
							try
							{
								File.Delete(sFile);
							}
							catch (Exception ex)
							{
								(new Logger()).WriteError(ex);
							}
							System.Threading.Thread.Sleep(1);  // проверка   //DNF
						}
                    }
                }
            }
        }
		//public ProcessStartInfo cProcessStartInfo
		//{
		//    get
		//    {
		//        if (null == _cProcessStartInfo)
		//        {
					//if (null == _sPythonFile)
					//    _sPythonFile = Path.GetTempFileName();
					//File.WriteAllText(_sPythonFile, _sPython, System.Text.Encoding.GetEncoding(1251));
		//            _cProcessStartInfo = ProcessStartInfoGet(_bOutput ? AddExclamationToFolder(_sOutputTarget) + "\\0" : null, _sBlendFile, _sPythonFile, _sEngine, _sThreads);
		//        }
		//        return _cProcessStartInfo;
		//    }
		//}
		public IVideo iVideo
		{
			get
			{
				//if (null != _iVideo)
				//{
				//    if (_iVideo is Animation)
				//        ((Animation)_iVideo).Init(_sOutputTarget);
				//    else if(_iVideo is Video)
				//        ((Video)_iVideo).Init(_sOutputTarget);
				//}
				return _iVideo;
			}
		}
		public string sPath
		{
			get
			{
				return _sPath;
			}
		}
		public string sOutputTarget
        {
            get
            {
				return AddExclamationToFolder(_sOutputTarget);
            }
        }
		public string sBlendFile
		{
			get
			{
				return _sBlendFile;
			}
		}
		public string sPythonFile
		{
			get
			{
				if (null == _sPythonFile)
					File.WriteAllText(_sPythonFile = Path.GetTempFileName(), _sPython, System.Text.Encoding.GetEncoding(1251));
				return _sPythonFile;
			}
		}
		public bool bExists
		{
			get
			{
				return _bExists;
			}
		}
		public bool bUseOutput
		{
			get
			{
				return _bUseOutput;
			}
		}
		public Data cData { get; private set; }

        static private string _sCache;
        static private string _sPath;
		static private object _oLock = new object();

        //private ProcessStartInfo _cProcessStartInfo;
        private string _sPythonFile;
        private string _sOutputTarget;
        private bool _bExists;
		private bool _bExistsExclamation;
		private IVideo _iVideo;

		private string _sEngine;
		private string _sThreads;
        private string _sPython;
        private string _sBlendFile;
		private bool _bUseOutput;

		private bool WaitForTargetFolder(string sTarget)
		{
			string sWaitTarget = AddExclamationToFolder(sTarget);
			if (Directory.Exists(sWaitTarget))
			{
				(new Logger()).WriteNotice("Ждём пока исчезнет папка: " + sWaitTarget);
				DateTime dtWait = DateTime.Now.AddMinutes(10);
				while (Directory.Exists(sWaitTarget) && DateTime.Now < dtWait)
					System.Threading.Thread.Sleep(1000);
				if (Directory.Exists(sWaitTarget))
					throw new Exception("Too long waiting for folder: " + sWaitTarget);
			}
			(new Logger()).WriteNotice("папка исчезла: " + sWaitTarget);
			return Directory.Exists(sTarget);
		}
		private string AddExclamationToFolder(string sTarget)
		{
			return sTarget.EndsWith("\\") || sTarget.EndsWith("/") ? sTarget.Substring(0, sTarget.Length - 1) + "!" : sTarget + "!";
		}
        public Preferences(string sWorkFolder, string sData)
        {
			_bUseOutput = true;
            XmlDocument cXmlDocument = new XmlDocument();
            XmlNode cXmlNode;
			string sExclamationTarget;
			bool bExclamationExists = false;
			bool bOutput = true;
            if (null == _sPath)
            {
                cXmlDocument.Load(Path.Combine(sWorkFolder, "preferences.xml"));
                cXmlNode = cXmlDocument.NodeGet("preferences/blender");
                _sPath = cXmlNode.AttributeValueGet("path");
                _sCache = cXmlNode.AttributeValueGet("cache");
                if (!Directory.Exists(_sCache))
                    Directory.CreateDirectory(_sCache);
            }
            cXmlDocument.LoadXml(sData);
            cXmlNode = cXmlDocument.NodeGet("data");
            string sValue = cXmlNode.AttributeValueGet("data", false);
            if (null != sValue)
                cData = new Data(this, sValue);
			_sOutputTarget = cXmlNode.AttributeValueGet("output", false);
			if(null != _sOutputTarget && _sOutputTarget.StartsWith("*"))
			{
				_bUseOutput = false;
				if((_sOutputTarget = _sOutputTarget.Substring(1)).IsNullOrEmpty())
					_sOutputTarget = null;
			}
			if ((null == _sOutputTarget))  // ЛЁХ! если output есть, всегда ли надо чтобы было !_bExists  даже если папка существует????????  раньше так было, но правильно ли это????
			{
				bOutput = false;
				int nHash = sData.GetHashCode();
				_sOutputTarget = Path.Combine(_sCache, nHash.ToString());
			}
				
			sExclamationTarget = AddExclamationToFolder(_sOutputTarget);
			lock (_oLock)
			{
				if (!(_bExists = Directory.Exists(_sOutputTarget)))
				{
					bExclamationExists = Directory.Exists(sExclamationTarget);
					if (bExclamationExists && Directory.GetLastWriteTime(sExclamationTarget) < DateTime.Now.AddHours(-3))  // временная папка заброшена
					{
						Directory.Delete(sExclamationTarget, true);
						(new Logger()).WriteWarning("Удалили заброшенную Папку: " + sExclamationTarget);
						bExclamationExists = false;
					}
					if (!bExclamationExists)
						Directory.CreateDirectory(sExclamationTarget);
				}
			}

			if (bExclamationExists)
			{
				_bExists = WaitForTargetFolder(_sOutputTarget);

				if (!_bExists)
					throw new Exception("imposible state - folder vanished: " + _sOutputTarget);
			}

			DateTime dtNow = DateTime.Now;

			if (_bExists)
			{
				try
				{
					Directory.SetLastWriteTime(_sOutputTarget, dtNow);
				}
				catch { }
			}

			if (!bOutput)
			{
				if (!_bExists)
				{
					TimeSpan tsAgeMaximum = TimeSpan.FromDays(7);
					string sFilesDeleted = "", sFilesDeleteFailed = "";
					foreach (FileSystemInfo cFSInf in (new DirectoryInfo(_sCache)).GetFileSystemInfos())
					{
						if (!cFSInf.Attributes.HasFlag(FileAttributes.Directory) || tsAgeMaximum > dtNow.Subtract(cFSInf.LastWriteTime))
							continue;
						try
						{
							Directory.Delete(cFSInf.FullName, true);
							sFilesDeleted += cFSInf.Name + ",";
						}
						catch (Exception ex)
						{
							(new Logger()).WriteError(ex);
							sFilesDeleteFailed += cFSInf.Name + ",";
						}
					}
					if (0 < sFilesDeleted.Length)
						(new Logger()).WriteNotice("Папки удалены из кэша:" + sFilesDeleted.TrimEnd(',') + ". Не удалось удалить:" + sFilesDeleteFailed.TrimEnd(','));//TODO LANG
				}
			}

			XmlNode cChildNode = null;
			_sEngine = cXmlNode.AttributeValueGet("engine", false);
			_sThreads = cXmlNode.AttributeValueGet("threads", false);
			cChildNode = cXmlNode.NodeGet("python", false);
			if (null != cChildNode)
				_sPython = cChildNode.InnerXml.Trim().FromXML();
			_sPythonFile = null;
			_sBlendFile = cXmlNode.AttributeValueGet("blend");

			if (null != (cChildNode = cXmlNode.NodeGet("animation", false)))
			{
				_iVideo = new Animation() { nLoopsQty= (null == (sValue = cChildNode.AttributeValueGet("loops", false)) ? (ushort)1 : sValue.ToUShort()) };
			}
			else if (null != (cChildNode = cXmlNode.NodeGet("video", false)))
			{
				_iVideo = new Video();
			}
			if(null != _iVideo)
			{
				if(null != (sValue = cChildNode.AttributeValueGet("cuda", false)))
					_iVideo.bCUDA = sValue.ToBool();
				if(null != (sValue = cChildNode.AttributeValueGet("opacity", false)))
					_iVideo.bOpacity = sValue.ToBool();
				if(null != (sValue = cChildNode.AttributeValueGet("layer", false)))
					((IEffect)_iVideo).nLayer = sValue.ToUShort();
				if (null != (cChildNode = cChildNode.NodeGet("size", false)))
				{
					_iVideo.stArea = new Area(
									cChildNode.AttributeGet<short>("left"),
									cChildNode.AttributeGet<short>("top"),
									cChildNode.AttributeGet<ushort>("width"),
									cChildNode.AttributeGet<ushort>("height")
									);
				}
			}

            /*
            Plugin
            (all)plugin@file                - обязательный параметр. путь к dll плагина
            (all)plugin@class               - обязательный параметр. название класса плагина
            (chat)plugin/data               - не используется
            (rssroll)plugin/data            - не используется
            (blender)plugin/data            - обязательный параметр. содержит текст python-скрипта
            (blender)plugin/data@effect     - обязательный параметр. может принимать значения: animation, video
            (blender)plugin/data@blend      - обязательный параметр. путь к blend-файлу
            (blender)plugin/data@engine     - необязательный параметр. может принимать значения из списка:  BLENDER_RENDER, BLENDER_GAME, CYCLES
            (blender)plugin/data@threads    - необязательный параметр. кол-во нитей. может принимать значения от 0 до 64. 0 - кол-во нитей равное кол-ву системных процессоров

            пример:
            <plugin file="c:/.../blender.dll" class="Blender">
                <data effect="animation" blend="c:/.../target.blend" engine="CYCLES" threads="0">
                    import bpy
                    bpy.ops.render.render(animation=True)
                </data>
            </plugin>

            */
        }
		//static private ProcessStartInfo ProcessStartInfoGet(string sOutputTarget, string sBlendFile, string sPythonFile, string sEngine, string sThreads)
		//{
		//    return new ProcessStartInfo()
		//    {
		//        CreateNoWindow = true,
		//        WindowStyle = ProcessWindowStyle.Hidden,
		//        UseShellExecute = false,
		//        //LoadUserProfile = true,
		//        RedirectStandardOutput = true,
		//        RedirectStandardError = true,
		//        FileName = _sExecutable,
		//        Arguments = "-b \"" + sBlendFile
		//            + "\"" + (null == sEngine ? "" : " -E " + sEngine)
		//            + " -t " + (null == sThreads ? "0" : sThreads)
		//            + (null == sPythonFile ? "" : " -P \"" + sPythonFile + "\"")
		//            + (null == sOutputTarget ? "" : " -o \"" + sOutputTarget + "\"") + " -x 1 -a"
		//    };
		//}
		public void EffectVideoInit()
		{
			if (null != _iVideo)
			{
				if (_iVideo is Animation)
					((Animation)_iVideo).Init(_sOutputTarget);
				else if (_iVideo is Video)
					((Video)_iVideo).Init(_sOutputTarget);
			}
		}
		public void RenderFinished()
		{
			try
			{
				if (Directory.Exists(AddExclamationToFolder(_sOutputTarget)))
				{
					if (Directory.Exists(_sOutputTarget))
						Directory.Delete(_sOutputTarget, true);
					Directory.Move(AddExclamationToFolder(_sOutputTarget), _sOutputTarget);
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
	}
}