﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.Util;

namespace RealtimeCamera
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            textBlock.Text = String.Empty;
            Window.Current.VisibilityChanged += (sender, args) =>
            {
                CvInvoke.WinrtOnVisibilityChanged(args.Visible);
            };
            //render the captured image in the bottom image view
            CvInvoke.WinrtSetFrameContainer(this.image2);

            CvInvoke.WinrtStartMessageLoop(Process);

        }

        private Mat _cameraMatrix;
        private Mat _distCoeffs;
        private Matrix<float> mapx, mapy;

        private VideoCapture _capture;
        public void Process()
        {
            Mat m = new Mat();
            Mat mProcessed = new Mat();
            while (true)
            {

                if (_captureEnabled)
                {
                    try
                    {
                        if (_capture == null)
                            _capture = new VideoCapture();

                        //Read the camera data to the mat
                        //Must use VideoCapture.Read function for UWP to read image from capture.
                        //Note that m is in 3 channel RGB color space, 
                        //our default color space is BGR for 3 channel Mat
                        _capture.Read(m);

                        CvInvoke.WinrtImshow();
                        if (!m.IsEmpty)
                        {
                            if (_cameraMatrix == null || _distCoeffs == null)
                            {
                                //Create a dummy camera calibration matrix for testing
                                //Use your own if you have calibrated your camera
                                _cameraMatrix = new Mat(new System.Drawing.Size(3, 3), DepthType.Cv32F, 1);
                                   
                                int centerY = m.Width >> 1;
                                int centerX = m.Height >> 1;
                                //CvInvoke.SetIdentity(_cameraMatrix, new MCvScalar(1.0));
                                _cameraMatrix.SetTo(new double[]
                                {
                                    1, 0, centerY,
                                    0, 1, centerX,
                                    0, 0, 1
                                });

                                _distCoeffs = new Mat(new System.Drawing.Size(5, 1), DepthType.Cv32F, 1);
                                _distCoeffs.SetTo(new double[] { -0.000003, 0, 0, 0, 0 });
                                mapx = new Matrix<float>(m.Height, m.Width);
                                mapy = new Matrix<float>(m.Height, m.Width);
                                CvInvoke.InitUndistortRectifyMap(
                                    _cameraMatrix, 
                                    _distCoeffs, 
                                    null, 
                                    _cameraMatrix, 
                                    m.Size,
                                    DepthType.Cv32F, 
                                    mapx,
                                    mapy);
                                //p.IntrinsicMatrix.Data[0, 2] = centerY;
                                //p.IntrinsicMatrix.Data[1, 2] = centerX;
                                //p.IntrinsicMatrix.Data[2, 2] = 1;
                                //p.DistortionCoeffs.Data[0, 0] = -0.000003;
                                //p.InitUndistortMap(m.Width, m.Height, out mapx, out mapy);
                            }

              

                            m.CopyTo(mProcessed);
                            CvInvoke.Undistort(m, mProcessed, _cameraMatrix, _distCoeffs );

                            //mProcess is in the same color space as m, which is RGB, 
                            //needed to change to BGR
                            CvInvoke.CvtColor(mProcessed, mProcessed, ColorConversion.Rgb2Bgr);
                            
                            
                            //Can apply simple image processing to the captured image, let just invert the pixels
                            //CvInvoke.BitwiseNot(m, m);

                            //render the processed image on the top image view
                            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                async () =>
                                {
                                    var wb = mProcessed.ToWritableBitmap();
                                    image1.Source = wb;
                                });

                            //The data in the mat that is read from the camera will 
                            //be drawn to the Image control
                            CvInvoke.WinrtImshow();
                        }
                    }
                    catch (Exception e)
                    {
                        CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            async () =>
                            {
                                textBlock.Text = e.Message;
                            });
                    }
                }
                else
                {
                    if (_capture != null)
                    {
                        _capture.Dispose();
                        _capture = null;
                    }

                    Task t = Task.Delay(100);
                    t.Wait();
                }
            }
        }

        private bool _captureEnabled = false;

        private void captureButton_Click(object sender, RoutedEventArgs e)
        {
            _captureEnabled = !_captureEnabled;

            if (_captureEnabled)
            {
                captureButton.Content = "Stop";
            }
            else
            {
                captureButton.Content = "Start Capture";
            }

        }
    }
}
