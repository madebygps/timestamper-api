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

            // Local
            hubConnection = new HubConnectionBuilder().WithUrl("http://localhost:7071/api").Build();
            hubConnection.On<Timestamp>("newMessage", (message) =>
                {
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