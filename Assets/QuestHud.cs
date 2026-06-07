using System.Globalization;
using System.Text;
using UnityEngine;

public class QuestHud : MonoBehaviour
{
    public Vector3 localPosition = new Vector3(-0.55f, 0.28f, 1.15f);
    public float characterSize = 0.018f;
    public int fontSize = 48;

    private TextMesh textMesh;
    private Transform currentCamera;

    private void Awake()
    {
        EnsureTextMesh();
    }

    private void LateUpdate()
    {
        AttachToCamera();
    }

    public void SetStatus(
        bool tcpConnected,
        bool controllerDetected,
        bool originSet,
        string instruction,
        bool hasRelativePose,
        Vector3 relativePosition,
        float trigger,
        bool recording)
    {
        EnsureTextMesh();

        StringBuilder builder = new StringBuilder();
        builder.Append("TCP: ");
        builder.AppendLine(tcpConnected ? "connected" : "disconnected");
        builder.Append("Right controller: ");
        builder.AppendLine(controllerDetected ? "detected" : "not detected");
        builder.Append("Calibration: ");
        builder.AppendLine(originSet ? "origin set" : "origin not set");
        builder.AppendLine(instruction);

        if (hasRelativePose)
        {
            builder.Append("Rel pos: x=");
            builder.Append(Format(relativePosition.x));
            builder.Append(" y=");
            builder.Append(Format(relativePosition.y));
            builder.Append(" z=");
            builder.AppendLine(Format(relativePosition.z));
        }

        builder.Append("Trigger: ");
        builder.AppendLine(Format(trigger));
        builder.Append("Recording: ");
        builder.Append(recording ? "recording" : "not recording");

        textMesh.text = builder.ToString();
    }

    private void EnsureTextMesh()
    {
        if (textMesh != null)
        {
            return;
        }

        GameObject hudObject = new GameObject("Quest Debug HUD");
        textMesh = hudObject.AddComponent<TextMesh>();
        textMesh.anchor = TextAnchor.UpperLeft;
        textMesh.alignment = TextAlignment.Left;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = characterSize;
        textMesh.color = Color.green;

        AttachToCamera();
    }

    private void AttachToCamera()
    {
        Camera camera = Camera.main;
        if (camera == null || camera.transform == currentCamera || textMesh == null)
        {
            return;
        }

        currentCamera = camera.transform;
        textMesh.transform.SetParent(currentCamera, false);
        textMesh.transform.localPosition = localPosition;
        textMesh.transform.localRotation = Quaternion.identity;
        textMesh.transform.localScale = Vector3.one;
    }

    private static string Format(float value)
    {
        return value.ToString("F4", CultureInfo.InvariantCulture);
    }
}
