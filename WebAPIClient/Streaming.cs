using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TradeStation.SystemTeam.Tools.WebAPI.WebAPIObjects;
using System.Runtime.Remoting.Messaging;

namespace TradeStation.SystemTeam.Tools.WebAPI.WebAPIClient
{
	#region StreamingEventArgs

	public class BarChartDataReceivedEventArgs : EventArgs
	{
		public IntradayBarData Data { get; private set; }

		public BarChartDataReceivedEventArgs(IntradayBarData data)
		{
			Data = data;
		}
	}

	public class QuoteReceivedEventArgs : EventArgs
	{
		public Quote Quote { get; private set; }

		public QuoteReceivedEventArgs(Quote quote)
		{
			Quote = quote;
		}
	}

	

	#endregion StreamingEventArgs

	public delegate void BarChartDataReceivedEventHandler(object sender, BarChartDataReceivedEventArgs args);
	public delegate void QuoteReceivedEventHandler(object sender, QuoteReceivedEventArgs args);

	#region listeners

	public enum ListenerStatus
	{ 
		Stopped, 
		Running, 
		Stopping
	}

	public abstract class StreamListener 
	{
		public event HttpEventHandler Timeout;

		protected const int timeOut = 10000;
		private object statusLock = new object(); 

		protected Uri ApiRoot { get; set; }
		public string ConnectedServer { get; set; }

		private ListenerStatus status;
		protected ListenerStatus Status 
		{
			get 
			{
				lock (statusLock)
				{
					return status;
				}
			}
			set 
			{
				lock (statusLock) 
				{
					status = value;
				}
			}
		}
		protected AccessToken Token { get; set; }
		

		public void Stop()
		{
			Status = ListenerStatus.Stopping;
		}

		protected void OnTimeout(object sender, HttpEventArgs args)
		{
			if (Timeout != null) Timeout(sender, args); 
		}

		public StreamListener() 
		{ }
		
	}

	
	#endregion listeners
}
