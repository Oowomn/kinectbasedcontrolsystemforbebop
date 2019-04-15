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
        public bool isDrawBody {
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

            if(_gestureDetector != null)
            {
                _gestureDetector.GestureRecongized -= _gestureDetector_GestureRecongized;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            log("System Initiating");
            _sensor = KinectSensor.GetDefault();
            _sensor.IsAvailableChanged += _sensor_IsAvailableChanged;
            _gestureDetector = new GestureDetector(0.15,0.15,0.1,0.15, 0.15, 0.1);
            _gestureDetector.GestureRecongized += _gestureDetector_GestureRecongized;

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
            //if (e.isNoAction)
            //    return;

            lhForwardBackwardGestureStatus.Text = e.leftHandGesture.forwardBackwardGestureArgs.ToString();
            lhLeftRightGestureStatus.Text = e.leftHandGesture.leftRightGestureArgs.ToString();
            lhUpDownGestureStatus.Text = e.leftHandGesture.upwardDownwardGestureArgs.ToString();

            rhForwardBackwardGestureStatus.Text = e.rightHandGesture.forwardBackwardGestureArgs.ToString();
            rhLeftRightGestureStatus.Text = e.rightHandGesture.leftRightGestureArgs.ToString();
            rhUpDownGestureStatus.Text = e.rightHandGesture.upwardDownwardGestureArgs.ToString();

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
    }
}
