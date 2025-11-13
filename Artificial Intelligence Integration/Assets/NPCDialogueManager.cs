using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPCDialogueManager : MonoBehaviour
{
    [Header("UI References (TextMeshPro)")]
    public TMP_Text npcText;           // NPC's dialogue output
    public TMP_InputField playerInput; // Player's text input
    public Button sendButton;          // Send button
    public TMP_Text telemetryText;     // Displays model name / response time

    [Header("Conversation Settings")]
    [TextArea(3, 6)]
    public string systemPrompt = "You are an NPC in a medieval town. Keep responses short, characterful, and friendly.";

    private void Start()
    {
        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendClicked);

        if (npcText != null)
            npcText.text = "Greetings, traveler!";
    }

    private void OnSendClicked()
    {
        string playerMessage = playerInput.text;
        if (string.IsNullOrWhiteSpace(playerMessage))
            return;

        npcText.text = "Thinking...";
        string prompt = $"{systemPrompt}\nPlayer: {playerMessage}\nNPC:";

        StartCoroutine(OllamaClient.Instance.Generate(prompt, OnOllamaResponse));
        playerInput.text = "";
    }

    private void OnOllamaResponse(bool ok, string rawJson, OllamaTelemetry telemetry)
    {
        if (!ok)
        {
            npcText.text = "Sorry, I can't think right now.";
            telemetryText.text = "Error: Could not reach Ollama server.";
            return;
        }

        // Show AI reply
        npcText.text = telemetry.generatedText?.Trim() ?? "(no response)";

        // Display telemetry on-screen
        telemetryText.text =
            $"Model: {telemetry.model}\n" +
            $"Time: {telemetry.inferenceMs:F0} ms\n" +
            $"Device: {telemetry.device}";

        // Save telemetry to file
        TelemetryLogger.Instance.Log(telemetry);
    }
}
