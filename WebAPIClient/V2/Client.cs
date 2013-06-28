using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using TradeStation.SystemTeam.Tools.WebAPI.WebAPIObjects;



namespace TradeStation.SystemTeam.Tools.WebAPI.WebAPIClient.V2
{
	/// <summary>
	/// Class which exposes methods in WebAPI v2. Return values are types from WebAPIObjects library, which are C# implementations of the JSON objects defined in the WebAPI 
	/// documentation.
	/// 
	/// Events and Exceptions: An exception that can be expected to prevent subsequent method calls from succeeding will throw an exception to the client, which will interrupt 
	/// application flow. For example, invalid login credentials or an invalid client id, or a bad URL. Something that is expected and that the client should be able to recover from 
	/// and continue processing will be returned to the client as an event. A common example of this is a timeout or a quota exceeded exception. In this case, the request can be retried
	/// or the next request can be safely processed.
	/// 
	/// QuotaExceededException: WebAPI v2 uses rate-limiting to restrict the number of consecutive method calls by clientid. The methods in this class will fire a QuotaExceededEvent
	/// when this happens and the class will sleep for a set period of time (5 seconds by default). After sleeping, the request will be retried once. This pattern must be implemented per method.
	/// </summary>
	public class Client
	{
		private readonly Uri root;
		public AccessToken Token { private set; get; }
		/// <summary>
		/// Useful for responding to access token expired event when using the password grant type. The login for the current client can be cached here and used to get a new token.
		/// </summary>
		public Login UserCredentials { get; set; }
		private const int TimeoutDefault = 10000;
		private const int SleepDefault = 5000;
		private readonly int sleepTime;
		private DateTime sleepUntil = DateTime.MinValue;
		private readonly object sync = new object(); 

		#region ctor

		public Client(AccessToken token, WebAPIEnvironment environment, int sleepTime = SleepDefault) :
			this(token, WebAPILocation.WebAPIEnvironments.First(e => e.Name == environment).Location, sleepTime)
		{ }

		public Client(AccessToken token, Uri apiRoot, int sleepTime = SleepDefault)
		{
			Token = token;
			root = apiRoot;
			this.sleepTime = sleepTime;
		}

		#endregion  ctor

		private DateTime SleepUntil
		{
			get 
			{
				return sleepUntil; 
			}
			set 
			{
				lock (sync)
				{
					sleepUntil = value;
				}
			}
		}

		#region events

		public event EventHandler KeepAliveFailure;
		public event HttpEventHandler QuotaExceeded;
		public event HttpEventHandler MessageResent;
		public event HttpEventHandler Timeout;
		public event SymbolNotFoundEventHandler SymbolNotFound;
		public event EventHandler AccessTokenExpired;

		/// <summary>
		/// This event is fired when an async call throws an exception.
		/// </summary>
		public event ClientTaskExceptionHandler ClientTaskException; 

		private void OnQuotaExceeded(Uri uri)
		{
			OnQuotaExceeded(this, new HttpEventArgs(uri));
		}

		protected void OnQuotaExceeded(object sender, HttpEventArgs args)
		{
			if (QuotaExceeded != null) QuotaExceeded(sender, args);
		}

		private void OnMessageResent(Uri uri)
		{
			OnMessageResent(this, new HttpEventArgs(uri));
		}

		protected void OnMessageResent(object sender, HttpEventArgs args)
		{
			if (MessageResent != null) MessageResent(sender, args);
		}

		private void OnTimeout(Uri uri)
		{
			OnTimeout(new HttpEventArgs(uri));
		}

		private void OnTimeout(HttpEventArgs args)
		{
			OnTimeout(this, args);
		}

		protected void OnTimeout(object sender, HttpEventArgs args)
		{
			if (Timeout != null) Timeout(sender, args);
		}

		private void OnSymbolNotFound(string symbol)
		{
			OnSymbolNotFound(new SymbolNotFoundArgs(symbol));
		}

		private void OnSymbolNotFound(SymbolNotFoundArgs args)
		{
			OnSymbolNotFound(this, args); 
		}

		protected void OnSymbolNotFound(object sender, SymbolNotFoundArgs args)
		{
			if (SymbolNotFound != null) SymbolNotFound(this, args);
		}


		protected void OnClientTaskException(object sender, ClientTaskExceptionArgs args)
		{
			if (ClientTaskException != null) ClientTaskException(sender, args); 
		}

		private void OnClientTaskException(Exception ex)
		{
			OnClientTaskException(this, new ClientTaskExceptionArgs(ex)); 
		}

		protected void OnAccessTokenExpired(object sender, EventArgs args)
		{
			if (AccessTokenExpired != null) AccessTokenExpired(sender, args);
		}

		protected void OnKeepAliveFailure(object sender, EventArgs args)
		{
			if (KeepAliveFailure != null) KeepAliveFailure(sender, args);
		}

		#endregion events

		#region methods

		#region AccountsService

		#region GetAccountBalance

		#region GetAccountBalanceAsync

		public event GetAccountBalanceCompletedHandler GetAccountBalanceCompleted;
		public event GetAccountBalancesCompletedHandler GetAccountBalancesCompleted;
		private delegate Account GetAccountBalanceCompletedTask(int accountId, int timeout);
		private delegate List<Account> GetAccountBalancesCompletedTask(string accountIds, int timeout);

		protected virtual void OnGetAccountBalanceCompleted(GetAccountBalanceCompletedArgs args)
		{
			if (GetAccountBalanceCompleted != null) GetAccountBalanceCompleted(this, args);
		}

		protected virtual void OnGetAccountBalancesCompleted(GetAccountBalancesCompletedArgs args)
		{
			if (GetAccountBalancesCompleted != null) GetAccountBalancesCompleted(this, args);
		}

		public void GetAccountBalanceAsync(int accountId, int timeout = TimeoutDefault)
		{
			GetAccountBalanceCompletedTask task = GetAccountBalance;
			AsyncCallback callback = GetAccountBalanceCompletedCallback;
			task.BeginInvoke(accountId, timeout, callback, null);
		}

		public void GetAccountBalancesAsync(int[] accountIds, int timeout = TimeoutDefault)
		{
			GetAccountBalancesAsync(string.Join(",", accountIds), timeout); 
		}

		public void GetAccountBalancesAsync(string accountIds, int timeout = TimeoutDefault)
		{
			GetAccountBalancesCompletedTask task = GetAccountBalances;
			AsyncCallback callback = GetAccountBalanceCompletedCallback;
			task.BeginInvoke(accountIds, timeout, callback, null);
		}

		private void GetAccountBalanceCompletedCallback(IAsyncResult result)
		{
			GetAccountBalanceCompletedTask task = (GetAccountBalanceCompletedTask)((AsyncResult)result).AsyncDelegate;
			try
			{
				Account account = task.EndInvoke(result);
				OnGetAccountBalanceCompleted(new GetAccountBalanceCompletedArgs(account));
			}
			catch (Exception ex)
			{
				OnClientTaskException(ex); 
			}
			
		}

		#endregion GetAccountBalanceAsync

		public Account GetAccountBalance(int accountId, int timeout = TimeoutDefault)
		{
			Account account = Account.Empty; 
			List<Account> accounts = GetAccountBalances(accountId.ToString(CultureInfo.InvariantCulture), timeout);
			if (accounts.Count > 0) account = accounts[0];
			return account;
		}

		public List<Account> GetAccountBalances(int[] accountIds, int timeout = TimeoutDefault)
		{ 
			return GetAccountBalances(string.Join(",", accountIds), timeout); 
		}

		public List<Account> GetAccountBalances(string accountIds, int timeout = TimeoutDefault)
		{
			Uri uri = new Uri(root, string.Format("/v2/accounts/{0}/balances", accountIds));
			string response = TryGet(uri, timeout);

			List<Account> accounts = new List<Account>();
			
			JArray jArray = JArray.Parse(response);
			foreach (JObject j in jArray)
			{
				if (j["BODOpenTradeEquity"] != null)
				{
					accounts.Add(JsonConvert.DeserializeObject<FuturesAccount>(j.ToString()));
				}
				if (j["BODMarginRequirement"] != null)
				{
					accounts.Add(JsonConvert.DeserializeObject<ForexAccount>(j.ToString()));
				}
				else
				{
					accounts.Add(JsonConvert.DeserializeObject<EquityAccount>(j.ToString()));
				}		
			}
			return accounts;
		}

		#endregion GetAccountBalance

		#region GetAccountOrders

		public event GetOrderDetailsCompletedHandler GetAccountOrdersCompleted;
		private delegate List<OrderDetail> GetAccountOrdersCompletedTask(int accountId, int timeout);

		protected virtual void OnGetAccountOrdersCompleted(GetOrderDetailsCompletedArgs args)
		{
			if (GetAccountOrdersCompleted != null) GetAccountOrdersCompleted(this, args);
		}

		public void GetAccountOrdersAsync(int accountId, int timeout = TimeoutDefault)
		{
			GetAccountOrdersCompletedTask task = GetAccountOrders;
			AsyncCallback callback = GetAccountOrdersCompletedCallback;
			task.BeginInvoke(accountId, timeout, callback, null);
		}

		private void GetAccountOrdersCompletedCallback(IAsyncResult result)
		{
			GetAccountOrdersCompletedTask task = (GetAccountOrdersCompletedTask)((AsyncResult)result).AsyncDelegate;
			try
			{
				List<OrderDetail> orders = task.EndInvoke(result);
				OnGetAccountOrdersCompleted(new GetOrderDetailsCompletedArgs(orders));
			}
			catch (Exception ex)
			{
				OnClientTaskException(ex); 
			}
		}

		public List<OrderDetail> GetAccountOrders(int accountId, int timeout = TimeoutDefault)
		{
			return GetAccountOrders(new[] { accountId }, timeout);
		}

		public List<OrderDetail> GetAccountOrders(int[] accountIds, int timeout = TimeoutDefault)
		{
			if (accountIds == null) throw new ArgumentNullException("accountIds", "Null Account ID List");
			if (accountIds.Length == 0) throw new ArgumentOutOfRangeException("accountIds", "Must pass at least one account ID.");

			Uri uri = new Uri(root, string.Format("/v2/accounts/{0}/orders", string.Join(",", accountIds)));
			string response = TryGet(uri, timeout);
			return JsonConvert.DeserializeObject<List<OrderDetail>>(response, new OrderDetailConverter());

		}

		/// <summary>
		/// Returns 
		/// </summary>
		/// <param name="accountIds"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public List<OrderDetail> GetAccountOrders(string accountIds, int timeout = TimeoutDefault)
		{ 
			int[] ids = Array.ConvertAll(accountIds.Split(','), int.Parse);
			return GetAccountOrders(ids, timeout); 
		}

		#endregion GetAccountOrders

		#region GetAccountPositions

		#region GetAccountPositionsAsync

		public event GetPositionsCompletedHandler GetAccountPositionsCompleted;
		private delegate List<Position> GetAccountPositionsCompletedTask(int accountId, int timeout);

		protected virtual void OnGetAccountPositionsCompleted(GetPositionsCompletedArgs args)
		{
			if (GetAccountPositionsCompleted != null) GetAccountPositionsCompleted(this, args);
		}

		public void GetAccountPositionsAsync(int accountId, int timeout = TimeoutDefault)
		{
			GetAccountPositionsCompletedTask task = GetAccountPositions;
			AsyncCallback callback = GetAccountPositionsCompletedCallback;
			task.BeginInvoke(accountId, timeout, callback, null);
		}

		private void GetAccountPositionsCompletedCallback(IAsyncResult result)
		{
			GetAccountPositionsCompletedTask task = (GetAccountPositionsCompletedTask)((AsyncResult)result).AsyncDelegate;
			try
			{
				List<Position> positions = task.EndInvoke(result);
				OnGetAccountPositionsCompleted(new GetPositionsCompletedArgs(positions));
			}
			catch (Exception ex)
			{
				OnClientTaskException(ex); 
			}
		}

		#endregion GetAccountPositionsAsync

		public List<Position> GetAccountPositions(int accountId, int timeout = 5000)
		{
			return GetAccountPositions(accountId.ToString(CultureInfo.InvariantCulture), timeout); 
		}

		public List<Position> GetAccountPositions(int[] accountIds, int timeout = 5000)
		{
			return GetAccountPositions(string.Join(",", accountIds), timeout); 
		}

		public List<Position> GetAccountPositions(string accountIds, int timeout = 5000)
		{
			Uri uri = new Uri(root, string.Format("/v2/accounts/{0}/positions", accountIds));
			string response = TryGet(uri, timeout);
			List<Position> positions = JsonConvert.DeserializeObject<List<Position>>(response);
			positions.Sort(new PositionDateComparer(ListSortDirection.Descending));
			return positions;
		}

		#endregion GetAccountPositions

		#endregion AccountsService


		#region Users Service

		#region GetUserAccounts

		public event GetAccountInfoListCompletedHandler GetUserAccountsCompleted;
		private delegate List<AccountInfo> GetUserAccountsCompletedTask(string userName, int timeout);

		protected virtual void OnGetUserAccountsCompleted(GetAccountInfoListCompletedArgs args)
		{
			if (GetUserAccountsCompleted != null) GetUserAccountsCompleted(this, args);
		}

		public void GetUserAccountsAsync(string userName, int timeout = TimeoutDefault)
		{
			GetUserAccountsCompletedTask task = GetUserAccounts;
			AsyncCallback callback = GetUserAccountsCompletedCallback;
			task.BeginInvoke(userName, timeout, callback, null);
		}

		private void GetUserAccountsCompletedCallback(IAsyncResult result)
		{
			GetUserAccountsCompletedTask task = (GetUserAccountsCompletedTask)((AsyncResult)result).AsyncDelegate;
			try
			{
				List<AccountInfo> accounts = task.EndInvoke(result);
				OnGetUserAccountsCompleted(new GetAccountInfoListCompletedArgs(accounts));
			}
			catch (Exception ex)
			{
				OnClientTaskException(ex); 
			}
		}

		public List<AccountInfo> GetUserAccounts(string userName, int timeout = TimeoutDefault)
		{
			Uri uri = new Uri(root, string.Format("/v2/users/{0}/accounts", userName));
			string response = TryGet(uri, timeout);
			return JsonConvert.DeserializeObject<List<AccountInfo>>(response);
		}

		#endregion GetUserAccounts

		#region GetUserPositions

		public event GetPositionsCompletedHandler GetUserPositionsCompleted;
		private delegate List<Position> GetUserPositionsCompletedTask(string userId, int timeout);

		protected virtual void OnGetUserPositionsCompleted(GetPositionsCompletedArgs args)
		{
			if (GetUserPositionsCompleted != null) GetUserPositionsCompleted(this, args);
		}

		public void GetUserPositionsAsync(string userId, int timeout = TimeoutDefault)
		{
			GetUserPositionsCompletedTask task = GetUserPositions;
			AsyncCallback callback = GetUserPositionsCompletedCallback;
			task.BeginInvoke(userId, timeout, callback, null);
		}

		private void GetUserPositionsCompletedCallback(IAsyncResult result)
		{
			GetUserPositionsCompletedTask task = (GetUserPositionsCompletedTask)((AsyncResult)result).AsyncDelegate;
			try
			{
				List<Position> positions = task.EndInvoke(result);
				OnGetAccountPositionsCompleted(new GetPositionsCompletedArgs(positions));
			}
			catch (Exception ex)
			{
				OnClientTaskException(ex); 
			}
		}

		public List<Position> GetUserPositions(string userId, int timeout)
		{
			Uri uri = new Uri(root, string.Format("/v2/users/{0}/positions", userId));
			string response = TryGet(uri, timeout);
			return JsonConvert.DeserializeObject<List<Position>>(response);
		}

		#endregion GetUserPositions

		#endregion Users Service

		#region Orders Service

		public List<OrderResult> PlaceOrder(Order order, int timeout = TimeoutDefault)
		{
			Uri uri = new Uri(root, "/v2/orders");
			string postData = JsonConvert.SerializeObject(order, new OrderConverter());

			string response = TryPost(uri, postData, timeout);
			JsonSerializerSettings settings = new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore};
			return JsonConvert.DeserializeObject<List<OrderResult>>(response, settings);
		}

		public OrderResult CancelOrder(int orderId, int timeout = TimeoutDefault)
		{
			Uri uri = new Uri(root, string.Format("/v2/orders/{0}", orderId));
			WebHeaderCollection headers = new WebHeaderCollection {{"Authorization", Token.Token}};

			string response = HttpClient.HttpDelete(uri, timeout, headers);
			return JsonConvert.DeserializeObject<OrderResult>(response);
		}

		public OrderResult UpdateOrder(Order order, int timeout = TimeoutDefault)
		{
			if (order == null) throw new ArgumentNullException("order", "null Order passed to UpdateOrder.");

			Uri uri = new Uri(root, string.Format("/v2/orders/{0}", order.OrderId));
			string postData = JsonConvert.SerializeObject(order, new OrderConverter()); 
			WebHeaderCollection headers = new WebHeaderCollection {{"Authorization", Token.Token}};

			string response = HttpClient.HttpPut(uri, postData, timeout, headers);
			return JsonConvert.DeserializeObject<OrderResult>(response);
		}


		public List<OrderConfirmation> ConfirmOrder(Order order, int timeout = TimeoutDefault)
		{
			if (order == null) throw new ArgumentException("NULL Order passed to OrderConfirm", "order");

			Uri uri = new Uri(root, "/v2/orders/confirm");
			List<OrderConfirmation> confirmationList = new List<OrderConfirmation>(1);

			string postData = JsonConvert.SerializeObject(order, new OrderConverter());
			string response = TryPost(uri, postData, timeout);

			switch (order.AssetType)
			{
				case AssetType.FU:
					List<FuturesOrderConfirmation> futuresConfirmationList = JsonConvert.DeserializeObject<List<FuturesOrderConfirmation>>(response);
					if (futuresConfirmationList != null) confirmationList.AddRange(futuresConfirmationList);
					break;
				case AssetType.FX:
					List<ForexOrderConfirmation> forexConfirmationList = JsonConvert.DeserializeObject<List<ForexOrderConfirmation>>(response);
					if (forexConfirmationList != null) confirmationList.AddRange(forexConfirmationList);
					break;
				default:
					List<EquityOrderConfirmation> equityConfirmationList = JsonConvert.DeserializeObject<List<EquityOrderConfirmation>>(response);
					if (equityConfirmationList != null) confirmationList.AddRange(equityConfirmationList);
					break;
			}

			return confirmationList;
		}

		public List<OrderConfirmation> ConfirmGroupOrder(GroupOrder orders, int timeout = TimeoutDefault)
		{
			// TODO: TEST THIS
			if (orders == null) throw new ArgumentException("NULL Order passed to OrderConfirm", "orders");
			if (orders.Orders == null) throw new ArgumentException("GroupOrder with null orders", "orders");
			if (orders.Orders.Count == 0) return new List<OrderConfirmation>(0);

			Uri uri = new Uri(root, "/v2/orders/groups/confirm");
			List<OrderConfirmation> confirmationList = new List<OrderConfirmation>(1);

			string postData = JsonConvert.SerializeObject(orders, new OrderConverter(), new StringEnumConverter() );
			string response = TryPost(uri, postData, timeout);

			switch (orders.Orders[0].AssetType)
			{
				case AssetType.FU:
					List<FuturesOrderConfirmation> futuresConfirmationList = JsonConvert.DeserializeObject<List<FuturesOrderConfirmation>>(response);
					if (futuresConfirmationList != null) confirmationList.AddRange(futuresConfirmationList);
					break;
				case AssetType.FX:
					List<ForexOrderConfirmation> forexConfirmationList = JsonConvert.DeserializeObject<List<ForexOrderConfirmation>>(response);
					if (forexConfirmationList != null) confirmationList.AddRange(forexConfirmationList);
					break;
				default:
					List<EquityOrderConfirmation> equityConfirmationList = JsonConvert.DeserializeObject<List<EquityOrderConfirmation>>(response);
					if (equityConfirmationList != null) confirmationList.AddRange(equityConfirmationList);
					break;
			}

			return confirmationList;
		}



		public List<OrderResult> SendGroupOrder(GroupOrder orders, int timeout = TimeoutDefault)
		{
			//if (!IsAuthorized) throw new ClientAuthorizationException();
			Uri uri = new Uri(root, "/v2/orders/groups");
			string postData = JsonConvert.SerializeObject(orders, new OrderConverter(), new StringEnumConverter());

			string response = TryPost(uri, postData, timeout);
			return JsonConvert.DeserializeObject<List<OrderResult>>(response);
		}

		#endregion Orders Service

		

		#region Data Service

		#region SymbolSearch 

		#region EquitySymbolSearch

		public event GetSymbolListCompletedHandler EquitySymbolSearchCompleted;
		private delegate List<Symbol> EquitySymbolSearchCompletedTask(CountryCode country = CountryCode.ALL, string name = "",
			string description = "", bool includeNoLongerTrading = false, int timeout = TimeoutDefault);

		protected virtual void OnEquitySymbolSearchCompleted(GetSymbolListCompletedArgs args)
		{
			if (EquitySymbolSearchCompleted != null) EquitySymbolSearchCompleted(this, args);
		}

		public void EquitySymbolSearchAsync(CountryCode country = CountryCode.ALL, string name = "",
			string description = "", bool includeNoLongerTrading = false, int timeout = TimeoutDefault)
		{
			EquitySymbolSearchCompletedTask task = EquitySymbolSearch;
			AsyncCallback callback = EquitySymbolSearchCompletedCallback;
			task.BeginInvoke(country, name, description, includeNoLongerTrading, timeout, callback, null);
		}

		private void EquitySymbolSearchCompletedCallback(IAsyncResult result)
		{
			EquitySymbolSearchCompletedTask task = (EquitySymbolSearchCompletedTask)((AsyncResult)result).AsyncDelegate;
			try
			{
				List<Symbol> symbols = task.EndInvoke(result);
				OnEquitySymbolSearchCompleted(new GetSymbolListCompletedArgs(symbols));
			}
			catch (Exception ex)
			{
				OnClientTaskException(ex);
			}
		}

		public List<Symbol> EquitySymbolSearch(CountryCode country = CountryCode.ALL, string name = "",
			string description = "", bool includeNoLongerTrading = false, int timeout = TimeoutDefault)
		{
			Uri uri = new Uri(root, string.Format("/v2/data/symbols/search/{0}",
				SymbolSearch.GetEquitySearchCriteria(SearchCategory.Stock, country, name, description, includeNoLongerTrading)));

			string response = TryGet(uri, timeout);
			return JsonConvert.DeserializeObject<List<Symbol>>(response, new CountryCodeConverter());
		}

		#endregion EquitySymbolSearch

		#region OptionSymbolSearch

		public event GetSymbolListCompletedHandler OptionSymbolSearchCompleted;

		private delegate List<Symbol> OptionSymbolSearchCompletedTask(SearchCategory category, string symbolRoot, uint strikeCount = 3, decimal strikePriceLow = 0,
			decimal strikePriceHigh = decimal.MaxValue, uint dateCount = 3, DateTime? expirationDateLow = null, DateTime? expirationDateHigh = null,
			OptionType optionType = OptionType.Both, FutureType futureType = FutureType.Electronic, SymbolType symbolType = SymbolType.Composite,
			CountryCode country = CountryCode.US, int timeout = TimeoutDefault);


		protected virtual void OnOptionSymbolSearchCompleted(GetSymbolListCompletedArgs args)
		{
			if (OptionSymbolSearchCompleted != null) OptionSymbolSearchCompleted(this, args);
		}

		/// <summary>
		/// Stripped down overload: category and symbol only, assume defaults for all missing params.
		/// </summary>
		/// <param name="category"></param>
		/// <param name="symbolRoot"></param>
		/// <returns></returns>
		public List<Symbol> OptionSymbolSearch(SearchCategory category, string symbolRoot)
		{
			// ReSharper disable RedundantArgumentName
			return OptionSymbolSearch(category: category, symbolRoot: symbolRoot, strikeCount: 3, strikePriceLow: 0);
			// ReSharper restore RedundantArgumentName
		}

		public void OptionSymbolSearchAsync(SearchCategory category, string symbolRoot)
		{
			// call full overload with all params (defaults)

			// ReSharper disable RedundantArgumentName
			OptionSymbolSearchAsync(category: category, symbolRoot: symbolRoot, strikeCount: 3, strikePriceLow: 0);
			// ReSharper restore RedundantArgumentName
		}


		/*
		 Option Symbol Search can either be called by passing a combination of strikecount or strike range with a datacount or date range (4 possible combinations). The 4 public overloads 
		 expose this functionality.
		 */

		/// <summary>
		/// Allows searching by strike count and date count.
		/// </summary>
		public List<Symbol> OptionSymbolSearch(SearchCategory category, string symbolRoot, uint strikeCount = 3, uint dateCount = 3, OptionType optionType = OptionType.Both,
			FutureType futureType = FutureType.Electronic, SymbolType symbolType = SymbolType.Composite, CountryCode country = CountryCode.US, int timeout = TimeoutDefault)
		{
			// call full overload with all params (defaults)
			return OptionSymbolSearch(category, symbolRoot, strikeCount, 0, decimal.MaxValue, dateCount, null, null, optionType, futureType, symbolType, country, timeout);
		}

		public void OptionSymbolSearchAsync(SearchCategory category, string symbolRoot, uint strikeCount = 3, uint dateCount = 3, OptionType optionType = OptionType.Both,
			FutureType futureType = FutureType.Electronic, SymbolType symbolType = SymbolType.Composite, CountryCode country = CountryCode.US, int timeout = TimeoutDefault)
		{
			// call full overload with all params (defaults)
			// ReSharper disable RedundantArgumentName
			OptionSymbolSearchAsync(category: category, symbolRoot: symbolRoot, strikeCount: strikeCount, strikePriceLow: 0, strikePriceHigh: decimal.MaxValue,
			dateCount: dateCount, expirationDateLow: null, expirationDateHigh: null, optionType: optionType, futureType: futureType,
				symbolType: symbolType, country: country, timeout: timeout);
			// ReSharper restore RedundantArgumentName
		}

		/// <summary>
		/// Allows searching by strike range and date count.
		/// </summary>
		public List<Symbol> OptionSymbolSearch(SearchCategory category, string symbolRoot, decimal strikePriceLow, decimal strikePriceHigh,
			uint dateCount = 3, OptionType optionType = OptionType.Both, FutureType futureType = FutureType.Electronic, SymbolType symbolType = SymbolType.Composite,
			CountryCode country = CountryCode.US, int timeout = TimeoutDefault)
		{
			// call full overload with all params (defaults)
			// ReSharper disable RedundantArgumentName
			return OptionSymbolSearch(category: category, symbolRoot: symbolRoot, strikeCount: 3, strikePriceLow: strikePriceLow, strikePriceHigh: strikePriceHigh,
			dateCount: dateCount, expirationDateLow: null, expirationDateHigh: null, optionType: optionType, futureType: futureType, symbolType: symbolType, country: country, timeout: timeout);
			// ReSharper restore RedundantArgumentName
		}

		public void OptionSymbolSearchAsync(SearchCategory category, string symbolRoot, decimal strikePriceLow, decimal strikePriceHigh,
			uint dateCount = 3, OptionType optionType = OptionType.Both, FutureType futureType = FutureType.Electronic, SymbolType symbolType = SymbolType.Composite,
			CountryCode country = CountryCode.US, int timeout = TimeoutDefault)
		{
			// call full overload with all params (defaults)
			// ReSharper disable RedundantArgumentName
			OptionSymbolSearchAsync(category: category, symbolRoot: symbolRoot, strikeCount: 3, strikePriceLow: strikePriceLow, strikePriceHigh: strikePriceHigh,
			dateCount: dateCount, expirationDateLow: null, expirationDateHigh: null, optionType: optionType, futureType: futureType, symbolType: symbolType, country: country, timeout: timeout);
			// ReSharper restore RedundantArgumentName
		}

		/// <summary>
		/// Allows searching by strike count and date range.
		/// </summary>
		public List<Symbol> OptionSymbolSearch(SearchCategory category, string symbolRoot, DateTime expirationDateLow, DateTime expirationDateHigh, uint strikeCount = 3,
			OptionType optionType = OptionType.Both, FutureType futureType = FutureType.Electronic, SymbolType symbolType = SymbolType.Composite,
			CountryCode country = CountryCode.US, int timeout = TimeoutDefault)
		{
			// call full overload with all params (defaults)
			// ReSharper disable RedundantArgumentName
			return OptionSymbolSearch(category: category, symbolRoot: symbolRoot, strikeCount: strikeCount, strikePriceLow: 0, strikePriceHigh: decimal.MaxValue, dateCount: 3,
			expirationDateLow: expirationDateLow, expirationDateHigh: expirationDateHigh, optionType: optionType, futureType: futureType, symbolType: symbolType,
			country: country, timeout: timeout);
			// ReSharper restore RedundantArgumentName
		}

		public void OptionSymbolSearchAsync(SearchCategory category, string symbolRoot, DateTime expirationDateLow, DateTime expirationDateHigh, uint strikeCount = 3,
			OptionType optionType = OptionType.Both, FutureType futureType = FutureType.Electronic, SymbolType symbolType = SymbolType.Composite,
			CountryCode country = CountryCode.US, int timeout = TimeoutDefault)
		{
			// call full overload with all params (defaults)
			// ReSharper disable RedundantArgumentName
			OptionSymbolSearchAsync(category: category, symbolRoot: symbolRoot, strikeCount: strikeCount, strikePriceLow: 0, strikePriceHigh: decimal.MaxValue, dateCount: 3,
			expirationDateLow: expirationDateLow, expirationDateHigh: expirationDateHigh, optionType: optionType, futureType: futureType, symbolType: symbolType,
			country: country, timeout: timeout);
			// ReSharper restore RedundantArgumentName
		}

		/// <summary>
		/// Allows searching by strike range and date range.
		/// </summary>
		public List<Symbol> OptionSymbolSearch(SearchCategory category, string symbolRoot, decimal strikePriceLow, decimal strikePriceHigh,
			DateTime? expirationDateLow = null, DateTime? expirationDateHigh = null, OptionType optionType = OptionType.Both, FutureType futureType = FutureType.Electronic,
			SymbolType symbolType = SymbolType.Composite, CountryCode country = CountryCode.US, int timeout = TimeoutDefault)
		{
			// call full overload with all params (defaults)
			// ReSharper disable RedundantArgumentName
			return OptionSymbolSearch(category: category, symbolRoot: symbolRoot, strikeCount: 3, strikePriceLow: strikePriceLow, strikePriceHigh: strikePriceHigh,
			dateCount: 3, expirationDateLow: expirationDateLow, expirationDateHigh: expirationDateHigh, optionType: optionType, futureType: futureType,
			symbolType: symbolType, country: country, timeout: timeout);
			// ReSharper restore RedundantArgumentName
		}

		public void OptionSymbolSearchAsync(SearchCategory category, string symbolRoot, decimal strikePriceLow, decimal strikePriceHigh,
			DateTime expirationDateLow, DateTime expirationDateHigh, OptionType optionType = OptionType.Both, FutureType futureType = FutureType.Electronic,
			SymbolType symbolType = SymbolType.Composite,
			CountryCode country = CountryCode.US, int timeout = TimeoutDefault)
		{
			// call full overload with all params (defaults)
			OptionSymbolSearchAsync(category, symbolRoot, 3, strikePriceLow, strikePriceHigh, 3, expirationDateLow, expirationDateHigh, optionType, futureType, symbolType, country, timeout);
		}

		/// <summary>
		/// Private method which exposes all possible parameters, with default values for the optional ones.
		/// </summary>
		/// <param name="category">Category=StockOption, IndexOption, FutureOption or CurrencyOption</param>
		/// <param name="symbolRoot">Symbol root. Required Field, the symbol the option is a derivative of, this search will not return options based on a partial root.</param>
		/// <param name="strikeCount">Number of strikes prices above and below the underlying price. Defaults to 3. Ignored if strike price high and low are passed.</param>
		/// <param name="strikePriceLow">Strike price low</param>
		/// <param name="strikePriceHigh">Strike price high</param>
		/// <param name="dateCount">Number of expiration dates. Default value 3. Ignored if expiration dates high and low are passed.</param>
		/// <param name="expirationDateLow">Expiration date low</param>
		/// <param name="expirationDateHigh">Expiration date high</param>
		/// <param name="optionType">Option type (Both, Call, Put) Default: Both</param>
		/// <param name="futureType">Future type (Electronic, Pit) Default: Electronic</param>
		/// <param name="symbolType">SymbolType (Both, Composite, Regional) Default: Composite</param>
		/// <param name="country">Country code (US, DE, CA) Default: US</param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		private List<Symbol> OptionSymbolSearch(SearchCategory category, string symbolRoot,
			uint strikeCount = 3, decimal strikePriceLow = 0, decimal strikePriceHigh = decimal.MaxValue,
			uint dateCount = 3, DateTime? expirationDateLow = null, DateTime? expirationDateHigh = null,
			OptionType optionType = OptionType.Both, FutureType futureType = FutureType.Electronic, SymbolType symbolType = SymbolType.Composite, CountryCode country = CountryCode.US,
			int timeout = TimeoutDefault)
		{
			//if (!IsAuthorized) throw new ClientAuthorizationException();
			if (strikePriceLow > 0 && strikePriceHigh == decimal.MaxValue) throw new ArgumentException("If strikePriceLow is passed a value must also be passed for strikePriceHigh.", "strikePriceHigh");
			if (expirationDateLow.HasValue || expirationDateHigh.HasValue)
			{
				if (!expirationDateLow.HasValue || !expirationDateHigh.HasValue) throw new ArgumentException("If a value is passed for expiration date high or low, the other value must also be passed.");
				if (expirationDateHigh <= expirationDateLow) throw new ArgumentOutOfRangeException("expirationDateHigh", "ExpirationHigh must be greater than ExpirationLow.");
			}

			Uri uri = new Uri(root, string.Format("/v2/data/symbols/search/{0}",
				SymbolSearch.GetOptionSearchCriteria(category, symbolRoot, strikeCount, strikePriceLow, strikePriceHigh, dateCount, expirationDateLow, expirationDateHigh, optionType, futureType, symbolType, country)));
			string response = TryGet(uri, timeout);
			List<Symbol> result = new List<Symbol>(255); 
			List<Symbol> deserialized = JsonConvert.DeserializeObject<List<Symbol>>(response, new CountryCodeConverter());
			if (deserialized != null)
			{
				result = deserialized; 
				result.Sort(new OptionComparer());
			}
			return result;
		}


		private void OptionSymbolSearchAsync(SearchCategory category, string symbolRoot,
			uint strikeCount = 3, decimal strikePriceLow = 0, decimal strikePriceHigh = decimal.MaxValue,
			uint dateCount = 3, DateTime? expirationDateLow = null, DateTime? expirationDateHigh = null,
			OptionType optionType = OptionType.Both, FutureType futureType = FutureType.Electronic, SymbolType symbolType = SymbolType.Composite, CountryCode country = CountryCode.US,
			int timeout = TimeoutDefault)
		{
			OptionSymbolSearchCompletedTask task = OptionSymbolSearch;
			AsyncCallback callback = OptionSymbolSearchCompletedCallback;
			task.BeginInvoke(category, symbolRoot, strikeCount, strikePriceLow, strikePriceHigh, dateCount, expirationDateLow, expirationDateHigh, optionType, futureType,
				symbolType, country, timeout, callback, null);
		}

		private void OptionSymbolSearchCompletedCallback(IAsyncResult result)
		{
			OptionSymbolSearchCompletedTask task = (OptionSymbolSearchCompletedTask)((AsyncResult)result).AsyncDelegate;
			try
			{
				List<Symbol> symbols = task.EndInvoke(result);
				OnOptionSymbolSearchCompleted(new GetSymbolListCompletedArgs(symbols));
			}
			catch (Exception ex)
			{
				OnClientTaskException(ex);
			}
			
		}

		#endregion OptionSymbolSearch

		#region ForexSymbolSearch

		public event GetSymbolListCompletedHandler ForexSymbolSearchCompleted;
		private delegate List<Symbol> ForexSymbolSearchCompletedTask(string name = "", string description = "", int timeout = TimeoutDefault);

		protected virtual void OnForexSymbolSearchCompleted(GetSymbolListCompletedArgs args)
		{
			if (ForexSymbolSearchCompleted != null) ForexSymbolSearchCompleted(this, args);
		}

		public void ForexSymbolSearchAsync(string name = "", string description = "", int timeout = TimeoutDefault)
		{
			ForexSymbolSearchCompletedTask task = ForexSymbolSearch;
			AsyncCallback callback = ForexSymbolSearchCompletedCallback;
			task.BeginInvoke(name, description, timeout, callback, null);
		}

		private void ForexSymbolSearchCompletedCallback(IAsyncResult result)
		{
			ForexSymbolSearchCompletedTask task = (ForexSymbolSearchCompletedTask)((AsyncResult)result).AsyncDelegate;
			try
			{
				List<Symbol> symbols = task.EndInvoke(result);
				OnForexSymbolSearchCompleted(new GetSymbolListCompletedArgs(symbols));
			}
			catch (Exception ex)
			{
				OnClientTaskException(ex);
			}
		}

		public List<Symbol> ForexSymbolSearch(string name = "", string description = "", int timeout = TimeoutDefault)
		{
			//if (!IsAuthorized) throw new ClientAuthorizationException();

			Uri uri = new Uri(root, string.Format("/v2/data/symbols/search/{0}", SymbolSearch.GetForexSearchCriteria(name, description)));

			string response = TryGet(uri, timeout);

			JsonSerializerSettings settings = new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore};
			settings.Converters.Add(new CountryCodeConverter());
			List<Symbol> symbols = new List<Symbol>(255); 
			List<Symbol> converted = JsonConvert.DeserializeObject<List<Symbol>>(response, settings);
			if (converted != null) symbols = converted;
			return symbols;

		}

		#endregion ForexSymbolSearch

		#region FutureSymbolSearch

		public event GetSymbolListCompletedHandler FutureSymbolSearchCompleted;
		private delegate List<Symbol> FutureSymbolSearchCompletedTask(string description = "", string symbolRoot = "", FutureType futureType = FutureType.Electronic,
			Currency currency = Currency.USD, bool includeExpired = false, CountryCode country = CountryCode.ALL, int timeout = TimeoutDefault);

		protected virtual void OnFutureSymbolSearchCompleted(GetSymbolListCompletedArgs args)
		{
			if (FutureSymbolSearchCompleted != null) FutureSymbolSearchCompleted(this, args);
		}

		public void FutureSymbolSearchAsync(string description = "", string symbolRoot = "", FutureType futureType = FutureType.Electronic,
			Currency currency = Currency.USD, bool includeExpired = false, CountryCode country = CountryCode.ALL, int timeout = TimeoutDefault)
		{
			FutureSymbolSearchCompletedTask task = FutureSymbolSearch;
			AsyncCallback callback = FutureSymbolSearchCompletedCallback;
			task.BeginInvoke(description, symbolRoot, futureType, currency, includeExpired, country, timeout, callback, null);
		}

		private void FutureSymbolSearchCompletedCallback(IAsyncResult result)
		{
			FutureSymbolSearchCompletedTask task = (FutureSymbolSearchCompletedTask)((AsyncResult)result).AsyncDelegate;
			try
			{
				List<Symbol> symbols = task.EndInvoke(result);
				OnForexSymbolSearchCompleted(new GetSymbolListCompletedArgs(symbols));
			}
			catch (Exception ex)
			{
				OnClientTaskException(ex);
			}
		}


		public List<Symbol> FutureSymbolSearch(string description = "", string symbolRoot = "", FutureType futureType = FutureType.Electronic,
			Currency currency = Currency.USD, bool includeExpired = false, CountryCode country = CountryCode.ALL, int timeout = TimeoutDefault)
		{
			//if (!IsAuthorized) throw new ClientAuthorizationException();

			Uri uri = new Uri(root, string.Format("/v2/data/symbols/search/{0}",
				SymbolSearch.GetFutureSearchCriteria(description, symbolRoot, futureType, currency, includeExpired, country)));

			string response = TryGet(uri, timeout);
			List<Symbol> symbols = new List<Symbol>(255); 
			List<Symbol> converted = JsonConvert.DeserializeObject<List<Symbol>>(response, new CountryCodeConverter());
			if (converted != null) symbols = converted;
			return symbols;

		}

		#endregion FutureSymbolSearch

		#region FundSymbolSearch

		public event GetSymbolListCompletedHandler FundSymbolSearchCompleted;
		private delegate List<Symbol> FundSymbolSearchTask(SearchCategory category, string description = "", string symbol = "", CountryCode country = CountryCode.ALL, int timeout = TimeoutDefault);

		protected virtual void OnFundSymbolSearchCompleted(GetSymbolListCompletedArgs args)
		{
			if (FundSymbolSearchCompleted != null) FundSymbolSearchCompleted(this, args);
		}

		public void FundSymbolSearchAsync(SearchCategory category, string description = "", string symbol = "", CountryCode country = CountryCode.ALL, int timeout = TimeoutDefault)
		{
			FundSymbolSearchTask task = FundSymbolSearch;
			AsyncCallback callback = FundSymbolSearchCompletedCallback;
			task.BeginInvoke(category, description, symbol, country, timeout, callback, null);
		}

		private void FundSymbolSearchCompletedCallback(IAsyncResult result)
		{
			FundSymbolSearchTask task = (FundSymbolSearchTask)((AsyncResult)result).AsyncDelegate;
			try
			{
				List<Symbol> symbols = task.EndInvoke(result);
				OnFundSymbolSearchCompleted(new GetSymbolListCompletedArgs(symbols));
			}
			catch (Exception ex)
			{
				OnClientTaskException(ex); 
			}
		}


		public List<Symbol> FundSymbolSearch(SearchCategory category, string description = "", string symbol = "", CountryCode country = CountryCode.ALL, int timeout = TimeoutDefault)
		{
			if (category != SearchCategory.MutualFund && category != SearchCategory.MoneyMarketFund) throw new ArgumentException("Invalid SearchCategory, should be MutualFund or MoneyMarketFund.", "category");

			string criteria = SymbolSearch.GetEquitySearchCriteria(category, country, symbol, description);
			Uri uri = new Uri(root, string.Format("/v2/data/symbols/search/{0}",
				criteria));
			string response = TryGet(uri, timeout);
			return JsonConvert.DeserializeObject<List<Symbol>>(response, new CountryCodeConverter());

		}

		#endregion FundSymbolSearch

		#endregion SymbolSearch


		#region GetQuotes

		public List<Quote> GetQuotes(string[] symbols, int timeout = TimeoutDefault)
		{
			return GetQuotes(string.Join(",", symbols), timeout);
		}

		public event GetQuoteListCompletedHandler GetQuotesCompleted;
		private delegate List<Quote> GetQuotesCompletedTask(string symbols, int timeout);

		protected virtual void OnGetQuotesCompleted(GetQuoteListCompletedArgs args)
		{
			if (GetQuotesCompleted != null) GetQuotesCompleted(this, args);
		}

		public void GetQuotesAsync(string symbols, int timeout = TimeoutDefault)
		{
			GetQuotesCompletedTask task = GetQuotes;
			AsyncCallback callback = GetQuotesCompletedCallback;
			task.BeginInvoke(symbols, timeout, callback, task);
		}

		private void GetQuotesCompletedCallback(IAsyncResult result)
		{
			GetQuotesCompletedTask task = (GetQuotesCompletedTask)((AsyncResult)result).AsyncDelegate;
			try
			{
				List<Quote> quotes = task.EndInvoke(result);
				OnGetQuotesCompleted(new GetQuoteListCompletedArgs(quotes));
			}
			catch(Exception ex)
			{
				OnClientTaskException(ex);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="symbols">comma-delimited list of symbols</param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public List<Quote> GetQuotes(string symbols, int timeout = TimeoutDefault)
		{
			Uri uri = new Uri(root, string.Format("/v2/data/quote/{0}", symbols));

			string response = TryGet(uri, timeout);
			return JsonConvert.DeserializeObject<List<Quote>>(response, new AssetTypeConverter());
		}

		#endregion GetQuotes

		#region GetSymbol

		public event GetSymbolCompletedHandler GetSymbolCompleted;
		private delegate Symbol GetSymbolTask(string symbol, int timeout);

		protected virtual void OnGetSymbolCompleted(GetSymbolCompletedArgs args)
		{
			if (GetSymbolCompleted != null) GetSymbolCompleted(this, args);
		}

		public void GetSymbolAsync(string symbol, int timeout = TimeoutDefault)
		{
			GetSymbolTask task = GetSymbol;
			AsyncCallback callback = SymbolCompletedCallback;
			task.BeginInvoke(symbol, timeout, callback, null);
		}

		private void SymbolCompletedCallback(IAsyncResult result)
		{
			GetSymbolTask task = (GetSymbolTask)((AsyncResult)result).AsyncDelegate;
			try
			{
				Symbol symbol = task.EndInvoke(result);
				OnGetSymbolCompleted(new GetSymbolCompletedArgs(symbol));
			}
			catch (Exception ex)
			{
				OnClientTaskException(ex); 
			}
		}

		public Symbol GetSymbol(string symbol, int timeout = TimeoutDefault)
		{
			if (string.IsNullOrEmpty(symbol)) throw new ArgumentException("Invalid Symbol, cannot be null or empty.", "symbol");

			Uri uri = new Uri(root, string.Format("/v2/data/symbol/{0}", symbol));
			string response = TryGet(uri, timeout);
			JsonSerializerSettings settings = new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore};
			settings.Converters.Add(new CountryCodeConverter()); 
			return JsonConvert.DeserializeObject<Symbol>(response, settings);
		}

		#endregion GetSymbol


		#region GetSymbolList
		// TODO: Add async... holding off on this since the entire async strategy will change when we move to 4.5 

		public SymbolList GetSymbolList(string symbolListId, int timeout = TimeoutDefault)
		{
			if (string.IsNullOrEmpty(symbolListId)) throw new ArgumentException("Invalid SymbolListId, cannot be null or empty.", "symbolListId");

			Uri uri = new Uri(root, string.Format("/v2/data/symbollists/{0}", symbolListId));
			string response = TryGet(uri, timeout);
			return JsonConvert.DeserializeObject<SymbolList>(response);
		}
		#endregion GetSymbolList

		#region GetSymbolLists
		// TODO: Add async... holding off on this since the entire async strategy will change when we move to 4.5 

		public List<SymbolList> GetSymbolLists(int timeout = TimeoutDefault)
		{
			Uri uri = new Uri(root, string.Format("/v2/data/symbollists"));
			string response = TryGet(uri, timeout);
			return JsonConvert.DeserializeObject<List<SymbolList>>(response);
		}
		#endregion GetSymbolLists

		#region GetSymbolListSymbols
		// TODO: Add async... holding off on this since the entire async strategy will change when we move to 4.5 

		public List<SymbolInfo> GetSymbolListSymbols(string listId, int timeout = TimeoutDefault)
		{
			Uri uri = new Uri(root, string.Format("/v2/data/symbollists/{0}/symbols", listId));
			string response = TryGet(uri, timeout);
			return JsonConvert.DeserializeObject<List<SymbolInfo>>(response);
		}
		#endregion GetSymbolListSymbols


		#region SymbolSuggest

		public event GetSymbolListCompletedHandler SymbolSuggestCompleted;
		private delegate List<Symbol> SymbolSuggestTask(string suggestText, int top, CountryCode country, AssetType assetType, int timeout);

		private void OnSymbolSuggestCompleted(GetSymbolListCompletedArgs args) 
		{
			if (SymbolSuggestCompleted != null) SymbolSuggestCompleted(this, args); 
		}

		public void SymbolSuggestAsync(string suggestText, int top = 20, CountryCode country = CountryCode.US, AssetType assetType = AssetType.EQ, int timeout = TimeoutDefault)
		{
			SymbolSuggestTask task = SymbolSuggest;
			AsyncCallback callback = SymbolSuggestCompletedCallback;
			task.BeginInvoke(suggestText, top, country, assetType, timeout, callback, null); 
		}

		private void SymbolSuggestCompletedCallback(IAsyncResult result)
		{
			SymbolSuggestTask task = (SymbolSuggestTask)((AsyncResult)result).AsyncDelegate;
			try
			{
				List<Symbol> symbols = task.EndInvoke(result);
				OnSymbolSuggestCompleted(new GetSymbolListCompletedArgs(symbols));
			}
			catch (Exception ex)
			{
				OnClientTaskException(ex); 
			}
		}

		public List<Symbol> SymbolSuggest(string suggestText, int top = 20, CountryCode country = CountryCode.US, AssetType assetType = AssetType.EQ, int timeout = TimeoutDefault)
		{
			// $top=20&$filter=Country%20eq%20'United%20States'&$filter=Category%20eq%20'Stock'
			string assetCriteria = string.Empty;
			// Symbol Suggest only supports EQ, FX, IDX
			switch (assetType)
			{ 
				case AssetType.EQ:
					assetCriteria = "Stock";
					break; 
				//case AssetType.FU:
				//	assetCriteria = "future";
				//	break; 
				case AssetType.FX:
					assetCriteria = "Forex";
					break; 
				case AssetType.IX:
					assetCriteria = "IDX";
					break; 
				//case AssetType.OP:
				//	assetCriteria = "StockOption";
				//	break; 
			}
			string countryCriteria = string.Empty;
			switch (country)
			{
				case CountryCode.CA:
					countryCriteria = "Canada";
					break; 
				case CountryCode.DE:
					countryCriteria = "Germany";
					break; 
				case CountryCode.JP:
					countryCriteria = "Japan";
					break; 
				case CountryCode.US:
					countryCriteria = "United%20States";
					break;
			}

			StringBuilder criteria = new StringBuilder(255);
			criteria.AppendFormat("?$top={0}", 20);
			// forex has no country criteria 
			if (countryCriteria.Length > 0 && assetType != AssetType.FX)
				criteria.AppendFormat("&$filter=Country%20eq%20'{0}'", countryCriteria);
			if (assetCriteria.Length > 0)
				criteria.AppendFormat("&$filter=Category%20eq%20'{0}'", assetCriteria); 

			if (string.IsNullOrEmpty(suggestText)) throw new ArgumentException("Required parameter missing", "suggestText");
			Uri uri = new Uri(root, string.Format("/v2/data/symbols/suggest/{0}{1}", suggestText, criteria));
			string response = TryGet(uri, timeout);
			JsonSerializerSettings settings = new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore};
			settings.Converters.Add(new CountryCodeConverter()); 
			return JsonConvert.DeserializeObject<List<Symbol>>(response, settings);
			
		}

		#endregion SymbolSuggest

		#endregion Data Service

		#region Streaming Service

		public QuoteListener GetQuoteListener()
		{
			return QuoteListener.InitQuoteListener(root, Token);
		}

		public BarChartDataListener GetBarChartDataListener()
		{
			return BarChartDataListener.InitBarchartListener(root, Token);
		}

		#endregion Streaming Service

		#region HttpHelperMethods

		/// <summary>
		/// If a quota exceeded exception has occurred on another thread, sleepuntil will be set to prevent any calls until 
		/// the sleep period has passed. In that case, the current thread will wait until the time is up
		/// </summary>
		private void CheckSleepStatus()
		{ 
			if (SleepUntil > DateTime.Now) 
			{
				Thread.Sleep(SleepUntil.Subtract(DateTime.Now));
			}
		}

		private string TryGet(Uri uri, int timeOut)
		{
			string result = string.Empty;
			CheckSleepStatus(); 
			int retryCount = 0; 
			while (retryCount < 5 && result == string.Empty)
			{
			try
			{
				result = HttpClient.HttpGet(uri, Token.Token, timeOut);
			}
			catch (KeepAliveFailureException)
			{
				OnKeepAliveFailure(this, EventArgs.Empty);
			}
			catch (AccessTokenExpiredException)
			{
				OnAccessTokenExpired(this, EventArgs.Empty);
			}
			catch (ClientQuotaExceededException)
			{
				SleepUntil = DateTime.Now.AddMilliseconds(sleepTime); 
				OnQuotaExceeded(uri);
				Thread.Sleep(sleepTime);
				try
				{
					result = HttpClient.HttpGet(uri, Token.Token, timeOut);
					OnMessageResent(uri);
				}
				catch (ClientQuotaExceededException)
				{
					OnQuotaExceeded(uri);
				}
			}
			catch (ClientTimeoutException)
			{
				OnTimeout(uri);
			}
			catch (SymbolNotFoundException ex)
			{
				OnSymbolNotFound(ex.Symbol); 
			}

				retryCount++;
			}
			return result;
		}


		private string TryPost(Uri uri, string postData, int timeout)
		{
			string result = string.Empty;
			int retryCount = 0;
			while (retryCount < 5 && result == string.Empty)
			{
			try
			{
				result = HttpClient.HttpPost(uri, Token.Token, postData, timeout);
			}
			catch (KeepAliveFailureException)
			{
				OnKeepAliveFailure(this, EventArgs.Empty);
			}
			catch (AccessTokenExpiredException)
			{
				OnAccessTokenExpired(this, EventArgs.Empty);
			}
			catch (ClientQuotaExceededException)
			{
				SleepUntil = DateTime.Now.AddMilliseconds(sleepTime); 
				OnQuotaExceeded(uri);
				try
				{
					Thread.Sleep(sleepTime);
					result = HttpClient.HttpPost(uri, Token.Token, postData, timeout);
					OnMessageResent(uri);
				}
				catch (ClientQuotaExceededException)
				{
					OnQuotaExceeded(uri);
				}
			}
			catch (ClientTimeoutException)
			{
				OnTimeout(uri);
			}
				retryCount++; 
			}

			return result;
		}

		#endregion HttpHelperMethods

		#region Refresh

		public void RefreshToken(AccessToken token)
		{
			Token = token;
		}

		#endregion Refresh

		#endregion methods
	}
}
