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

        JointType _start = JointType.ShoulderRight;
        JointType _center = JointType.ElbowRight;
        JointType _end = JointType.WristRight;

        public MainWindow()
        {

            this.Loaded += MainWindow_Loaded;
            this.Unloaded += MainWindow_Unloaded;
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
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _sensor = KinectSensor.GetDefault();
            if(_sensor != null)
            {
                Debug.WriteLine("Kinect start");
                _sensor.Open();
                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            }
            else
            {
                Debug.WriteLine("There is no kinect connected");
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
                    if (viewer.Visualization == Visualization.Color)
                    {
                        viewer.Image = frame.ToBitmap();
                    }
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
                        viewer.Clear();
                        viewer.DrawBody(body);
                        GestureLabel.Content = "Body Found";
                        angle.Update(body.Joints[_start], body.Joints[_center], body.Joints[_end]);
                        Angle.Content =  ((int)(body.Joints[_center].Angle(body.Joints[_start], body.Joints[_end], Axis.X))).ToString();
                        //Debug.WriteLine("Body found");
                    }
                    else
                    {
                        viewer.Clear();
                        GestureLabel.Content = "Body Not Found";
                        //Debug.WriteLine("Body not found");
                    }
                }

            }

        }
    }
}
