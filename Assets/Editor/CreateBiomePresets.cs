using UnityEditor;
using UnityEngine;

namespace VoxelEngine.Editor
{
    public static class CreateBiomePresets
    {
        [MenuItem("VoxelEngine/Create Biome Presets")]
        public static void CreatePresets()
        {
            string dir = "Assets/Data";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets", "Data");

            CreateBiome("Ocean", 5f, new Color32(60, 100, 160, 255), new Color32(40, 80, 140, 255), new Color32(20, 60, 120, 255),
                BlockData.BlockType.TerrSand, BlockData.BlockType.TerrSand, BlockData.BlockType.TerrSand, new Vector3(0.5f, 0.5f, 0f));

            CreateBiome("Beach", 6f, new Color32(240, 220, 140, 255), new Color32(230, 200, 120, 255), new Color32(200, 180, 100, 255),
                BlockData.BlockType.TerrSand, BlockData.BlockType.TerrSand, BlockData.BlockType.TerrSand, new Vector3(0.6f, 0.6f, 0.08f));

            CreateBiome("Mountains", 40f, new Color32(120, 120, 120, 255), new Color32(100, 100, 100, 255), new Color32(80, 80, 80, 255),
                BlockData.BlockType.RockStone, BlockData.BlockType.RockStone, BlockData.BlockType.RockCore, new Vector3(0.3f, 0.4f, 0.9f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateBiome(string name, float terrainScale, Color32 top, Color32 mid, Color32 deep,
            BlockData.BlockType topBlock, BlockData.BlockType midBlock, BlockData.BlockType deepBlock, Vector3 pref)
        {
            var asset = ScriptableObject.CreateInstance<VoxelEngine.BiomeDefinition>();
            asset.biomeName = name;
            asset.terrainScale = terrainScale;
            asset.topColor = top;
            asset.midColor = mid;
            asset.deepColor = deep;
            asset.topBlock = topBlock;
            asset.midBlock = midBlock;
            asset.deepBlock = deepBlock;

            string path = $"Assets/Data/{name}.asset";
            AssetDatabase.CreateAsset(asset, path);

            // Set climate pref on matching BiomeSource is manual in inspector
        }
    }
}
