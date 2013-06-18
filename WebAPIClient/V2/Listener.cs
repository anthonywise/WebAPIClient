using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TradeStation.SystemTeam.Tools.WebAPI.WebAPIObjects;

namespace TradeStation.SystemTeam.Tools.WebAPI.WebAPIClient.V2
{
	#region Barchart
	public class BarChartDataListener : StreamListener
	{
		#region properties

		public string Symbol { get; private set; }
		public int BarsReceived { get; private set; }
		private int BarsBack { get; set; }

		#endregion properties

		private BarChartDataListener() { }

		internal static BarChartDataListener InitBarchartListener(Uri root, AccessToken token)
		{
			return new BarChartDataListener { ApiRoot = root, Token = token };
		}

		#region events

		public event BarChartDataReceivedEventHandler BarChartDataReceived;

		/// <summary>
		/// If the client specifies a certain number of bars back, this event fires after the prescribed number of bars has been received.
		/// </summary>
		public event EventHandler BarsBackQuotaReached;

		protected void OnBarChartDataReceived(BarChartDataReceivedEventArgs args)
		{
			if (BarChartDataReceived != null) BarChartDataReceived(this, args);

			//Increment();
			//if (LimitReached) Stop();
		}

		protected void OnBarsBackQuotaReached(object sender, EventArgs args)
		{
			if (BarsBackQuotaReached != null) BarsBackQuotaReached(this, args);
		}

		#endregion events

		public void GetBarchartData(string symbol, int intervalQuantity, BarIntervalUnit interval, DateTime startDate, DateTime? endDate = null)
		{
			if ((interval == BarIntervalUnit.Daily || interval == BarIntervalUnit.Weekly || interval == BarIntervalUnit.Monthly) && intervalQuantity != 1)
				throw new ArgumentException("Quantity must be 1 if the interval is Daily, Weekly, or Monthly", "intervalQuantity");

			string url = string.Format("/v2/stream/barchart/{0}/{1}/{2}/{3}{4}", symbol, intervalQuantity, interval.ToString(), startDate.ToString("MM-dd-yyyy"), endDate != null ? "/" + ((DateTime)endDate).ToString("MM-dd-yyyy") : "");
			Symbol = symbol;
			GetBarchartData(new Uri(ApiRoot, url));
		}

		public void GetBarchartData(string symbol, int intervalQuantity, BarIntervalUnit interval, int barsBack, DateTime lastDate)
		{
			if ((interval == BarIntervalUnit.Daily || interval == BarIntervalUnit.Weekly || interval == BarIntervalUnit.Monthly) && intervalQuantity != 1)
				throw new ArgumentException("Quantity must be 1 if the interval is Daily, Weekly, or Monthly", "intervalQuantity");

			string url = string.Format("/v2/stream/barchart/{0}/{1}/{2}/{3}/{4}", symbol, intervalQuantity, interval.ToString(), barsBack, lastDate.ToString("MM-dd-yyyy"));
			Symbol = symbol;
			BarsBack = barsBack;
			GetBarchartData(new Uri(ApiRoot, url));
		}

		private void GetBarchartData(Uri uri)
		{

			if (Status == ListenerStatus.Running) Stop();

			while (Status != ListenerStatus.Stopped)
			{
				// Stop() sets the status to Sleeping... it will be set to Stopped 
				Thread.Sleep(500);
			}

			Task.Factory.StartNew(() =>
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
				request.Headers.Add("Authorization", Token.Token);
				request.Method = "GET";
				request.KeepAlive = true;
				request.Timeout = TimeOut;
				//request.ConnectionGroupName = new Guid().ToString();
				request.ServicePoint.ConnectionLimit = 100;

				try
				{
					HttpWebResponse response = (HttpWebResponse)request.GetResponse();
					Status = ListenerStatus.Running;
					ConnectedServer = HttpClient.ParseMachineName(response.Headers);
					Stream stream = response.GetResponseStream();
					if (stream == null) return;
					using (stream)
					{
						using (StreamReader reader = new StreamReader(stream))
						{
							try
							{
								BarsReceived = 0;
								while (!reader.EndOfStream && Status == ListenerStatus.Running)
								{
									string line = reader.ReadLine();
									if (line != null && line.Trim().ToUpper() != "END")
									{
										try
										{
											IntradayBarData data = JsonConvert.DeserializeObject<IntradayBarData>(line);
											BarsReceived++;
											OnBarChartDataReceived(new BarChartDataReceivedEventArgs(data));
											if (BarsReceived == BarsBack) OnBarsBackQuotaReached(this, EventArgs.Empty);
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
				catch (WebException ex)
				{
					if (ex.Status == WebExceptionStatus.Timeout)
					{
						OnTimeout(this, new HttpEventArgs(uri));
					}
					else
						throw;
				}

			},
			TaskCreationOptions.LongRunning);

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

		}

		public void GetQuoteSnapshot(string symbolList)
		{

			if (Status == ListenerStatus.Running) Stop();
			while (Status != ListenerStatus.Stopped)
			{
				Thread.Sleep(500);
			}

			Task.Factory.StartNew(() =>
				{
					Uri uri = new Uri(ApiRoot, string.Format("/v2/stream/quote/snapshots/{0}", symbolList));
					HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
					request.Headers.Add("Authorization", Token.Token);
					request.Method = "GET";
					request.KeepAlive = true;
					request.Timeout = TimeOut;
					try
					{
						HttpWebResponse response = (HttpWebResponse)request.GetResponse();
						ConnectedServer = HttpClient.ParseMachineName(response.Headers);

						Status = ListenerStatus.Running;

						Stream stream = response.GetResponseStream();
						if (stream == null) return;
						using (stream)
						{
							using (StreamReader reader = new StreamReader(stream))
							{
								try
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
								catch (WebException)
								{
									Stop();
									throw;
								}
							}
							stream.Close();
							Status = ListenerStatus.Stopped;
						}
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
				},
				TaskCreationOptions.LongRunning);



		}
		
		public void GetQuoteChanges(string symbolList)
		{
			if (Status == ListenerStatus.Running) Stop();
			while (Status != ListenerStatus.Stopped)
			{
				Thread.Sleep(500);
			}

			Task.Factory.StartNew(() =>
			{
				Uri uri = new Uri(ApiRoot, string.Format("/v2/stream/quote/changes/{0}", symbolList));
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
				request.Headers.Add("Authorization", Token.Token);
				request.Method = "GET";
				request.KeepAlive = true;
				request.Timeout = TimeOut;
				try
				{
					HttpWebResponse response = (HttpWebResponse)request.GetResponse();
					ConnectedServer = HttpClient.ParseMachineName(response.Headers);

					Status = ListenerStatus.Running;

					Stream stream = response.GetResponseStream();
					if (stream == null) return;
					using (stream)
					{
						using (StreamReader reader = new StreamReader(stream))
						{
							try
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
							catch (WebException)
							{
								Stop();
								throw;
							}
						}
						stream.Close();
						Status = ListenerStatus.Stopped;
					}
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
			},
				TaskCreationOptions.LongRunning);
		}



	}

	#endregion QuoteListener


}
