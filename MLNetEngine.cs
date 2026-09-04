using System;
using Microsoft.ML;
using Microsoft.ML.Data;

public class MLNetEngine
{
    
    public class PatientDataInput
    {
        [LoadColumn(0)] public float PrologAutomatonCode { get; set; } // PrologOutput(0, 1, 2)
        [LoadColumn(1)] public float KeyHoldTimeMean { get; set; }     // MeanKeystrokePress time
        [LoadColumn(2)] public float KeyFlightTimeMean { get; set; }   // Pause between pressing of the keys
    }

    // 1. Entropy
    public class EntropyPrediction
    {
        [ColumnName("Score")]
        public float EntropyScore { get; set; } // from 0.0 to 1.0 (pathology matching Metric)
    }

    private static MLContext _mlContext;
    private static PredictionEngine<PatientDataInput, EntropyPrediction> _predictionEngine;

    public static void InitializeEngine()
    {
        _mlContext = new MLContext(seed: 42);

        var emptyData = new PatientDataInput[0];
        var dataView = _mlContext.Data.LoadFromEnumerable(emptyData);

        // Instead of LSTM
        var pipeline = _mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: nameof(PatientDataInput.KeyHoldTimeMean))
            .Append(_mlContext.Transforms.Concatenate("Features",
                nameof(PatientDataInput.PrologAutomatonCode),
                nameof(PatientDataInput.KeyHoldTimeMean),
                nameof(PatientDataInput.KeyFlightTimeMean)))
            .Append(_mlContext.Regression.Trainers.Sdca()); // DecisionTree had been chosen for example


        var model = pipeline.Fit(dataView);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<PatientDataInput, EntropyPrediction>(model);
    }

    // Realtime ML Calculation
    public static double CalculateLiveEntropy(int prologCode, double holdTime = 0.12, double flightTime = 0.25)
    {
        if (_predictionEngine == null) InitializeEngine();

        var input = new PatientDataInput
        {
            PrologAutomatonCode = prologCode,
            KeyHoldTimeMean = (float)holdTime,
            KeyFlightTimeMean = (float)flightTime
        };

        // Inference
        var prediction = _predictionEngine.Predict(input);

        double baseEntropy = prediction.EntropyScore;
        if (prologCode == 2) baseEntropy += 0.45; // Prolog process calculated the pathology probability

        return Math.Clamp(baseEntropy, 0.0, 1.0);
    }
}