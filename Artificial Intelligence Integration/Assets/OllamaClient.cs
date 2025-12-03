using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class OllamaClient : MonoBehaviour
{
    [Tooltip("Ollama base URL (default http://localhost:11434)")]
    public string ollamaBaseUrl = "http://localhost:11434";

    [Tooltip("Model name to call (e.g. llama3, gemma3:4b)")]
    public string model = "llama3";

    // Timeout for UnityWebRequest in seconds
    public int requestTimeout = 60;

    // Singleton convenience
    public static OllamaClient Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Send a prompt to Ollama's /api/generate endpoint and return raw JSON and telemetry.
    /// callback receives (success, rawJson, Telemetry)
    /// </summary>
    public IEnumerator Generate(string prompt, Action<bool, string, OllamaTelemetry> callback, bool stream = false)
    {
        var url = $"{ollamaBaseUrl}/api/chat";
        var payloadObj = new
        {
            model = model,
            prompt = prompt,
            stream = stream // we'll use non-streaming by default
        };

        string jsonPayload = JsonUtility.ToJson(payloadObj);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = requestTimeout;

            // Start timer
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            yield return req.SendWebRequest();

            stopwatch.Stop();
            double ms = stopwatch.Elapsed.TotalMilliseconds;

#if UNITY_2020_1_OR_NEWER
            bool error = req.result != UnityWebRequest.Result.Success;
#else
            bool error = req.isNetworkError || req.isHttpError;
#endif

            if (error)
            {
                UnityEngine.Debug.LogError($"Ollama request failed: {req.error}");
                callback?.Invoke(false, req.error, new OllamaTelemetry { success = false, inferenceMs = ms });
                yield break;
            }

            string raw = req.downloadHandler.text;
            var telemetry = new OllamaTelemetry
            {
                success = true,
                inferenceMs = ms,
                model = model,
                platform = SystemInfo.operatingSystem,
                device = SystemInfo.deviceModel,
                deviceType = SystemInfo.deviceType.ToString(),
                timestampUtc = DateTime.UtcNow.ToString("o"),
                rawResponse = raw
            };

            
            try
            {
                if (!string.IsNullOrEmpty(raw) && raw.Length > 0)
                {
                    // attempt to extract short preview
                    telemetry.generatedText = raw.Length > 1000 ? raw.Substring(0, 1000) + "..." : raw;
                }
            }
            catch { /* ignore */ }

            callback?.Invoke(true, raw, telemetry);
        }
    }
}

[Serializable]
public class OllamaTelemetry
{
    public bool success;
    public string model;
    public string timestampUtc;
    public double inferenceMs; // milliseconds
    public int tokens; // optional (try to fill if available)
    public int tokensGenerated; // optional
    public string platform;
    public string device;
    public string deviceType;
    public string rawResponse; // raw JSON from API
    public string generatedText; // best-effort preview
}