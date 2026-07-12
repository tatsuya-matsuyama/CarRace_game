using UnityEngine;

/// <summary>
/// プリミティブを組み合わせ、チョロQ風の丸みを帯びた小型レーシングカーを作る見た目専用コンポーネントです。
/// 物理挙動は親ObjectのArcadeCarControllerとRigidbodyが担当します。
/// </summary>
public class ChoroQCarVisual : MonoBehaviour
{
    [Header("カラー")]
    [SerializeField] private Color bodyColor = new Color(0.96f, 0.9f, 0.72f);
    [SerializeField] private Color stripeColor = new Color(0.82f, 0.08f, 0.06f);

    private void Awake()
    {
        if (transform.Find("ChoroQVisual") != null)
        {
            return;
        }

        BuildVisual();
    }

    /// <summary>
    /// 車体、窓、ストライプ、タイヤ、ライト、リアスポイラーを子Objectとして組み立てます。
    /// </summary>
    private void BuildVisual()
    {
        Material bodyMaterial = CreateMaterial("Body", bodyColor);
        Material stripeMaterial = CreateMaterial("Stripe", stripeColor);
        Material darkMaterial = CreateMaterial("Dark", new Color(0.03f, 0.05f, 0.07f));
        Material tireMaterial = CreateMaterial("Tire", new Color(0.025f, 0.025f, 0.03f));
        Material hubMaterial = CreateMaterial("Hub", new Color(0.42f, 0.45f, 0.48f));
        Material lightMaterial = CreateMaterial("Light", new Color(1f, 0.84f, 0.35f));

        Renderer rootRenderer = GetComponent<Renderer>();
        if (rootRenderer != null)
        {
            rootRenderer.material = bodyMaterial;
        }

        GameObject root = new GameObject("ChoroQVisual");
        root.transform.SetParent(transform, false);

        CreatePart(root.transform, PrimitiveType.Cube, "RedLowerBody", new Vector3(0f, -0.24f, 0f), new Vector3(1.06f, 0.32f, 1.06f), stripeMaterial);
        CreatePart(root.transform, PrimitiveType.Cube, "Cabin", new Vector3(0f, 0.36f, -0.08f), new Vector3(0.76f, 0.66f, 0.55f), bodyMaterial);
        CreatePart(root.transform, PrimitiveType.Cube, "Windshield", new Vector3(0f, 0.48f, 0.23f), new Vector3(0.63f, 0.36f, 0.035f), darkMaterial);
        CreatePart(root.transform, PrimitiveType.Cube, "RearWindow", new Vector3(0f, 0.48f, -0.39f), new Vector3(0.63f, 0.34f, 0.035f), darkMaterial);

        CreatePart(root.transform, PrimitiveType.Cube, "HoodStripe", new Vector3(0f, 0.37f, 0.43f), new Vector3(0.17f, 0.025f, 0.56f), stripeMaterial);
        CreatePart(root.transform, PrimitiveType.Cube, "RoofStripe", new Vector3(0f, 0.7f, -0.08f), new Vector3(0.18f, 0.025f, 0.58f), stripeMaterial);
        CreatePart(root.transform, PrimitiveType.Cube, "Spoiler", new Vector3(0f, 0.54f, -0.63f), new Vector3(0.9f, 0.11f, 0.1f), bodyMaterial);

        CreatePart(root.transform, PrimitiveType.Sphere, "HeadlightLeft", new Vector3(-0.34f, 0.04f, 0.55f), new Vector3(0.2f, 0.2f, 0.1f), lightMaterial);
        CreatePart(root.transform, PrimitiveType.Sphere, "HeadlightRight", new Vector3(0.34f, 0.04f, 0.55f), new Vector3(0.2f, 0.2f, 0.1f), lightMaterial);
        CreatePart(root.transform, PrimitiveType.Cube, "FrontGrille", new Vector3(0f, -0.05f, 0.57f), new Vector3(0.42f, 0.16f, 0.04f), darkMaterial);

        CreateWheel(root.transform, "WheelFL", new Vector3(-0.56f, -0.32f, 0.43f), tireMaterial, hubMaterial);
        CreateWheel(root.transform, "WheelFR", new Vector3(0.56f, -0.32f, 0.43f), tireMaterial, hubMaterial);
        CreateWheel(root.transform, "WheelRL", new Vector3(-0.56f, -0.32f, -0.43f), tireMaterial, hubMaterial);
        CreateWheel(root.transform, "WheelRR", new Vector3(0.56f, -0.32f, -0.43f), tireMaterial, hubMaterial);
    }

    private static void CreateWheel(Transform parent, string partName, Vector3 position, Material tireMaterial, Material hubMaterial)
    {
        GameObject tire = CreatePart(parent, PrimitiveType.Cylinder, partName, position, new Vector3(0.28f, 0.12f, 0.28f), tireMaterial);
        tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        GameObject hub = CreatePart(tire.transform, PrimitiveType.Cylinder, "Hub", Vector3.zero, new Vector3(0.16f, 0.13f, 0.16f), hubMaterial);
        hub.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }

    private static GameObject CreatePart(Transform parent, PrimitiveType primitiveType, string partName, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().material = material;
        Collider partCollider = part.GetComponent<Collider>();
        if (partCollider != null)
        {
            Destroy(partCollider);
        }

        return part;
    }

    private static Material CreateMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader) { name = "ChoroQ_" + materialName };
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        return material;
    }

    /// <summary>
    /// ペイント屋から呼び出し、生成済みの車体とストライプの色を即時に変更します。
    /// 見た目専用のマテリアルだけを更新するため、車両の物理設定には影響しません。
    /// </summary>
    public void ApplyPaint(Color newBodyColor, Color newStripeColor)
    {
        bodyColor = newBodyColor;
        stripeColor = newStripeColor;

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            Material material = renderer.material;
            if (material.name.StartsWith("ChoroQ_Body"))
            {
                SetMaterialColor(material, bodyColor);
            }
            else if (material.name.StartsWith("ChoroQ_Stripe"))
            {
                SetMaterialColor(material, stripeColor);
            }
        }
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
    }
}
