using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;

using BTL;
using BTL.Play;
using System.Collections;
using System.Diagnostics;
using System.Xml;
using System.Runtime.InteropServices;
using System.Timers;
using System.IO;
using System.Threading;
using helpers;
using helpers.extensions;


namespace ingenie.plugins
{
    public class SMS
    {
        //public struct LINE_ELEMENT
        //{
        //    public enum TYPE
        //    {
        //        Text,
        //        Animation
        //    }
        //    public TYPE enType;
        //    public EffectVideo cValue;
        //    public int nLeftMargin;
        //    public int nRightMargin;
        //    public int nContentWidth;
        //    public int Width
        //    {
        //        get
        //        {
        //            return nLeftMargin + nContentWidth + nRightMargin;
        //        }
        //    }
        //    public LINE_ELEMENT(TYPE enT, EffectVideo cV)
        //    {
        //        enType = enT;
        //        cValue = cV;
        //        nLeftMargin = 0;
        //        nRightMargin = 0;
        //        nContentWidth = cValue.stArea.nWidth;
        //        switch (enType)
        //        {
        //            case TYPE.Text:
        //                nLeftMargin = 0;
        //                nRightMargin = 0;
        //                break;
        //            case TYPE.Animation:
        //                nLeftMargin = 2;
        //                nRightMargin = 2;
        //                break;
        //        }
        //    }
        //}
        private class Item
        {
            public enum Type
            {
                text,
                smile,
                flag
            }
            public Type eType;
            public string sText;
            public SMILE stSmile;
            private SMS _cSMS;
            private ushort _nMaxWidth;
            public List<EffectVideo> GetEffect(SMS cSMS, ushort nMaxWidth)
            {
                _cSMS = cSMS;
                _nMaxWidth = nMaxWidth;
                List<EffectVideo> aRetVal = new List<EffectVideo>();
                Text cText;
                switch (eType)
                {
                    case Type.smile:
                        Animation cAnim = stSmile.cAnimation;
                        if (null != cAnim)
                            aRetVal.Add(stSmile.cAnimation);
                        else
                        {
                            (new Logger()).WriteNotice("-------SMILE------ попытка создать Animation из смайла [" + stSmile.sText + "] не удалась. Смс вышла c текстовым смайлом.");
                            cText = new BTL.Play.Text(stSmile.sText, cSMS.cPreferences.cFont, cSMS.cPreferences.nBorderWidth);
                            cText.bCUDA = false;
                            cText.stColor = SMILE.stTextSmileColor;
                            cText.stColorBorder = cSMS.cPreferences.stBorderColor;
                            aRetVal.Add(cText);
                        }
                        return aRetVal;
                    case Type.flag:
                        if (null != cSMS._cFlagAnim)
                            aRetVal.Add(cSMS._cFlagAnim);
                        else
                            (new Logger()).WriteError(new Exception("-------FLAG------ попытка получить Animation флага из SMS с телефоном [" + cSMS._sPhone + "] не удалась. Смс вышла без флага."));
                        return aRetVal;
                    case Type.text:
                        cText = new BTL.Play.Text(sText, cSMS.cPreferences.cFont, cSMS.cPreferences.nBorderWidth);
                        if (nMaxWidth < cText.stArea.nWidth)
                        {
                            aRetVal.AddRange(SplitLongWord(sText));
                        }
                        else
                        {
                            cText.bCUDA = false;
                            cText.stColor = cSMS.cPreferences.stColor;
                            cText.stColorBorder = cSMS.cPreferences.stBorderColor;
                            aRetVal.Add(cText);
                        }
                        return aRetVal;
                }
                return null;
            }
            private List<EffectVideo> SplitLongWord(string sText)
            {
                List<EffectVideo> aRetVal = new List<EffectVideo>();
                List<string> aSplited = new List<string>();
                int nk = 0;
                bool bLastLetter = false, bLastDigit = false; //, bLastLetter = false;
                for (int ni = 0; sText.Length > ni; ni++)
                {
                    if (0 != ni && ((!bLastDigit && !bLastLetter && Char.IsLetterOrDigit(sText[ni])) || (bLastLetter && !(Char.IsLetter(sText[ni]))) || (bLastDigit && !(Char.IsDigit(sText[ni])))))
                    {
                        aSplited.Add(sText.Substring(nk, ni - nk));
                        nk = ni;
                    }
                    bLastLetter = Char.IsLetter(sText[ni]);
                    bLastDigit = Char.IsDigit(sText[ni]);
                }
                foreach (string sStr in aSplited)
                {
                    aRetVal.AddRange(CutWord(sStr));
                }
                return aRetVal;
            }
            private List<EffectVideo> CutWord(string sText)
            {
                List<EffectVideo> aRetVal = new List<EffectVideo>();
                Text cText = new BTL.Play.Text(sText, _cSMS.cPreferences.cFont, _cSMS.cPreferences.nBorderWidth);
                Text cTextPrev = null; ;
                int ni = 0, nk = 1;
                if (_nMaxWidth < cText.stArea.nWidth)
                {
                    while (ni + nk < sText.Length)
                    {
                        cText = new BTL.Play.Text(sText.Substring(ni, nk), _cSMS.cPreferences.cFont, _cSMS.cPreferences.nBorderWidth);
                        while (_nMaxWidth > cText.stArea.nWidth && (ni + nk < sText.Length))
                        {
                            nk++;
                            cTextPrev = cText;
                            cText = new BTL.Play.Text(sText.Substring(ni, nk), _cSMS.cPreferences.cFont, _cSMS.cPreferences.nBorderWidth);
                        }
                        ni += nk - 1;
                        nk = 1;
                        if (ni + nk == sText.Length)
                            cTextPrev = cText;
                        aRetVal.Add(cTextPrev);
                    }
                }
                else
                    aRetVal.Add(cText);
                foreach (Text cEff in aRetVal)
                {
                    cEff.bCUDA = false;
                    cEff.stColor = _cSMS.cPreferences.stColor;
                    cEff.stColorBorder = _cSMS.cPreferences.stBorderColor;
                }
                return aRetVal;
            }
            static public List<Item> GetTextItems(string sText)
            {
                List<Item> aRetVal = new List<Item>();
                foreach (string ss in sText.Split(' '))
                {
                    if ("" != ss)
                    {
                        aRetVal.Add(new Item() { eType = Item.Type.text, sText = ss });
                    }
                }
                return aRetVal;
            }
        }
        enum Region : long
        {
            UKNOWN = 0,
            RU = 7,
            NL = 31,
            BE = 32,
            FR = 33,
            CH = 41,
            UK = 44,
            NO = 47,
            SE = 46,
            DE = 49,
            IE = 353,
            US = 1,
            KZ1 = 7700,
            KZ2 = 7701,
            KZ3 = 7702,
            KZ4 = 7705,
            KZ5 = 7707,
            KZ6 = 7777,
            BY = 375,
            GE = 995,
            UA = 380,
            PROMO = 70000000002,
            ATTENTION = 70000000003
        }
        public enum Type
        {
            Common,
            VIP,
            Promo,
            Photo
        }

        public Preferences.SMS cPreferences;
        long _nId;
        public long ID
        {
            get { return _nId; }
            set { _nId = value; }
        }
        string _sText;
        public string sText
        {
			set { _sText = value.Trim().NormalizeNewLines().Replace(Environment.NewLine, " "); }
            get { return _sText; }
        }
        private ushort _nWidthOfSpace = 0;

        public Animation _cFlagAnim;
        string _sPhone;
        static public Dictionary<string, string> _ahFlagFolderBinds = null;

        public string Phone
        {
            set
            {
                FlagsAnimInit();
                _sPhone = value;
                string sReg;
                _enRegion = Region.UKNOWN;
                int nLen = 0;
                foreach (long nRegValue in Enum.GetValues(typeof(Region)))
                {
                    sReg = "+" + nRegValue.ToString();
                    if (sReg.Length > _sPhone.Length)
                        continue;
                    if (_sPhone.Substring(0, sReg.Length) != sReg)
                        continue;
                    if (nLen < sReg.Length)
                    {
                        _enRegion = (Region)nRegValue;
                        nLen = sReg.Length;
                    }
                }
                string sKey;
                if (_enRegion != Region.PROMO && _enRegion != Region.ATTENTION)
                {
                    if (7700 <= (int)_enRegion && 7777 >= (int)_enRegion)
                    {
                        sKey = "kz";
                    }
                    else
                    {
                        sKey = (_enRegion.ToString()).ToLower();
                    }

                    if (_ahFlagFolderBinds.ContainsKey(sKey))
                    {
                        _cFlagAnim = GetFlagAnim(_ahFlagFolderBinds[sKey]);
                    }
                    else if (_enRegion != Region.RU)
                        (new Logger()).WriteError(new Exception("-------FLAG------ Данный регион не найден в футаджах флагов и это не Россия: [" + _enRegion + "] ожидаемое название папки: [" + sKey + "] телефон: [" + _sPhone + "]"));
                }
            }
            get
            {
                return _sPhone;
            }
        }
        static Animation cAnim;
        private void FlagsAnimInit()   // нельзя держать анимы из-за невозможности давать один эффект в разных местах экрана одновр.
        {
            if (null == _ahFlagFolderBinds)
            {
                _ahFlagFolderBinds = new Dictionary<string, string>();
                string sParentF = cPreferences.sFlagsFolder;
                string[] aS = Directory.GetDirectories(sParentF);
                foreach (string sS in aS)
                {
                    string sTMP = sS.Substring(sParentF.Length, sS.Length - sParentF.Length);
                    _ahFlagFolderBinds.Add(sTMP, sS);
                }
            }
        }


        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        static public extern bool DeleteObject(IntPtr hObject);
        private IntPtr _nHBitmap = IntPtr.Zero;
        public int HBitmap
        {
            get
            {
                if (null == _bmpFlag)
                    return 0;
                if (IntPtr.Zero == _nHBitmap)
                    _nHBitmap = _bmpFlag.GetHbitmap();
                return _nHBitmap.ToInt32();
            }
        }
        public int FlagWidth
        {
            get
            {
                if (null == _cFlagAnim)
                    return 0;

                return _cFlagAnim.stArea.nWidth.ToShort();
            }
        }

        public Type eType;
        public ushort _nAnimTextIndent = 6;
        public ushort _nFlagIndent = 8;
        public List<Composite> _aSMSasEffects; // для диспоза потом

        ArrayList _aLines;
        ArrayList _anLinesHeight;
        Region _enRegion;
        Bitmap _bmpFlag;

        public SMS()
        {
        }
        ~SMS()
        {
            Dispose();
        }

        public void Dispose()
        {
            try
            {
                if (null != _bmpFlag)
                {
                    _bmpFlag.Dispose();
                    _bmpFlag = null;
                }

                if (IntPtr.Zero != _nHBitmap)
                {
                    DeleteObject(_nHBitmap);
                    _nHBitmap = IntPtr.Zero;
				}
				if (null != _aSMSasEffects)
					foreach (Composite cComp in _aSMSasEffects)
					{
						if (cComp.eStatus == EffectStatus.Running)
							cComp.Stop();
						cComp.Dispose();
					}
				(new Logger()).WriteDebug3("sms disposed: " + ID);
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
			}
		}

		public List<Composite> RollItemsGet(ushort nMaxWidth)
        {
            //_sText = "dfjdnfjdn ndjfndjfn ;) ssdsdsds";
            _sText = _sText.RemoveNewLines();
            List<SMILE> aSmiles = SMILE.FindSmiles(_sText, cPreferences.sSmilesFolder);
            List<Item> aItems = new List<Item>();
            List<Item> aTMP;
            int nFlagWith = FlagWidth;
            if (0 < nFlagWith)
                aItems.Add(new Item() { eType = Item.Type.flag });
            int nPos = 0;
			if (0 == _nWidthOfSpace)
			{
				_nWidthOfSpace = (ushort)(BTL.Play.Text.Measure("SSS SSS", cPreferences.cFont, 0).nWidth - BTL.Play.Text.Measure("SSSSSS", cPreferences.cFont, 0).nWidth);
				if (cPreferences.cFont.Style == FontStyle.Italic)
					_nWidthOfSpace = 0;    //(ushort)(cPreferences.cFont.Size / 6f + 0.5);
			}
            foreach (SMILE stSmile in aSmiles)
            {
                aItems.AddRange(Item.GetTextItems(_sText.Substring(nPos, stSmile.stPosition.Start - nPos)));
                aItems.Add(new Item() { eType = Item.Type.smile, stSmile = stSmile });
                nPos = stSmile.stPosition.End;
            }
            if (_sText.Length > nPos)
                aItems.AddRange(Item.GetTextItems(_sText.Substring(nPos)));
            return _aSMSasEffects = MakeComposites(aItems, nMaxWidth);
        }
        private List<Composite> MakeComposites(List<Item> aItems, ushort nMaxWidth)
        {
            List<Composite> aRetVal = new List<Composite>();
            ushort nIdent = 0;
            List<EffectVideo> aEffectsInSMS = new List<EffectVideo>();
            List<EffectVideo> aEff;
            int nIndx = 0;

            foreach (Item cItem in aItems)
                if (null != (aEff = cItem.GetEffect(this, nMaxWidth)))
                    aEffectsInSMS.AddRange(aEff);
            Composite cTemp;
            while (nIndx < aEffectsInSMS.Count)
            {
                cTemp = new Composite(nMaxWidth, Composite.Type.Vertical);
				
                nIdent = 0;
                while (true)
                {
                    if (cTemp.stArea.nWidth + aEffectsInSMS[nIndx].stArea.nWidth + nIdent > nMaxWidth)
                        break;

                    cTemp.EffectAdd(aEffectsInSMS[nIndx], nIdent);
                    nIdent = _nWidthOfSpace;
                    nIndx++;
                    if (nIndx >= aEffectsInSMS.Count)
                        break;
                }
                cTemp.bCUDA = false;

                aRetVal.Add(cTemp);
            }
            return aRetVal;
        }

        public SMILE GetSmile(int nPosition)
        {
            SMILE stRetVal = new SMILE();
            return stRetVal;
        }
        private Animation GetFlagAnim(string sFolder)
        {
            cAnim = new Animation(sFolder, 0, true);
            cAnim.bCUDA = false;
            cAnim.Prepare();
            return cAnim;
        }
    }

    public struct POSITION
    {
        private int nStart;
        private int nLength;
        private bool bInit;
        public int Start
        {
            set
            {
                nStart = value;
                bInit = true;
            }
            get
            {
                if (this == Empty)
                    return -1;
                return nStart;
            }
        }
        public int Length
        {
            set
            {
                nLength = value;
                bInit = true;
            }
            get
            {
                if (this == Empty)
                    return -1;
                return nLength;
            }
        }
        static public POSITION Empty;
        public int End
        {
            get
            {
                if (this == Empty)
                    return -1;
                return nStart + nLength;
            }
        }
        public POSITION(int nS, int nL)
        {
            nStart = nS;
            nLength = nL;
            bInit = true;
        }
        static public bool operator ==(POSITION value, POSITION new_value)
        {
            if (value.bInit == new_value.bInit && value.nStart == new_value.nStart && value.nLength == new_value.nLength)
                return true;
            return false;
        }
        static public bool operator !=(POSITION value, POSITION new_value)
        {
            if (value == new_value)
                return false;
            return true;
        }
    }
    public struct SMILE
    {
        static Hashtable _ahSmilesBinds = null;
        static Hashtable _ahSmilesAnimationBinds = null;
        static public Color stTextSmileColor = Color.FromArgb(238, 84, 195);
        public Animation cAnimation;
        public string sText;
        public POSITION stPosition;
        public string sWorkFolder;
        //Animation cAnimation;
        public SMILE(string sSml, string sWorkFolder)
            : this(sSml, sWorkFolder, POSITION.Empty)
        {
        }
        public SMILE(string sSml, string sWorkFolder, POSITION stPos)
        {
            this.sWorkFolder = sWorkFolder;
            sText = sSml;
            stPosition = stPos;
            if (null == _ahSmilesBinds)
                InitSmiles();
            cAnimation = GetAnimation(sText, sWorkFolder);
        }
        private static Animation GetAnimation(string sSml, string sWorkFolder)
        {
            string sFolder;
            Animation cRetVal = null;
            if (null != _ahSmilesBinds && _ahSmilesBinds.ContainsKey(sSml))
            {
                sFolder = sWorkFolder + _ahSmilesBinds[sSml].ToString();
                if (Directory.Exists(sFolder))
                {
                    cRetVal = new Animation(sFolder, 0);
                    cRetVal.bCUDA = false;
                    cRetVal.Prepare();
                }
            }
            return cRetVal;
        }
        private static void InitSmilesAnimations(string sWorkFolder)
        {
            if (null == _ahSmilesAnimationBinds)
            {
                _ahSmilesAnimationBinds = new Hashtable();
                Animation cAnimSmile;
                foreach (DictionaryEntry ni in _ahSmilesBinds)
                {
                    string sFolder = sWorkFolder + ni.Value.ToString();
                    if (Directory.Exists(sFolder))
                    {
                        cAnimSmile = new Animation(sFolder, 0);
                        cAnimSmile.bCUDA = false;
                        cAnimSmile.Prepare();
                        _ahSmilesAnimationBinds.Add(ni.Key, cAnimSmile);
                    }
                }
            }
        }
        private static void InitSmiles()
        {
            if (null == _ahSmilesBinds)
            {
                _ahSmilesBinds = new Hashtable();
                //_ahSmilesBinds["VIP"] = "vip";
                _ahSmilesBinds["PHOTO"] = "photo";
                _ahSmilesBinds["O:-)"] = "00";
                _ahSmilesBinds["0:-)"] = "00";
                _ahSmilesBinds["O=)"] = "00";
                _ahSmilesBinds["0=)"] = "00";
                _ahSmilesBinds[":-)"] = "01";
                _ahSmilesBinds[":)"] = "01";
                _ahSmilesBinds[":=)"] = "01";
                _ahSmilesBinds["=)"] = "01";
                _ahSmilesBinds[":-("] = "02";
                _ahSmilesBinds[":("] = "02";
                _ahSmilesBinds[";("] = "02";
                _ahSmilesBinds[";-)"] = "03";
                _ahSmilesBinds[";)"] = "03";
                _ahSmilesBinds[":-P"] = "04";
                _ahSmilesBinds["8-)"] = "05";
                _ahSmilesBinds["8=)"] = "05";
                _ahSmilesBinds[":-D"] = "06";
                _ahSmilesBinds[":-["] = "07";
                _ahSmilesBinds["=-O"] = "08";
                _ahSmilesBinds["=-0"] = "08";
                _ahSmilesBinds[":-*"] = "09";
                _ahSmilesBinds[":*"] = "09";
                _ahSmilesBinds[":'("] = "10";
                _ahSmilesBinds[":-X"] = "11";
                _ahSmilesBinds[":-Х"] = "11";
                _ahSmilesBinds[">:o"] = "12";
                _ahSmilesBinds[">:0"] = "12";
                _ahSmilesBinds[":-|"] = "13";
                _ahSmilesBinds[":-\\"] = "14";
                _ahSmilesBinds[":\\"] = "14";
                _ahSmilesBinds[":-/"] = "14";
                _ahSmilesBinds[":/"] = "14";
                _ahSmilesBinds["*JOKINGLY*"] = "15";
                _ahSmilesBinds["]:->"] = "16";
                _ahSmilesBinds["[:-}"] = "17";
                _ahSmilesBinds["*KISSED*"] = "18";
                _ahSmilesBinds[":-!"] = "19";
                _ahSmilesBinds["*TIRED*"] = "20";
                _ahSmilesBinds["*STOP*"] = "21";
                _ahSmilesBinds["*KISSING*"] = "22";
                _ahSmilesBinds["@}->--"] = "23";
                _ahSmilesBinds["*THUMBS UP*"] = "24";
                _ahSmilesBinds["*DRINK*"] = "25";
                _ahSmilesBinds["*IN LOVE*"] = "26";
                _ahSmilesBinds["@="] = "27";
                _ahSmilesBinds["*HELP*"] = "28";
                _ahSmilesBinds["\\m/"] = "29";
                _ahSmilesBinds["%)"] = "30";
                _ahSmilesBinds["*OK*"] = "31";
                _ahSmilesBinds["*WASSUP*"] = "32";
                _ahSmilesBinds["*SUP*"] = "32";
                _ahSmilesBinds["*SORRY*"] = "33";
                _ahSmilesBinds["*BRAVO*"] = "34";
                _ahSmilesBinds["*ROFL*"] = "35";
                _ahSmilesBinds["*LOL*"] = "35";
                _ahSmilesBinds["*PARDON*"] = "36";
                _ahSmilesBinds["*NO*"] = "37";
                _ahSmilesBinds["*CRAZY*"] = "38";
                _ahSmilesBinds["*DONT_KNOW*"] = "39";
                _ahSmilesBinds["*UNKNOWN*"] = "39";
                _ahSmilesBinds["*DANCE*"] = "40";
                _ahSmilesBinds["*YAHOO*"] = "41";
                _ahSmilesBinds["*YAHOO!*"] = "41";
                _ahSmilesBinds["*HI*"] = "42";
                _ahSmilesBinds["*PREVED*"] = "42";
                _ahSmilesBinds["*PRIVET*"] = "42";
                _ahSmilesBinds["*BYE*"] = "43";
                _ahSmilesBinds["*YES*"] = "44";
                _ahSmilesBinds[";D"] = "45";
                _ahSmilesBinds["*ACUTE*"] = "45";
                _ahSmilesBinds["*WALL*"] = "46";
                _ahSmilesBinds["*DASH*"] = "46";
                _ahSmilesBinds["*WRITE*"] = "47";
                _ahSmilesBinds["*MAIL*"] = "47";
                _ahSmilesBinds["*SCRATCH*"] = "48";
                _ahSmilesBinds["<3"] = "49";
                _ahSmilesBinds["<з"] = "49";
                _ahSmilesBinds["e>"] = "49";
                _ahSmilesBinds["е>"] = "49";
            }
        }
        static public List<SMILE> FindSmiles(string sText, string sWorkFolder)
        {
            List<SMILE> aRetVal = new List<SMILE>();
            int nPos = 0;
            if (null == _ahSmilesBinds)
                InitSmiles();
            //if (null == _ahSmilesAnimationBinds)     // временно не актуально. сейчас на каждую даже одинаковую анимашку загружаем 
            //    InitSmilesAnimations(sWorkFolder);
            foreach (string sSml in _ahSmilesBinds.Keys)
            {
                POSITION stPos;
                string sTex = sText.ToLower();
                string sSm = sSml.ToLower();
                int nPosText = 0;
                while (-1 < (nPos = sTex.Substring(nPosText).IndexOf(sSm)))
                {
                    stPos = new POSITION(nPos + nPosText, sSml.Length);
                    aRetVal.Add(new SMILE(sSml, sWorkFolder, stPos));
                    nPosText = stPos.End;
                }
            }
            if (1 < aRetVal.Count)
                aRetVal.Sort(delegate(SMILE a, SMILE b) { return a.stPosition.Start - b.stPosition.Start; });
            return aRetVal;
        }
        static public POSITION IsSmilePersist(string sText)
        {
            POSITION stRetVal = POSITION.Empty;
            int nPos = -1;
            if (null == _ahSmilesBinds)
                InitSmiles();
            foreach (string sSml in _ahSmilesBinds.Keys)
            {
                nPos = sText.ToLower().IndexOf(sSml.ToLower());
                if (-1 < nPos)
                {
                    if (stRetVal.Start > nPos || POSITION.Empty == stRetVal)
                    {
                        stRetVal.Start = nPos;
                        stRetVal.Length = sSml.Length;
                    }
                }
            }
            return stRetVal;
        }
    }
}