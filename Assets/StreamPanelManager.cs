using UnityEngine;
using UnityEngine.Rendering;

public class StreamPanelManager : MonoBehaviour
{
    public Vector3 localPosition = new Vector3(0.0f, 0.0f, 2.05f);
    public float panelWidthMeters = 2.2f;
    public float panelHeightMeters = 1.2375f;
    public Color placeholderColor = new Color(0.04f, 0.04f, 0.04f, 1.0f);

    private const string BootstrapName = "Quest2Skill WebRTC Stream Layer";

    private WebRTCStreamReceiver receiver;
    private Renderer panelRenderer;
    private Material panelMaterial;
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
    }

    private void LateUpdate()
    {
        AttachToCamera();
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
        ConfigurePanelMaterial();
        SetMaterialColor(placeholderColor);
        panelRenderer.sharedMaterial = panelMaterial;

        panel.transform.SetParent(transform, false);
        panel.transform.localPosition = Vector3.zero;
        panel.transform.localRotation = Quaternion.identity;
        panel.transform.localScale = new Vector3(panelWidthMeters, panelHeightMeters, 1.0f);

        AttachToCamera();
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
        if (panelMaterial == null)
        {
            return;
        }

        if (panelMaterial.HasProperty("_BaseColor"))
        {
            panelMaterial.SetColor("_BaseColor", color);
        }

        if (panelMaterial.HasProperty("_Color"))
        {
            panelMaterial.color = color;
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

    private void ConfigurePanelMaterial()
    {
        if (panelMaterial == null)
        {
            return;
        }

        if (panelMaterial.HasProperty("_Cull"))
        {
            panelMaterial.SetFloat("_Cull", (float)CullMode.Off);
        }

        if (panelMaterial.HasProperty("_Surface"))
        {
            panelMaterial.SetFloat("_Surface", 0.0f);
        }
    }
}
