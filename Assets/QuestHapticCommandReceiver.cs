using System;
using UnityEngine;
using UnityEngine.XR;

public class QuestHapticCommandReceiver : MonoBehaviour
{
    public float fallbackAmplitude = 0.25f;
    public float fallbackDurationSeconds = 0.035f;

    private QuestTcpClient tcpClient;
    private QuestHaptics haptics;

    private void Start()
    {
        tcpClient = GetComponent<QuestTcpClient>();
        if (tcpClient == null)
        {
            tcpClient = gameObject.AddComponent<QuestTcpClient>();
        }

        haptics = GetComponent<QuestHaptics>();
        if (haptics == null)
        {
            haptics = gameObject.AddComponent<QuestHaptics>();
        }

        tcpClient.LineReceived += OnTcpLineReceived;
    }

    private void OnDestroy()
    {
        if (tcpClient != null)
        {
            tcpClient.LineReceived -= OnTcpLineReceived;
        }
    }

    private void OnTcpLineReceived(string line)
    {
        HapticCommand command;
        try
        {
            command = JsonUtility.FromJson<HapticCommand>(line);
        }
        catch (Exception)
        {
            return;
        }

        if (command == null || command.type != "haptic")
        {
            return;
        }

        XRNode node = command.side == "left" ? XRNode.LeftHand : XRNode.RightHand;
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        float duration = command.duration_ms > 0 ? command.duration_ms / 1000.0f : fallbackDurationSeconds;
        float amplitude = command.amplitude > 0.0f ? command.amplitude : fallbackAmplitude;
        haptics.Pulse(device, amplitude, duration);
    }

    [Serializable]
    private class HapticCommand
    {
        public string type;
        public string side;
        public float amplitude;
        public int duration_ms;
    }
}
