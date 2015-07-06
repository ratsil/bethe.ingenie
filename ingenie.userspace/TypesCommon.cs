using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using helpers;
using helpers.extensions;

namespace ingenie.userspace
{
	public class Color
	{
		public byte nAlpha;
		public byte nRed;
		public byte nGreen;
		public byte nBlue;
		public Color()
		{
			nAlpha = 255;
			nRed = 0;
			nGreen = 0;
			nBlue = 0;
		}
		public void LoadXML(XmlNode cXmlNode)
		{
			if (null == cXmlNode)
				return;
			if (0 < cXmlNode.Attributes.Count)
			{
				foreach (XmlAttribute cAttr in cXmlNode.Attributes)
					switch (cAttr.Name)
					{
						case "alpha":
							try
							{
								nAlpha = cAttr.Value.Trim().ToByte();
							}
							catch
							{
								throw new Exception("Указана некорректная прозрачность цвета [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
						case "red":
							try
							{
								nRed = cAttr.Value.Trim().ToByte();
							}
							catch
							{
								throw new Exception("Указана некорректная красная составляющая цвета [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
						case "green":
							try
							{
								nGreen = cAttr.Value.Trim().ToByte();
							}
							catch
							{
								throw new Exception("Указана некорректная зеленая составляющая цвета [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
						case "blue":
							try
							{
								nBlue = cAttr.Value.Trim().ToByte();
							}
							catch
							{
								throw new Exception("Указана некорректная синяя составляющая цвета [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
					}
			}
		}
	}
	public class Border
	{
		public float nWidth;
		public Color cColor;
		public Border()
		{
			nWidth = 0;
			cColor = new Color();
		}
		public void LoadXML(XmlNode cXmlNode)
		{
			if (null == cXmlNode)
				return;
			if (0 < cXmlNode.Attributes.Count)
			{
				foreach (XmlAttribute cAttr in cXmlNode.Attributes)
					switch (cAttr.Name)
					{
						case "width":
							try
							{
								nWidth = cAttr.Value.Trim().ToFloat();
							}
							catch
							{
								throw new Exception("Указана некорректная ширина кромки [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
					}
			}
			XmlNode cChildNode = cXmlNode.SelectSingleNode("color");
			if (null != cChildNode)
				cColor.LoadXML(cChildNode);
		}
	}
	public class Font
	{
		public string sName;
		public int nSize;
		public int nWidth;
		public int nFontStyle;
		public Color cColor;
		public Border cBorder;
		public Template cTemplate;
		public Font()
		{
			sName = "Arial";
			nSize = 10;
			nWidth = -1;
			nFontStyle = 0;
			cColor = new Color();
			cBorder = new Border();
			cTemplate = null;
		}
		public void LoadXML(XmlNode cXmlNode)
		{
			if (null == cXmlNode)
				return;
			if (0 < cXmlNode.Attributes.Count)
			{
				foreach (XmlAttribute cAttr in cXmlNode.Attributes)
					switch (cAttr.Name)
					{
						case "name":
							try
							{
								sName = cAttr.Value.Trim();
							}
							catch
							{
								throw new Exception("Указано некорректное имя шрифта [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
						case "size":
							try
							{
								string sValue = cAttr.Value.Trim();
								if (null == sValue || 1 > sValue.Length)
									sValue = "30";
								nSize = sValue.ToInt32();
							}
							catch
							{
								throw new Exception("Указан некорректный размер шрифта [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
						case "width":
							try
							{
								nWidth = cAttr.Value.Trim().ToInt32();
							}
							catch
							{
								throw new Exception("Указана некорректная ширина шрифта [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
						case "fontstyle":
							try
							{
								nFontStyle = cAttr.Value.Trim().ToInt32();
							}
							catch
							{
								throw new Exception("Указан некорректный стиль шрифта [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
					}
			}
			XmlNode cChildNode = cXmlNode.SelectSingleNode("color");
			if (null != cChildNode)
				cColor.LoadXML(cChildNode);
			cChildNode = cXmlNode.SelectSingleNode("border");
			if (null != cChildNode)
				cBorder.LoadXML(cChildNode);
		}
		public System.Drawing.Font FontSystemDrawingGet()
		{
			return new System.Drawing.Font(sName, nSize, (System.Drawing.FontStyle)nFontStyle);
		}
	}
	public class Position
	{
		public short nLeft;
		public short nTop;
		public Position()
		{
			nLeft = 0;
			nTop = 0;
		}
		public void LoadXML(XmlNode cXmlNode)
		{
			if (null == cXmlNode)
				return;
			if (0 < cXmlNode.Attributes.Count)
			{
				foreach (XmlAttribute cAttr in cXmlNode.Attributes)
					switch (cAttr.Name)
					{
						case "left":
						case "x":
							try
							{
								nLeft = cAttr.Value.Trim().ToInt16();
							}
							catch
							{
								throw new Exception("указано некорректное горизонтальное значение [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
						case "top":
						case "y":
							try
							{
								nTop = cAttr.Value.Trim().ToInt16();
							}
							catch
							{
								throw new Exception("указано некорректное вертикальное значение [" + cAttr.Name + "=" + cAttr.Value + "][TPL:" + cXmlNode.BaseURI + "]"); //TODO LANG
							}
							break;
					}
			}
		}
	}
}
