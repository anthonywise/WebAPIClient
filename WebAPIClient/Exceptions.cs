using System;
using System.Runtime.Serialization;

using Newtonsoft.Json;
using TradeStation.SystemTeam.Tools.WebAPI.WebAPIObjects;

namespace TradeStation.SystemTeam.Tools.WebAPI.WebAPIClient
{
	public class AccessTokenExpiredException : APIClientException
	{
		public AccessTokenExpiredException() { }
		public AccessTokenExpiredException(string message) : base(message) { }
		public AccessTokenExpiredException(string message, Exception innerException) : base(message, innerException) { }
		public AccessTokenExpiredException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	public abstract class APIClientException : Exception 
	{
		protected APIClientException() { }
		protected APIClientException(string message) : base(message) { }
		protected APIClientException(string message, Exception innerException) : base(message, innerException) { }
		protected APIClientException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	
	public class ClientAuthorizationException : APIClientException
	{
		public ClientAuthorizationException() { }
		public ClientAuthorizationException(string message) : base(message) { }
		public ClientAuthorizationException(string message, Exception innerException) : base(message, innerException) { }
		public ClientAuthorizationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
	

	public class ClientTimeoutException : APIClientException
	{ 
		public ClientTimeoutException() { }
		public ClientTimeoutException(string message) : base(message) { }
		public ClientTimeoutException(string message, Exception innerException) : base(message, innerException) { }
		public ClientTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	public class ClientNotAuthorizedException : APIClientException
	{
		public ClientNotAuthorizedException() { }
		public ClientNotAuthorizedException(string message) : base(message) { }
		public ClientNotAuthorizedException(string message, Exception innerException) : base(message, innerException) { }
		public ClientNotAuthorizedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	public class ClientQuotaExceededException : APIClientException
	{ }

	public class SymbolNotFoundException : APIClientException
	{
		public string Symbol{get;private set;}
		public SymbolNotFoundException(string symbol)
		{
			Symbol = symbol;
		}

		public SymbolNotFoundException(string symbol, string message) : base(message) 
		{
			Symbol = symbol;
		}

		public SymbolNotFoundException(string symbol, string message, Exception innerException) : base(message, innerException)
		{
			Symbol = symbol;
		}
		
		public SymbolNotFoundException(string symbol, SerializationInfo info, StreamingContext context) : base(info, context) 
		{
			Symbol = symbol;
		}


	}

	public class ClientBadRequestException : APIClientException
	{ 
		public Error RequestError {get; private set;}
		public string Json { get; private set; }
		public bool ParseErrorOccurred{get; private set;}

		public ClientBadRequestException(string json)
		{
			ParseError(json); 
		}
		
		public ClientBadRequestException(string json, string message) : base(message) 
		{
			ParseError(json); 
		}
		
		public ClientBadRequestException(string json, string message, Exception innerException) : base(message, innerException) 
		{
			ParseError(json); 
		}
		
		public ClientBadRequestException(string json, SerializationInfo info, StreamingContext context) : base(info, context) 
		{ 
			ParseError(json); 
		} 

		private void ParseError(string json)
		{
			Json = json; 
			try
			{
				RequestError = JsonConvert.DeserializeObject<Error>(json); 
				ParseErrorOccurred = false;
			}
			catch(JsonSerializationException)
			{
				ParseErrorOccurred = true;
			}
		}

	}

	public class ClientGenericProtocolException : APIClientException
	{
		public string ErrorMessage { get; private set; }

		public ClientGenericProtocolException(string errorMessage)
		{
			ErrorMessage = errorMessage;
		}

		public ClientGenericProtocolException(string errorMessage, string message)
			: base(message)
		{
			ErrorMessage = errorMessage;
		}

		public ClientGenericProtocolException(string errorMessage, string message, Exception innerException)
			: base(message, innerException)
		{
			ErrorMessage = errorMessage;
		}

		public ClientGenericProtocolException(string errorMessage, SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			ErrorMessage = errorMessage;
		}

	}

	public class MaintenanceException : APIClientException
	{
		public string ResponseText { get; private set; }

		public MaintenanceException(string responseText) 
		{
			ResponseText = responseText; 
		}

		public MaintenanceException(string message, string responseText) : base(message)
		{
			ResponseText = responseText;
		}

		public MaintenanceException(string message, Exception innerException, string responseText) : base(message, innerException)
		{
			ResponseText = responseText;
		}

		public MaintenanceException(SerializationInfo info, StreamingContext context, string responseText) : base(info, context)
		{
			ResponseText = responseText;
		}
	}
}
