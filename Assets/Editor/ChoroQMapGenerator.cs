using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// チョロQ風の箱庭都市を、Inspector風のウィンドウ操作で生成するEditor拡張です。
/// 実行時ビルドには含まれず、Unity Editor上だけで利用されます。
/// </summary>
public class ChoroQMapGenerator : EditorWindow
{
    private const string MapRootName = "CityMap";
    private const string MaterialsFolder = "Assets/Generated/MapMaterials";

    [SerializeField] private float mapSize = 150f;
    [SerializeField] private int gridSize = 8;
    [SerializeField] private float roadWidth = 8f;
    [SerializeField] private int randomSeed = 12345;

    private readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

    [MenuItem("Tools/Choro-Q Map Generator")]
    public static void ShowWindow()
    {
        GetWindow<ChoroQMapGenerator>("マップ生成ツール");
    }

    private void OnGUI()
    {
        GUILayout.Label("チョロQ風 街マップ自動生成", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "道路・区画・建物・物理障害物・外壁をCityMapとして生成します。\n" +
            "同じSeedなら、いつでも同じ街を作り直せます。",
            MessageType.Info);

        mapSize = EditorGUILayout.FloatField("マップ全体のサイズ", mapSize);
        gridSize = EditorGUILayout.IntField("1辺の区画数", gridSize);
        roadWidth = EditorGUILayout.FloatField("道路の幅", roadWidth);
        randomSeed = EditorGUILayout.IntField("ランダムSeed", randomSeed);

        GUILayout.Space(10f);

        using (new EditorGUI.DisabledScope(!CanGenerate()))
        {
            if (GUILayout.Button("街マップを生成する", GUILayout.Height(40f)))
            {
                GenerateMap();
            }
        }

        if (!CanGenerate())
        {
            EditorGUILayout.HelpBox("マップサイズ、区画数、道路幅を有効な範囲に設定してください。", MessageType.Warning);
        }
    }

    /// <summary>
    /// 設定値を検証してから、既存のCityMapを置き換え、新しい街を生成します。
    /// </summary>
    private void GenerateMap()
    {
        GameObject existingMap = GameObject.Find(MapRootName);
        if (existingMap != null)
        {
            Undo.DestroyObjectImmediate(existingMap);
        }

        Random.InitState(randomSeed);
        materialCache.Clear();

        GameObject mapRoot = new GameObject(MapRootName);
        Undo.RegisterCreatedObjectUndo(mapRoot, "Generate Choro-Q City Map");

        CreateGroundAndRoads(mapRoot.transform);
        CreateBoundaryWalls(mapRoot.transform);
        CreateBlocks(mapRoot.transform);

        Selection.activeGameObject = mapRoot;
        EditorSceneManager.MarkSceneDirty(mapRoot.scene);
        Debug.Log("チョロQ風の街マップを生成しました。Seed: " + randomSeed);
    }

    /// <summary>
    /// 草地の土台、アスファルト道路、区画ごとの敷地を生成します。
    /// </summary>
    private void CreateGroundAndRoads(Transform parent)
    {
        CreateCube(parent, "CityGround", new Vector3(0f, -0.5f, 0f), new Vector3(mapSize, 1f, mapSize),
            GetOrCreateMaterial("Grass", new Color(0.12f, 0.36f, 0.14f)));

        // 道路は全体へ敷き、その上に各区画の敷地を置くことで格子状に見せます。
        CreateCube(parent, "RoadSurface", new Vector3(0f, 0.02f, 0f), new Vector3(mapSize, 0.04f, mapSize),
            GetOrCreateMaterial("Road", new Color(0.12f, 0.13f, 0.16f)));
    }

    /// <summary>
    /// マップ外へ出られないように、4方向の見える境界壁を配置します。
    /// </summary>
    private void CreateBoundaryWalls(Transform parent)
    {
        const float wallHeight = 12f;
        const float wallThickness = 2f;
        Material wallMaterial = GetOrCreateMaterial("BoundaryWall", new Color(0.22f, 0.46f, 0.66f));

        CreateCube(parent, "Wall_North", new Vector3(0f, wallHeight * 0.5f, mapSize * 0.5f), new Vector3(mapSize, wallHeight, wallThickness), wallMaterial);
        CreateCube(parent, "Wall_South", new Vector3(0f, wallHeight * 0.5f, -mapSize * 0.5f), new Vector3(mapSize, wallHeight, wallThickness), wallMaterial);
        CreateCube(parent, "Wall_East", new Vector3(mapSize * 0.5f, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, mapSize), wallMaterial);
        CreateCube(parent, "Wall_West", new Vector3(-mapSize * 0.5f, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, mapSize), wallMaterial);
    }

    /// <summary>
    /// 格子状の区画へ建物、障害物、空き地をランダムに配置します。
    /// 中央区画はプレイヤーやNPCを置ける広場として空けます。
    /// </summary>
    private void CreateBlocks(Transform parent)
    {
        float cellSize = mapSize / gridSize;
        float lotSize = cellSize - roadWidth;
        Material lotMaterial = GetOrCreateMaterial("Lot", new Color(0.2f, 0.52f, 0.22f));

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                Vector3 blockCenter = new Vector3(
                    -mapSize * 0.5f + cellSize * (x + 0.5f),
                    0.08f,
                    -mapSize * 0.5f + cellSize * (z + 0.5f));

                bool isCentralPlaza = x == gridSize / 2 && z == gridSize / 2;
                CreateCube(parent, isCentralPlaza ? "CentralPlaza" : "Lot", blockCenter,
                    new Vector3(lotSize, 0.12f, lotSize),
                    isCentralPlaza
                        ? GetOrCreateMaterial("Plaza", new Color(0.72f, 0.65f, 0.48f))
                        : lotMaterial);

                if (isCentralPlaza)
                {
                    continue;
                }

                float randomValue = Random.value;
                if (randomValue < 0.65f)
                {
                    CreateBuilding(parent, blockCenter, lotSize);
                }
                else if (randomValue < 0.85f)
                {
                    CreateObstacle(parent, blockCenter, lotSize);
                }
            }
        }
    }

    /// <summary>
    /// パステルカラーとランダムなサイズを使い、箱庭らしい建物を配置します。
    /// </summary>
    private void CreateBuilding(Transform parent, Vector3 blockCenter, float lotSize)
    {
        float height = Random.Range(5f, 22f);
        float width = lotSize * Random.Range(0.55f, 0.9f);
        float depth = lotSize * Random.Range(0.55f, 0.9f);
        Vector3 position = blockCenter + new Vector3(
            Random.Range(-lotSize * 0.1f, lotSize * 0.1f),
            height * 0.5f,
            Random.Range(-lotSize * 0.1f, lotSize * 0.1f));

        Color color = Color.HSVToRGB(Random.value, Random.Range(0.25f, 0.5f), Random.Range(0.72f, 1f));
        Material material = GetOrCreateMaterial("Building_" + ColorUtility.ToHtmlStringRGB(color), color);
        CreateCube(parent, "Building", position, new Vector3(width, height, depth), material);
    }

    /// <summary>
    /// 車が衝突すると転がる、または吹き飛ぶ物理障害物を生成します。
    /// </summary>
    private void CreateObstacle(Transform parent, Vector3 blockCenter, float lotSize)
    {
        bool isSphere = Random.value > 0.5f;
        float size = Random.Range(1.5f, 3f);

        GameObject obstacle = GameObject.CreatePrimitive(isSphere ? PrimitiveType.Sphere : PrimitiveType.Cube);
        obstacle.name = isSphere ? "Obstacle_Ball" : "Obstacle_Box";
        obstacle.transform.SetParent(parent);
        obstacle.transform.position = blockCenter + new Vector3(
            Random.Range(-lotSize * 0.25f, lotSize * 0.25f),
            size * 0.5f,
            Random.Range(-lotSize * 0.25f, lotSize * 0.25f));
        obstacle.transform.localScale = Vector3.one * size;
        obstacle.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Obstacle", new Color(1f, 0.68f, 0.08f));

        Rigidbody rigidbody = obstacle.AddComponent<Rigidbody>();
        rigidbody.mass = 2f;
        rigidbody.linearDamping = 1f;
        Undo.RegisterCreatedObjectUndo(obstacle, "Generate Map Obstacle");
    }

    /// <summary>
    /// CubeとColliderを作り、指定されたマテリアルを個別に割り当てます。
    /// </summary>
    private static GameObject CreateCube(Transform parent, string objectName, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(parent);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        Undo.RegisterCreatedObjectUndo(cube, "Generate City Map Object");
        return cube;
    }

    /// <summary>
    /// URP対応マテリアルをアセットとして作成・再利用します。
    /// 共通のDefault-Materialを変更しないため、地面と車両の色が同化しません。
    /// </summary>
    private Material GetOrCreateMaterial(string materialName, Color color)
    {
        if (materialCache.TryGetValue(materialName, out Material cachedMaterial))
        {
            return cachedMaterial;
        }

        EnsureMaterialsFolder();
        string path = MaterialsFolder + "/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        EditorUtility.SetDirty(material);
        materialCache.Add(materialName, material);
        return material;
    }

    /// <summary>
    /// 生成したマテリアルを保存するフォルダを必要に応じて作成します。
    /// </summary>
    private static void EnsureMaterialsFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Generated"))
        {
            AssetDatabase.CreateFolder("Assets", "Generated");
        }

        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
        {
            AssetDatabase.CreateFolder("Assets/Generated", "MapMaterials");
        }
    }

    private bool CanGenerate()
    {
        return mapSize > 20f && gridSize >= 2 && roadWidth > 0f && roadWidth < mapSize / gridSize;
    }
}
