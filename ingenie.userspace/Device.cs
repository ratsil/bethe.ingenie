using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using helpers;
using helpers.extensions;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Lifetime;

namespace ingenie.userspace
{
	static public class Device
	{
		public class DownStreamKeyer
		{
			public byte nLevel;
			public bool bInternal;

			public DownStreamKeyer()
			{
				this.nLevel = 0;
				this.bInternal = true;
			}
			public DownStreamKeyer(byte nLevel, bool bInternal)
			{
				this.nLevel = nLevel;
				this.bInternal = bInternal;
			}
		}

		static private shared.Device _cDevice;

		static private DownStreamKeyer _cDownStreamKeyer;
		static public DownStreamKeyer cDownStreamKeyer
		{
			get
			{
				if (null != _cDevice)
				{
					shared.Device.DownStreamKeyer cDownStreamKeyer = ((shared.Device)_cDevice).cDownStreamKeyer;
					if (null != cDownStreamKeyer)
					{
						if(null == _cDownStreamKeyer)
							_cDownStreamKeyer = new DownStreamKeyer();
						_cDownStreamKeyer.nLevel = cDownStreamKeyer.nLevel;
						_cDownStreamKeyer.bInternal = cDownStreamKeyer.bInternal;
					}
					else
						_cDownStreamKeyer = null;
				}
				return _cDownStreamKeyer;
			}
			set
			{
				_cDownStreamKeyer = value;
				if (null != _cDevice)
				{
					(new Logger()).WriteNotice("userspace:keyer enabling");
					if (null != _cDownStreamKeyer)
						((shared.Device)_cDevice).cDownStreamKeyer = new shared.Device.DownStreamKeyer() { nLevel = _cDownStreamKeyer.nLevel, bInternal = _cDownStreamKeyer.bInternal };
					else
						((shared.Device)_cDevice).cDownStreamKeyer = null;
				}
				else
					(new Logger()).WriteNotice("userspace:device is null");
			}
		}

		static Device()
		{
			_cDevice = null;
			Create();
		}

		static public void Create()
		{
			if (null != _cDevice)
				return;
            Helper.InitializeTCPChannel();
			_cDevice = (shared.Device)Activator.CreateInstance(typeof(shared.Device), null, new object[] { Preferences.cUrlAttribute });
			if (null == _cDevice)
				throw new Exception("невозможно создать удаленное устройство"); //TODO LANG
			(new Logger()).WriteDebug3("device:create: [hc:" + _cDevice.GetHashCode() + "]");
		}
	}
}
