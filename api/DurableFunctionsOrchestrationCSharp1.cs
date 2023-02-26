using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using serverlesstimestamper.shared;
using YoutubeExplode;

namespace serverlesstimestamper.api
{
    public static class DurableFunctionsOrchestrationCSharp1
    {
        [Function(nameof(DurableFunctionsOrchestrationCSharp1))]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context, string videoUrl)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(DurableFunctionsOrchestrationCSharp1));
            logger.LogInformation("Saying hello.");
            List<Timestamp> timestamps = new List<Timestamp>();
            var youtube = new YoutubeClient();

            var trackManifest = await youtube.Videos.ClosedCaptions.GetManifestAsync(
                videoUrl
            );


            // Find closed caption track in English
            var trackInfo = trackManifest.GetByLanguage("en");
            var track = await youtube.Videos.ClosedCaptions.GetAsync(trackInfo);

            int slices = 20;

            int captionsPerSlice = track.Captions.Count / slices;

            Console.WriteLine($"The amount of captions is {track.Captions.Count}");
            Console.WriteLine($"The amount of slices is {slices}");
            Console.WriteLine($"The amount of captions per slice is {captionsPerSlice}");

            int startIndex = 0;
            int endIndex = captionsPerSlice;
            string words = "";

            for (int l = 0; l < slices; l++)
            {
                var caption = track.Captions[startIndex];
                Timestamp newTimestamp = new Timestamp(
                    time: caption.Offset.ToString(),
                    summary: ""
                );

                words = "";
                // call existing function
                for (int k = startIndex; k < endIndex; k++)
                {
                    caption = track.Captions[k];
                    if (!string.IsNullOrWhiteSpace(caption.Text))
                    {
                        words += $"{caption.Text}";
                    }
                }

                // call new function from orchestrator
                string result = await context.CallActivityAsync<string>(nameof(GenerateSummary), words);
                newTimestamp.summary = result;
                timestamps.Add(newTimestamp);

                var client = new HttpClient();
                //var responseFromSignalR = await client.PostAsync($"http://localhost:7071/api/BroadcastToAll?videoUrl={newTimestamp.time + ": " + result}", new StringContent(result, Encoding.UTF8, "application/json"));

                var responseFromSignalR = await context.CallActivityAsync<HttpResponseMessage>(nameof(BroadcastToAll), newTimestamp.time + ": " + result);
                logger.LogInformation($"Response: {responseFromSignalR.StatusCode}");

                if (endIndex + captionsPerSlice < track.Captions.Count)
                {
                    startIndex = startIndex + captionsPerSlice;
                    endIndex = endIndex + captionsPerSlice;
                }
                else
                {
                    startIndex = endIndex;
                }
            }
            var outputs = new List<string>();
            string json = JsonSerializer.Serialize(timestamps);

            outputs.Add(json);


            // Replace name and input with values relevant for your Durable Functions Activity

            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }


        [Function("GenerateSummary")]
        public async static Task<string> GenerateSummary([ActivityTrigger] string text, FunctionContext executionContext, int slices, int startIndex, int endIndex)
        {
            var openAiService = new OpenAIService(new OpenAI.GPT3.OpenAiOptions()
            {
                ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            });

            string summary = "";

            var completionResult = await openAiService.Completions.CreateCompletion(new CompletionCreateRequest()
            {
                Prompt = $"(Summarize the following in 10 words: {text} Summary:",
                Model = Models.TextDavinciV3,
                Temperature = (float?)0.75,
                TopP = 1,
                MaxTokens = 630,
                FrequencyPenalty = 0,
                PresencePenalty = 0

            });

            if (completionResult.Successful)
            {
                summary = completionResult.Choices.FirstOrDefault().ToString().Remove(0, 25);
                int index = summary.IndexOf("Index =");
                if (index >= 0)
                {
                    summary = summary.Substring(0, index);

                }
            }
            else
            {
                if (completionResult.Error == null)
                {
                    throw new Exception("Unknown Error");
                }
                Console.WriteLine($"{completionResult.Error.Code}: {completionResult.Error.Message}");
            }

            // call signalr hub to send message to client
            // call SendToSignalR function
            //var myInfo = new MyInfo()

            //http://localhost:7071/api/SendToSignalR
            // make api get call to url

            //http://localhost:7071/api/BroadcastToAll
            return summary;



        }

        [Function(nameof(SayHello))]
        public static string SayHello([ActivityTrigger] string name, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("SayHello");
            logger.LogInformation("Saying hello to {name}.", name);
            return $"Hello {name}!";
        }

        [Function("BroadcastToAll")]
        [SignalROutput(HubName = "timestamper", ConnectionStringSetting = "AzureSignalRConnectionString")]
        public static SignalRMessageAction BroadcastToAll([ActivityTrigger] string message)
        {
            //using var bodyReader = new StreamReader(req.Body);
            return new SignalRMessageAction("newMessage")
            {
                // broadcast to all the connected clients without specifying any connection, user or group.
                Arguments = new[] { message }
            };
        }


        [Function("DurableFunctionsOrchestrationCSharp1_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("DurableFunctionsOrchestrationCSharp1_HttpStart");

            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(DurableFunctionsOrchestrationCSharp1));

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return client.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
