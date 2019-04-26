using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Rosbridge.Client;

namespace KinectPoseRecognitionApp
{
    class Flight
    {


        private MessageDispatcher md;
        public Subscriber cameraFrameSubscriber;
        public FlightOperationMode mode;
        public bool isLanded = true;
        public bool isSwitchingMode = false;
        private String _addr;
        public bool isConnected
        {
            get
            {
                return md.CurrentState == MessageDispatcher.State.Started;
            }
        }

        public Flight(string addr)
        {
            _addr = addr;
        }

        public async Task Connect()
        {
            md = new MessageDispatcher(new Socket(new Uri(_addr)), new MessageSerializerV2_0());
            await md.StartAsync();
            cameraFrameSubscriber = new Subscriber("/bebop/image_raw", "sensor_msgs/Image", md);
            await cameraFrameSubscriber.SubscribeAsync();
        }

        public async Task takeOff()
        {
            if (md != null)
            {
                Publisher publisher = new Publisher("/bebop/takeoff", "std_msgs/String", md);
                await publisher.AdvertiseAsync();
                var msg = JObject.Parse("{}");
                await publisher.PublishAsync(msg);
                await publisher.UnadvertiseAsync();
                publisher = null;
            }
        }

        public async Task landing()
        {
            if (md != null)
            {
                Publisher publisher = new Publisher("/bebop/land", "std_msgs/Empty", md);
                await publisher.AdvertiseAsync();
                var msg = JObject.Parse("{}");
                await publisher.PublishAsync(msg);
                await publisher.UnadvertiseAsync();
                publisher = null;
            }
        }

        public async Task navigate(Twist twist)
        {

            if (md != null)
            {
                Publisher publisher = new Publisher("/bebop/cmd_vel", "geometry_msgs/Twist", md);
                await publisher.AdvertiseAsync();
                await publisher.PublishAsync(twist);
                await publisher.UnadvertiseAsync();
                publisher = null;
            }
        }

        public async Task Disconnect()
        {
            if (cameraFrameSubscriber != null)
            {
                await cameraFrameSubscriber.UnsubscribeAsync();
                cameraFrameSubscriber = null;
            }


            if (md != null)
            {
                await md.StopAsync();
                md = null;
            }
        }
    }
}
