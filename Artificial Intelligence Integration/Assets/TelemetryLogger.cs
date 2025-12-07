using System;
using System.IO;
using UnityEngine;

public class TelemetryLogger : MonoBehaviour
{
    public static TelemetryLogger Instance { get; private set; }
    string csvPath;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);

        csvPath = Path.Combine(Application.persistentDataPath, "ollama_telemetry.csv");
        if (!File.Exists(csvPath))
        {
            File.WriteAllText(csvPath, "timestampUtc,model,inferenceMs,tokens,tokensGenerated,platform,device,deviceType\n");
        }
    }

    public void Log(OllamaTelemetry t)
    {
        try
        {
            var line = $"{t.timestampUtc},{Escape(t.model)},{t.inferenceMs:F0},{t.tokens},{t.tokensGenerated},{Escape(t.platform)},{Escape(t.device)},{Escape(t.deviceType)}\n";
            File.AppendAllText(csvPath, line);
            Debug.Log($"Telemetry logged to: {csvPath}");
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to write telemetry: " + e.Message);
        }
    }

    string Escape(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace(",", ";").Replace("\n", " ");
}