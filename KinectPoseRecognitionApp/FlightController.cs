using System;
using System.Net.WebSockets;

namespace KinectPoseRecognitionApp
{

    public class FlightController
    {
        ClientWebSocket _client;
        System.Threading.CancellationToken _cancelToken;

        public FlightController(string connection)
        {
            _client = new ClientWebSocket();
            _cancelToken = new System.Threading.CancellationToken();
            _client.ConnectAsync(connection, _cancelToken);
        }


    }
}
