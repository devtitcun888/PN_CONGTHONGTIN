using Microsoft.AspNetCore.SignalR;

namespace PN_HDSWeb_Admin.Hubs
{
    public class DataHub : Hub
    {
        public const string Endpoint = "/datahub";
    }
}
