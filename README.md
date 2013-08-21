WebAPIClient
============

Client library for the TradeStation WebAPI  

History:   
  
08/21/2013  Modified HttpClient to URLEncode the LocalPath section of endpoints because of an issue with endpoints with commas in them.  
08/03/2013  Added ApiUtility class with static methods for IsOnline (marketdatanormal) and GetVersion  
07/26/2013  Fixed bug with listeners where they were not put in a Stopped state in certain cases  
06/28/2013  Added KeepAliveFailure event  
06/13/2013  Updated Listener code to use TPL instaed of EAP. Added Symbol to barchart listener. Added IsRunning property to base listener class.  
06/12/2013  Added UserCredentials to the Client object which is used when requesting a new access token after in response to the AccessTokenExpired event  
06/07/2013	Added Client.RefreshToken method to allow client to pass in new access token info after the previous one has expired.  
06/07/2013	Added AccessTokenExpired event to V2.Client and AccessTokenExpiredException  
05/16/2013	Added NuGet references  
05/15/2013	Initial checkin of project.  