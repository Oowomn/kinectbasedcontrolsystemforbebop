using System;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json.Linq;
using Rosbridge.Client;
using System.Windows.Threading;
using Newtonsoft.Json;
using System.Drawing;
using System.Linq;

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
                    //_uav.cameraFrameSubscriber.MessageReceived += _subscriber_MessageReceived;
                    await SwitchMode(FlightOperationMode.idle, _uav);
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
                _uav = null;
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
                    _mw.log("islanding");
                    await SwitchMode(FlightOperationMode.landingOrTakingOff,_uav);
                    try
                    {
                        await _uav.landing();
                    }catch(Exception ex)
                    {

                    }
                    _uav.isLanded = true;
                    await SwitchMode(FlightOperationMode.idle, _uav);
                }
                else if (args.rightHandGesture.upwardDownwardGestureArgs == UpwardDownwardGesture.Up && _uav.isLanded)
                {
                    _mw.log("is takeoff");
                    await SwitchMode(FlightOperationMode.landingOrTakingOff, _uav);
                    try
                    {
                        await _uav.takeOff();
                    }catch(Exception ex)
                    {

                    }
                    _uav.isLanded = false;
                    await SwitchMode(FlightOperationMode.idle, _uav);
                }
            }

            if (args.rightHandGesture.forwardBackwardGestureArgs == ForwardBackwardGesture.Forward && !_uav.isLanded)
            {
                if (_uav.mode == FlightOperationMode.idle)
                {
                    await SwitchMode(FlightOperationMode.navigate, _uav,1000);
                }
                else
                {
                    await SwitchMode(FlightOperationMode.idle, _uav, 1000);
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
                        twist.linear.x = 0.2f;
                        break;
                    case ForwardBackwardGesture.Backward:
                        twist.linear.x = -0.2f;
                        break;
                    case ForwardBackwardGesture.None:
                        twist.linear.x = 0;
                        break;
                }
                switch (args.leftHandGesture.upwardDownwardGestureArgs)
                {
                    case UpwardDownwardGesture.Up:
                        twist.linear.z = 0.2f;
                        break;
                    case UpwardDownwardGesture.Down:
                        twist.linear.z = -0.2f;
                        break;
                    case UpwardDownwardGesture.None:
                        twist.linear.z = 0;
                        break;
                }

                switch (args.leftHandGesture.leftRightGestureArgs)
                {
                    case LeftRightGesture.Left:
                        twist.linear.y = 0.2f;
                        break;
                    case LeftRightGesture.Right:
                        twist.linear.y = -0.2f;
                        break;
                    case LeftRightGesture.None:
                        twist.linear.y = 0;
                        break;
                }

                switch (args.rightHandGesture.leftRightGestureArgs)
                {
                    case LeftRightGesture.Left:
                        twist.angular.z = 0.3f;
                        break;
                    case LeftRightGesture.Right:
                        twist.angular.z = -0.3f;
                        break;
                    case LeftRightGesture.None:
                        twist.linear.y = 0;
                        break;
                }

                try
                {
                    await _uav.navigate(twist);
                }catch(Exception ex)
                {

                }
            }
        }

        private void _subscriber_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            //_mw.log(String.Format("[Message From Flight] {0}", e.Message.ToString()));
            FlightCameraVideoFrameReceivedArgs args = new FlightCameraVideoFrameReceivedArgs();
            args.dataStr = e.Message["data"].ToString();
            args.data = Convert.FromBase64String(e.Message["data"].ToString());//e.Message["data"] != null ? StringToByte(e.Message["data"].to) : null;
            CameraFrameReceived.Invoke(this, args);
        }

        protected bool isValidWSAddr(string addr)
        {
            return addr != null && addr.Length != 0 && addr.StartsWith("ws://");
        }

        //public static byte[] StringToByte(string hex)
        //{
        //    int num = hex.Length;
        //    byte[] bytes = new byte[num / 2];
        //    for(int i=0;i<num; i += 2)
        //    {
        //        bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
           
        //    }
        //    return bytes;
        //}

        //public static byte[] StringToByteArray(string hex)
        //{
        //    return Enumerable.Range(0, hex.Length)
        //                     .Where(x => x % 2 == 0)
        //                     .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
        //                     .ToArray();
        //}

        public async void emergency()
        {
            if (_uav != null)
            {
                await _uav.landing();
                _uav.isLanded = true;
                await SwitchMode(FlightOperationMode.idle, _uav);
            }
        }
    }


    public class FlightCameraVideoFrameReceivedArgs
    {
        public byte[] data;
        public string dataStr;
    }

    public class FlightModeChangedArgs
    {
        public FlightOperationMode from;
        public FlightOperationMode to;
    }
}
