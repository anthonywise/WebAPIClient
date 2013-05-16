using System;
using System.Net;
using Newtonsoft.Json;
using TradeStation.SystemTeam.Tools.WebAPI.WebAPIObjects;

namespace TradeStation.SystemTeam.Tools.WebAPI.WebAPIClient.V2
{
	public static class Authorization
	{
		public static AccessToken GetAccessToken(Uri apiRoot, string authCode, string clientId, string clientSecret,
		                                         Uri redirectUri)
		{
			// TODO: This seems like a bug - the ids have to be cast to string and ucased or the api won't recognize them... once this is fixed (if) this following cast can be removed.
			string postData =
				string.Format(
					"grant_type=authorization_code&code={0}&client_id={1}&redirect_uri={2}&client_secret={3}",
					authCode,
					clientId.ToString().ToUpper(),
					redirectUri,
					clientSecret.ToString().ToUpper());
			
			Uri authUrl = new Uri(string.Format("{0}v2/security/authorize", apiRoot));
			WebHeaderCollection headers = new WebHeaderCollection {{"CONTENT-TYPE", "application/x-www-form-urlencoded"}};

			string result = HttpClient.HttpPost(authUrl, postData, 20000, headers);
			AccessToken token = JsonConvert.DeserializeObject<AccessToken>(result, new V2TokenReader());
			return token;
			
		}
	}
}
