using UnityEngine;
using UnityEngine.Rendering;

public class QuestSettingsDialog : MonoBehaviour
{
    public Vector3 localPosition = new Vector3(0.0f, -0.05f, 1.25f);
    public Vector2 sizeMeters = new Vector2(1.15f, 0.62f);
    public float characterSize = 0.015f;
    public int fontSize = 48;
    public Color backgroundColor = new Color(0.015f, 0.018f, 0.022f, 1.0f);
    public Color textColor = new Color(0.92f, 0.96f, 1.0f, 1.0f);

    private TextMesh textMesh;
    private Transform backgroundTransform;
    private Transform currentCamera;

    private void Awake()
    {
        EnsureDialog();
    }

    private void LateUpdate()
    {
        AttachToCamera();
    }

    public void SetVisible(bool visible)
    {
        EnsureDialog();
        textMesh.gameObject.SetActive(visible);
        backgroundTransform.gameObject.SetActive(visible);
    }

    public void SetText(string text)
    {
        EnsureDialog();
        textMesh.text = text;
    }

    private void EnsureDialog()
    {
        if (textMesh != null && backgroundTransform != null)
        {
            return;
        }

        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.name = "Quest Settings Dialog Background";

        Collider backgroundCollider = background.GetComponent<Collider>();
        if (backgroundCollider != null)
        {
            Destroy(backgroundCollider);
        }

        Renderer renderer = background.GetComponent<Renderer>();
        Material material = new Material(FindUnlitShader());
        ConfigureMaterial(material);
        SetMaterialColor(material, backgroundColor);
        renderer.sharedMaterial = material;
        backgroundTransform = background.transform;

        GameObject textObject = new GameObject("Quest Settings Dialog Text");
        textMesh = textObject.AddComponent<TextMesh>();
        textMesh.anchor = TextAnchor.UpperLeft;
        textMesh.alignment = TextAlignment.Left;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = characterSize;
        textMesh.color = textColor;

        SetVisible(false);
        AttachToCamera();
    }

    private void AttachToCamera()
    {
        if (textMesh == null || backgroundTransform == null)
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        if (currentCamera != camera.transform)
        {
            currentCamera = camera.transform;
            backgroundTransform.SetParent(currentCamera, false);
            textMesh.transform.SetParent(currentCamera, false);
        }

        backgroundTransform.localPosition = localPosition;
        backgroundTransform.localRotation = Quaternion.identity;
        backgroundTransform.localScale = new Vector3(sizeMeters.x, sizeMeters.y, 1.0f);

        textMesh.transform.localPosition = localPosition + new Vector3(
            -sizeMeters.x * 0.43f,
            sizeMeters.y * 0.38f,
            -0.015f);
        textMesh.transform.localRotation = Quaternion.identity;
        textMesh.transform.localScale = Vector3.one;
    }

    private static Shader FindUnlitShader()
    {
        Shader shader = Resources.Load<Shader>("Quest2SkillUnlitTexture");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Unlit/Color");
        if (shader != null)
        {
            return shader;
        }

        return Shader.Find("Standard");
    }

    private static void ConfigureMaterial(Material material)
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
}
