using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace Dim.Kinect.Excercise1
{
    /// <summary> Interaction logic for MainWindow.xaml </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor sensor;
        private byte[] colorPixels;
        private WriteableBitmap colorBitmap;
        private DepthImagePixel[] depthPixels;
        private bool isColorSelectedStream;

        public MainWindow()
        {
            InitializeComponent();

            this.selectedStream.SelectionChanged += this.SelectionChange;

            // Set the color stream as the default stream.
            this.selectedStream.SelectedItem = this.colorStreamOption;
            this.isColorSelectedStream = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.sensor = ConfigureKinectSensor();

            this.KinectImage.Source = colorBitmap;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            this.sensor.Stop();
        }

        private void SelectionChange(object sender, SelectionChangedEventArgs e)
        {
            this.isColorSelectedStream = e.AddedItems.Contains(this.colorStreamOption);
            e.Handled = true;
        }

        private KinectSensor ConfigureKinectSensor()
        {
            KinectSensor sensor = GetKinectSensor();

            if (sensor == null)
            {
                this.ErrorText.Text = "Error: No Kinect Device detected.";
                this.ErrorText.Visibility = Visibility.Visible;

                this.KinectImage.Visibility = Visibility.Hidden;

                return sensor;
            }

            ConfigureColorStream(sensor);

            ConfigureDepthStream(sensor);

            return sensor;
        }

        private KinectSensor GetKinectSensor()
        {
            foreach (KinectSensor potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status != KinectStatus.Connected)
                {
                    continue;
                }

                try
                {
                    potentialSensor.Start();

                    return potentialSensor;
                }
                catch (IOException exception)
                {
                    Console.WriteLine(exception.ToString());
                }
            }

            return null;
        }

        private void ConfigureDepthStream(KinectSensor sensor)
        {
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            sensor.DepthFrameReady += this.SensorDepthFrameReady;

            this.depthPixels = new DepthImagePixel[sensor.DepthStream.FramePixelDataLength];
            this.colorPixels = new byte[sensor.DepthStream.FramePixelDataLength * sizeof(int)];
            this.colorBitmap = new WriteableBitmap(
                sensor.DepthStream.FrameWidth,
                sensor.DepthStream.FrameHeight,
                dpiX: 96.0,
                dpiY: 96.0,
                PixelFormats.Bgr32,
                palette: null);
        }

        private KinectSensor ConfigureColorStream(KinectSensor sensor)
        {
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            sensor.ColorFrameReady += this.SensorColorFrameReady;

            this.colorPixels = new byte[sensor.ColorStream.FramePixelDataLength];

            this.colorBitmap = new WriteableBitmap(
                sensor.ColorStream.FrameWidth,
                sensor.ColorStream.FrameHeight,
                dpiX: 96.0,
                dpiY: 96.0,
                PixelFormats.Bgr32,
                palette: null);

            return sensor;
        }

        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            if (!isColorSelectedStream)
            {
                return;
            }

            using (ColorImageFrame colorImageFrame = e.OpenColorImageFrame())
            {
                if (colorImageFrame == null)
                {
                    return;
                }

                colorImageFrame.CopyPixelDataTo(this.colorPixels);

                this.colorBitmap.WritePixels(
                    new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                    this.colorPixels,
                    stride: this.colorBitmap.PixelWidth * sizeof(int),
                    offset: 0);
            }
        }

        private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            if (isColorSelectedStream)
            {
                return;
            }

            int minDepth;
            int maxDepth;

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame == null)
                {
                    return;
                }

                depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);

                minDepth = depthFrame.MinDepth;
                maxDepth = depthFrame.MaxDepth;
            }

            int colorPixelIndex = 0;

            for (int i = 0; i < this.depthPixels.Length; i++)
            {
                short depth = depthPixels[i].Depth;

                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                this.colorPixels[colorPixelIndex++] = intensity;
                this.colorPixels[colorPixelIndex++] = intensity;
                this.colorPixels[colorPixelIndex++] = intensity;
                ++colorPixelIndex;
            }

            this.colorBitmap.WritePixels(
                new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                this.colorPixels,
                stride: this.colorBitmap.PixelWidth * sizeof(int),
                offset: 0);
        }
    }
}