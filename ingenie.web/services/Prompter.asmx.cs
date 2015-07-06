#define chatdebug
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Activation;
using System.Threading;
using System.Web.Services;
using System.Drawing;
using helpers;
using helpers.extensions;
//using helpers.replica;
using ingenie.web.lib;

namespace ingenie.web.services
{
	/// <summary>
	/// Summary description for ingenie
	/// </summary>
	[WebService(Namespace = "http://replica/ig/services/Prompter.asmx")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	// To allow this web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
	// [System.web.Script.Services.ScriptService]
	public class Prompter : Common //EMERGENCY сделать по образу и подобию Cues... т.е. создать тут класс Item : GarbageCollector.Item и понеслась
	{
		private class Logger : lib.Logger
		{
			public Logger()
				: base("prompter")
			{
			}
		}
		public class DynamicValue
		{
			public string sName;
			public string sValue;

			public DynamicValue()
			{
			}
		}
		public class PrompterPrepareResult
		{
			public int nTemplatesHashCode;
			public string[] aSplittedText;
		}

		static private TemplatePrompter _cPrompterTemplate;

		public Prompter()
		{ }

		[WebMethod(EnableSession = true)]
		public PrompterPrepareResult Prepare(string sTemplateName, DynamicValue[] ahDinamicValues)
		{
			string sTemplateFile = "";
			switch (sTemplateName)
			{
				case "prompter":
					sTemplateFile = "c:/cues/scr/prompter.xml";
					break;
				default:
					throw new Exception("неизвестный шаблон");
			}
			if (!System.IO.File.Exists(sTemplateFile))
				throw new System.IO.FileNotFoundException("отсутствует файл шаблона [" + sTemplateFile + "]");
			_cPrompterTemplate = new TemplatePrompter(sTemplateFile);
			_cPrompterTemplate.RuntimeGet = (sRuntime) => { return ahDinamicValues.First(row => row.sName == sRuntime).sValue; };
			_cPrompterTemplate.MacroExecute = (sMacro) => { return ahDinamicValues.First(row => row.sName == sMacro).sValue; };
			_cPrompterTemplate.ParseDone += new userspace.Template.ParseDoneDelegate(cPrompterTemplate_ParseDone);
			_cPrompterTemplate.Prepare();
			int nHash=_cPrompterTemplate.GetHashCode();
			PrompterPrepareResult cRetVal = new PrompterPrepareResult() { nTemplatesHashCode = nHash, aSplittedText = _cPrompterTemplate.aSplittedText.ToArray() };
			return cRetVal;
		}
		void cPrompterTemplate_ParseDone(userspace.Template cTemplate)
		{
			try
			{
				_cPrompterTemplate.TextSplit();
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
				throw;
			}
		}
		[WebMethod(EnableSession = true)]
		public void Start(long nID)
		{
			try
			{
				if (nID == _cPrompterTemplate.GetHashCode())
					_cPrompterTemplate.Start();
				else
					throw new Exception("Prompter ID does not match");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
				throw;
			}
		}
		[WebMethod(EnableSession = true)]
		public void Stop(long nID)
		{
			//_cPrompterTemplate.Stop();
			try
			{
				if (nID == _cPrompterTemplate.GetHashCode())
					_cPrompterTemplate.Dispose();
				else
					throw new Exception("Prompter ID does not match");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
				throw;
			}
		}
		[WebMethod(EnableSession = true)]
		public int[] OnOffScreenGet(long nID)
		{
			try
			{
				if (nID == _cPrompterTemplate.GetHashCode())
					return new int[2] { _cPrompterTemplate.nOnScreen, _cPrompterTemplate.nOffScreen };
				else
					throw new Exception("Prompter ID does not match");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
				throw;
			}
			return null;
		}
		[WebMethod(EnableSession = true)]
		public void RollSpeedSet(short nSpeed, long nID)
		{
			try
			{
				if (nID == _cPrompterTemplate.GetHashCode())
					_cPrompterTemplate.RollSpeedSet(nSpeed);
				else
					throw new Exception("Prompter ID does not match");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
				throw;
			}
		}
		[WebMethod(EnableSession = true)]
		public void RestartFrom(int nLine, long nID)
		{
			try
			{
				if (nID == _cPrompterTemplate.GetHashCode())
					_cPrompterTemplate.PrompterRestartFrom(nLine);
				else
					throw new Exception("Prompter ID does not match");
			}
			catch (Exception ex)
			{
				(new Logger()).WriteError(ex);
				throw;
			}
		}
	}
}
