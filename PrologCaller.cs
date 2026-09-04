using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public class PrologCaller : IDisposable
{
    private Process _prologProcess;
    private StreamWriter _inputWriter;
    private StreamReader _outputReader;

    // Tick of Prolog Tree
    public event Action<int> OnAutomatonTickProcessed;

    public void StartEngine(string exePath)
    {
        _prologProcess = new Process();
        _prologProcess.StartInfo.FileName = exePath;
        _prologProcess.StartInfo.UseShellExecute = false;
        _prologProcess.StartInfo.CreateNoWindow = true; 
        _prologProcess.StartInfo.RedirectStandardInput = true;
        _prologProcess.StartInfo.RedirectStandardOutput = true;

        _prologProcess.Start();

        _inputWriter = _prologProcess.StandardInput;
        _outputReader = _prologProcess.StandardOutput;

        // Run the AIPsychiatrist.exe prolog process outside of WinForms
        Task.Run(() => ListenToProlog());
    }

 
    // CLK OF Automata receives ID, 3D-matrix and score of the test
    public void SendDataTick(string patientId, double liveX, double liveY, double liveZ, int liveScore)
    {
        if (_inputWriter != null && !_prologProcess.HasExited)
        {
            string dataLine = string.Format(System.Globalization.CultureInfo.InvariantCulture, //data convertion
                "{0} {1:F2} {2:F2} {3:F2} {4}", patientId, liveX, liveY, liveZ, liveScore);

            _inputWriter.WriteLine(dataLine);
        }
    }


    private void ListenToProlog()
    {
        try
        {
            while (!_outputReader.EndOfStream)
            {
                string response = _outputReader.ReadLine();
                if (int.TryParse(response, out int resultCode))
                {
                    // ReturnCall to the Main Program
                    OnAutomatonTickProcessed?.Invoke(resultCode);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка чтения потока Пролога: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            if (_prologProcess != null && !_prologProcess.HasExited)
            {
                _prologProcess.Kill();
            }
            _inputWriter?.Dispose();
            _outputReader?.Dispose();
            _prologProcess?.Dispose();
        }
        catch { }
    }
}
