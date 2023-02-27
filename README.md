<div align="center">

  <h1 align="center">Serverless Timestamper</h1>
  <p>A serverless API that uses AI to generate YouTube video timestamps for you.</p>
    <img src="https://publicnotes.blob.core.windows.net/publicnotes/Screenshot 2023-02-26 at 9.15.11 PM.png"/>
  
	
</div>


## Install

- [Azure SignalR service instance](https://learn.microsoft.com/en-us/azure/azure-signalr/signalr-overview)
- [OpenAI API key](https://platform.openai.com/)
- [Everything to run Azure Functions locally](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide)
- [.NET 7](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)


### How to run the API

1. In the `api` folder, create a file called `local.settings.json`
2. Add the following to that file and make sure to populate with your OPENAI API key and Azure SignalR Service connection string. 
  ```json
  {
    "IsEncrypted": false,
    "Values": {
      "AzureWebJobsStorage": "UseDevelopmentStorage=true",
      "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
      "OPENAI_API_KEY": "",
      "AzureSignalRConnectionString": ""
    },
    "Host": {
      "CORS": "*"
    }
  }
  ```
3. In the `api` folder use `func host start` command (or you can use debugger with F5) to run the API.
4. Make a get call to the URL of the `DurableFunctionsOrchestrationCSharp1_HttpStart` function and provide the `videoUrl` parameter with the url of the video you want to generate timestamps for.

### How to run the client

1. Make sure API is running. 
2. In Pages > `FetchData.razor.cs` make sure `hubConnection = new HubConnectionBuilder().WithUrl("http://localhost:7071/api").Build();` is set to the port your API is running on.
3. In the `client` folder run `dotnet run`

## How it works

The API is powered by [Azure Durable Functions](). It uses [YouTube Explode]() to get the caption track of the requested video, slices the caption track into segments, and sends that segment to [OpenAI API] for a summary. The summary is then broadcasted to SignalR which the client app is connected to.

## To do 

- At the moment, it will create 20 segments no matter the length of the video. This is set manually with the `slices` variable in the API. I plan on making this user setable.
- Local setup isn't ideal.
  - `devcontainer` support in the works.
  - `azd` support in the works.
- The `api` folder currently holds a Durable and non durable function. This is because this whole thing orignally started as a console app then I turned it into a HTTPTrigger Azure Function and then Durable Function which is ultimately the best model for this. I will clean this up.
- Need to improve SignalR connection so API will only broadcast to client and not to all connected.

## Contributing

Feel free to open up an issue or reach out to me with.

- **Gwyneth Pena-Siguenza**: [@madebygps](https://github.com/madebygps)
