using System;
using System.IO;
using UnityEngine;


public class TelemetryLogger : MonoBehaviour
{
    public static TelemetryLogger Instance { get; private set; }

    private string csvPath;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        csvPath = Path.Combine(Application.persistentDataPath, "ollama_telemetry.csv");

        if (!File.Exists(csvPath))
        {
            File.WriteAllText(csvPath,
                "timestampUtc,model,inferenceMs,tokens,tokensGenerated,platform,device,deviceType\n");
        }
    }

    public void Log(OllamaTelemetry t)
    {
        try
        {
            string line = $"{t.timestampUtc},{Escape(t.model)},{t.inferenceMs:F0},{t.tokens},{t.tokensGenerated}," +
                          $"{Escape(t.platform)},{Escape(t.device)},{Escape(t.deviceType)}\n";

            File.AppendAllText(csvPath, line);
            Debug.Log($"Telemetry logged: {t.model} ({t.inferenceMs:F0} ms)");
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to write telemetry: " + e.Message);
        }
    }

    private string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace(",", ";").Replace("\n", " ");
    }
}
