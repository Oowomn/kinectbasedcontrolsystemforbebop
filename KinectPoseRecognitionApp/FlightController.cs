using System;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json.Linq;
using Rosbridge.Client;
using System.Windows.Threading;
using Newtonsoft.Json;
using System.Drawing;

namespace KinectPoseRecognitionApp
{
    public enum FlightOperation { landing, takeOff, forward, backward, up, down, left, right, rotateLeft, rotateRight, none}
    public enum FlightOperationMode { idle, navigate, landingOrTakingOff}
    class FlightController
    {
        public event EventHandler<FlightCameraVideoFrameReceivedArgs> CameraFrameReceived;
        public event EventHandler<FlightModeChangedArgs> FlightModeChanged;
        protected bool _isConnected = false;
        public bool isConnected { get { return _isConnected; } }
        protected FlightOperationMode _mode = FlightOperationMode.idle;
        public FlightOperationMode mode {
            get
            {
                return _mode;
            }
        }
        protected MessageDispatcher _md;
        private bool _isLanded = true;
        protected MainWindow _mw;
        protected Subscriber _subscriber;
        protected String _uavWSAddr;
        protected bool switchingMode = false;
        public FlightController(MainWindow mw, string uavWSAddr)
        {
            _mw = mw;
            _uavWSAddr = uavWSAddr;

        }

        public async virtual Task Connect()
        {
            if (isValidWSAddr(_uavWSAddr) && !_isConnected)
            {
                try
                {
                    _md = new MessageDispatcher(new Socket(new Uri(_uavWSAddr)), new MessageSerializerV2_0());
                    await _md.StartAsync();
                    _subscriber = new Subscriber("/bebop/image_raw", "sensor_msgs/Image", _md);
                    _subscriber.MessageReceived += _subscriber_MessageReceived;
                    await _subscriber.SubscribeAsync();
                    _isConnected = true;
                    _mw.log("Success to connect the remote ros server");
                }
                catch (Exception ex)
                {
                    _mw.log("Error!! Could not connect to the rosbridge server");
                    _md = null;
                    throw ex;
                }
            }
        }

        public virtual async Task Disconnect()
        {
            if (_isConnected)
            {
                await _md.StopAsync();
                _md = null;
                _isConnected = false;
            }
        }

        protected void SwitchMode(FlightOperationMode newMode)
        {
            if (switchingMode)
            {
                return;
            }

            switchingMode = true;
            _mode = newMode;
            Thread.Sleep(3000);
            switchingMode = false;
            FlightModeChangedArgs args = new FlightModeChangedArgs();
            args.from = _mode;
            args.to = newMode;
            FlightModeChanged.Invoke(this, args);
           
        }

        public async virtual void TranslateGestureToFlightOperation(GestureRecongizedArgs args)
        {
            if (!_isConnected)
                return;

            if (_mode == FlightOperationMode.landingOrTakingOff)
            {
                return;
            }

            if (_mode == FlightOperationMode.idle)
            {
                if (args.rightHandGesture.upwardDownwardGestureArgs == UpwardDownwardGesture.Down && !_isLanded)
                {
                    SwitchMode(FlightOperationMode.landingOrTakingOff);
                    await landing();
                    _isLanded = true;
                    SwitchMode(FlightOperationMode.idle);
                }
                else if (args.rightHandGesture.upwardDownwardGestureArgs == UpwardDownwardGesture.Up && _isLanded)
                {
                    SwitchMode(FlightOperationMode.landingOrTakingOff);
                    await takeOff();
                    _isLanded = false;
                    SwitchMode(FlightOperationMode.idle);
                }
            }

            if (args.rightHandGesture.forwardBackwardGestureArgs == ForwardBackwardGesture.Forward)
            {
                if (_mode == FlightOperationMode.idle)
                    SwitchMode(FlightOperationMode.navigate);
                else
                    SwitchMode(FlightOperationMode.idle);
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
                    case ForwardBackwardGesture.Forward:
                        twist.linear.x = 0.1f;
                        break;
                    case ForwardBackwardGesture.Backward:
                        twist.linear.x = -0.1f;
                        break;
                    case ForwardBackwardGesture.None:
                        twist.linear.x = 0;
                        break;
                }
                switch (args.leftHandGesture.upwardDownwardGestureArgs)
                {
                    case UpwardDownwardGesture.Up:
                        twist.linear.z = 0.1f;
                        break;
                    case UpwardDownwardGesture.Down:
                        twist.linear.z = -0.1f;
                        break;
                    case UpwardDownwardGesture.None:
                        twist.linear.z = 0;
                        break;
                }

                switch (args.leftHandGesture.leftRightGestureArgs)
                {
                    case LeftRightGesture.Left:
                        twist.linear.y = 0.1f;
                        break;
                    case LeftRightGesture.Right:
                        twist.linear.y = -0.1f;
                        break;
                    case LeftRightGesture.None:
                        twist.linear.y = 0;
                        break;
                }

                switch (args.rightHandGesture.leftRightGestureArgs)
                {
                    case LeftRightGesture.Left:
                        twist.angular.z = 0.1f;
                        break;
                    case LeftRightGesture.Right:
                        twist.angular.z = -0.1f;
                        break;
                    case LeftRightGesture.None:
                        twist.linear.y = 0;
                        break;
                }

                await navigate(twist);
            }
        }

        protected async Task takeOff() 
        {
            if (_isConnected)
            {
                Publisher publisher = new Publisher("/bebop/takeoff", "std_msgs/String", _md);
                await publisher.AdvertiseAsync();
                var msg = JObject.Parse("{}");
                await publisher.PublishAsync(msg);
                await publisher.UnadvertiseAsync();
                publisher = null;
            }
        }

        protected async Task landing()
        {
            if (_isConnected)
            {
                Publisher publisher = new Publisher("/bebop/land", "std_msgs/Empty",_md);
                await publisher.AdvertiseAsync();
                var msg = JObject.Parse("{}");
                await publisher.PublishAsync(msg);
                await publisher.UnadvertiseAsync();
                publisher = null;
            }
        }

        protected async Task navigate(Twist twist)
        {

            if (_isConnected)
            {
                Publisher publisher = new Publisher("/bebop/cmd_vel", "geometry_msgs/Twist",_md);
                await publisher.AdvertiseAsync();
                await publisher.PublishAsync(twist);
                await publisher.UnadvertiseAsync();
                publisher = null;
            }
        }

        private void _subscriber_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            _mw.log(String.Format("[Message From Flight] image:{0} x {1}", e.Message["width"].ToString(), e.Message["height"].ToString()));
            FlightCameraVideoFrameReceivedArgs args = new FlightCameraVideoFrameReceivedArgs();
            args.data = e.Message["data"] != null ? e.Message["data"].ToObject<byte[]>() : null;
            CameraFrameReceived.Invoke(this, args);

        }

        public async Task ClearUp()
        {
            if (null != _subscriber)
            {
                _subscriber.MessageReceived -= _subscriber_MessageReceived;
                await _subscriber.UnsubscribeAsync();
                _subscriber = null;
            }
        }

        protected bool isValidWSAddr(string addr)
        {
            return addr != null && addr.Length != 0 && addr.StartsWith("ws://");
        }
    }

    public class FlightCameraVideoFrameReceivedArgs
    {
        public byte[] data;
    }

    public class FlightModeChangedArgs
    {
        public FlightOperationMode from;
        public FlightOperationMode to;
    }
}
