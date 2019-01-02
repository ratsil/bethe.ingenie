using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using BTL.Play;
using btl = BTL.Play;
using helpers;
using System.Xml;
using helpers.extensions;
using System.Text;

namespace ingenie.plugins
{
	class Preferences
	{
		public class WeatherItem
		{
			public int nID;
			public string sCity;
			public string sTime;
			public string sTemperature;
			public string sIcon;
			public string sDetales;
			public string sRegion;
			private WeatherItem(XmlNode cXmlNode)
			{
				nID = cXmlNode.AttributeGet<int>("output");
				string sValue;
				foreach (XmlNode cXN in cXmlNode.SelectNodes("item"))
				{
					sValue = cXN.FirstChild.Value.Trim();
					switch (cXN.AttributeValueGet("id"))
					{
						case "POST_CITY":
							sCity = sValue;
							break;
						case "POST_TIME":
							sTime = sValue;
							break;
						case "POST_TEMPERATURE":
							sTemperature = sValue;
							break;
						case "POST_ICON":
							sIcon = sValue;
							break;
						case "POST_DETAILS":
							sDetales = sValue;
							break;
						case "POST_REGION":
							sRegion = sValue;
							break;
					}
				}
			}
			static public List<WeatherItem> LoadItems(XmlNode cXmlNode)
			{
				List<WeatherItem> aRetVal = new List<WeatherItem>();
				foreach (XmlNode cXN in cXmlNode.SelectNodes("item"))
					aRetVal.Add(new WeatherItem(cXN));
				return aRetVal;
			}
		}
		public class DataXML
		{
			public string sRequest;
			public byte nTemplate;
			public string sCode;
			public string sValue;
			public int nInterval;
			public XmlNode cYandexData;
			public DataXML(XmlNode cXmlNode)
			{
				sRequest = cXmlNode.AttributeValueGet("request");
				nTemplate = cXmlNode.AttributeGet<byte>("template");
				sCode = cXmlNode.AttributeValueGet("code");
				sValue = cXmlNode.AttributeValueGet("value");
				nInterval = cXmlNode.AttributeGet<int>("interval");

				cYandexData = Data.Get(sRequest, nTemplate, "code=" + sCode + ";" + sValue);
			}
		}
		public class Item
		{
			public BTL.IVideo cVideo
			{
				get
				{
					return (BTL.IVideo)_cEffect;
				}
			}
			private BTL.IEffect _cEffect;
			public BTL.IEffect cEffect
			{
				get
				{
					if (cEffectXML != null)
					{
						if (_cEffect != null)
							_cEffect.Dispose();
						if (sFolderAttr == null)
							sFolderAttr = cEffectXML.AttributeValueGet("folder");

						cEffectXML.Attributes["folder"].Value = sFolderAttr.Replace("{%FOLDER%}", sFolder);
						return Effect.EffectGet(cEffectXML);
					}
					else
						return _cEffect;
				}
				set
				{
					if (cEffectXML == null)
						_cEffect = value;
					else
						throw new Exception("operation is not permitted!");
				}
			}
			public List<Roll.Keyframes> aKFs;
			public XmlNode cEffectXML;
			private string sFolderAttr;
			public string sFolder; // для icon
			public Item()
			{
			}
			public void AddKFs(Roll.Keyframes aKF)
			{
				if (null==aKFs)
					aKFs = new List<Roll.Keyframes>();
				aKFs.Add(aKF);
			}
			public XmlNode XMLReplace(string sTarget, string sReplacement)
			{
				string sNode = cEffectXML.OuterXml.Replace(sTarget, sReplacement);
				XmlDocument cXmlDocument = new XmlDocument();
				cXmlDocument.LoadXml(sNode);
				XmlNode cXmlNode = cXmlDocument.NodesGet()[0];
				return cXmlNode;
			}
			static public Dictionary<string, Item> DictionaryGet(XmlNode cXmlNode)
			{
				Dictionary<string, Item> ahRetVal = new Dictionary<string, Item>();
				Dictionary<string, XmlNode> ahNodes = new Dictionary<string, XmlNode>();
				foreach (XmlNode cXN in cXmlNode.ChildNodes)
				{
					if (null != cXN.AttributeValueGet("name", false))
						ahNodes.Add(cXN.AttributeValueGet("name"), cXN);
				}
				ahRetVal.Add("text_city_1", new Item());
				ahRetVal["text_city_1"].cEffect = Effect.EffectGet(ahNodes["city"]);
				ahRetVal["text_city_1"].AddKFs(new Roll.Keyframes(ahNodes["kf_city"]));
				Roll.Keyframe.BezierPreCalculate(ahRetVal["text_city_1"].aKFs[0]);

				ahRetVal.Add("text_city_2", new Item());
				ahRetVal["text_city_2"].cEffect = Effect.EffectGet(ahNodes["city"]);
				ahRetVal["text_city_2"].AddKFs(new Roll.Keyframes(ahNodes["kf_city"]));
				Roll.Keyframe.BezierPreCalculate(ahRetVal["text_city_2"].aKFs[0]);

				ahRetVal.Add("text_time", new Item());
				ahRetVal["text_time"].cEffect = Effect.EffectGet(ahNodes["time"]);
				ahRetVal["text_time"].AddKFs(new Roll.Keyframes(ahNodes["kf_time"]));
				Roll.Keyframe.BezierPreCalculate(ahRetVal["text_time"].aKFs[0]);

				ahRetVal.Add("text_temperature", new Item());
				ahRetVal["text_temperature"].cEffect = Effect.EffectGet(ahNodes["temperature"]);
				ahRetVal["text_temperature"].AddKFs(new Roll.Keyframes(ahNodes["kf_temperature"]));
				Roll.Keyframe.BezierPreCalculate(ahRetVal["text_temperature"].aKFs[0]);
				ahRetVal["text_temperature"].AddKFs(new Roll.Keyframes(ahNodes["kf_temperature_out"]));
				Roll.Keyframe.BezierPreCalculate(ahRetVal["text_temperature"].aKFs[1]);
				ahRetVal["text_temperature"].AddKFs(new Roll.Keyframes(ahNodes["kf_temperature_hold"]));
				Roll.Keyframe.BezierPreCalculate(ahRetVal["text_temperature"].aKFs[2]);

				ahRetVal.Add("text_yandex", new Item());
				ahRetVal["text_yandex"].cEffect = Effect.EffectGet(ahNodes["yandex"]);

				ahRetVal.Add("animation_icon", new Item());
				ahRetVal["animation_icon"].cEffect = null; // на ходу создадим с правильным файлом
				ahRetVal["animation_icon"].cEffectXML = ahNodes["icon"];
				ahRetVal["animation_icon"].AddKFs(new Roll.Keyframes(ahNodes["kf_icon"]));
				Roll.Keyframe.BezierPreCalculate(ahRetVal["animation_icon"].aKFs[0]);

				ahRetVal.Add("backgr_intro", new Item());
				ahRetVal["backgr_intro"].cEffect = Effect.EffectGet(ahNodes["begin_left"]);

				ahRetVal.Add("backgr_loop", new Item());
				ahRetVal["backgr_loop"].cEffect = Effect.EffectGet(ahNodes["begin_static"]);  // no

				ahRetVal.Add("backgr_pink", new Item());
				ahRetVal["backgr_pink"].cEffect = Effect.EffectGet(ahNodes["mid_pink"]);

				//ahRetVal.Add("backgr_pink_in", new Item());
				//ahRetVal["backgr_pink_in"].cEffect = Effect.EffectGet(ahNodes["mid_pink_in"]);

				//ahRetVal.Add("backgr_pink_loop", new Item());
				//ahRetVal["backgr_pink_loop"].cEffect = Effect.EffectGet(ahNodes["mid_pink_loop"]);

				//ahRetVal.Add("backgr_pink_out", new Item());
				//ahRetVal["backgr_pink_out"].cEffect = Effect.EffectGet(ahNodes["mid_pink_out"]);

				ahRetVal.Add("backgr_black_in", new Item());
				ahRetVal["backgr_black_in"].cEffect = Effect.EffectGet(ahNodes["mid_black_in"]);

				ahRetVal.Add("backgr_black_loop", new Item());
				ahRetVal["backgr_black_loop"].cEffect = Effect.EffectGet(ahNodes["mid_black_loop"]);

				ahRetVal.Add("backgr_black_out", new Item());
				ahRetVal["backgr_black_out"].cEffect = Effect.EffectGet(ahNodes["mid_black_out"]);

				ahRetVal.Add("backgr_final_loop", new Item());
				ahRetVal["backgr_final_loop"].cEffect = Effect.EffectGet(ahNodes["end_white"]);

				ahRetVal.Add("backgr_final_pink", new Item());
				ahRetVal["backgr_final_pink"].cEffect = Effect.EffectGet(ahNodes["end_pink"]);

				//ahRetVal.Add("mask_final", new Item());
				//ahRetVal["mask_final"].cEffect = Effect.EffectGet(ahNodes["end_mask"]);

				ahRetVal.Add("mask_city_loop_1", new Item());
				ahRetVal["mask_city_loop_1"].cEffect = Effect.EffectGet(ahNodes["begin_static"]);
                ((BTL.IVideo)ahRetVal["mask_city_loop_1"].cEffect).cMask = new Mask() { eMaskType = DisCom.Alpha.mask_invert };

                ahRetVal.Add("mask_city_loop_2", new Item());
				ahRetVal["mask_city_loop_2"].cEffect = Effect.EffectGet(ahNodes["begin_static"]);
                ((BTL.IVideo)ahRetVal["mask_city_loop_2"].cEffect).cMask = new Mask() { eMaskType = DisCom.Alpha.mask_invert };

                ahRetVal.Add("mask_time", new Item());
				ahRetVal["mask_time"].cEffect = Effect.EffectGet(ahNodes["mid_pink"]);
                ((BTL.IVideo)ahRetVal["mask_time"].cEffect).cMask = new Mask() { eMaskType = DisCom.Alpha.mask_invert };

                //ahRetVal.Add("mask_time_in", new Item());
                //ahRetVal["mask_time_in"].cEffect = Effect.EffectGet(ahNodes["mid_pink_in"]);

                //ahRetVal.Add("mask_time_out", new Item());
                //ahRetVal["mask_time_out"].cEffect = Effect.EffectGet(ahNodes["mid_pink_out"]);

                ahRetVal.Add("mask_tempr_in", new Item());
				ahRetVal["mask_tempr_in"].cEffect = Effect.EffectGet(ahNodes["mid_black_in"]);
                ((BTL.IVideo)ahRetVal["mask_tempr_in"].cEffect).cMask = new Mask() { eMaskType = DisCom.Alpha.mask_invert };

    //            ahRetVal.Add("mask_tempr_out", new Item());
				//ahRetVal["mask_tempr_out"].cEffect = Effect.EffectGet(ahNodes["mid_black_out"]);

				return ahRetVal;
			}
		}

		public btl.Roll cRoll { get { return _cRoll; } }
		public List<WeatherItem> aWeatherItems { get { return _aWeatherItems; } }
		public Dictionary<string, Item> ahItems { get { return _ahItems; } }

		private btl.Roll _cRoll;
		private Dictionary<string, Item> _ahItems;

		private List<WeatherItem> _aWeatherItems;
		
		public Preferences(string sData)
		{
			XmlDocument cXmlDocument = new XmlDocument();
			cXmlDocument.LoadXml(sData);
			XmlNode cXmlNode = cXmlDocument.NodeGet("data");

			DataXML cSourceData = new DataXML(cXmlNode);
			_aWeatherItems = WeatherItem.LoadItems(cSourceData.cYandexData);
			if (_aWeatherItems.Count < 2)
				throw new Exception("There are must be more than 1 weather items. Add some other cities");

			_cRoll = new Roll(cXmlNode.NodeGet("roll"));

			_ahItems = Item.DictionaryGet(cXmlNode);
		}
	}
}