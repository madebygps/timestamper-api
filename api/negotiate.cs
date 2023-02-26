

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace serverlesstimestamper.api
{
    public static class negotiate
    {
        [Function("negotiate")]
        public static string Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [SignalRConnectionInfoInput(HubName = "timestamper")] string connectionInfo,
        
            FunctionContext executionContext)
        {
            

            return connectionInfo;
        }
    }

}