using UnityEngine;

public class QuestHud : MonoBehaviour
{
    public Vector3 fallbackLocalPosition = new Vector3(-0.55f, 0.42f, 1.6f);
    public float characterSize = 0.0085f;
    public int fontSize = 32;
    public Color textColor = new Color(0.85f, 0.92f, 1.0f, 1.0f);
    public Color disconnectedColor = new Color(1.0f, 0.65f, 0.25f, 1.0f);

    private TextMesh textMesh;
    private Transform currentParent;

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
        bool recording,
        bool rosTopicEnable)
    {
        EnsureTextMesh();
        textMesh.color = tcpConnected ? textColor : disconnectedColor;
        string rosState = rosTopicEnable ? "ROS ON" : "ROS OFF";
        textMesh.text = (tcpConnected ? "TCP OK" : "TCP OFF") + " | " + rosState;
    }

    private void EnsureTextMesh()
    {
        if (textMesh != null)
        {
            return;
        }

        GameObject hudObject = new GameObject("Quest Debug HUD");
        textMesh = hudObject.AddComponent<TextMesh>();
        textMesh.anchor = TextAnchor.MiddleLeft;
        textMesh.alignment = TextAlignment.Left;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = characterSize;
        textMesh.color = textColor;

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

    private static Transform FindStatusAnchor()
    {
        GameObject anchor = GameObject.Find(StreamPanelManager.StatusLeftAnchorName);
        return anchor != null ? anchor.transform : null;
    }
}
