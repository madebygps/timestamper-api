using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using serverlesstimestamper.shared;

namespace serverlesstimestamper.client
{
    public class FetchDataBase : ComponentBase
    {
        protected List<Timestamp> timestamps = new List<Timestamp>();
        [Inject]
        public HttpClient Http { get; set; }
        protected HubConnection hubConnection;
        public bool IsConnected => hubConnection.State == HubConnectionState.Connected;
        protected override async Task OnInitializedAsync()
        {

            // Prod
            //timestamps = await Http.GetFromJsonAsync<Timestamp[]>("https://cryptotickersnetgps.azurewebsites.net/api/GetPricesJson");
            // Local
            // timestamps = await Http.GetFromJsonAsync<Timestamp[]>("http://localhost:7071/api/TimestamperFunctions?videoUrl={VideoURL}");

            // Prod    
            hubConnection = new HubConnectionBuilder().WithUrl("https://timestamper.azurewebsites.net/api/").Build();
            // Local
            // https://timestamper.azurewebsites.net/runtime/webhooks/durabletask/instances/25cb279e16774af18328b155b84c0ed7?code=-gxsvn015plk0D8ninqwn7VBpkl8wo3RlZLlDb4R7f9RAzFuOeSePw==
            // hubConnection = new HubConnectionBuilder().WithUrl("http://localhost:7071/api").Build();
            // http://localhost:7071/api/negotiate
            //const connection = new signalR.HubConnectionBuilder()

            hubConnection.On<Timestamp>("newMessage", (message) =>
                {
                    //timestamps = timestamp;
                    // messagesList.Items.Add(newMessage);
                    //var newMessage = $"{user}: {message}";

                    Console.WriteLine("new message:" + message);
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