using UnityEngine;

namespace VoxelEngine
{
    // Small helper to expose the existing cave math as a reusable module (SIMD-only)
    public static class CaveModule
    {
        // Mirrors the transform used in TerrainGeneratorSIMD.GenerateCaves
        public static float[] GenerateCaveNoiseSet(FastNoiseSIMDUnity caveNoise, Vector3i chunkPos, int interpSize, int interpBitStep, float caveRatio)
        {
            int offsetShift = Chunk.BIT_SIZE - interpBitStep;

            float[] caveSet = caveNoise.fastNoiseSIMD.GetNoiseSet(chunkPos.x << offsetShift,
                chunkPos.y << offsetShift, chunkPos.z << offsetShift, interpSize, interpSize, interpSize, 1 << interpBitStep);

            for (int i = 0; i < caveSet.Length; i++)
                caveSet[i] = (caveRatio - caveSet[i]) * 32f;

            return caveSet;
        }
    }
}
