using System;
using System.Collections.Generic;
using System.Text;
using BTL;

namespace ingenie.plugins
{
	public delegate void EventDelegate(IPlugin iSender);
	public interface IPlugin
	{
		event EventDelegate Prepared;
		event EventDelegate Started;
		event EventDelegate Stopped;

		BTL.EffectStatus eStatus { get; }
		DateTime dtStatusChanged { get; }

        void Create(string sWorkFolder, string sData);
		void Prepare();
        void Start();
        void Stop();
	}
}
