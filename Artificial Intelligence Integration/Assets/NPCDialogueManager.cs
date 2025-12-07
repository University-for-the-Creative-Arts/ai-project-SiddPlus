using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCDialogueManager : MonoBehaviour
{
    [Header("UI References (TextMeshPro)")]
    public TMP_Text npcText;          // NPC�s dialogue output
    public TMP_InputField playerInput; // Player�s input field
    public Button sendButton;          // Send button
    public TMP_Text telemetryText;     // Shows model name / response time

    [Header("Conversation Settings")]
    [TextArea(3, 6)]
    public string systemPrompt = "You are an NPC in a medieval town. Keep responses short, characterful, and friendly.";

    void Start()
    {
        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendClicked);

        if (npcText != null)
            npcText.text = "Greetings, traveler!";
    }

    public void OnSendClicked()
    {
        string playerMessage = playerInput.text;
        if (string.IsNullOrWhiteSpace(playerMessage))
            return;

        npcText.text = "Thinking...";
        //telemetryText.text = "URL: " + OllamaClient.Instance.ollamaBaseUrl + "/api/chat";
        string prompt = $"{systemPrompt}\nPlayer: {playerMessage}\nNPC:";

        // Start the Ollama generation coroutine
        StartCoroutine(OllamaClient.Instance.Generate(prompt, OnOllamaResponse));

        playerInput.text = ""; // Clear input field
    }

    private void OnOllamaResponse(bool ok, string rawJson, OllamaTelemetry telemetry)
    {
        if (!ok)
        {
            npcText.text = "Sorry, I can�t think right now.";
            telemetryText.text = "Error: Failed to reach Ollama server.";
            return;
        }

        // Display the generated response
        if (!string.IsNullOrEmpty(telemetry.generatedText))
            npcText.text = telemetry.generatedText.Trim();
        else
            npcText.text = rawJson.Length > 800 ? rawJson.Substring(0, 800) + "..." : rawJson;

        // Display telemetry info on screen
        telemetryText.text =
            $"Model: {telemetry.model}\n" +
            $"Time: {telemetry.inferenceMs:F0} ms\n" +
            $"Device: {telemetry.device}";

        // Log telemetry to disk
        TelemetryLogger.Instance.Log(telemetry);
    }
}
