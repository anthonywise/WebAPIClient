WebAPIClient
============

Client library for the TradeStation WebAPI

History: 

06/07/2013  Added UserCredentials to the Client object which is used when requesting a new access token after in response to the AccessTokenExpired event
06/07/2013	Added Client.RefreshToken method to allow client to pass in new access token info after the previous one has expired.
06/07/2013	Added AccessTokenExpired event to V2.Client and AccessTokenExpiredException
05/16/2013	Added NuGet references
05/15/2013	Initial checkin of project.