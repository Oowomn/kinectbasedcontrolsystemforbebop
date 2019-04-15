using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace KinectPoseRecognitionApp
{
    public enum UpwardDownwardGesture { Up, Down, None };
    public enum ForwardBackwardGesture { Forward, Backward, None };
    public enum LeftRightGesture { Left, Right, None };

    class GestureDetector
    {
        public event EventHandler<GestureRecongizedArgs> GestureRecongized;
        private double _leftThreshold;
        private double _rightThreshold;
        private double _upThreshold;
        private double _downThreshold;
        private double _forwardThreshold;
        private double _backwardThreshold;

        public GestureDetector(double leftThreshold, double rightThreshold, double upThreshold, double downThreshold, double forwardThreshold, double backwardThreshold)
        {
            _leftThreshold = leftThreshold;
            _rightThreshold = rightThreshold;
            _upThreshold = upThreshold;
            _downThreshold = downThreshold;
            _forwardThreshold = forwardThreshold;
            _backwardThreshold = backwardThreshold;
        }

        public void detect(Body body)
        {
            GestureRecongizedArgs args = new GestureRecongizedArgs();
            args.leftHandGesture = new Gesture
            {
                forwardBackwardGestureArgs = compareForwardBackward(body.Joints[JointType.ShoulderLeft], body.Joints[JointType.ElbowLeft]),
                upwardDownwardGestureArgs = compareUpDown(body.Joints[JointType.ElbowLeft], body.Joints[JointType.WristLeft]),
                leftRightGestureArgs = compareLeftRight(body.Joints[JointType.ElbowLeft], body.Joints[JointType.WristLeft])
            };

            args.rightHandGesture = new Gesture
            {
                forwardBackwardGestureArgs = compareForwardBackward(body.Joints[JointType.ShoulderRight], body.Joints[JointType.ElbowRight]),
                upwardDownwardGestureArgs = compareUpDown(body.Joints[JointType.ElbowRight], body.Joints[JointType.WristRight]),
                leftRightGestureArgs = compareLeftRight(body.Joints[JointType.ElbowRight], body.Joints[JointType.WristRight])
            };

            GestureRecongized.Invoke(this, args);

        }

        private UpwardDownwardGesture compareUpDown(Joint fix, Joint outer)
        {
            if (fix.Position.Y - outer.Position.Y > _downThreshold)
            {
                return UpwardDownwardGesture.Down;
            }
            else if (outer.Position.Y - fix.Position.Y > _upThreshold)
            {
                return UpwardDownwardGesture.Up;
            }
            else
            {
                return UpwardDownwardGesture.None;
            }

        }

        private LeftRightGesture compareLeftRight(Joint first, Joint second)
        {
            if (first.Position.X - second.Position.X > _leftThreshold)
            {
                return LeftRightGesture.Left;
            }
            else if (second.Position.X - first.Position.X > _rightThreshold)
            {
                return LeftRightGesture.Right;
            }
            else
            {
                return LeftRightGesture.None;
            }

        }

        private ForwardBackwardGesture compareForwardBackward(Joint fix, Joint move)
        {
            if (fix.Position.Z - move.Position.Z > _forwardThreshold)
            {
                return ForwardBackwardGesture.Forward;
            }
            else if (move.Position.Z - fix.Position.Z > _backwardThreshold)
            {
                return ForwardBackwardGesture.Backward;
            }
            else
            {
                return ForwardBackwardGesture.None;
            }

        }
    }

    public class GestureRecongizedArgs
    {
        public static GestureRecongizedArgs DefaultArgs
        {
            get
            {
                return new GestureRecongizedArgs
                {
                    leftHandGesture = new Gesture
                    {
                        upwardDownwardGestureArgs = UpwardDownwardGesture.None,
                        forwardBackwardGestureArgs = ForwardBackwardGesture.None,
                        leftRightGestureArgs = LeftRightGesture.None
                    },
                    rightHandGesture = new Gesture
                    {
                        upwardDownwardGestureArgs = UpwardDownwardGesture.None,
                        forwardBackwardGestureArgs = ForwardBackwardGesture.None,
                        leftRightGestureArgs = LeftRightGesture.None
                    }
                };
            }
        }
        public Gesture leftHandGesture { get; set; }
        public Gesture rightHandGesture { get; set; }
        public bool isNoAction
        {
            get
            {
                return leftHandGesture.isAllNone && rightHandGesture.isAllNone;
            }
        }
    }

    public class Gesture
    {
        public UpwardDownwardGesture upwardDownwardGestureArgs { get; set; }
        public ForwardBackwardGesture forwardBackwardGestureArgs { get; set; }
        public LeftRightGesture leftRightGestureArgs { get; set; }

        public bool isAllNone {
            get {
                return upwardDownwardGestureArgs == UpwardDownwardGesture.None && forwardBackwardGestureArgs == ForwardBackwardGesture.None && leftRightGestureArgs == LeftRightGesture.None;
            }
        }
    }
}
