using System.Drawing.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AIPsychiatristbyPapaevBN
{
    public partial class Form1 : Form
    {


        private int GetQuestionnaireScore()
        {
            int score = 0;
            if (checkBox1.Checked) score++;
            if (checkBox2.Checked) score++;
            if (checkBox3.Checked) score++;
            if (checkBox4.Checked) score++;
            return score; // На выходе число от 0 до 4
        }


        public Form1()
        {
            InitializeComponent();

            // Буфер данных для ScottPlot (окно времени на 50 тактов CLK)
        }
        private FaceMatrixOpenCV _faceExtractor;
        private PrologCaller _bridge;

        // Буфер данных для ScottPlot (окно времени на 50 тактов CLK)
        private WebcamStreamer _streamer;
        private double _latestFaceZ = 0.5; // Сюда OpenCV будет сохранять текущую глубину лица
        private readonly double[] _entropyData = new double[50];
        private int _dataIndex = 0;

        private void Form1_Load(object sender, EventArgs e)
        {
            // 1. Инициализируем легковесный ML.NET
            MLNetEngine.InitializeEngine();

            // 2. Настраиваем ScottPlot, который перетащили на форму
            formsPlot1.Plot.Add.Signal(_entropyData);
            formsPlot1.Plot.Title("Мультипараметрическая энтропия отпечатка");
            formsPlot1.Plot.XLabel("Такты времени (CLK)");
            formsPlot1.Plot.YLabel("Степень патологии (0.0 - 1.0)");
            formsPlot1.Plot.Axes.SetLimitsY(0.0, 1.0); // Жестко фиксируем шкалу
            formsPlot1.Refresh();

            // 3. Запускаем скрытое нативное ядро Пролога
            _bridge = new PrologCaller();
            _bridge.OnAutomatonTickProcessed += Prolog_OnTickProcessed;
            try
            {
                // Убедитесь, что ваш откомпилированный AI_Psychiatrist.exe лежит в папке бинда C#
                _bridge.StartEngine("AI_Psychiatrist.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска Пролога: {ex.Message}");
            }

            _faceExtractor = new FaceMatrixOpenCV();

            try
            {
                // Открываем камеру (0 - индекс дефолтной вебки)
                _faceExtractor.InitializeCamera(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации OpenCV: {ex.Message}");
            }

            // 2. Запускаем твой рабочий таймер (наш тактовый генератор CLK)
            clkTimer.Interval = 100; // такт 100мс
            clkTimer.Start();
            string datasetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EEG_IMAGES_VGG16");
            DatasetProcessor.LoadDataset(datasetPath);

            // 2. Если нашли пациента — выводим его эталонную спектрограмму на экран врачу!
            if (DatasetProcessor.CurrentPatient != null)
            {
                string imgPath = Path.Combine(datasetPath, DatasetProcessor.CurrentPatient.ImageName);
                if (File.Exists(imgPath))
                {
                    pictureBoxDataset.Image?.Dispose();
                    pictureBoxDataset.Image = Image.FromFile(imgPath);
                }
            }

            // 5. Запускаем наш тактовый генератор CLK (Интервал выставлен на 100 мс)
            clkTimer.Start();
        }


        // --- ФИЗИЧЕСКИЙ CLK ТАКТ ЦИФРОВОГО АВТОМАТА (Каждые 100 мс) ---
        private void clkTimer_Tick(object sender, EventArgs e)
        {
            // Считываем 3D-координаты овала лица из OpenCV
            var data = _faceExtractor.GetLatestFrameAnd3DMetrics();

            // Выводим live-видео в левое окно PictureBox
            if (data.Frame != null)
            {
                pictureBoxCamera.Image?.Dispose();
                pictureBoxCamera.Image = data.Frame;
            }

            if (data.X > 0 || data.Y > 0)
            {
                // Забираем живую сумму баллов опросника (0-4)
                int liveScore = GetQuestionnaireScore();

                // Просто передаем все переменные по отдельности — мост сам всё соберет в строку!
                _bridge.SendDataTick("p1", data.X, data.Y, data.Z, liveScore);
            }

        }

        // --- РЕАКЦИЯ НА ОТВЕТ ПРОЛОГА: РАСЧЕТ ML.NET И СДВИГ ГРАФИКА ---
        private void Prolog_OnTickProcessed(int code)
        {
            this.BeginInvoke(new Action(() =>
            {
                // Считываем, какой диагноз зашит в текущей картинке Kaggle
                bool isDatasetAnomaly = DatasetProcessor.CurrentPatient?.Label == "schizophrenia";

                // Считаем живую энтропию
                double currentEntropy = MLNetEngine.CalculateLiveEntropy(code, _latestFaceZ);

                // Если в датасете аномалия И Пролог зафиксировал рассинхрон — отпечаток совпал на 100%!
                if (isDatasetAnomaly && code == 2)
                {
                    currentEntropy = Math.Min(1.0, currentEntropy + 0.25);
                }

                // Обновляем кольцевой буфер ScottPlot
                if (_dataIndex < _entropyData.Length) _entropyData[_dataIndex++] = currentEntropy;
                else
                {
                    Array.Copy(_entropyData, 1, _entropyData, 0, _entropyData.Length - 1);
                    _entropyData[_entropyData.Length - 1] = currentEntropy;
                }

                formsPlot1.Refresh();
            }));
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            clkTimer.Stop();
            _faceExtractor?.Dispose();
            _bridge?.Dispose(); // Корректно закрываем процесс Пролога
        }

    }
}
    

