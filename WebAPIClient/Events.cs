using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TradeStation.SystemTeam.Tools.WebAPI.WebAPIObjects;

namespace TradeStation.SystemTeam.Tools.WebAPI.WebAPIClient
{
	public delegate void AuthorizedEventHandler(object sender, AuthorizedEventArgs args); 

	public class AuthorizedEventArgs : EventArgs
	{
		public AccessToken Token { private set; get; }
		public AuthorizedEventArgs(AccessToken token)
		{
			Token = token;
		}
	}

	// Account Balance
	public delegate void GetAccountBalanceCompletedHandler(object sender, GetAccountBalanceCompletedArgs args); 

	public class GetAccountBalanceCompletedArgs : EventArgs
	{
		public Account Account { private set; get; }

		public GetAccountBalanceCompletedArgs(Account account)
		{
			Account = account; 
		}
	}

	// ClientTaskException: An exception that occurs when calling a method via an async task
	public delegate void ClientTaskExceptionHandler(object sender, ClientTaskExceptionArgs args);

	public class ClientTaskExceptionArgs : EventArgs 
	{
		public Exception AsyncException { get; private set; }

		public ClientTaskExceptionArgs(Exception ex)
		{
			AsyncException = ex;
		}
	}

	public delegate void GetAccountBalancesCompletedHandler(object sender, GetAccountBalancesCompletedArgs args);

	public class GetAccountBalancesCompletedArgs : EventArgs
	{
		public List<Account> Accounts { private set; get; }

		public GetAccountBalancesCompletedArgs(List<Account> accounts)
		{
			Accounts = accounts;
		}
	}

	// Account Orders 
	public delegate void GetOrderDetailsCompletedHandler(object sender, GetOrderDetailsCompletedArgs args);

	public class GetOrderDetailsCompletedArgs : EventArgs
	{
		public List<OrderDetail> Orders { private set; get; }

		public GetOrderDetailsCompletedArgs(List<OrderDetail> orders)
		{
			Orders = orders;
		}
	}


	// Account Positions
	public delegate void GetPositionsCompletedHandler(object sender, GetPositionsCompletedArgs args);

	public class GetPositionsCompletedArgs : EventArgs
	{
		public List<Position> Positions { private set; get; }

		public GetPositionsCompletedArgs(List<Position> positions)
		{
			Positions = positions;
		}
	}

	// AccountInfoList
	public delegate void GetAccountInfoListCompletedHandler(object sender, GetAccountInfoListCompletedArgs args);

	public class GetAccountInfoListCompletedArgs : EventArgs
	{
		public List<AccountInfo> Accounts { private set; get; }

		public GetAccountInfoListCompletedArgs(List<AccountInfo> accounts)
		{
			Accounts = accounts;
		}
	}

	// HttpEvent - an event that occurs when hitting a URL
	public delegate void HttpEventHandler(object sender, HttpEventArgs args);

	public class HttpEventArgs : EventArgs
	{
		public Uri Uri { get; private set; }

		public HttpEventArgs(Uri uri)
		{
			Uri = uri;
		}
	}

	// Fired when the user searched for a symbol that was not found, prevents 404 error from bubbling up to the client
	public delegate void SymbolNotFoundEventHandler(object sender, SymbolNotFoundArgs args);

	public class SymbolNotFoundArgs : EventArgs
	{
		public string Symbol{get;private set;}
		
		public SymbolNotFoundArgs(string symbol)
		{
			Symbol = symbol;
		}

	}

	// SymbolList
	public delegate void GetSymbolListCompletedHandler(object sender, GetSymbolListCompletedArgs args);

	public class GetSymbolListCompletedArgs : EventArgs
	{
		public List<Symbol> Symbols { private set; get; }

		public GetSymbolListCompletedArgs(List<Symbol> symbols)
		{
			Symbols = symbols;
		}
	}

	

	// QuoteList
	public delegate void GetQuoteListCompletedHandler(object sender, GetQuoteListCompletedArgs args);

	public class GetQuoteListCompletedArgs : EventArgs
	{
		public List<Quote> Quotes { private set; get; }

		public GetQuoteListCompletedArgs(List<Quote> quotes)
		{
			Quotes = quotes;
		}
	}

	// Symbol
	public delegate void GetSymbolCompletedHandler(object sender, GetSymbolCompletedArgs args); 

	public class GetSymbolCompletedArgs : EventArgs
	{
		public Symbol Symbol { private set; get; }

		public GetSymbolCompletedArgs(Symbol symbol)
		{
			Symbol = symbol;
		}
	}

}
