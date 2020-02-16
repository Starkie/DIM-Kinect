using System;
using System.IO;
using System.Windows;
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
        private WriteableBitmap ColorBitmap;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.sensor = ConfigureKinectSensor();

            this.KinectImage.Source = ColorBitmap;
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

            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            sensor.ColorFrameReady += this.SensorColorFrameReady;

            this.colorPixels = new byte[sensor.ColorStream.FramePixelDataLength];

            this.ColorBitmap = new WriteableBitmap(
                sensor.ColorStream.FrameWidth,
                sensor.ColorStream.FrameHeight,
                dpiX: 96.0,
                dpiY: 96.0,
                PixelFormats.Bgr32,
                palette: null);

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

        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorImageFrame = e.OpenColorImageFrame())
            {
                if (colorImageFrame == null)
                {
                    return;
                }

                colorImageFrame.CopyPixelDataTo(this.colorPixels);

                this.ColorBitmap.WritePixels(
                    new Int32Rect(0, 0, this.ColorBitmap.PixelWidth, this.ColorBitmap.PixelHeight),
                    this.colorPixels,
                    stride: this.ColorBitmap.PixelWidth * sizeof(int),
                    offset: 0);
            }
        }
    }
}