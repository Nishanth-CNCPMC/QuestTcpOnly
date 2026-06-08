using System.Text;
using UnityEngine;

public class StreamStatusHud : MonoBehaviour
{
    public Vector3 localPosition = new Vector3(-0.78f, -0.56f, 1.75f);
    public float characterSize = 0.018f;
    public int fontSize = 48;
    public Color connectedColor = new Color(0.2f, 1.0f, 0.35f, 1.0f);
    public Color warningColor = new Color(1.0f, 0.75f, 0.2f, 1.0f);
    public Color errorColor = new Color(1.0f, 0.25f, 0.25f, 1.0f);

    private WebRTCStreamReceiver receiver;
    private TextMesh textMesh;
    private Transform currentCamera;

    private void Awake()
    {
        receiver = GetComponent<WebRTCStreamReceiver>();
        EnsureTextMesh();
    }

    private void LateUpdate()
    {
        AttachToCamera();
        UpdateText();
    }

    private void EnsureTextMesh()
    {
        if (textMesh != null)
        {
            return;
        }

        GameObject hudObject = new GameObject("WebRTC Stream Status HUD");
        textMesh = hudObject.AddComponent<TextMesh>();
        textMesh.anchor = TextAnchor.UpperLeft;
        textMesh.alignment = TextAlignment.Left;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = characterSize;
        textMesh.color = warningColor;

        AttachToCamera();
    }

    private void AttachToCamera()
    {
        Camera camera = Camera.main;
        if (camera == null || textMesh == null)
        {
            return;
        }

        if (currentCamera != camera.transform)
        {
            currentCamera = camera.transform;
            textMesh.transform.SetParent(currentCamera, false);
        }

        textMesh.transform.localPosition = localPosition;
        textMesh.transform.localRotation = Quaternion.identity;
        textMesh.transform.localScale = Vector3.one;
    }

    private void UpdateText()
    {
        if (receiver == null || textMesh == null)
        {
            return;
        }

        textMesh.color = GetStateColor(receiver.State);

        StringBuilder builder = new StringBuilder();
        builder.Append("Stream: ");
        builder.AppendLine(receiver.StreamUrl);
        builder.Append("State: ");
        builder.AppendLine(GetStateLabel(receiver.State));

        if (receiver.State == StreamReceiverState.ConnectedNoVideoTrack)
        {
            builder.AppendLine(receiver.HasVideoTrack ? "Waiting for first video frame." : "Connected but no video track.");
        }

        if (!string.IsNullOrEmpty(receiver.LastError))
        {
            builder.Append("Error: ");
            builder.AppendLine(receiver.LastError);
        }

        textMesh.text = builder.ToString();
    }

    private Color GetStateColor(StreamReceiverState state)
    {
        if (state == StreamReceiverState.Connected)
        {
            return connectedColor;
        }

        if (state == StreamReceiverState.Failed || state == StreamReceiverState.Disconnected)
        {
            return errorColor;
        }

        return warningColor;
    }

    private static string GetStateLabel(StreamReceiverState state)
    {
        switch (state)
        {
            case StreamReceiverState.Idle:
                return "idle";
            case StreamReceiverState.Connecting:
                return "connecting";
            case StreamReceiverState.Connected:
                return "connected";
            case StreamReceiverState.ConnectedNoVideoTrack:
                return "connected, waiting for video";
            case StreamReceiverState.Failed:
                return "failed";
            case StreamReceiverState.Disconnected:
                return "disconnected";
            default:
                return state.ToString();
        }
    }
}
