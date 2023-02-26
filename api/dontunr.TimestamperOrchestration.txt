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
    public class TimestamperOrchestration
    {

        public static bool isComplete { get; set; } = false;

        [Function(nameof(TimestamperOrchestration))]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context, string videoUrl)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(TimestamperOrchestration));
            //logger.LogInformation("Saying hello.");


            var outputs = new List<string>();

            // Replace name and input with values relevant for your Durable Functions Activity
            outputs.Add(await context.CallActivityAsync<string>(nameof(GenerateJsonTimestamps), videoUrl));


            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]


            // We don't want the orchestration to run infinitely
            // If the operation has not completed within 30 mins, end the orchestration
            var operationTimeoutTime = context.CurrentUtcDateTime.AddMinutes(30);

            while (true)
            {
                var operationHasTimedOut = context.CurrentUtcDateTime > operationTimeoutTime;

                if (operationHasTimedOut)
                {
                    context.SetCustomStatus("Encoding has timed out, please submit the job again.");
                    break;
                }

                var isEncodingComplete = await context.CallActivityAsync<bool>("IsGenerationComplete");

                if (isEncodingComplete)
                {
                    context.SetCustomStatus("Encoding has completed successfully.");
                    break;
                }

                // If no timeout and encoding still being processed we want to put the orchestration to sleep,
                // and awaking it again after a specified interval
                var nextCheckTime = context.CurrentUtcDateTime.AddSeconds(15);
                logger.LogInformation($"************** Sleeping orchestration until {nextCheckTime.ToLongTimeString()}");
                await context.CreateTimer(nextCheckTime, CancellationToken.None);

                
            }
            return outputs;

        }

        [Function(nameof(IsGenerationComplete))]
        public bool IsGenerationComplete([ActivityTrigger] string videoUrl, ILogger log)
        {
            //log.LogInformation($"************** Checking if {fileName} encoding is complete...");
            // Here you would make a call to an API, query a database, check blob storage etc 
            // to check whether the long running asyn process is complete

            // For demo purposes, we'll just signal completion every so often


            return isComplete;
        }



        [Function("GenerateJsonTimestamps")]
        public static async Task<string> GenerateJsonTimestamps([ActivityTrigger] string videoUrl, FunctionContext executionContext)
        {

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

                for (int k = startIndex; k < endIndex; k++)
                {
                    caption = track.Captions[k];
                    if (!string.IsNullOrWhiteSpace(caption.Text))
                    {
                        words += $"{caption.Text}";
                    }
                }
                string result = await generateTimestamp(words);
                newTimestamp.summary = result;
                timestamps.Add(newTimestamp);

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

            string json = JsonSerializer.Serialize(timestamps);
            isComplete = true;

            return json;
        }

        public static async Task<String> generateTimestamp(string text)
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
            return summary;
        }


        [Function(nameof(SayHello))]
        public static string SayHello([ActivityTrigger] string name, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("SayHello");
            logger.LogInformation("Saying hello to {name}.", name);
            return $"Hello {name}!";
        }

        [Function("TimestamperOrchestration_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req, string videoUrl,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("TimestamperOrchestration_HttpStart");

            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(TimestamperOrchestration), videoUrl);

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return client.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
