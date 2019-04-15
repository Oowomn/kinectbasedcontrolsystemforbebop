using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectPoseRecognitionApp
{
    public enum FlightOperation { landing, takeOff, forward, backward, up, down, left, right, rotateLeft, rotateRight, stop}
    public enum FlightOperationMode { idle, navigate, landingOrTakingOff}

    class FlightController
    {
        string _connectionUri;
        private bool _isConnected = false;
        private FlightOperationMode _mode = FlightOperationMode.idle;
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
                _isConnected = true;
            }
        }

        public void Disconnect()
        {
            if (_isConnected)
            {
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

        public void SendFlightOperation(FlightOperation operation)
        {
            if (_mode == FlightOperationMode.landingOrTakingOff)
            {
                return;
            }

            switch (operation)
            {
                case FlightOperation.takeOff:
                case FlightOperation.landing:
                    if(_mode != FlightOperationMode.idle)
                    {
                        return;
                    }
                    break;
                case FlightOperation.forward:
                case FlightOperation.backward:
                case FlightOperation.up:
                case FlightOperation.down:
                case FlightOperation.left:
                case FlightOperation.right:
                case FlightOperation.rotateLeft:
                case FlightOperation.rotateRight:
                    if(_mode != FlightOperationMode.navigate)
                    {
                        return;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
