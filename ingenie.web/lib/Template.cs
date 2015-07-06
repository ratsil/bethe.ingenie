using System;
using System.Collections.Generic;
using System.Linq;
using helpers.extensions;
using ingenie.userspace;
using SysDrw = System.Drawing;
using helpers;
using helpers.replica.mam;

namespace ingenie.web.lib
{
    public class Template : userspace.Template
    {
		new private class Logger : lib.Logger
		{
			public Logger()
				: base("template")
			{
			}
		}
		private Atom.Status _ePreviousStatus;

		public Template(string sFile)
            : base(sFile, COMMAND.unknown)
        {
			_ePreviousStatus = Atom.Status.Idle;
        }

		public Atom.Status AtomsStatusGet()
		{
			Atom.Status eRetVal = Atom.Status.Unknown;
			foreach (Atom cAtom in _aAtoms)
			{
				try
				{
					if (Atom.Status.Error == cAtom.eStatus)
						return Atom.Status.Error;
					if (Atom.Status.Unknown == eRetVal)
						eRetVal = cAtom.eStatus;
					else if (cAtom.eStatus != eRetVal)
						return _ePreviousStatus;
				}
				catch (Exception ex)
				{
					(new Logger()).WriteError(ex);
					return Atom.Status.Error;
				}
			}
			return _ePreviousStatus = eRetVal;
		}

        #region Plugin
        public void PluginCreate(string sXML, string sWorkFolder)
        {
            Plugin cPlugin = new Plugin();
            cPlugin.sFile = sWorkFolder + "bin\\chat.dll";
            cPlugin.sClass = "Chat";
            _aAtoms.Add(cPlugin);
        }
        #endregion
        #region Cues
        private Dictionary<string, string> ahUserCues = null;
        public void SetCallbackForUserCues(Dictionary<string, string> ahUserCues)
        {
            if (null != ahUserCues)
            {
                this.ahUserCues = ahUserCues;
				MacroExecute = OnRuntimeGetUserCues;
				RuntimeGet = OnRuntimeGetUserCues;
			}
        }
        string OnRuntimeGetUserCues(string sKey)
		{
			string sRetVal = null;
			helpers.replica.mam.Macro.Flags eFlags = helpers.replica.mam.Macro.ParseFlags(ref sKey);
			if (null != ahUserCues)
			{
				if (ahUserCues.Keys.Contains(sKey))
					sRetVal = ahUserCues[sKey];
				else if (sKey.Contains("ARTIST") && ahUserCues.Keys.Contains("ARTIST"))
					sRetVal = ahUserCues["ARTIST"];
				else if (sKey.Contains("SONG") && ahUserCues.Keys.Contains("SONG"))
					sRetVal = ahUserCues["SONG"];
				else if (sKey.Contains("FOLDER") && ahUserCues.Keys.Contains("FOLDER"))
					sRetVal = ahUserCues["FOLDER"];
				else if (sKey.Contains("LOOPS") && ahUserCues.Keys.Contains("LOOPS"))
					sRetVal = ahUserCues["LOOPS"];

				if (null != sRetVal)
				{
					if (eFlags.HasFlag(helpers.replica.mam.Macro.Flags.Escaped))
						sRetVal = sRetVal.Replace("\\", "\\\\").Replace("\"", "\\\"");
					if (eFlags.HasFlag(helpers.replica.mam.Macro.Flags.Caps))
						sRetVal = sRetVal.ToUpper();
					return sRetVal;
				}
			}
			throw new Exception("обнаружен запрос неизвестного runtime-свойства [" + sKey + "] в темплейте [" + sFile + "]"); //TODO LANG
        }
        public void SetCallbackForMacroCues()
        {
            if (null == ahUserCues)
                MacroExecute = OnMacroExecute;
        }
        private string OnMacroExecute(string sName)
        {
			helpers.replica.mam.Macro.Flags eFlags = helpers.replica.mam.Macro.ParseFlags(ref sName);
            string sRetVal = "";
            DBInteract cDBI = new DBInteract();
            Macro cMacro = Macro.Get(sName);
            switch (cMacro.cType.sName)
            {
                case "sql":
                    sRetVal = cMacro.Execute();
                    break;
                default:
                    throw new Exception("обнаружен неизвестный тип макро-строки [" + cMacro.cType.sName + "] в темплейте [" + sFile + "]"); //TODO LANG
            }
			if (null != sRetVal)
			{
				if (eFlags.HasFlag(helpers.replica.mam.Macro.Flags.Escaped))
					sRetVal = sRetVal.Replace("\\", "\\\\").Replace("\"", "\\\"");
				if (eFlags.HasFlag(helpers.replica.mam.Macro.Flags.Caps))
					sRetVal = sRetVal.ToUpper();
			}
            return sRetVal;
        }
        public void TextCreate(string sText)
        {
            Text cText = new Text();
            cText.cFont = new Font() { sName = "Arial", nSize = 1, nFontStyle = 1, cBorder = new Border(), cColor = new Color(), nWidth = 0 };
            cText.sText = sText;
            _aAtoms.Add(cText);
        }
        public bool AddTextToRollIfFound(string sText)
        {
            if (null == _aAtoms)
                return false;
            Roll cRoll = (Roll)_aAtoms.FirstOrDefault(o => o is Roll);
            if (cRoll != null)
                return cRoll.TryAddText(sText);
            return false;
        }
        #endregion
    }
    public class TemplatePrompter : userspace.Template
    {
		new private class Logger : lib.Logger
		{
			public Logger()
				: base("template:prompter")
			{
			}
		}
		private bool bSplitted;
		public List<string> aSplittedText;
		public int nOnScreen;
		public int nOffScreen;
		private List<Text> aSplittedEffects;
		private Text cSourceText;
        public TemplatePrompter(string sFile)
            : base(sFile, COMMAND.unknown)
        {
			bSplitted = false;
			aSplittedText = null;
			aSplittedEffects = null;
			cSourceText = null;
			nOnScreen = 0;
			nOffScreen = 0;
        }
		public void RollSpeedSet(short nspeed)
		{
			Roll cRoll = (Roll)_aAtoms.FirstOrDefault(o => o is Roll);
			if (null != cRoll)
				cRoll.nSpeed = nspeed;
		}
		public void PrompterRestartFrom(int nLine)
		{
			Roll cOldRoll = (Roll)_aAtoms.FirstOrDefault(o => o is Roll);
			Roll cNewRoll = new Roll();
			cNewRoll.EffectOnScreen += new Container.EventDelegate(cRoll_EffectOnScreen);
			cNewRoll.EffectOffScreen += new Container.EventDelegate(cRoll_EffectOffScreen);
			if (null != cOldRoll)
			{
				cOldRoll.Stop();
				_aAtoms.Remove(cOldRoll);
				_aAtoms.Add(cNewRoll);
				cNewRoll.nLayer = cOldRoll.nLayer;
				cNewRoll.nSpeed = cOldRoll.nSpeed;
				cNewRoll.nLayer = cOldRoll.nLayer;
				cNewRoll.stArea = cOldRoll.stArea;
				cNewRoll.cHide = cOldRoll.cHide;
				cNewRoll.cShow = cOldRoll.cShow;
				cNewRoll.bOpacity = cOldRoll.bOpacity;
				cNewRoll.cDock = cOldRoll.cDock;
				cNewRoll.eDirection = cOldRoll.eDirection;
				List<Effect> aEffectsInRoll = cNewRoll.EffectsGet();
				for (int nI = nLine - 1; aSplittedEffects.Count > nI; nI++)
				{
					if (aSplittedEffects[nI].eStatus == Atom.Status.Idle || aSplittedEffects[nI].eStatus == Atom.Status.Prepared)
						cNewRoll.EffectAdd(aSplittedEffects[nI]);  //aEffectsInRoll.Add(aSplittedEffects[nI]);
					else
					{
						Text cT = new Text();
						cT.sText = aSplittedText[nI];
						cT.nLayer = cSourceText.nLayer;
						//cT.bOpacity = cT.bOpacity;
						cT.cDock = cSourceText.cDock;
						cT.cFont = cSourceText.cFont;
						aSplittedEffects[nI] = cT;
						cNewRoll.EffectAdd(cT);
						//aEffectsInRoll.Add(cT);
					}
				}
				nOnScreen = nOffScreen = nLine - 1;
				cNewRoll.Prepare();
				cNewRoll.Start();
			}
		}
		#region . text splitting .
		public void TextSplit()
		{
			if (bSplitted)
				return;
			Roll cRoll = (Roll)_aAtoms.FirstOrDefault(o => o is Roll);
			if (null != cRoll)
			{
				cRoll.EffectOnScreen += new Container.EventDelegate(cRoll_EffectOnScreen);
				cRoll.EffectOffScreen += new Container.EventDelegate(cRoll_EffectOffScreen);
				List<Effect> aEffectsInRoll = cRoll.EffectsGet();
				cSourceText = (Text)aEffectsInRoll.FirstOrDefault(o => o is Text);
				if (null != cSourceText)
				{
					cRoll.EffectRemove(cSourceText);
					TextSplitting cTS = new TextSplitting(cSourceText.sText, Preferences.nMaxTextWidth, cSourceText.cFont);
					aSplittedText = cTS.Split();
					aSplittedEffects = new List<Text>();
					foreach (string sLine in aSplittedText)
					{
						Text cT = new Text();
						cT.sText = sLine;
						cT.nLayer = cSourceText.nLayer;
						//cT.bOpacity = cT.bOpacity;
						cT.cDock = cSourceText.cDock;
						cT.cFont = cSourceText.cFont;
						aSplittedEffects.Add(cT);
						cRoll.EffectAdd(cT);
					}
					//aEffectsInRoll.Remove(cSourceText);
					//aEffectsInRoll.AddRange(aSplittedEffects);
					
					bSplitted = true;
				}
			}
		}
		void cRoll_EffectOffScreen(Container cContainer, Effect cEffect)
		{
			nOffScreen++;
		}
		void cRoll_EffectOnScreen(Container cContainer, Effect cEffect)
		{
			nOnScreen++;
		}
		private class TextSplitting
		{
			private SysDrw.Font _cSDFont;
			private Font _cFont;
			private int _nWidth;
			private string _sText;
			private ushort _nWidthOfSpace;
			int nWidth10, nHeight;
			public TextSplitting(string sText, int nWidth, Font cFont)
			{
				this._sText = sText;
				this._nWidth = nWidth;
				this._cFont = cFont;
				this._cSDFont = cFont.FontSystemDrawingGet();
				_nWidthOfSpace = 0;
				nWidth10 = 0;
				nHeight = 0;
			}
			public List<string> Split()
			{
				List<string> aRetVal = new List<string>();
				List<string> aSplittedByEnterRows = SplitByEnter(_sText);
				foreach (string sS in aSplittedByEnterRows)
					aRetVal.AddRange(SplitSimpleRow(Trim_ButNoEmptyStrings(sS)));
				return aRetVal;
			}
			private List<string> SplitSimpleRow(string sS)
			{
				List<string> aRetVal = new List<string>();
				if (0 == _nWidthOfSpace)
				{
					_nWidthOfSpace = (ushort)(MeasureWidth("SSS SSS", _cSDFont) - MeasureWidth("SSSSSS", _cSDFont));
					Area cA = Measure("Жьитыi ,Рх", _cSDFont, true);
					nWidth10 = cA.nWidth;
					nHeight = cA.nHeight;
				}
				int nMiddleLettersQtyInLine = (int)((float)_nWidth * 10 / nWidth10);
				int nCur=0;
				int nLength = 0;
				while (nCur + nMiddleLettersQtyInLine < sS.Length)
				{
					nLength = MeasureAndFindLengthAround(sS, nCur, nMiddleLettersQtyInLine);
					aRetVal.Add(sS.Substring(nCur, nLength).Trim());
					nCur += nLength;
				}
				nLength = MeasureAndFindLengthAround(sS, nCur, sS.Length - nCur);
				aRetVal.Add(sS.Substring(nCur, nLength).Trim());
				nCur += nLength;
				if (nCur < sS.Length)
					aRetVal.Add(sS.Substring(nCur, sS.Length - nCur).Trim());
				return aRetVal;
			}
			private int MeasureAndFindLengthAround(string sS, int nIn, int nLength)
			{
				int nNewLen, nPrevLen = 0;
				nNewLen = IncreaseSubStringToEndOfCurrentWord(sS, nIn, nLength - 1);
				while (nPrevLen != nNewLen && _nWidth >= MeasureWidth(sS.Substring(nIn, nNewLen), _cSDFont))
				{
					nPrevLen = nNewLen;
					nNewLen = IncreaseSubStringToEndOfNextWord(sS, nIn, nLength);
				}
				if (0 < nPrevLen)
					return nPrevLen;

				nPrevLen = 0;
				nNewLen = DecreaseSubStringToPrevousWord(sS, nIn, nLength - 1);
				if (0 == nNewLen) // т.е. предыдущего слова небыло вовсе....
					return nLength;  // режем прям тут;
				while (nPrevLen != nNewLen && _nWidth < MeasureWidth(sS.Substring(nIn, nNewLen), _cSDFont))
				{
					nPrevLen = nNewLen;
					nNewLen = DecreaseSubStringToPrevousWord(sS, nIn, nLength - 1);
				}
				return nNewLen;
			}
			private int IncreaseSubStringToEndOfCurrentWord(string sS, int nIn, int nLength)
			{
				while (sS.Length > nLength + nIn && " " != sS.Substring(nIn + nLength, 1))
					nLength++;
				return nLength;
			}
			private int IncreaseSubStringToEndOfNextWord(string sS, int nIn, int nLength)
			{
				while (sS.Length > nLength + nIn && " " == sS.Substring(nIn + nLength, 1))
					nLength++;
				return IncreaseSubStringToEndOfCurrentWord(sS, nIn, nLength);
			}
			private int DecreaseSubStringByRemovingCurrentGap(string sS, int nIn, int nLength)
			{
				while (1 < nLength && " " == sS.Substring(nIn + nLength, 1))
					nLength--;
				return nLength + 1;
			}
			private int DecreaseSubStringToPrevousWord(string sS, int nIn, int nLength)
			{
				while (0 < nLength && " " != sS.Substring(nIn + nLength, 1))
					nLength--;
				if (0 == nLength)
					return 0;
				else
					return DecreaseSubStringByRemovingCurrentGap(sS, nIn, nLength);
			}
			private List<string> SplitByEnter(string sText)
			{
				string sESC = sText.Replace("\t", " ").Replace("  ", " ");
                return sESC.Replace("\r\r", "\r \r").Replace("\n\n", "\n \n").Replace("\r", "\n").Replace("\n\n", "\n").Split('\n').ToList();
			}
			private string Trim_ButNoEmptyStrings(string sS)
			{
				string sRetVal = sS.Trim();
				if (sRetVal == "")
					sRetVal = " ";
				return sRetVal;
			}
			private ushort MeasureWidth(string sText, SysDrw.Font cFont)
			{
				return Measure(sText, cFont, false).nWidth;
			}
			static public Area Measure(string sText, SysDrw.Font cFont, bool bHeight)
			{
				try    // ВНИМАНИЕ!  MeasureString  не учитывает последние пробелы!!   //  И не учитывает шрифт италик (наклонный)!!!! 
				{
					SysDrw.Graphics cGraphics = SysDrw.Graphics.FromImage(new SysDrw.Bitmap(1, 1));
					SysDrw.SizeF cTextSize = cGraphics.MeasureString(sText, cFont, 10000, SysDrw.StringFormat.GenericTypographic);
					if (bHeight)
						return new Area(0, 0, (ushort)(cTextSize.Width * 0.75F + 1), (ushort)(cTextSize.Height * 0.75F + 1));
					else
						return new Area(0, 0, (ushort)(cTextSize.Width * 0.75F + 1), 0);
				}
				catch (Exception ex)
				{
					(new Logger()).WriteError(ex);
					return new Area(0, 0, 0, 0);
				}
			}     
		}
		#endregion
    }
}
