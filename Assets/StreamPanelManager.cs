using UnityEngine;
using UnityEngine.Rendering;

public class StreamPanelManager : MonoBehaviour
{
    public const string StatusLeftAnchorName = "Quest2Skill Stream Status Left";
    public const string StatusRightAnchorName = "Quest2Skill Stream Status Right";

    public Vector3 localPosition = new Vector3(0.0f, 0.0f, 2.05f);
    public float panelWidthMeters = 2.2f;
    public float panelHeightMeters = 1.2375f;
    public float statusBarHeightMeters = 0.085f;
    public float statusBarPaddingMeters = 0.04f;
    public Color placeholderColor = new Color(0.04f, 0.04f, 0.04f, 1.0f);
    public Color statusBarColor = new Color(0.015f, 0.018f, 0.022f, 1.0f);

    private const string BootstrapName = "Quest2Skill WebRTC Stream Layer";

    private WebRTCStreamReceiver receiver;
    private Renderer panelRenderer;
    private Material panelMaterial;
    private Transform statusBarTransform;
    private Transform statusLeftAnchor;
    private Transform statusRightAnchor;
    private Material statusBarMaterial;
    private Texture appliedTexture;
    private Transform currentCamera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<StreamPanelManager>() != null)
        {
            return;
        }

        GameObject streamLayer = new GameObject(BootstrapName);
        streamLayer.AddComponent<WebRTCStreamReceiver>();
        streamLayer.AddComponent<StreamPanelManager>();
        streamLayer.AddComponent<StreamStatusHud>();
    }

    private void Awake()
    {
        receiver = GetComponent<WebRTCStreamReceiver>();
        if (receiver == null)
        {
            receiver = gameObject.AddComponent<WebRTCStreamReceiver>();
        }

        EnsurePanel();
        EnsureStatusBar();
    }

    private void LateUpdate()
    {
        AttachToCamera();
        LayoutStatusBar();
        ApplyReceiverTexture();
    }

    private void EnsurePanel()
    {
        if (panelRenderer != null)
        {
            return;
        }

        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
        panel.name = "Floating WebRTC Stream Panel";

        Collider panelCollider = panel.GetComponent<Collider>();
        if (panelCollider != null)
        {
            Destroy(panelCollider);
        }

        panelRenderer = panel.GetComponent<Renderer>();
        panelMaterial = new Material(FindUnlitShader());
        ConfigurePanelMaterial(panelMaterial);
        SetMaterialColor(placeholderColor);
        panelRenderer.sharedMaterial = panelMaterial;

        panel.transform.SetParent(transform, false);
        panel.transform.localPosition = Vector3.zero;
        panel.transform.localRotation = Quaternion.identity;
        panel.transform.localScale = new Vector3(panelWidthMeters, panelHeightMeters, 1.0f);

        AttachToCamera();
    }

    private void EnsureStatusBar()
    {
        if (statusBarTransform != null)
        {
            return;
        }

        GameObject statusBar = GameObject.CreatePrimitive(PrimitiveType.Quad);
        statusBar.name = "Quest2Skill Stream Status Bar";

        Collider statusCollider = statusBar.GetComponent<Collider>();
        if (statusCollider != null)
        {
            Destroy(statusCollider);
        }

        Renderer statusRenderer = statusBar.GetComponent<Renderer>();
        statusBarMaterial = new Material(FindUnlitShader());
        ConfigurePanelMaterial(statusBarMaterial);
        SetMaterialColor(statusBarMaterial, statusBarColor);
        statusRenderer.sharedMaterial = statusBarMaterial;

        statusBarTransform = statusBar.transform;
        statusBarTransform.SetParent(transform, false);

        statusLeftAnchor = new GameObject(StatusLeftAnchorName).transform;
        statusLeftAnchor.SetParent(transform, false);

        statusRightAnchor = new GameObject(StatusRightAnchorName).transform;
        statusRightAnchor.SetParent(transform, false);

        LayoutStatusBar();
    }

    private void LayoutStatusBar()
    {
        if (statusBarTransform == null)
        {
            return;
        }

        float y = (panelHeightMeters * 0.5f) - (statusBarHeightMeters * 0.5f);
        statusBarTransform.localPosition = new Vector3(0.0f, y, -0.012f);
        statusBarTransform.localRotation = Quaternion.identity;
        statusBarTransform.localScale = new Vector3(panelWidthMeters, statusBarHeightMeters, 1.0f);

        if (statusLeftAnchor != null)
        {
            statusLeftAnchor.localPosition = new Vector3(
                (-panelWidthMeters * 0.5f) + statusBarPaddingMeters,
                y,
                -0.025f);
            statusLeftAnchor.localRotation = Quaternion.identity;
            statusLeftAnchor.localScale = Vector3.one;
        }

        if (statusRightAnchor != null)
        {
            statusRightAnchor.localPosition = new Vector3(
                (panelWidthMeters * 0.5f) - statusBarPaddingMeters,
                y,
                -0.025f);
            statusRightAnchor.localRotation = Quaternion.identity;
            statusRightAnchor.localScale = Vector3.one;
        }
    }

    private void AttachToCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        if (currentCamera != camera.transform)
        {
            currentCamera = camera.transform;
            transform.SetParent(currentCamera, false);
        }

        transform.localPosition = localPosition;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private void ApplyReceiverTexture()
    {
        if (receiver == null || panelMaterial == null)
        {
            return;
        }

        Texture texture = receiver.ReceivedTexture;
        if (texture == null)
        {
            if (appliedTexture != null)
            {
                appliedTexture = null;
                SetMaterialTexture(null);
                SetMaterialColor(placeholderColor);
            }

            return;
        }

        if (texture == appliedTexture)
        {
            return;
        }

        appliedTexture = texture;
        SetMaterialTexture(texture);
        SetMaterialColor(Color.white);
    }

    private static Shader FindUnlitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Unlit/Texture");
        if (shader != null)
        {
            return shader;
        }

        return Shader.Find("Standard");
    }

    private void SetMaterialColor(Color color)
    {
        SetMaterialColor(panelMaterial, color);
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.color = color;
        }
    }

    private void SetMaterialTexture(Texture texture)
    {
        if (panelMaterial == null)
        {
            return;
        }

        if (panelMaterial.HasProperty("_BaseMap"))
        {
            panelMaterial.SetTexture("_BaseMap", texture);
        }

        if (panelMaterial.HasProperty("_MainTex"))
        {
            panelMaterial.SetTexture("_MainTex", texture);
        }
    }

    private static void ConfigurePanelMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", (float)CullMode.Off);
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 0.0f);
        }
    }
}
