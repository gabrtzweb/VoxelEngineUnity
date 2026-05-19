using UnityEngine;

namespace VoxelEngine
{
    [CreateAssetMenu(menuName = "VoxelEngine/BiomeDefinition", fileName = "NewBiome")]
    public class BiomeDefinition : ScriptableObject
    {
        public string biomeName = "New Biome";

        [Tooltip("How strongly this biome should affect the density field. Larger values produce taller, rougher terrain.")]
        public float terrainScale = 20f;

        // Simple color palette for quick visualization
        [Header("Preview Colors")]
        public Color32 topColor = new Color32(112, 150, 48, 255);
        public Color32 midColor = new Color32(97, 75, 66, 255);
        public Color32 deepColor = new Color32(150, 150, 150, 255);

        // Basic block mapping (used as a fallback until per-voxel biome ids are stored)
        [Header("Block Mapping")]
        public BlockData.BlockType topBlock = BlockData.BlockType.TerrGrass;
        public BlockData.BlockType midBlock = BlockData.BlockType.TerrDirt;
        public BlockData.BlockType deepBlock = BlockData.BlockType.RockStone;
    }
}
