using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using RosbridgeNet.RosbridgeClient.Common;
using RosbridgeNet.RosbridgeClient.Common.Interfaces;
using RosbridgeNet.RosbridgeClient.ProtocolV2;
using RosbridgeNet.RosbridgeClient.ProtocolV2.Generics;
using RosbridgeNet.RosbridgeClient.ProtocolV2.Generics.Interfaces;
using Newtonsoft.Json.Linq;

namespace KinectPoseRecognitionApp
{
    public enum FlightOperation { landing, takeOff, forward, backward, up, down, left, right, rotateLeft, rotateRight, none}
    public enum FlightOperationMode { idle, navigate, landingOrTakingOff}
    class FlightController
    {
        public event EventHandler<FlightCameraVideoFrameReceivedArgs> CameraFrameReceived;
        string _connectionUri;
        private bool _isConnected = false;
        public bool isConnected { get { return _isConnected; } }
        private FlightOperationMode _mode = FlightOperationMode.idle;
        public FlightOperationMode mode {
            get
            {
                return _mode;
            }
        }
        private CancellationTokenSource _cts;
        private IRosbridgeMessageDispatcher _md;
        private bool _isLanded = true;
        public FlightController(string conn)
        {
            if(conn != null && conn.Length != 0)
            {
                _connectionUri = conn;
            }
        }

        public void Connect()
        {
            if (_connectionUri != null && _connectionUri.Length != 0 && !_isConnected)
            {
                _cts = new CancellationTokenSource();
                _md = Connect(new Uri(_connectionUri), _cts);
                _isConnected = true;
                Subscribe(_md);
            }
        }

        public void Disconnect()
        {
            if (_isConnected)
            {
                _cts.Cancel();
                _isConnected = false;
            }
        }

        public void SwitchMode(FlightOperationMode mode)
        {
            if (_isConnected)
            {
                _mode = mode;
            }
        }

        public async void TranslateGestureToFlightOperation(GestureRecongizedArgs args)
        {
            if (_mode == FlightOperationMode.landingOrTakingOff)
            {
                return;
            }

            if(_mode == FlightOperationMode.idle)
            {
                if (args.rightHandGesture.upwardDownwardGestureArgs == UpwardDownwardGesture.Down && !_isLanded)
                {
                    _mode = FlightOperationMode.landingOrTakingOff;
                    await landing();
                    _isLanded = true;
                    _mode = FlightOperationMode.idle;
                }
                else if (args.rightHandGesture.upwardDownwardGestureArgs == UpwardDownwardGesture.Up && _isLanded)
                {
                    _mode = FlightOperationMode.landingOrTakingOff;
                    await takeOff();
                    _isLanded = false;
                    _mode = FlightOperationMode.idle;
                }
            }


            if (_mode == FlightOperationMode.navigate)
            {
                Twist twist = new Twist
                {
                    angular = new Vector
                    {
                        x = 0,
                        y = 0,
                        z = 0
                    },
                    linear = new Vector
                    {
                        x = 0,
                        y = 0,
                        z = 0
                    }
                };

                switch (args.leftHandGesture.forwardBackwardGestureArgs)
                {
                    case ForwardBackwardGesture.Backward:
                        twist.linear.x = -1;
                        break;
                    case ForwardBackwardGesture.Forward:
                        twist.linear.x = -1;
                        break;
                    case ForwardBackwardGesture.None:
                        twist.linear.x = 0;
                        break;
                }
                switch (args.leftHandGesture.upwardDownwardGestureArgs)
                {
                    case UpwardDownwardGesture.Up:
                        twist.linear.z = 1;
                        break;
                    case UpwardDownwardGesture.Down:
                        twist.linear.z = -1;
                        break;
                    case UpwardDownwardGesture.None:
                        twist.linear.z = 0;
                        break;
                }

                switch (args.leftHandGesture.leftRightGestureArgs)
                {
                    case LeftRightGesture.Left:
                        twist.linear.y = 1;
                        break;
                    case LeftRightGesture.Right:
                        twist.linear.y = -1;
                        break;
                    case LeftRightGesture.None:
                        twist.linear.y = 0;
                        break;
                }

                switch (args.rightHandGesture.leftRightGestureArgs)
                {
                    case LeftRightGesture.Left:
                        twist.angular.z = 1;
                        break;
                    case LeftRightGesture.Right:
                        twist.angular.z = -1;
                        break;
                    case LeftRightGesture.None:
                        twist.linear.y = 0;
                        break;
                }

                await navigate(twist);
            }
        }

        private async Task takeOff() 
        {
            if (_isConnected)
            {
                RosPublisher publisher = new RosPublisher(_md, "/bebop/takeoff", "std_msgs/Empty");
                await publisher.AdvertiseAsync();
                var msg = JObject.Parse("{}");
                await publisher.PublishAsync(msg);
                publisher = null;
            }
        }

        private async Task landing()
        {
            if (_isConnected)
            {
                RosPublisher publisher = new RosPublisher(_md, "/bebop/land", "std_msgs/Empty");
                await publisher.AdvertiseAsync();
                var msg = JObject.Parse("{}");
                await publisher.PublishAsync(msg);
                publisher = null;
            }
        }

        private async Task navigate(Twist twist)
        {

            if (_isConnected)
            {
                RosPublisher<Twist> publisher = new RosPublisher<Twist>(_md, "/bebop/cmd_vel");
                await publisher.AdvertiseAsync();
                await publisher.PublishAsync(twist);
                publisher = null;
            }
        }

        static void Subscribe(IRosbridgeMessageDispatcher messageDispatcher)
        {
            RosSubscriber subscriber = new RosSubscriber(messageDispatcher, "/bebop/image_raw", "sensor_msgs/Image");

            subscriber.RosMessageReceived += (s, e) => { Console.WriteLine(e.RosMessage); };

            subscriber.SubscribeAsync();
        }

        static IRosbridgeMessageDispatcher Connect(Uri webSocketAddress, CancellationTokenSource cancellationTokenSource)
        {
            ISocket socket = new Socket(new ClientWebSocket(), webSocketAddress, cancellationTokenSource);
            IRosbridgeMessageSerializer messageSerializer = new RosbridgeMessageSerializer();
            IRosbridgeMessageDispatcher messageDispatcher = new RosbridgeMessageDispatcher(socket, messageSerializer);

            messageDispatcher.StartAsync().Wait();

            return messageDispatcher;
        }
    }

    public class FlightCameraVideoFrameReceivedArgs
    {
        public Byte[] frame;
    }
}
