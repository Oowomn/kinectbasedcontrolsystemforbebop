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
        public event EventHandler<FlightCameraVideoFrameReceivedArgs> Camera1FrameReceived;
        public event EventHandler<FlightCameraVideoFrameReceivedArgs> Camera2FrameReceived;

        protected Flight _uav2;
        public TwinFlightController(MainWindow mw) : base(mw)
        {

        }

        public void setUAV2WSAddress(String addr)
        {
            _uav2WSAddr = addr;
        }

        override public async Task Connect()
        {
            if (isValidWSAddr(_uavWSAddr) && isValidWSAddr(_uav2WSAddr) && _uav == null && _uav2 == null )
            {

                

                try
                {
                    _uav = new Flight(_uavWSAddr);
                    await _uav.Connect();
                    _uav.cameraFrameSubscriber.MessageReceived += _subscriber_MessageReceived;
                    _mw.log("Success to connect the UAV1");
                }
                catch (Exception ex)
                {
                    _mw.log("Error!! Could not connect to the UAV1");
                    _uav = null;
                    throw ex;
                }


                try
                {
                    _uav2 = new Flight(_uav2WSAddr);
                    await _uav2.Connect();
                    _uav2.cameraFrameSubscriber.MessageReceived += _subscriber2_MessageReceived;
                    _mw.log("Success to connect the UAV2");
                }
                catch(Exception ex)
                {
                    _mw.log("Error!! Could not connect to the UAV1");
                    _uav2 = null;
                    throw ex;
                }
            }
        }

        override public async Task Disconnect()
        {
            await base.Disconnect();
            if (_uav2 != null && _uav2.isConnected)
            {
                await _uav2.Disconnect();
                _uav2 = null;
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
            if (_uav != null && _uav.isConnected)
            {
                TranslateGestureForUAV1(args);
            }

            if (_uav2 != null && _uav2.isConnected)
            {
                TranslateGestureForUAV2(args);
            }



            //if (_mode == FlightOperationMode.navigate)
            //{
            //    Twist twist = new Twist
            //    {
            //        angular = new Vector
            //        {
            //            x = 0,
            //            y = 0,
            //            z = 0
            //        },
            //        linear = new Vector
            //        {
            //            x = 0,
            //            y = 0,
            //            z = 0
            //        }
            //    };

            //    switch (args.leftHandGesture.forwardBackwardGestureArgs)
            //    {
            //        case ForwardBackwardGesture.Backward:
            //            twist.linear.x = -1;
            //            break;
            //        case ForwardBackwardGesture.Forward:
            //            twist.linear.x = -1;
            //            break;
            //        case ForwardBackwardGesture.None:
            //            twist.linear.x = 0;
            //            break;
            //    }
            //    switch (args.leftHandGesture.upwardDownwardGestureArgs)
            //    {
            //        case UpwardDownwardGesture.Up:
            //            twist.linear.z = 1;
            //            break;
            //        case UpwardDownwardGesture.Down:
            //            twist.linear.z = -1;
            //            break;
            //        case UpwardDownwardGesture.None:
            //            twist.linear.z = 0;
            //            break;
            //    }

            //    switch (args.leftHandGesture.leftRightGestureArgs)
            //    {
            //        case LeftRightGesture.Left:
            //            twist.linear.y = 1;
            //            break;
            //        case LeftRightGesture.Right:
            //            twist.linear.y = -1;
            //            break;
            //        case LeftRightGesture.None:
            //            twist.linear.y = 0;
            //            break;
            //    }

            //    switch (args.rightHandGesture.leftRightGestureArgs)
            //    {
            //        case LeftRightGesture.Left:
            //            twist.angular.z = 1;
            //            break;
            //        case LeftRightGesture.Right:
            //            twist.angular.z = -1;
            //            break;
            //        case LeftRightGesture.None:
            //            twist.linear.y = 0;
            //            break;
            //    }

            //    await navigate(twist);
            //}

        }

        private async void TranslateGestureForUAV1(GestureRecongizedArgs args)
        {
            FlightOperationMode _mode = _uav.mode;

            if (_mode == FlightOperationMode.landingOrTakingOff)
            {
                return;
            }

            // Switch Mode
            if (args.rightHandGesture.leftRightGestureArgs == LeftRightGesture.Right)
            {
                if (_mode == FlightOperationMode.idle)
                    await SwitchMode(FlightOperationMode.navigate, _uav);
                else
                    await SwitchMode(FlightOperationMode.idle, _uav);
            }

            // Control landing and takeoff
            if (_mode == FlightOperationMode.idle)
            {
                if (args.rightHandGesture.upwardDownwardGestureArgs == UpwardDownwardGesture.Down && !_uav.isLanded)
                {
                    await SwitchMode(FlightOperationMode.landingOrTakingOff, _uav);
                    await _uav.landing();
                    await SwitchMode(FlightOperationMode.idle, _uav);
                }
                else if (args.rightHandGesture.upwardDownwardGestureArgs == UpwardDownwardGesture.Up && _uav.isLanded)
                {
                    await SwitchMode(FlightOperationMode.landingOrTakingOff, _uav);
                    await _uav.takeOff();
                    await SwitchMode(FlightOperationMode.idle, _uav);
                }
            }
        }
        private async void TranslateGestureForUAV2(GestureRecongizedArgs args)
        {
            FlightOperationMode _mode = _uav2.mode;

            if (_uav2.mode == FlightOperationMode.landingOrTakingOff)
            {
                return;
            }

            if (args.leftHandGesture.leftRightGestureArgs == LeftRightGesture.Left)
            {
                if (_mode == FlightOperationMode.idle)
                    await SwitchMode(FlightOperationMode.navigate, _uav, 3000);
                else
                    await SwitchMode(FlightOperationMode.idle, _uav, 3000);
            }

            if (_mode == FlightOperationMode.idle)
            {
                if (args.leftHandGesture.upwardDownwardGestureArgs == UpwardDownwardGesture.Down && !_uav2.isLanded)
                {
                    await SwitchMode(FlightOperationMode.landingOrTakingOff,_uav2);
                    await _uav2.landing();
                    await SwitchMode(FlightOperationMode.idle, _uav2);
                }
                else if (args.leftHandGesture.upwardDownwardGestureArgs == UpwardDownwardGesture.Up && _uav2.isLanded)
                {
                    await SwitchMode(FlightOperationMode.landingOrTakingOff, _uav2);
                    await _uav2.takeOff();
                    await SwitchMode(FlightOperationMode.idle, _uav2);
                }
            }

        }
    }
}
