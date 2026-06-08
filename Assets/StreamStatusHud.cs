using UnityEngine;

public class StreamStatusHud : MonoBehaviour
{
    public Vector3 fallbackLocalPosition = new Vector3(0.55f, 0.42f, 1.6f);
    public float characterSize = 0.0085f;
    public int fontSize = 32;
    public Color connectedColor = new Color(0.85f, 0.92f, 1.0f, 1.0f);
    public Color warningColor = new Color(1.0f, 0.75f, 0.2f, 1.0f);
    public Color errorColor = new Color(1.0f, 0.25f, 0.25f, 1.0f);

    private WebRTCStreamReceiver receiver;
    private TextMesh textMesh;
    private Transform currentParent;

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
        textMesh.anchor = TextAnchor.MiddleRight;
        textMesh.alignment = TextAlignment.Right;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = characterSize;
        textMesh.color = warningColor;

        AttachToCamera();
    }

    private void AttachToCamera()
    {
        if (textMesh == null)
        {
            return;
        }

        Transform target = FindStatusAnchor();
        bool usingStatusBar = target != null;

        if (target == null)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            target = camera.transform;
        }

        if (target != currentParent)
        {
            currentParent = target;
            textMesh.transform.SetParent(currentParent, false);
        }

        textMesh.transform.localPosition = usingStatusBar ? Vector3.zero : fallbackLocalPosition;
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
        textMesh.text = BuildStatusText();
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
                return "STREAM IDLE";
            case StreamReceiverState.Connecting:
                return "STREAM CONNECTING";
            case StreamReceiverState.Connected:
                return "STREAM OK";
            case StreamReceiverState.ConnectedNoVideoTrack:
                return "STREAM NO VIDEO";
            case StreamReceiverState.Failed:
                return "STREAM FAILED";
            case StreamReceiverState.Disconnected:
                return "STREAM OFF";
            default:
                return state.ToString();
        }
    }

    private string BuildStatusText()
    {
        string label = GetStateLabel(receiver.State);

        if (receiver.State != StreamReceiverState.Failed
            && receiver.State != StreamReceiverState.Disconnected
            && receiver.State != StreamReceiverState.ConnectedNoVideoTrack)
        {
            return label;
        }

        if (string.IsNullOrWhiteSpace(receiver.LastError))
        {
            return label;
        }

        return label + ": " + Shorten(receiver.LastError, 58);
    }

    private static string Shorten(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength - 3) + "...";
    }

    private static Transform FindStatusAnchor()
    {
        GameObject anchor = GameObject.Find(StreamPanelManager.StatusRightAnchorName);
        return anchor != null ? anchor.transform : null;
    }
}
