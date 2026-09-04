using System;
using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Extensions;

public class FaceMatrixOpenCV : IDisposable
{
    private VideoCapture _capture;
    private CascadeClassifier _faceCascade;
    private bool _isStreamRunning;

    public void InitializeCamera(int cameraIndex = 0)
    {
        _capture = new VideoCapture(cameraIndex);


        _faceCascade = new CascadeClassifier("haarcascade_frontalface_default.xml");

        if (!_capture.IsOpened())
        {
            throw new Exception("Не удалось открыть веб-камеру.");
        }
        _isStreamRunning = true;
    }

    public (double X, double Y, double Z, Bitmap Frame) GetLatestFrameAnd3DMetrics()
    {
        if (!_isStreamRunning || _capture == null)
            return (0, 0, 0, null);

        using (Mat frame = new Mat())
        {
            _capture.Read(frame);
            if (frame.Empty())
                return (0, 0, 0, null);

            // Cadre Copy
            using (Mat processedFrame = frame.Clone())
            using (Mat gray = new Mat())
            {
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.EqualizeHist(gray, gray);

                var faces = _faceCascade.DetectMultiScale(gray, 1.1, 3, HaarDetectionTypes.ScaleImage, new OpenCvSharp.Size(30, 30));

                if (faces.Length > 0)
                {
                    var face = faces[0];
                    Cv2.Rectangle(processedFrame, face, Scalar.FromRgb(0, 255, 0), 2);

                    double faceX = face.X + (face.Width / 2.0);
                    double faceY = face.Y + (face.Height / 2.0);
                    double frameArea = frame.Width * frame.Height;
                    double faceArea = face.Width * face.Height;
                    double faceZ = 1.0 - (faceArea / frameArea);


                    return (faceX, faceY, faceZ, BitmapConverter.ToBitmap(processedFrame));
                }
            }

  
            return (0, 0, 0, BitmapConverter.ToBitmap(frame));
        }
    }

    public void Dispose()
    {
        _capture?.Release();
        _capture?.Dispose();
        _faceCascade?.Dispose();
        _isStreamRunning = false;
    }
}
