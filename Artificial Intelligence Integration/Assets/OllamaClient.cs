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

    public int requestTimeout = 60;

    public static OllamaClient Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);

        // Remove hidden whitespace from Inspector
        ollamaBaseUrl = ollamaBaseUrl.Trim();
    }

    // ---------------------------
    //   CHAT API DATA STRUCTURES
    // ---------------------------

    [Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class ChatRequest
    {
        public string model;
        public ChatMessage[] messages;
        public bool stream = false;
    }

    // -----------------------------------
    //   WORKING GENERATE() FOR /api/chat
    // -----------------------------------

    public IEnumerator Generate(string prompt, Action<bool, string, OllamaTelemetry> callback)
    {
        string url = $"{ollamaBaseUrl}/api/chat";

        // Build proper chat request body
        var reqBody = new ChatRequest
        {
            model = model,
            messages = new ChatMessage[]
            {
                new ChatMessage { role = "system", content = "You are an NPC in a medieval town." },
                new ChatMessage { role = "user",   content = prompt }
            },
            stream = false
        };

        string jsonPayload = JsonUtility.ToJson(reqBody);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = requestTimeout;

            var sw = new Stopwatch();
            sw.Start();

            yield return req.SendWebRequest();
            sw.Stop();

            bool error = req.result != UnityWebRequest.Result.Success;

            if (error)
            {
                //Debug.LogError($"Ollama request failed: {req.error}\nSent JSON:\n{jsonPayload}");
                callback(false, req.error, new OllamaTelemetry
                {
                    success = false,
                    inferenceMs = sw.Elapsed.TotalMilliseconds
                });
                yield break;
            }

            string raw = req.downloadHandler.text;

            var telemetry = new OllamaTelemetry
            {
                success = true,
                inferenceMs = sw.Elapsed.TotalMilliseconds,
                model = model,
                rawResponse = raw,
                timestampUtc = DateTime.UtcNow.ToString("o"),
                platform = SystemInfo.operatingSystem,
                device = SystemInfo.deviceModel,
                deviceType = SystemInfo.deviceType.ToString()
            };

            callback(true, raw, telemetry);
        }
    }
}


// -------------------------------
//     TELEMETRY CLASS
// -------------------------------

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