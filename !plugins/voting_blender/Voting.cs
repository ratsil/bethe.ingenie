//#define DEBUG1
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Linq;
using BTL.Play;
using btl = BTL.Play;
using System.Collections;
using System.Diagnostics;
using System.Xml;
using System.Timers;
using System.IO;
using System.Threading;
using ingenie;
using helpers;
using helpers.extensions;

namespace ingenie.plugins
{
    public class Voting : MarshalByRefObject, IPlugin
    {
        #region Members
        private Preferences _cPreferences;
        private EventDelegate Prepared;
        private EventDelegate Started;
        private EventDelegate Stopped;
        private btl.Playlist _cPLPhotoLeft, _cPLPhotoRight, _cPLMatTop, _cPLMatMiddle, _cPLMatBottom;
		private bool _bStopping = false;
		private bool _bBottomStopping = false;
		private bool _bBlenderDidNewVotes = false;
		private bool _bBlenderIsPreparing = false;
		private DateTime _dtPhotosLastStart;
		private int _dtPhotosStartInterval;
		private uint _nEmergencyDuration;
		private ushort _nLoopTop, _nLoopMid, _nLoopImages;
		private DateTime _dtStatusChanged;
		private BTL.EffectStatus _eStatus;
		private DateTime _dtTestDelay;
		private uint _nVotesL, _nVotesR;
		private bool _bFirstTime;
		public BTL.EffectStatus eStatus
		{
			get
			{
				return _eStatus;
			}
			set
			{
				_eStatus = value;
				_dtStatusChanged = DateTime.Now;
			}
		}
		private string sFolderVotes
		{
			get
			{
				return Path.Combine(_cPreferences.sFolderBlender, "votes");
			}
		}
		private ushort nLoopMid
		{
			get
			{
				if (!_bStopping)
					return _nLoopMid;
				else
					return 10;
			}
		}
		private ushort nLoopTop
		{
			get
			{
				if (!_bStopping)
					return _nLoopTop;
				else
					return 10;
			}
		}
		private ushort nLoopImages
		{
			get
			{
				if (!_bStopping)
					return _nLoopImages;
				else
					return 10;
			}
		}

		private List<Effect> _aLoops;
		private ManualResetEvent _cPrepare;
		#endregion

		public Voting()
        {
			_bFirstTime = true;
			_dtTestDelay = DateTime.Now;
			_nVotesL = 1;
			_nVotesR = 2;
			_nLoopTop = 200;
			_nLoopMid = 125;
			_nLoopImages = 300;
			_nEmergencyDuration = 50;
			_aLoops = new List<Effect>();
			_dtPhotosStartInterval = 60; // секунд
			eStatus = BTL.EffectStatus.Idle;
        }
        public void Create(string sWorkFolder, string sData)
        {
			_cPreferences = new Preferences(sWorkFolder, sData);
        }
		public void Prepare()
        {
			eStatus = BTL.EffectStatus.Preparing;
			_cPrepare = new ManualResetEvent(false);
            try
            {
                //PixelsMap.DisComInit();

				if (!Directory.Exists(Path.Combine(_cPreferences.sFolderMat, "voting_bot_loop")))
				{
					Render(_cPreferences.cMat.OuterXml, (IPlugin) => _cPrepare.Set());
					_cPrepare.WaitOne();
					_cPrepare.Reset();
				}
				PrepareVotes();
				_cPrepare.WaitOne();
				_cPrepare.Reset();
				PreparePlaylists();
				if (null != Prepared)
					Plugin.EventSend(Prepared, this);
			}
            catch (Exception ex)
            {
				eStatus = BTL.EffectStatus.Error;
                (new Logger()).WriteError(ex);
			}
        }
		public void PrepareVotes()
        {
            try
            {
				XmlNode cXNData = helpers.data.Data.Get("polls.zed", 0, _cPreferences.cPoll.sName);
				Preferences.Poll.Candidate[] aCandidates = _cPreferences.cPoll.aCandidates;
				uint[] aVotes = cXNData.NodesGet("item").
					Select(o => new Preferences.Poll.Candidate() { sName = o.AttributeValueGet("name").ToLower(), nVotesQty = o.AttributeGet<uint>("votes") }).
					Where(o => 0 < aCandidates.Count(o1 => o1.sName == o.sName)).
					OrderBy(o => o.sName == aCandidates[0].sName ? 0 : 1). 
					Select(o => o.nVotesQty).
					ToArray();

				if (2 == aVotes.Length)
				{
#if DEBUG
					//DNF
					if (DateTime.Now.Subtract(_dtTestDelay).TotalSeconds > 50)
					{
						_nVotesL += 97;
						_nVotesR = (uint)((_nVotesR + 1) * 1.01);
						aVotes[0] = _nVotesL;
						aVotes[1] = _nVotesR;
					}
					//DNF
#endif
					if (_bFirstTime)
					{
						(new Logger()).WriteDebug("initial votes are: [" + aCandidates[0].sName + "=" + aVotes[0] + "][" + aCandidates[1].sName + "=" + aVotes[1] + "]");
						_bFirstTime = false;
						aCandidates[0].nVotesQty = aVotes[0];
						aCandidates[1].nVotesQty = aVotes[1];
						_cPreferences.cPoll.aCandidates[0].nVotesQty = aVotes[0];
						_cPreferences.cPoll.aCandidates[1].nVotesQty = aVotes[1];
					}


					if (BTL.EffectStatus.Preparing == eStatus || aVotes[0] != aCandidates[0].nVotesQty || aVotes[1] != aCandidates[1].nVotesQty)
					{
						if (BTL.EffectStatus.Preparing != eStatus)
							(new Logger()).WriteDebug("votes have changed to: [L=" + aVotes[0] + "][R=" + aVotes[1] + "]");
						// votes_ - это для более аккуратного копирования вместо штатного см. MoveRenderedVotesToNormalPlace()
						Render(_cPreferences.cVotes.OuterXml.Replace("votes\"><python", "votes_\"><python").   
							Replace("{%_TEXT_VOTES_LEFT_%}", _cPreferences.cPoll.aCandidates[0].nVotesQty.ToStr()).
							Replace("{%_TEXT_VOTES_RIGHT_%}", _cPreferences.cPoll.aCandidates[1].nVotesQty.ToStr()).
							Replace("{%_TEXT_NEW_VOTES_LEFT_%}", aVotes[0].ToStr()).
							Replace("{%_TEXT_NEW_VOTES_RIGHT_%}", aVotes[1].ToStr())
							, VotesPrepared);
						_cPreferences.cPoll.aCandidates[0].nVotesQty = aVotes[0];
						_cPreferences.cPoll.aCandidates[1].nVotesQty = aVotes[1];
					}
					else
						_bBlenderIsPreparing = false;
				}
				else
				{
					(new Logger()).WriteError("received wrong votes");
					_bBlenderIsPreparing = false;
				}
			}
            catch (Exception ex)
            {	
				_bBlenderIsPreparing = false;
				eStatus = BTL.EffectStatus.Error;
				(new Logger()).WriteError(ex);
            }
		}
		public void PreparePlaylists()
        {
			_bBlenderDidNewVotes = false;
			_bBlenderIsPreparing = false;

			_cPLMatTop = new btl.Playlist()
			{
                stMergingMethod = _cPreferences.stMerging,
				nLayer = _cPreferences.nLayer,
				nDelay = 0,
				cDock = new Dock(_cPreferences.nLeft, (short)(_cPreferences.nTop + 67)),
				bStopOnEmpty = true,
				bOpacity = false
			};
			_cPLMatMiddle = new btl.Playlist()
			{
                stMergingMethod = _cPreferences.stMerging,
				nLayer = _cPreferences.nLayer,
				nDelay = 34,
				cDock = new Dock((short)(_cPreferences.nLeft + 186), (short)(_cPreferences.nTop + 87)),
				bStopOnEmpty = true,
				bOpacity = false
			};
			_cPLMatBottom = new btl.Playlist()
			{
                stMergingMethod = _cPreferences.stMerging,
				nLayer = _cPreferences.nLayer,
				nDelay = 22,
				cDock = new Dock(_cPreferences.nLeft, (short)(_cPreferences.nTop + 87)),
				bStopOnEmpty = true,
				bOpacity = false
			};
			_cPLPhotoLeft = new btl.Playlist()   
			{
                stMergingMethod = _cPreferences.stMerging,
				nLayer = _cPreferences.nLayer, 
				nDelay = 50,
				cDock = new Dock(_cPreferences.nLeft, _cPreferences.nTop),
				bStopOnEmpty = false,
				bOpacity = false
			};
			_cPLPhotoRight = new btl.Playlist()
			{
                stMergingMethod = _cPreferences.stMerging,
				nLayer = _cPreferences.nLayer,
				nDelay = 50,
				cDock = new Dock((short)(_cPreferences.nLeft + 253), _cPreferences.nTop),
				bStopOnEmpty = false,
				bOpacity = false
			};
			Animation cLoop;

			_cPLMatTop.AnimationAdd(new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_top_in")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = false, bOpacity = false, nLoopsQty = 1 }, 0);
			cLoop = new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_top_loop1")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = true, bOpacity = false, nLoopsQty = _nLoopTop, oTag = "voting_top_loop1" };
			lock (_aLoops)
				_aLoops.Add(cLoop);
			_cPLMatTop.AnimationAdd(cLoop, 0);
			_cPLMatTop.EffectStarted += new ContainerVideoAudio.EventDelegate(_cPLMatTop_EffectStarted);
			_cPLMatTop.EffectStopped += new ContainerVideoAudio.EventDelegate(RemoveOnStopped);

			_cPLMatBottom.AnimationAdd(new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_bot_in")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = false, bOpacity = false, nLoopsQty = 1 }, 0);
			_cPLMatBottom.AnimationAdd(new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_bot_loop")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = true, bOpacity = false, nLoopsQty = 0 }, 0);

			_cPLMatMiddle.AnimationAdd(new Animation(Path.Combine(_cPreferences.sFolderVotes, "voting_mid_in")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = false, bOpacity = false, nLoopsQty = 1 }, 0);
			cLoop = new Animation(Path.Combine(_cPreferences.sFolderVotes, "voting_mid_loop1")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = true, bOpacity = false, nLoopsQty = _nLoopMid, oTag = "voting_mid_loop1" };
			_cPLMatMiddle.AnimationAdd(cLoop, 0);
			lock (_aLoops)
				_aLoops.Add(cLoop);
			_cPLMatMiddle.EffectStarted += new ContainerVideoAudio.EventDelegate(_cPLMatMiddle_EffectStarted);
			_cPLMatMiddle.EffectStopped += new ContainerVideoAudio.EventDelegate(RemoveOnStopped);

			_cPLMatTop.Stopped += new Effect.EventDelegate(PL_Stopped);
			_cPLMatBottom.Stopped += new Effect.EventDelegate(PL_Stopped);
			_cPLMatMiddle.Stopped += new Effect.EventDelegate(PL_Stopped);
			_cPLPhotoLeft.Stopped += new Effect.EventDelegate(PL_Stopped);
			_cPLPhotoRight.Stopped += new Effect.EventDelegate(PL_Stopped);
			_cPLPhotoLeft.EffectStopped += new ContainerVideoAudio.EventDelegate(RemoveOnStopped);
			_cPLPhotoRight.EffectStopped += new ContainerVideoAudio.EventDelegate(RemoveOnStopped);

			_cPLMatTop.Prepare();
			_cPLMatBottom.Prepare();  
			_cPLMatMiddle.Prepare();

			_cPLPhotoLeft.Prepare();
			_cPLPhotoRight.Prepare();
			_cPLPhotoLeft.Start();
			_cPLPhotoRight.Start();

            (new Logger()).WriteDebug3("ok");
        }

		void RemoveOnStopped(Effect cSender, Effect cEffect)
		{
			if (_bStopping)
			{
				if (cEffect.oTag == "voting_imagesL_out")
					_cPLPhotoLeft.Stop();
				if (cEffect.oTag == "voting_imagesR_out")
					_cPLPhotoRight.Stop();
			}

			lock (_aLoops)
			{
				if (_aLoops.Contains(cEffect))
					_aLoops.Remove(cEffect);
			}
		}
		void PL_Stopped(Effect cSender)
		{
			if (_cPLMatTop.eStatus == BTL.EffectStatus.Stopped
					&& _cPLMatBottom.eStatus == BTL.EffectStatus.Stopped
					&& _cPLMatMiddle.eStatus == BTL.EffectStatus.Stopped
					&& _cPLPhotoLeft.eStatus == BTL.EffectStatus.Stopped
					&& _cPLPhotoRight.eStatus == BTL.EffectStatus.Stopped)
			{
				eStatus = BTL.EffectStatus.Stopped;
			}
		}
		void VotesPrepared(IPlugin iSender)
		{
			MoveRenderedVotesToNormalPlace();  // from votes_  to votes
			if (_bBlenderIsPreparing)
			{
				_bBlenderDidNewVotes = true;
				_bBlenderIsPreparing = false;
			}
			_cPrepare.Set();
		}
		void Render(string sData, EventDelegate fPrepared)
		{
			Blender cBlender = new Blender();
			cBlender.Create(_cPreferences.sFolderBlender, sData);
			((IPlugin)cBlender).Stopped += fPrepared;
			cBlender.Start();
		}
		void _cPLMatMiddle_EffectStarted(Effect cSender, Effect cEffect)
		{
			Animation cLoop = null;

			if (cEffect is Animation && ((Animation)cEffect).oTag == "new_votes_loop")  // временные новые голоса
			{
				CopyTMPToNormalPlace();
				lock (_aLoops)
				{
					cLoop = new Animation(Path.Combine(_cPreferences.sFolderVotes, "voting_mid_loop1")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = true, bOpacity = false, nLoopsQty = nLoopMid, oTag = "voting_mid_loop1" };
					_cPLMatMiddle.AnimationAdd(cLoop, 0);
					_aLoops.Add(cLoop);
				}
				return;
			}

			if (cEffect is Animation && ((Animation)cEffect).oTag == "voting_mid_loop1")  // старые голоса
			{
				if (_bStopping)
				{
					_cPLMatBottom.AnimationAdd(new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_bot_loop")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = true, bOpacity = false, nLoopsQty = (ushort)(nLoopMid - 7) }, 0); // на 7 кадров раньше должен уходить чем мид
					_cPLMatBottom.AnimationAdd(new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_bot_out")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = false, bOpacity = false, nLoopsQty = 1 }, 0);
					_cPLMatBottom.nDuration = _cPLMatBottom.nFrameCurrent + _nEmergencyDuration;
					_cPLMatBottom.Skip(true, 0);
					_bBottomStopping = true;
					_cPLMatMiddle.AnimationAdd(new Animation(Path.Combine(_cPreferences.sFolderVotes, "voting_mid_loop1")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = true, bOpacity = false, nLoopsQty = 10 }, 0);
					_cPLMatMiddle.AnimationAdd(new Animation(Path.Combine(_cPreferences.sFolderVotes, "voting_mid_out")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = false, bOpacity = false, nLoopsQty = 1 }, 0);
					_cPLMatMiddle.nDuration = _cPLMatMiddle.nFrameCurrent + _nEmergencyDuration;
					_cPLMatMiddle.Skip(false, 0);
					return;
				}

				if (_bBlenderDidNewVotes)
				{
					_bBlenderDidNewVotes = false;
					_cPLMatMiddle.AnimationAdd(new Animation(Path.Combine(_cPreferences.sFolderVotes, "!old_percents__new_votes")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = false, bOpacity = false, nLoopsQty = 1 }, 0);
					lock (_aLoops)
					{
						cLoop = new Animation(Path.Combine(_cPreferences.sFolderVotes, "!new_votes_loop")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = true, bOpacity = false, nLoopsQty = nLoopMid, oTag = "new_votes_loop" };
						_cPLMatMiddle.AnimationAdd(cLoop, 0);
						_aLoops.Add(cLoop);
					}
					return;
				}
				lock (_aLoops)
				{
					cLoop = new Animation(Path.Combine(_cPreferences.sFolderVotes, "voting_mid_loop1")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = true, bOpacity = false, nLoopsQty = nLoopMid, oTag = "voting_mid_loop1" };
					_cPLMatMiddle.AnimationAdd(cLoop, 0);
					_aLoops.Add(cLoop);
				}
			}

			if (!_bStopping && !_bBlenderDidNewVotes && !_bBlenderIsPreparing)
			{
				_bBlenderIsPreparing = true;
				PrepareVotes(); 
			}
		}
		
		void _cPLMatTop_EffectStarted(Effect cSender, Effect cEffect)
		{
			Animation cLoop = null;

			if (!_bStopping && cEffect is Animation && ((Animation)cEffect).oTag == "voting_top_loop1" && DateTime.Now.Subtract(_dtPhotosLastStart).TotalSeconds > _dtPhotosStartInterval)
			{
				StartPhotos();
			}

			if (cEffect is Animation && ((Animation)cEffect).oTag == "voting_top_loop1")
			{
				if (_bStopping)
				{
					if (_bBottomStopping)
					{
						_cPLMatTop.AnimationAdd(new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_top_loop1")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = true, bOpacity = false, nLoopsQty = nLoopTop }, 0);
						_cPLMatTop.AnimationAdd(new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_top_out")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = false, bOpacity = false, nLoopsQty = 1 }, 0);
						_cPLMatTop.nDuration = _cPLMatTop.nFrameCurrent + _nEmergencyDuration;
						_cPLMatTop.Skip(true, 0);
					}
					else
						_cPLMatTop.AnimationAdd(new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_top_loop1")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = true, bOpacity = false, nLoopsQty = 25, oTag = "voting_top_loop1" }, 0);
					return;
				}
				_cPLMatTop.AnimationAdd(new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_top_switch1")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = false, bOpacity = false, nLoopsQty = 1, oTag = "voting_top_switch1" }, 0);
				lock (_aLoops)
				{
					cLoop = new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_top_loop2")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = true, bOpacity = false, nLoopsQty = nLoopTop, oTag = "voting_top_loop2" };
					_cPLMatTop.AnimationAdd(cLoop, 0);
					_aLoops.Add(cLoop);
				}
				_cPLMatTop.AnimationAdd(new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_top_switch2")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = false, bOpacity = false, nLoopsQty = 1 }, 0);
				lock (_aLoops)
				{
					cLoop = new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_top_loop1")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = true, bOpacity = false, nLoopsQty = nLoopTop, oTag = "voting_top_loop1" };
					_cPLMatTop.AnimationAdd(cLoop, 0);
					_aLoops.Add(cLoop);
				}
			}
		}

        public void Start()
        {

			eStatus = BTL.EffectStatus.Running;
			_cPLMatTop.Start();  
			_cPLMatBottom.Start();
			_cPLMatMiddle.Start();

			_cPreferences.PollUpdate();

			//DNF
			//System.Threading.Thread _cThreadFramesGettingWorker;
			//_cThreadFramesGettingWorker = new System.Threading.Thread(FramesGettingWorker);
			//_cThreadFramesGettingWorker.IsBackground = true;
			//_cThreadFramesGettingWorker.Priority = System.Threading.ThreadPriority.Normal;
			//_cThreadFramesGettingWorker.Start();




            if (null != Started)
                Plugin.EventSend(Started, this);
        }

		private void FramesGettingWorker(object cState)
		{
			System.Threading.Thread.Sleep(4000);
			if (_bBlenderDidNewVotes)
				_bBlenderDidNewVotes = false;
			else
				PrepareVotes();
		}


			// асинхронно выполнить!

			//изготовить 3 пары переходов и лупов - со старых процентов на новые голоса, с новых голосов на новые проценты и с новых % на новые голоса
			// надо передать блендеру типа так:
			//sPath = "d:/tmp/test/"
			//sTextVotesLeft = 23242
			//sTextVotesRight = 1221
			//sTextNewVotesLeft = 24550
			//sTextNewVotesRight = 2113

			//он сделает папки такие:
			// !old_percents__new_Votes
			// !new_votes_loop
			// !new_percents__new_Votes
			// !new_votes__new_percents
			// !new_percents_loop

		private void MoveRenderedVotesToNormalPlace()
		{
			string[] aFrom = Directory.GetDirectories(_cPreferences.sFolderVotes.Replace("votes", "votes_"));
			string[] aPNGs;
			string sTo;
			foreach (string sFrom in aFrom)
			{
				sTo=sFrom.Replace("\\votes_\\", "\\votes\\");
				if ((aPNGs = Directory.GetFiles(sFrom, "*.png")).Length > 0)
					CopyDirectory(sFrom, sTo);
				else if (!Directory.Exists(sTo))
					Directory.CreateDirectory(sTo);
			}
		}
		private void CopyTMPToNormalPlace()
		{
			// копируем  новые секвенции на обычные места пока мы стоим в new_votes_loop
			// т.е.

			// !old_percents__new_votes		не надо
			// !new_votes_loop				---->  voting_mid_loop1
			// !new_percents__new_votes		---->  voting_mid_switch2
			// !new_votes__new_percents		---->  voting_mid_switch1
			// !new_percents_loop			---->  voting_mid_loop2

			//GC.Collect(); // иногда файлы оказываются занятыми процессом, хотя они в эфире уже были    // убрал - может показалось, что из-за этого?
			CopyDirectory(Path.Combine(_cPreferences.sFolderVotes, "!new_votes_loop"), Path.Combine(_cPreferences.sFolderVotes, "voting_mid_loop1"));
			//CopyDirectory(Path.Combine(_cPreferences.sFolderVotes, "!new_percents__new_votes"), Path.Combine(_cPreferences.sFolderVotes, "voting_mid_switch2"));
			//CopyDirectory(Path.Combine(_cPreferences.sFolderVotes, "!new_votes__new_percents"), Path.Combine(_cPreferences.sFolderVotes, "voting_mid_switch1"));
			//CopyDirectory(Path.Combine(_cPreferences.sFolderVotes, "!new_percents_loop"), Path.Combine(_cPreferences.sFolderVotes, "voting_mid_loop2"));
		}
		private void CopyDirectory(string sPathFrom, string sPathTo)
		{
			if (!Directory.Exists(sPathTo))
				Directory.CreateDirectory(sPathTo);
			string[] aNamesFrom = Directory.GetFiles(sPathFrom).Select(o => Path.GetFileName(o)).ToArray();
			string[] aNamesTo = Directory.GetFiles(sPathTo).Select(o => Path.GetFileName(o)).ToArray();
			try
			{
				foreach (string sFile in aNamesTo)
					if (!aNamesFrom.Contains(sFile))
						File.Delete(Path.Combine(sPathTo, sFile));

				foreach (string sFile in aNamesFrom)
				{
					File.Copy(Path.Combine(sPathFrom, sFile), Path.Combine(sPathTo, sFile), true);
				}
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}
		public void StartPhotos() // (просьба была стартовать каждую минуту)
		{
			Animation cLoop = null;
			if (_bStopping)
				return;

			_cPLPhotoLeft.AnimationAdd(new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_imagesL_in")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = false, bOpacity = false, nLoopsQty = 1 }, 0);
			lock (_aLoops)
			{
				cLoop = new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_imagesL_loop")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = true, bOpacity = false, nLoopsQty = nLoopImages, oTag = "voting_imagesL_loop" };
				_cPLPhotoLeft.AnimationAdd(cLoop, 0);
				_aLoops.Add(cLoop);
			}
			_cPLPhotoLeft.AnimationAdd(new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_imagesL_out")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = false, bOpacity = false, nLoopsQty = 1, oTag = "voting_imagesL_out" }, 0);

			_cPLPhotoRight.AnimationAdd(new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_imagesR_in")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = false, bOpacity = false, nLoopsQty = 1 }, 0);
			lock (_aLoops)
			{
				cLoop = new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_imagesR_loop")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = true, bOpacity = false, nLoopsQty = nLoopImages, oTag = "voting_imagesR_loop" };
				_cPLPhotoRight.AnimationAdd(cLoop, 0);
				_aLoops.Add(cLoop);
			}
			_cPLPhotoRight.AnimationAdd(new Animation(Path.Combine(_cPreferences.sFolderMat, "voting_imagesR_out")) { stMergingMethod = _cPreferences.stMerging, bKeepAlive = false, bOpacity = false, nLoopsQty = 1, oTag = "voting_imagesR_out" }, 0);

			_dtPhotosLastStart = DateTime.Now;
		}


		public void Stop()
		{
			try
			{
				_bStopping = true;


				if (_cPLPhotoLeft.eStatus == BTL.EffectStatus.Running && 0 == _cPLPhotoLeft.nSumDuration)
					_cPLPhotoLeft.Stop();
				if (_cPLPhotoRight.eStatus == BTL.EffectStatus.Running && 0 == _cPLPhotoRight.nSumDuration)
					_cPLPhotoRight.Stop();

				lock (_aLoops)
				{
					foreach (Effect cEffect in _aLoops)
						if (cEffect.eStatus == BTL.EffectStatus.Running)
						{
							if (cEffect.oTag == "voting_imagesL_loop")
							{
								_cPLPhotoLeft.nDuration = _cPLPhotoLeft.nFrameCurrent + _nEmergencyDuration;
								_cPLPhotoLeft.Skip(false, 0, cEffect);
							}
							if (cEffect.oTag == "voting_imagesR_loop")
							{
								_cPLPhotoRight.nDuration = _cPLPhotoRight.nFrameCurrent + _nEmergencyDuration; 
								_cPLPhotoRight.Skip(false, 0, cEffect);
							}
							if (cEffect.oTag == "voting_top_loop1" || cEffect.oTag == "voting_top_loop2")
							{
								_cPLMatTop.nDuration = _cPLMatTop.nFrameCurrent + _nEmergencyDuration;
								_cPLMatTop.Skip(false, 0, cEffect);
							}
							if (cEffect.oTag == "new_votes_loop" || cEffect.oTag == "voting_mid_loop1")
							{
								_cPLMatMiddle.nDuration = _cPLMatMiddle.nFrameCurrent + _nEmergencyDuration; 
								_cPLMatMiddle.Skip(false, 0, cEffect);
							}
						}
						else
							((Animation)cEffect).nLoopsQty = 10;
				}
				(new Logger()).WriteDebug("stopping");
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
				return _eStatus;
            }
        }
        DateTime IPlugin.dtStatusChanged
        {
            get
            {
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
