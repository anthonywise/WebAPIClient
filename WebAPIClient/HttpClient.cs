using System;
using System.IO;
using System.Net;
using System.Text;

namespace TradeStation.SystemTeam.Tools.WebAPI.WebAPIClient
{
	internal static class HttpClient
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="uri">Destination URL</param>
		/// <param name="headers">Request headers collection</param>
		/// <param name="timeout">Timeout in milliseconds. If request times out a WebException is thrown with the Status property set to Timeout.</param>
		/// <returns></returns>
		public static string HttpGet(Uri uri, WebHeaderCollection headers, int timeout)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
			SetHeaders(headers, request); 
			request.Timeout = timeout;
			request.Method = "GET"; 
			IWebProxy proxy = WebRequest.DefaultWebProxy;
			proxy.Credentials = CredentialCache.DefaultCredentials;
			request.Proxy = proxy;

			try
			{
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				return ReadResponse(response);
			}
			catch (WebException ex)
			{
				HandleWebException(ex);
				return string.Empty; 
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="uri">Endpoint URL</param>
		/// <param name="token">Valid auth token</param>
		/// <param name="timeout">Timeout in milliseconds</param>
		/// <param name="acceptType">One of the accepted data formats, either application/JSON or application/XML</param>
		/// <returns></returns>
		public static string HttpGet(Uri uri, string token, int timeout, string acceptType = "application/JSON")
		{
			WebHeaderCollection headers = InitHeaders(token, acceptType); 
			return HttpGet(uri, headers, timeout); 
		}

		public static string HttpPost(Uri uri, string token, string postData, int timeout, string acceptType = "application/JSON")
		{
			try
			{
				return HttpPost(uri, postData, timeout, InitHeaders(token, acceptType));
			}
			catch (WebException ex)
			{
				HandleWebException(ex);
				return string.Empty; 
			}
		}

		public static string HttpPost(Uri uri, string postData, int timeout, WebHeaderCollection headers)
		{
			return SendData(uri, postData, "POST", timeout, headers); 
		}

		public static string HttpDelete(Uri uri, int timeout, WebHeaderCollection headers)
		{
			HttpWebRequest request = InitRequest(uri, timeout, headers, "DELETE");
			
			try
			{
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				return ReadResponse(response);
			}
			catch (WebException ex)
			{
				HandleWebException(ex);
				return string.Empty;
			}
		}

		public static string HttpPut(Uri uri, string data, int timeout, WebHeaderCollection headers)
		{
			return SendData(uri, data, "PUT", timeout, headers); 
		}

		private static string SendData(Uri uri, string data, string method, int timeout, WebHeaderCollection headers)
		{
			HttpWebRequest request = InitRequest(uri, timeout, headers, method);

			byte[] dataBytes = Encoding.Default.GetBytes(data);
			Stream postStream = request.GetRequestStream();
			postStream.Write(dataBytes, 0, dataBytes.Length);
			postStream.Close();
			try
			{
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				return ReadResponse(response);
			}
			catch (WebException ex)
			{
				HandleWebException(ex);
				return string.Empty;
			}
		}
		
		private static HttpWebRequest InitRequest(Uri uri, int timeout, WebHeaderCollection headers, string method)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
			if (string.IsNullOrEmpty(headers["CONTENT-TYPE"])) headers.Add("CONTENT-TYPE", "application/JSON");
			SetHeaders(headers, request);
			request.Timeout = timeout;
			request.Method = method;
			return request; 
		}

		private static void HandleWebException(WebException ex)
		{
			if (ex.Status == WebExceptionStatus.Timeout) throw new ClientTimeoutException();
			
			if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
			{
				Stream responseStream = ex.Response.GetResponseStream();
				if (responseStream != null)
				{
					string message = new StreamReader(responseStream).ReadToEnd();
					 
					if (message.Contains("quota exceeded"))
						throw new ClientQuotaExceededException();

					string symbol;
					if (IsSymbolNotFound(ex, out symbol))
						throw new SymbolNotFoundException(symbol); 
					
					if (message.Contains("Message") && message.Contains("StatusCode"))
						throw new ClientBadRequestException(message);
					
					if (ex.Message.Contains("(401) Unauthorized"))
						throw new ClientAuthorizationException();
					
					throw new ClientGenericProtocolException(message); 

				}
				
				
			}

			throw ex; 
		}

		private static bool IsSymbolNotFound(WebException ex, out string symbol)
		{
			symbol = string.Empty;
			string url = ex.Response.ResponseUri.ToString(); 
			if (ex.Message.Contains("(404) Not Found") && url.ToLower().Contains("symbols/search"))
			{
				string[] queryVals = !string.IsNullOrEmpty(ex.Response.ResponseUri.Query) ? ex.Response.ResponseUri.Query.Split('&') : url.Substring(url.LastIndexOf('/')).Split('&');

				foreach (string val in queryVals)
				{
					string[] pair = val.Split('=');
					if (pair.Length == 2)
					{
						if (pair[0].ToUpper() == "R")
						{
							symbol = pair[1];
							break;
						}
					}
				}
				return true;
			}
			return false;
		}

		private static void SetHeaders(WebHeaderCollection headers, HttpWebRequest request)
		{
			foreach (string key in headers.AllKeys)
			{
				DateTime testDate;
				switch (key.ToUpper())
				{
					case "AUTHORIZATION": /* Add "Bearer" to the beginning of the token (if v2). This fixes an issue where the token is not read correctly if it contains equals characters at the end. */
						string value = headers[key];
						if (request.Address.ToString().ToLower().Contains("/v2/"))
							request.Headers.Add(key, value.Contains("Bearer") ? value : "Bearer " + value); 
						else
							request.Headers.Add(key, value); 
						break;
					case "CONTENT-TYPE":
						request.ContentType = headers[key];
						break;
					case "ACCEPT":
						request.Accept = headers[key];
						break;
					case "CONNECTION":
						request.Connection = headers[key];
						break; 
					case "CONTENT-LENGTH":
						long testLong;
						if (long.TryParse(headers[key], out testLong))
							request.ContentLength = testLong;
						break;
					case "DATE":
						if (DateTime.TryParse(headers[key], out testDate))
							request.Date = testDate;
						break;
					case "EXPECT":
						request.Expect = headers[key];
						break;
					case "HOST":
						request.Host = headers[key];
						break;
					case "IF-MODIFIED-SINCE":
						if (DateTime.TryParse(headers[key], out testDate))
							request.IfModifiedSince = testDate;
						break;
					case "RANGE":
						int testInt;
						if (int.TryParse(headers[key], out testInt))
							request.AddRange(testInt); 
						break;
					case "REFERER":
						request.Referer = headers[key];
						break;
					case "TRANSFER-ENCODING":
						request.TransferEncoding = headers[key];
						break;
					case "USER-AGENT":
						request.UserAgent = headers[key];
						break; 
					case "PROXY-CONNECTION":
						request.Proxy = new WebProxy(headers[key]);
						break;
					default:
						request.Headers.Add(key, headers[key]);
						break;
				}
			}
		}

		private static WebHeaderCollection InitHeaders(string token, string acceptType)
		{
			if (string.IsNullOrEmpty(acceptType)) throw new ArgumentException("Missing required parameter.", "acceptType");
			switch (acceptType.ToUpper())
			{
				case "APPLICATION/JSON":
				case "APPLICATION/XML":
				case "APPLICATION/X-WWW-FORM-URLENCODED": 
					break;
				default:
					throw new ArgumentException("Invalid value for acceptType: " + acceptType, "acceptType");
			}

			if (string.IsNullOrEmpty(token)) throw new ArgumentException("Auth token cannot be null or empty.", "token");

			WebHeaderCollection headers = new WebHeaderCollection
				{
					{"Authorization", token},
					{HttpRequestHeader.Accept, acceptType}
				};

			return headers;
		}

		private static string ReadResponse(HttpWebResponse response)
		{
			if (response == null) return string.Empty;

			Stream responseStream = response.GetResponseStream();
			string responseText = string.Empty;
			if (responseStream != null)
			{
				using (StreamReader reader = new StreamReader(responseStream))
				{
					responseText = reader.ReadToEnd();
					reader.Close();
				}
			}
			
			response.Close();
			return responseText;
		}

		public static string ParseMachineName(WebHeaderCollection responseHeaders)
		{
			string machineName = "?";

			for (int i = 0; i < responseHeaders.Count; i++)
			{
				if (responseHeaders.Keys[i] == "X-ServerName")
				{
					string[] values = responseHeaders.GetValues(i);
					if (values != null && values.Length > 0) machineName = values[0];
					break;
				}
			}
			return machineName; 
		}
	}
}
