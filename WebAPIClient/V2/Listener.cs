using System;
using System.IO;
using System.Text;
using System.Net;
using System.Threading;

using Newtonsoft.Json;
using TradeStation.SystemTeam.Tools.WebAPI.WebAPIObjects;
using System.Runtime.Remoting.Messaging;

namespace TradeStation.SystemTeam.Tools.WebAPI.WebAPIClient.V2
{
	#region Barchart
	public class BarChartDataListener : StreamListener
	{
		
		
		private BarChartDataListener() { }

		internal static BarChartDataListener InitBarchartListener(Uri root, AccessToken token)
		{
			return new BarChartDataListener { ApiRoot = root, Token = token };
		}

		public event BarChartDataReceivedEventHandler BarChartDataReceived;

		protected void OnBarChartDataReceived(BarChartDataReceivedEventArgs args)
		{
			if (BarChartDataReceived != null) BarChartDataReceived(this, args);

			//Increment();
			//if (LimitReached) Stop();
		}

		public void GetBarchartData(string symbol, int quantity, BarIntervalUnit interval, DateTime startDate, DateTime? endDate = null)
		{
			if ((interval == BarIntervalUnit.Daily || interval == BarIntervalUnit.Weekly || interval == BarIntervalUnit.Monthly) && quantity != 1)
				throw new ArgumentException("Quantity must be 1 if the interval is Daily, Weekly, or Monthly", "quantity");

			string url = string.Format("/v2/stream/barchart/{0}/{1}/{2}/{3}{4}", symbol, quantity, interval.ToString(), startDate.ToString("MM-dd-yyyy"), endDate != null ? "/" + ((DateTime)endDate).ToString("MM-dd-yyyy") : "");
			GetBarchartData(new Uri(ApiRoot, url));
		}

		public void GetV2BarchartData(string symbol, int quantity, BarIntervalUnit interval, DateTime startDate, DateTime? endDate = null)
		{
			if ((interval == BarIntervalUnit.Daily || interval == BarIntervalUnit.Weekly || interval == BarIntervalUnit.Monthly) && quantity != 1)
				throw new ArgumentException("Quantity must be 1 if the interval is Daily, Weekly, or Monthly", "quantity");

			string url = string.Format("/v2/stream/barchart/{0}/{1}/{2}/{3}{4}", symbol, quantity, interval.ToString(), startDate.ToString("MM-dd-yyyy"), endDate != null ? "/" + ((DateTime)endDate).ToString("MM-dd-yyyy") : "");
			GetBarchartData(new Uri(ApiRoot, url));
		}

		public void GetBarchartData(string symbol, int quantity, BarIntervalUnit interval, int barsBack, DateTime lastDate)
		{
			if ((interval == BarIntervalUnit.Daily || interval == BarIntervalUnit.Weekly || interval == BarIntervalUnit.Monthly) && quantity != 1)
				throw new ArgumentException("Quantity must be 1 if the interval is Daily, Weekly, or Monthly", "quantity");

			string url = string.Format("/v2/stream/barchart/{0}/{1}/{2}/{3}/{4}", symbol, quantity, interval.ToString(), barsBack, lastDate.ToString("MM-dd-yyyy"));
			GetBarchartData(new Uri(ApiRoot, url));
		}

		private void GetBarchartData(Uri uri)
		{
			new Thread(SendBarchartRequest).Start(uri); 
		}

		private void SendBarchartRequest(object param)
		{
			Uri uri = param as Uri;
			if (uri == null) throw new ArgumentException("Unable to parse Uri", "param");

			if (Status == ListenerStatus.Running) Stop();

			while (Status != ListenerStatus.Stopped)
			{
				Thread.Sleep(500); 
			}

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
			request.Headers.Add("Authorization", Token.Token);
			request.Method = "GET";
			request.KeepAlive = true;
			request.Timeout = timeOut;
			try
			{
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				Status = ListenerStatus.Running;
				ConnectedServer = HttpClient.ParseMachineName(response.Headers); 
				new Action<Stream>(BeginRead).BeginInvoke(
					response.GetResponseStream(),
					EndRead,
					null);
			}
			catch (WebException ex)
			{
				if (ex.Status == WebExceptionStatus.Timeout)
				{
					OnTimeout(this, new HttpEventArgs(uri));
				}
				else
					throw;
			}
		}

		private void BeginRead(Stream stream)
		{
			string line;
			IntradayBarData data;
			using (stream)
			{
				using (StreamReader reader = new StreamReader(stream))
				{
					try
					{
						while (!reader.EndOfStream && Status == ListenerStatus.Running)
						{
							line = reader.ReadLine();
							if (line != null && line.Trim().ToUpper() != "END")
							{
								try
								{
									data = JsonConvert.DeserializeObject<IntradayBarData>(line);
									OnBarChartDataReceived(new BarChartDataReceivedEventArgs(data));
								}
								catch (JsonReaderException)
								{
									// ignore exception if one of the lines we receive cannot be deserialized. Ex: "ERROR"
								}
							}
							else
							{
								Status = ListenerStatus.Stopping;
							}
						}
					}
					catch (WebException)
					{
						Status = ListenerStatus.Stopping;
						throw;
					}
					
					stream.Close();
					Status = ListenerStatus.Stopped;
				}
			}
		}

		private void EndRead(IAsyncResult iResult)
		{
			try
			{
				AsyncResult result = iResult as AsyncResult;
				if (result != null)
				{
					Action<Stream> action = (Action<Stream>)result.AsyncDelegate;
					action.EndInvoke(iResult);
				}
				Status = ListenerStatus.Stopped;
			}
			catch
			{
				Status = ListenerStatus.Stopped;
			}
		}

	}
	#endregion BarChart

	#region QuoteListener
	public class QuoteListener : StreamListener
	{
		private QuoteListener() { }

		internal static QuoteListener InitQuoteListener(Uri root, AccessToken token)
		{
			return new QuoteListener { ApiRoot = root, Token = token };
		}

		public event QuoteReceivedEventHandler QuoteReceived;

		protected void OnQuoteReceived(QuoteReceivedEventArgs args)
		{
			if (QuoteReceived != null) QuoteReceived(this, args);

			//Increment();
			//if (LimitReached) Stop();
		}

		public void GetQuoteSnapshot(string symbolList)
		{
			new Thread(SendSnapshotRequest).Start(symbolList);
		}

		private void SendSnapshotRequest(object param)
		{
			string symbolList = param as string;
			if (Status == ListenerStatus.Running) Stop();
			while (Status != ListenerStatus.Stopped)
			{
				Thread.Sleep(500); 
			}

			Uri uri = new Uri(ApiRoot, string.Format("/v2/stream/quote/snapshots/{0}", symbolList));
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
			request.Headers.Add("Authorization", Token.Token);
			request.Method = "GET";
			request.KeepAlive = true;
			request.Timeout = timeOut;
			try
			{
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				ConnectedServer = HttpClient.ParseMachineName(response.Headers); 
				
				Status = ListenerStatus.Running;
				new Action<Stream>(BeginRead).BeginInvoke(
					response.GetResponseStream(),
					EndRead,
					null);
			}
			catch (WebException ex)
			{
				if (ex.Status == WebExceptionStatus.Timeout)
				{
					OnTimeout(this, new HttpEventArgs(uri));
				}
				else
					throw; 
			}
		}

		public void GetQuoteChanges(string symbolList)
		{
			new Thread(SendQuoteChangeRequest).Start(symbolList); 
		}

		private void SendQuoteChangeRequest(object param)
		{ 
			string symbolList = param as string; 
			if (Status == ListenerStatus.Running) Stop();
			while (Status != ListenerStatus.Stopped)
			{
				Thread.Sleep(500); 
			}

			Uri uri = new Uri(ApiRoot, string.Format("/v2/stream/quote/changes/{0}", symbolList));
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
			request.Headers.Add("Authorization", Token.Token);
			request.Method = "GET";
			request.KeepAlive = true;
			request.Timeout = timeOut;
			try
			{
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				Status = ListenerStatus.Running;
				ConnectedServer = HttpClient.ParseMachineName(response.Headers); 
				new Action<Stream>(BeginRead).BeginInvoke(
					response.GetResponseStream(),
					EndRead,
					null);
			}
			catch (WebException ex)
			{
				if (ex.Status == WebExceptionStatus.Timeout)
				{
					OnTimeout(this, new HttpEventArgs(uri));
				}
				else
					throw;
			}
		}

		private void BeginRead(Stream stream)
		{
			using (stream)
			{
				using (StreamReader reader = new StreamReader(stream))
				{
					while (!reader.EndOfStream && Status == ListenerStatus.Running)
					{
						string line = reader.ReadLine();
						if (line != null && line.Trim().ToUpper() != "END")
						{
							try
							{
								Quote quote = JsonConvert.DeserializeObject<Quote>(line, new AssetTypeConverter());
								OnQuoteReceived(new QuoteReceivedEventArgs(quote));
							}
							catch (JsonReaderException)
							{
								// Ignore errors because of non-JSON values
							}
						}
						else
						{
							Stop();
						}
					}
				}
				stream.Close();
				Status = ListenerStatus.Stopped;
			}
		}

		private void EndRead(IAsyncResult iResult)
		{
			AsyncResult result = iResult as AsyncResult;
			if (result != null)
			{
				Action<Stream> action = (Action<Stream>)result.AsyncDelegate;
				action.EndInvoke(iResult);
			}
			
			Status = ListenerStatus.Stopped;
		}

	}

	#endregion QuoteListener

}
