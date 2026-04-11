using UnityEditor;
using UnityEngine;

public static class ApplyRoverCoursePalette
{
    private const string OutputFolder = "Assets/Levels/Controllers Trial/Generated/RoverCoursePalette";
    private const string TerrainBaseLayerPath = OutputFolder + "/TerrainBase_1E2C22.terrainlayer";
    private const string TerrainDarkLayerPath = OutputFolder + "/TerrainDark_112520.terrainlayer";
    private const string RoadSurfaceMaterialPath = OutputFolder + "/RoadSurface_9A9616.mat";
    private const string RoadEdgeMaterialPath = OutputFolder + "/RoadEdge_112520.mat";
    private const string RoadBaseMaterialPath = OutputFolder + "/RoadBase_1E2C22.mat";
    private const string WaterMaterialPath = OutputFolder + "/Water_112520.mat";
    private const string FinishAccentMaterialPath = OutputFolder + "/FinishAccent_9A9616.mat";
    private const string FinishDarkMaterialPath = OutputFolder + "/FinishDark_1E2C22.mat";
    private const string SourceGrassLayerPath = "Assets/Handpainted_Grass_and_Ground_Textures/Demo/terrain_layers/Grass_desatured_up.terrainlayer";
    private const string SourceDirtLayerPath = "Assets/Handpainted_Grass_and_Ground_Textures/Demo/terrain_layers/dirt_desatured_up.terrainlayer";
    private const string SourceRoadMaterialPath = "Assets/EasyRoads3D/Resources/Materials/roads/dirt material.mat";

    private static readonly Color DarkColor = ParseHexColor("112520");
    private static readonly Color OliveColor = ParseHexColor("9A9616");
    private static readonly Color MidColor = ParseHexColor("1E2C22");

    [MenuItem("Tools/Terrain/Apply Rover Course Palette")]
    public static void ApplyPalette()
    {
        GameObject course = GameObject.Find("TerrainRoverCourse");
        if (course == null)
        {
            Debug.LogError("Apply Rover Course Palette: could not find 'TerrainRoverCourse' in the active scene.");
            return;
        }

        EnsureFolder(OutputFolder);

        TerrainLayer sourceGrassLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(SourceGrassLayerPath);
        TerrainLayer sourceDirtLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(SourceDirtLayerPath);
        Material sourceRoadMaterial = AssetDatabase.LoadAssetAtPath<Material>(SourceRoadMaterialPath);

        if (sourceGrassLayer == null || sourceDirtLayer == null || sourceRoadMaterial == null)
        {
            Debug.LogError("Apply Rover Course Palette: missing source terrain layers or road material.");
            return;
        }

        TerrainLayer terrainBaseLayer = LoadOrCreateTerrainLayerVariant(
            TerrainBaseLayerPath,
            sourceGrassLayer,
            "Rover Terrain Base",
            DarkColor * 0.30f,
            MidColor * 0.95f);
        TerrainLayer terrainDarkLayer = LoadOrCreateTerrainLayerVariant(
            TerrainDarkLayerPath,
            sourceDirtLayer,
            "Rover Terrain Dark",
            DarkColor * 0.35f,
            OliveColor * 0.72f);

        Material roadSurfaceMaterial = LoadOrCreateMaterialVariant(RoadSurfaceMaterialPath, sourceRoadMaterial, "Rover Road Surface", OliveColor * 0.62f, 0.02f);
        Material roadEdgeMaterial = LoadOrCreateMaterialVariant(RoadEdgeMaterialPath, sourceRoadMaterial, "Rover Road Edge", DarkColor * 0.82f, 0.01f);
        Material roadBaseMaterial = LoadOrCreateMaterialVariant(RoadBaseMaterialPath, sourceRoadMaterial, "Rover Road Base", MidColor * 0.92f, 0.01f);
        Material waterMaterial = LoadOrCreateMaterial(WaterMaterialPath, "Rover Water", DarkColor, 0.08f);
        Material finishAccentMaterial = LoadOrCreateMaterial(FinishAccentMaterialPath, "Rover Finish Accent", OliveColor * 0.72f, 0.04f);
        Material finishDarkMaterial = LoadOrCreateMaterial(FinishDarkMaterialPath, "Rover Finish Dark", MidColor, 0.02f);

        ApplyTerrainLayers(course.transform, terrainBaseLayer, terrainDarkLayer);
        ApplyMaterials(course.transform, roadSurfaceMaterial, roadEdgeMaterial, roadBaseMaterial, waterMaterial, finishAccentMaterial, finishDarkMaterial);

        EditorUtility.SetDirty(course);
        AssetDatabase.SaveAssets();

        Debug.Log("Apply Rover Course Palette: applied palette 112520 / 9A9616 / 1E2C22 to the active rover course scene.");
    }

    private static void ApplyTerrainLayers(Transform course, TerrainLayer baseLayer, TerrainLayer darkLayer)
    {
        Terrain terrain = course.GetComponentInChildren<Terrain>(true);
        if (terrain == null || terrain.terrainData == null)
        {
            return;
        }

        Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Apply Rover Course Palette");
        terrain.terrainData.terrainLayers = new[] { baseLayer, darkLayer };
        EditorUtility.SetDirty(terrain.terrainData);
        EditorUtility.SetDirty(terrain);
    }

    private static void ApplyMaterials(
        Transform course,
        Material roadSurfaceMaterial,
        Material roadEdgeMaterial,
        Material roadBaseMaterial,
        Material waterMaterial,
        Material finishAccentMaterial,
        Material finishDarkMaterial)
    {
        Transform roadRoot = course.Find("RoverRoad");
        Transform roadEdgesRoot = course.Find("RoverRoad_Edges");
        Transform roadBaseRoot = course.Find("RoverRoad_Base");
        Transform waterRoot = course.Find("Water");
        Transform finishRoot = course.Find("FinishSummit");

        ApplyMaterialRecursive(roadRoot, roadSurfaceMaterial);
        ApplyMaterialRecursive(roadEdgesRoot, roadEdgeMaterial);
        ApplyMaterialRecursive(roadBaseRoot, roadBaseMaterial);
        ApplyMaterialRecursive(waterRoot, waterMaterial);

        if (finishRoot != null)
        {
            foreach (Renderer renderer in finishRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.gameObject.name.Contains("Pad") || renderer.gameObject.name.Contains("Bar"))
                {
                    AssignSharedMaterial(renderer, finishAccentMaterial);
                }
                else
                {
                    AssignSharedMaterial(renderer, finishDarkMaterial);
                }
            }
        }
    }

    private static void ApplyMaterialRecursive(Transform root, Material material)
    {
        if (root == null || material == null)
        {
            return;
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            AssignSharedMaterial(renderer, material);
        }
    }

    private static void AssignSharedMaterial(Renderer renderer, Material material)
    {
        if (renderer == null || material == null)
        {
            return;
        }

        Undo.RecordObject(renderer, "Apply Rover Course Palette");
        renderer.sharedMaterial = material;
        EditorUtility.SetDirty(renderer);
    }

    private static Material LoadOrCreateMaterialVariant(string path, Material sourceMaterial, string materialName, Color color, float emissionStrength)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(sourceMaterial);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = sourceMaterial.shader;
            material.CopyPropertiesFromMaterial(sourceMaterial);
        }

        material.name = materialName;
        ApplyMaterialColor(material, color, emissionStrength);
        return material;
    }

    private static Material LoadOrCreateMaterial(string path, string materialName, Color color, float emissionStrength)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.name = materialName;
        ApplyMaterialColor(material, color, emissionStrength);
        return material;
    }

    private static void ApplyMaterialColor(Material material, Color color, float emissionStrength)
    {
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emissionStrength);
        }

        EditorUtility.SetDirty(material);
    }

    private static TerrainLayer LoadOrCreateTerrainLayerVariant(string layerPath, TerrainLayer sourceLayer, string layerName, Color remapMin, Color remapMax)
    {
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
        if (layer == null)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, layerPath);
        }

        layer.name = layerName;
        layer.diffuseTexture = sourceLayer.diffuseTexture;
        layer.normalMapTexture = sourceLayer.normalMapTexture;
        layer.maskMapTexture = sourceLayer.maskMapTexture;
        layer.tileSize = sourceLayer.tileSize;
        layer.tileOffset = sourceLayer.tileOffset;
        layer.specular = sourceLayer.specular;
        layer.metallic = sourceLayer.metallic;
        layer.smoothness = sourceLayer.smoothness;
        layer.normalScale = sourceLayer.normalScale;
        layer.diffuseRemapMin = new Vector4(remapMin.r, remapMin.g, remapMin.b, 0f);
        layer.diffuseRemapMax = new Vector4(remapMax.r, remapMax.g, remapMax.b, 1f);
        EditorUtility.SetDirty(layer);
        return layer;
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static Color ParseHexColor(string value)
    {
        if (ColorUtility.TryParseHtmlString("#" + value, out Color color))
        {
            return color;
        }

        return Color.magenta;
    }
}
