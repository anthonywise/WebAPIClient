using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TradeStation.SystemTeam.Tools.WebAPI.WebAPIClient
{
	internal class AuthParams
	{
		public string UserName { get; set; }
		public string Password { get; set; }
		public int Timeout { get; set; }
		public Guid ClientId { get; set; }
		public Guid ClientSecret { get; set; }

	}
}
