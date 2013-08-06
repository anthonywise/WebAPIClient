using System;
using System.Net;
using TradeStation.SystemTeam.Tools.WebAPI.WebAPIObjects;


namespace TradeStation.SystemTeam.Tools.WebAPI.WebAPIClient.V2
{
	public static class ApiUtility
	{
		private const int UtilityTimeout = 5000;
		

		public static bool IsOnline(Uri apiRoot)
		{
			try
			{
				Uri uri = new Uri(apiRoot, "/v2/Status.ashx");
				WebHeaderCollection headers = new WebHeaderCollection();
				string result = HttpClient.HttpGet(uri, headers, UtilityTimeout);
				return result == "marketdatanormal";
			}
			catch
			{
				return false;
			}
		}

		public static bool IsOnline(WebAPIEnvironment environment)
		{		
			return IsOnline(WebAPILocation.GetEnvironmentUri(environment));
		}

		public static int GetVersion(Uri apiRoot)
		{
			int version = 0;
			try
			{
				Uri uri = new Uri(apiRoot, "/v2/version.txt");
				WebHeaderCollection headers = new WebHeaderCollection();
				string result = HttpClient.HttpGet(uri, headers, UtilityTimeout);
				string[] fields = result.Split('/');
				if (fields.Length == 4)
				{
					int.TryParse(fields[fields.Length - 1], out version);
				}
			}
			catch
			{
				version = -1;
			}

			return version;
		}

		public static int GetVersion(WebAPIEnvironment environment)
		{
			return GetVersion(WebAPILocation.GetEnvironmentUri(environment));
		}
	}
}
