using System.Net.Http.Json;
using serverlesstimestamper.shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace serverlesstimestamper.client
{
    public class FetchDataBase : ComponentBase
    {
        protected List<string> timestamps = new List<string>();
        [Inject]
        public HttpClient Http {get;set;}
    protected HubConnection hubConnection;
    public bool IsConnected => hubConnection.State == HubConnectionState.Connected;

    protected override async Task OnInitializedAsync()
    {
        
        // Prod
        //timestamps = await Http.GetFromJsonAsync<Timestamp[]>("https://cryptotickersnetgps.azurewebsites.net/api/GetPricesJson");
        // Local
        // timestamps = await Http.GetFromJsonAsync<Timestamp[]>("http://localhost:7071/api/TimestamperFunctions?videoUrl={VideoURL}");

       // Prod    
        //hubConnection = new HubConnectionBuilder().WithUrl("https://cryptotickersnetgps.azurewebsites.net/api/").Build();
        // Local
         hubConnection = new HubConnectionBuilder().WithUrl("http://localhost:7071/api").Build();
         // http://localhost:7071/api/negotiate
         //const connection = new signalR.HubConnectionBuilder()


        hubConnection.On<string>("newMessage", (message) =>
            {
            //timestamps = timestamp;
            // messagesList.Items.Add(newMessage);
            //var newMessage = $"{user}: {message}";

            Console.WriteLine("new message:"+message);

            timestamps.Add(message);

                StateHasChanged();
            }                  
        );                                                                                          
   
        await hubConnection.StartAsync();
        
                 
    }

     public async ValueTask DisposeAsync()
    {
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }
    }
    }
}