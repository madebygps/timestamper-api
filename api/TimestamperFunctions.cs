using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using YoutubeExplode;

namespace timestamper.Function
{
    public class TimestamperFunctions
    {
        private readonly ILogger _logger;

        string youtubeUrl = "https://www.youtube.com/watch?v=LgWRbXw9dRU";

        public TimestamperFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TimestamperFunctions>();
        }

        [Function("TimestamperFunctions")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            var youtube = new YoutubeClient();

            var video = await youtube.Videos.GetAsync(youtubeUrl);

            //using StreamWriter file = new("WriteLines.txt", append: true);

            var trackManifest = await youtube.Videos.ClosedCaptions.GetManifestAsync(
                youtubeUrl
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

            for (int l = 0; l < slices; l++)
            {
                var caption = track.Captions[startIndex];
                //await file.WriteLineAsync($"TIMESTAMP: {caption.Offset}");
                response.WriteString($"TIMESTAMP: {caption.Offset}");
                string words = "";

                for (int k = startIndex; k < endIndex; k++)
                {
                    caption = track.Captions[k];
                    if (!string.IsNullOrWhiteSpace(caption.Text))
                    {
                        words += $"{caption.Text} ";
                    }
                }
                string result = await generateTimestamp(words);
                response.WriteString(result + "/n");
                //writeTofile(result);
                if (endIndex + captionsPerSlice < track.Captions.Count)
                {
                    startIndex = startIndex + captionsPerSlice;
                    endIndex = endIndex + captionsPerSlice;
                }
                else
                {
                    startIndex = endIndex;
                    words = "";
                    //await file.WriteLineAsync($"TIMESTAMP: {caption.Offset}");
                    response.WriteString($"TIMESTAMP: {caption.Offset}");

                    for (var m = endIndex; m < track.Captions.Count; m++)
                    {
                        caption = track.Captions[m];

                        // Check if the last caption is not empty
                        if (!string.IsNullOrWhiteSpace(caption.Text))
                        {
                            words += $"{caption.Text} ";
                        }
                    }
                    string theresult = await generateTimestamp(words);
                    response.WriteString(theresult+"/n");
                    //writeTofile(theresult);

                }


            }
            return response;
        }

        async Task<String> generateTimestamp(string text)
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
                // remove last 25 characters 
                int index = summary.IndexOf("Index =");
                if (index >= 0)
                {
                    summary = summary.Substring(0, index);
                }
                //Console.WriteLine(summary);
                // await file.WriteLineAsync(summary);
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


    }
}