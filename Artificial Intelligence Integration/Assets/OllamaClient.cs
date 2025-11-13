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

    [Tooltip("Model name to call (e.g. llama3, mistral, phi3)")]
    public string model = "llama3";

    public int requestTimeout = 60;

    public static OllamaClient Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator Generate(string prompt, Action<bool, string, OllamaTelemetry> callback, bool stream = false)
    {
        var url = $"{ollamaBaseUrl}/api/chat";

        // ? Correct payload for Ollama /api/chat
        string jsonPayload = $@"
{{
  ""model"": ""{model}"",
  ""messages"": [
    {{""role"": ""system"", ""content"": ""You are a helpful NPC in a medieval town. Keep responses short, friendly, and immersive."" }},
    {{""role"": ""user"", ""content"": ""{EscapeJson(prompt)}"" }}
  ],
  ""stream"": false
}}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = requestTimeout;

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
                Debug.LogError($"Ollama request failed: {req.error}\nResponse: {req.downloadHandler.text}");
                callback?.Invoke(false, req.downloadHandler.text, new OllamaTelemetry { success = false, inferenceMs = ms });
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

            // ? Extract the assistant's message.content
            string content = TryExtractMessageContent(raw);
            telemetry.generatedText = content ?? "(no reply found)";

            callback?.Invoke(true, raw, telemetry);
        }
    }

    private string TryExtractMessageContent(string json)
    {
        // Very lightweight JSON extraction without needing Newtonsoft
        string key = "\"content\":\"";
        int start = json.IndexOf(key);
        if (start == -1) return null;
        start += key.Length;
        int end = json.IndexOf('"', start);
        if (end == -1) return null;
        string extracted = json.Substring(start, end - start);
        return extracted.Replace("\\n", "\n").Replace("\\\"", "\"");
    }

    private string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}

[Serializable]
public class OllamaTelemetry
{
    public bool success;
    public string model;
    public string timestampUtc;
    public double inferenceMs;
    public int tokens;
    public int tokensGenerated;
    public string platform;
    public string device;
    public string deviceType;
    public string rawResponse;
    public string generatedText;
}
