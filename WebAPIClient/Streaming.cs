using System;
using System.Threading;
using System.Threading.Tasks;

using TradeStation.SystemTeam.Tools.WebAPI.WebAPIObjects;

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

		protected const int TimeOut = 10000;
		private readonly object statusLock = new object(); 

		public StreamListener()
		{
			CancellationSource = new CancellationTokenSource();
			ConnectedSince = DateTime.MinValue;
			LastPacketReceived = DateTime.MinValue;
		}

		#region properties

		protected CancellationTokenSource CancellationSource { get; set; }
		protected Uri ApiRoot { get; set; }
		public string ConnectedServer { get; set; }
		public DateTime ConnectedSince { get; private set; }
		public DateTime LastPacketReceived { get; protected set; }
		public int ConnectedSeconds {
			get
			{
				if (status == ListenerStatus.Running && ConnectedSince > DateTime.MinValue)
					return (int)DateTime.Now.Subtract(ConnectedSince).TotalSeconds;

				return 0;
			}
		}

		public int ElapsedSecondsSinceLastPacket
		{
			get
			{
				if (LastPacketReceived > DateTime.MinValue)
					return (int) DateTime.Now.Subtract(LastPacketReceived).TotalSeconds;

				return 0;

			}
		}
		public bool IsRunning
		{
			get { return Status == ListenerStatus.Running; }
		}

		private ListenerStatus status;
		protected ListenerStatus Status 
		{
			get 
			{
				return status;
			}
			set 
			{
				lock (statusLock) 
				{
					if (value == ListenerStatus.Stopped) 
						ConnectedSince = DateTime.MinValue;
					else if (value == ListenerStatus.Running) 
						ConnectedSince = DateTime.Now;
					status = value;
				}
			}
		}
		protected AccessToken Token { get; set; }
		
		#endregion properties

		#region methods

		public void Stop()
		{
			Status = ListenerStatus.Stopping;
		}

		protected void OnTimeout(object sender, HttpEventArgs args)
		{
			if (Timeout != null) Timeout(sender, args); 
		}
	
		#endregion methods
	}

	
	#endregion listeners
}
