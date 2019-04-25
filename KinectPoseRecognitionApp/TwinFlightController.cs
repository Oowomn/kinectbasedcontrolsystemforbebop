using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rosbridge.Client;

namespace KinectPoseRecognitionApp
{
    class TwinFlightController : FlightController
    {
        protected String _uav2WSAddr;
        protected MessageDispatcher _md2;
        protected Subscriber _subscriber2;
        public event EventHandler<FlightCameraVideoFrameReceivedArgs> Camera1FrameReceived;
        public event EventHandler<FlightCameraVideoFrameReceivedArgs> Camera2FrameReceived;

        private bool _isUAV1Landed = true;
        private bool _isUAV2Landed = true;
        public TwinFlightController(MainWindow mw, string addr1, string addr2) : base(mw,addr1)
        {
            _uav2WSAddr = addr2;
        }

        override public async Task Connect()
        {
            if (isValidWSAddr(_uavWSAddr) && isValidWSAddr(_uav2WSAddr) && !_isConnected)
            {
                try
                {
                    _md = new MessageDispatcher(new Socket(new Uri(_uavWSAddr)), new MessageSerializerV2_0());
                    await _md.StartAsync();
                    _subscriber = new Subscriber("/bebop/image_raw", "sensor_msgs/Image", _md);
                    _subscriber.MessageReceived += _subscriber_MessageReceived;
                    await _subscriber.SubscribeAsync();

                    _md2 = new MessageDispatcher(new Socket(new Uri(_uavWSAddr)), new MessageSerializerV2_0());
                    await _md2.StartAsync();
                    _subscriber2 = new Subscriber("/bebop/image_raw", "sensor_msgs/Image", _md);
                    _subscriber2.MessageReceived += _subscriber2_MessageReceived;
                    await _subscriber2.SubscribeAsync();

                    _isConnected = true;
                    _mw.log("Success to connect the remote ros server");
                }
                catch (Exception ex)
                {
                    _mw.log("Error!! Could not connect to the rosbridge server");
                    _md = null;
                    _md2 = null;
                    _subscriber = null;
                    _subscriber2 = null;
                    throw ex;
                }
            }
        }

        override public async Task Disconnect()
        {
            if (_isConnected)
            {
                await _md.StopAsync();
                _md = null;
                await _md2.StopAsync();
                _md2 = null;
                _isConnected = false;
            }
        }

        private void _subscriber_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            _mw.log(String.Format("[Message From Flight1] image:{0} x {1}", e.Message["width"].ToString(), e.Message["height"].ToString()));
            FlightCameraVideoFrameReceivedArgs args = new FlightCameraVideoFrameReceivedArgs();
            args.data = e.Message["data"] != null ? e.Message["data"].ToObject<byte[]>() : null;
            Camera1FrameReceived.Invoke(this, args);

        }

        private void _subscriber2_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            _mw.log(String.Format("[Message From Flight2] image:{0} x {1}", e.Message["width"].ToString(), e.Message["height"].ToString()));
            FlightCameraVideoFrameReceivedArgs args = new FlightCameraVideoFrameReceivedArgs();
            args.data = e.Message["data"] != null ? e.Message["data"].ToObject<byte[]>() : null;
            Camera2FrameReceived.Invoke(this, args);
        }

        public override async void TranslateGestureToFlightOperation(GestureRecongizedArgs args)
        {
            if (!_isConnected)
                return;

            if (_mode == FlightOperationMode.landingOrTakingOff)
            {
                return;
            }

            if (_mode == FlightOperationMode.idle)
            {
                if (args.rightHandGesture.upwardDownwardGestureArgs == UpwardDownwardGesture.Down && !_isUAV2Landed)
                {
                    SwitchMode(FlightOperationMode.landingOrTakingOff);
                    await landing();
                    _isUAV2Landed = true;
                    SwitchMode(FlightOperationMode.idle);
                }
                else if (args.rightHandGesture.upwardDownwardGestureArgs == UpwardDownwardGesture.Up && _isUAV2Landed)
                {
                    SwitchMode(FlightOperationMode.landingOrTakingOff);
                    await takeOff();
                    _isUAV2Landed = false;
                    SwitchMode(FlightOperationMode.idle);
                }

                if (args.leftHandGesture.upwardDownwardGestureArgs == UpwardDownwardGesture.Down && !_isUAV1Landed)
                {
                    SwitchMode(FlightOperationMode.landingOrTakingOff);
                    await landing();
                    _isUAV1Landed = true;
                    SwitchMode(FlightOperationMode.idle);
                }
                else if (args.leftHandGesture.upwardDownwardGestureArgs == UpwardDownwardGesture.Up && _isUAV1Landed)
                {
                    SwitchMode(FlightOperationMode.landingOrTakingOff);
                    await takeOff();
                    _isUAV1Landed = false;
                    SwitchMode(FlightOperationMode.idle);
                }
            }

            if (args.rightHandGesture.leftRightGestureArgs == LeftRightGesture.Right)
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
    }
}
