using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class DatasetProcessor
{
    public class DatasetItem
    {
        public string ImageName { get; set; }
        public string Label { get; set; } // e.g., "schizophrenia" or "normal"
    }

    private static List<DatasetItem> _items = new List<DatasetItem>();
    public static DatasetItem CurrentPatient { get; private set; }

    public static void LoadDataset(string folderPath)
    {
        string csvPath = Path.Combine(folderPath, "labels.csv");
        if (!File.Exists(csvPath)) return;

        _items.Clear();
        var lines = File.ReadAllLines(csvPath).Skip(1); // 

        foreach (var line in lines)
        {
            var parts = line.Split(',');
            if (parts.Length >= 2)
            {
                _items.Add(new DatasetItem
                {
                    ImageName = parts[0].Trim(),
                    Label = parts[1].Trim()
                });
            }
        }

       
        if (_items.Count > 0)
        {
            var rand = new Random();
            CurrentPatient = _items[rand.Next(_items.Count)];
        }
    }
}
