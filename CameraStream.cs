using System;
using System.Drawing;
using System.Threading.Tasks;

public class WebcamStreamer
{
    private FaceMatrixOpenCV _extractor;
    private bool _isRunning;

    public event Action<Bitmap, double, double, double> OnTickCaptured;

    public void Start(int cameraIndex = 0)
    {
        _extractor = new FaceMatrixOpenCV();
        _extractor.InitializeCamera(cameraIndex);
        _isRunning = true;

        Task.Run(() => Loop());
    }

    private void Loop()
    {
        while (_isRunning)
        {

            var data = _extractor.GetLatestFrameAnd3DMetrics();

            if (data.Frame != null)
            {
  
                OnTickCaptured?.Invoke(data.Frame, data.X, data.Y, data.Z);
            }

            // (CLK). 
            System.Threading.Thread.Sleep(33);
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _extractor?.Dispose();
    }
}
