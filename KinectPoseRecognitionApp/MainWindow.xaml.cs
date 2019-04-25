using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using LightBuzz.Vitruvius;
using System.Diagnostics;

namespace KinectPoseRecognitionApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        private bool _isDrawBody = false;
        public bool isDrawBody
        {
            get
            {
                return _isDrawBody;
            }
            set
            {
                _isDrawBody = value;
                log("Kinect viewer: Changed to " + (_isDrawBody ? "Draw body" : "Not draw body"));
                kinectViewer.Clear();
            }
        }
        GestureDetector _gestureDetector;
        FlightController _flightController;

        public String FlightStatus { get
            {
                if (_flightController != null && _flightController.isConnected)
                {
                    return _flightController.mode.ToString();
                }
                else
                {
                    return "Not connected";
                
                }
            } }

        private double _leftThreshold = 0.15;
        public double leftThreshold
        {
            get { return _leftThreshold; }
            set
            {
                _leftThreshold = value; log("Left threshold updated to " + value); UpdateThreshold();
            }
        }
        private double _rightThreshold = 0.15;
        public double rightThreshold
        {
            get { return _rightThreshold; }
            set
            {
                _rightThreshold = value; log("Right threshold updated to " + value); UpdateThreshold();
            }
        }
        private double _upThreshold = 0.15;
        public double upThreshold
        {
            get { return _upThreshold; }
            set
            {
                _upThreshold = value; log("Up threshold updated to " + value); UpdateThreshold();
            }
        }
        private double _downThreshold = 0.1;
        public double downThreshold
        {
            get { return _downThreshold; }
            set
            {
                _downThreshold = value; log("Down threshold updated to " + value); UpdateThreshold();
            }
        }
        private double _forwardThreshold = 0.15;
        public double forwardThreshold
        {
            get { return _forwardThreshold; }
            set
            {
                _forwardThreshold = value; log("Forward threshold updated to " + value); UpdateThreshold();
            }
        }
        private double _backwardThreshold = 0.1;
        public double backwardThreshold
        {
            get { return _backwardThreshold; }
            set
            {
                _backwardThreshold = value; log("Backward threshold updated to " + value); UpdateThreshold();
            }
        }

        private string _uavMode = "Single";
        public string uavMode
        {
            get
            {
                return _uavMode;
            }
            set
            {
                ComboBoxItem i = (ComboBoxItem)uavModeCombo.SelectedItem;
                if (_uavMode == i.Content.ToString())
                {
                    return;
                }
                _uavMode = i.Content.ToString();
                tbUav2Addr.IsEnabled = _uavMode == "Two";
            }
        }

        private string _uav1Address;
        public string uav1Address
        {
            get
            {
                return _uav1Address;
            }
            set
            {
                _uav1Address = value;
            }
        }

        private string _uav2Address;
        public string uav2Address
        {
            get
            {
                return _uav2Address;
            }
            set
            {
                _uav2Address = value;
            }
        }

        public bool canConnect
        {
            get
            {
                return _flightController == null;
            }
        }

        public MainWindow()
        {
            this.Loaded += MainWindow_Loaded;
            this.Unloaded += MainWindow_Unloaded;
            this.DataContext = this;
            InitializeComponent();
        }

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_reader != null)
            {
                _reader.Dispose();
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }

            if (_gestureDetector != null)
            {
                _gestureDetector.GestureRecongized -= _gestureDetector_GestureRecongized;
            }
        }

        void UpdateThreshold()
        {
            if (_gestureDetector != null)
            {
                _gestureDetector.updateThresholds(leftThreshold, rightThreshold, upThreshold, downThreshold, forwardThreshold, backwardThreshold);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            log("System Initiating");
            _sensor = KinectSensor.GetDefault();
            _sensor.IsAvailableChanged += _sensor_IsAvailableChanged;
            _gestureDetector = new GestureDetector(0.15, 0.15, 0.1, 0.15, 0.15, 0.1);
            _gestureDetector.GestureRecongized += _gestureDetector_GestureRecongized;
            //try
            //{
            //    if (_flightController != null) _flightController.Connect();
            //}
            //catch (Exception err)
            //{
            //    log("Fail to connect the ros web socket");
            //    log("Error Message: " + err.Message);
            //}

            if (_sensor != null)
            {
                Debug.WriteLine("Kinect start");
                log("Kinect start");
                _sensor.Open();
                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            }
            else
            {
                Debug.WriteLine("There is no kinect connected");
                log("There is no kinect connected");
                kinectStatusText.Text = "Not Connected";
                //kinect_status.Text = "Not connected";
            }

        }

        private void _sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            kinectStatusText.Text = e.IsAvailable ? "Connected" : "Not connected";
            log("Kinect Availability: Changed to " + (e.IsAvailable ? "Connected" : "Not connected"));
        }

        private void _gestureDetector_GestureRecongized(object sender, GestureRecongizedArgs e)
        {

            lhForwardBackwardGestureStatus.Text = e.leftHandGesture.forwardBackwardGestureArgs.ToString();
            lhLeftRightGestureStatus.Text = e.leftHandGesture.leftRightGestureArgs.ToString();
            lhUpDownGestureStatus.Text = e.leftHandGesture.upwardDownwardGestureArgs.ToString();

            rhForwardBackwardGestureStatus.Text = e.rightHandGesture.forwardBackwardGestureArgs.ToString();
            rhLeftRightGestureStatus.Text = e.rightHandGesture.leftRightGestureArgs.ToString();
            rhUpDownGestureStatus.Text = e.rightHandGesture.upwardDownwardGestureArgs.ToString();

            if (_flightController != null && _flightController.isConnected)
            {
                _flightController.TranslateGestureToFlightOperation(e);
            }

        }

        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            // Get a reference to the multi-frame    
            var reference = e.FrameReference.AcquireFrame();

            // Open color frame    

            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    //if (camera_view.Visualization == Visualization.Color)
                    //{
                    kinectViewer.Image = frame.ToBitmap();
                    //}
                }
            }

            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {

                if (frame != null)
                {
                    //Debug.WriteLine("Proccessing Body frame");
                    Body body = frame.Bodies().Closest();

                    if (body != null)
                    {
                        if (_isDrawBody)
                        {
                            kinectViewer.Clear();
                            
                            kinectViewer.DrawBody(body);
                        }


                        if (_gestureDetector != null)
                        {
                            _gestureDetector.detect(body);
                        }



                        //Debug.WriteLine("Body found");
                    }
                    else
                    {


                        //Debug.WriteLine("Body not found");
                    }
                }

            }

        }
        public void log(string message)
        {
            MessageConsole.Text = MessageConsole.Text + Environment.NewLine + message;
            MessageConsoleSroll.ScrollToBottom();
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if(_flightController != null && _flightController.isConnected)
            {
                log("You should disconnect the previous connect before init a new connection");
                return;
            }
            else
            {
                if (_uavMode == "Single")
                {
                    if(_uav1Address.Length > 0 && _uav1Address.StartsWith("ws://"))
                    {
                        _flightController = new FlightController(this, _uav1Address);
                        try
                        {
                            btnConnect.IsEnabled = false;
                            flightStatus.Text = "Connecting...";
                            _flightController.FlightModeChanged += _flightController_FlightModeChanged;
                            await _flightController.Connect();
                            btnDisconnect.IsEnabled = true;
                        }
                        catch (Exception error)
                        {
                            log("Flight connection error");
                            log(error.Message);
                            if (_flightController != null)
                            {
                                flightStatus.Text = "Not Connected";
                                await _flightController.ClearUp();
                                _flightController.FlightModeChanged -= _flightController_FlightModeChanged;
                                _flightController = null;
                                btnConnect.IsEnabled = true;
                                btnDisconnect.IsEnabled = false;
                            }
                            
                        }
                    }
                    else
                    {
                        log("UAV1 Connection URL is not valid");
                    }
                }else if (_uavMode == "Two")
                {
                    if (_uav1Address.Length > 0 && _uav1Address.StartsWith("ws://") && _uav2Address.Length > 0 && _uav2Address.StartsWith("ws://"))
                    {
                        log("Not Support yet");
                    }
                    else
                    {
                        log("UAV1/UAV2 Connection URL is not valid");
                    }
                }
                else
                {
                    
                }
                
            }
        }

        private void _flightController_FlightModeChanged(object sender, FlightModeChangedArgs e)
        {
            flightStatus.Text = e.to.ToString();
        }

        private async void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (_flightController != null && _flightController.isConnected)
            {
                try
                {
                    await _flightController.Disconnect();
                    _flightController.FlightModeChanged -= _flightController_FlightModeChanged;
                    _flightController = null;
                    flightStatus.Text = "Not Connected";
                    btnConnect.IsEnabled = true;
                    btnDisconnect.IsEnabled = false;

                    log("[Flight Connection] Disconnected");
                }
                catch (Exception error)
                {
                    log("[Flight Connection]Flight disconnect fail. Force disconnected");
                    log(error.Message);
                    _flightController.FlightModeChanged -= _flightController_FlightModeChanged;
                    _flightController = null;
                    flightStatus.Text = "Not Connected";
                    btnConnect.IsEnabled = true;
                    btnDisconnect.IsEnabled = false;
                }
            }
        }
    }
}
