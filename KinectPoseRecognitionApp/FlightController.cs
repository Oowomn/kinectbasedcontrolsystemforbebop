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
    public enum FlightOperationMode { idle, navigate, landingOrTakingOff, notConnected}
    class FlightController
    {
        public event EventHandler<FlightCameraVideoFrameReceivedArgs> CameraFrameReceived;
        public event EventHandler<FlightModeChangedArgs> FlightModeChanged;
        public FlightOperationMode mode {
            get
            {
                if (_uav == null)
                {
                    return FlightOperationMode.notConnected;
                }
                return _uav.mode;
            }
        }

        protected MainWindow _mw;
        protected string _uavWSAddr;
        protected Flight _uav;
        public FlightController(MainWindow mw)
        {
            _mw = mw;
        }

        public void setUAVWSAddress(String addr)
        {
            _uavWSAddr = addr;
        }

        public async virtual Task Connect()
        {
            if (isValidWSAddr(_uavWSAddr) && _uav == null)
            {
                try
                {
                    _uav = new Flight(_uavWSAddr);
                    await _uav.Connect();
                    _mw.log("Success to connect the remote ros server");
                }
                catch (Exception ex)
                {
                    _mw.log("Error!! Could not connect to the rosbridge server");
                    _uav = null;
                    throw ex;
                }
            }
        }

        public virtual async Task Disconnect()
        {
            if (_uav != null && _uav.isLanded)
            {
                await _uav.Disconnect();
            }
        }

        protected async Task SwitchMode(FlightOperationMode newMode, Flight u, int delay = 0)
        {
            if (u.isSwitchingMode)
            {
                return;
            }

            if (u != null && u.isConnected)
            {
                u.isSwitchingMode = true;
                FlightModeChangedArgs args = new FlightModeChangedArgs();
                args.from = _uav.mode;
                args.to = newMode;
                _uav.mode = newMode;
                if(delay > 0)
                {
                    await Task.Delay(delay);
                }
                u.isSwitchingMode = false;
                FlightModeChanged.Invoke(this, args);
            }
        }

        public async virtual void TranslateGestureToFlightOperation(GestureRecongizedArgs args)
        {
            if (_uav == null || !_uav.isConnected)
                return;

            if (_uav.mode == FlightOperationMode.landingOrTakingOff) 
            {
                return;
            }


            if (_uav.mode == FlightOperationMode.idle)
            {
                if (args.rightHandGesture.upwardDownwardGestureArgs == UpwardDownwardGesture.Down && !_uav.isLanded)
                {
                    await SwitchMode(FlightOperationMode.landingOrTakingOff,_uav);
                    await _uav.landing();
                    _uav.isLanded = true;
                    await SwitchMode(FlightOperationMode.idle, _uav);
                }
                else if (args.rightHandGesture.upwardDownwardGestureArgs == UpwardDownwardGesture.Up && _uav.isLanded)
                {
                    await SwitchMode(FlightOperationMode.landingOrTakingOff, _uav);
                    await _uav.takeOff();
                    _uav.isLanded = false;
                    await SwitchMode(FlightOperationMode.idle, _uav);
                }
            }

            if (args.rightHandGesture.forwardBackwardGestureArgs == ForwardBackwardGesture.Forward)
            {
                if (_uav.mode == FlightOperationMode.idle)
                {
                    await SwitchMode(FlightOperationMode.navigate, _uav,3000);
                }
                else
                {
                    await SwitchMode(FlightOperationMode.idle, _uav, 3000);
                }
            }

            if (_uav.mode == FlightOperationMode.navigate && !_uav.isLanded)
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

                await _uav.navigate(twist);
            }
        }

        private void _subscriber_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            _mw.log(String.Format("[Message From Flight] image:{0} x {1}", e.Message["width"].ToString(), e.Message["height"].ToString()));
            FlightCameraVideoFrameReceivedArgs args = new FlightCameraVideoFrameReceivedArgs();
            args.data = e.Message["data"] != null ? e.Message["data"].ToObject<byte[]>() : null;
            CameraFrameReceived.Invoke(this, args);
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
