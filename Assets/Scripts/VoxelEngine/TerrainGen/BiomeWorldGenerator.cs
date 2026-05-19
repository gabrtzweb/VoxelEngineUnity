using UnityEngine;

namespace VoxelEngine
{
    // SIMD-first biome-capable generator (initial skeleton)
    public class BiomeWorldGenerator : TerrainGeneratorBase
    {
        [System.Serializable]
        public struct BiomeSource
        {
            public BiomeDefinition biome;
            public FastNoiseSIMDUnity terrainNoise;
        }

        [Header("Biome selector")]
        public FastNoiseSIMDUnity biomeSelector;
        [Range(1, 4)]
        public int biomeSelectorInterpBitStep = 3;
        [Range(0f, 0.5f)]
        public float biomeSelectorBias = 0.08f;
        public BiomeSource[] biomeSources = new BiomeSource[0];

        [Header("Caves")]
        public FastNoiseSIMDUnity caveNoise;
        public float caveRatio = 0.88f;

        void Reset()
        {
            // sensible default vertical bounds for now
            minHeight = -256f;
            maxHeight = 256f;
        }

        public override void Awake()
        {
            SetInterpBitStep(1);
            // clamp vertical bounds as requested
            minHeight = -256f;
            maxHeight = 256f;
            base.Awake();
        }

        protected float[] GetNoiseSet(FastNoiseSIMDUnity noise, Vector3i chunkPos)
        {
            return GetNoiseSet(noise, chunkPos, interpBitStep);
        }

        protected float[] GetNoiseSet(FastNoiseSIMDUnity noise, Vector3i chunkPos, int bitStep)
        {
            int offsetShift = Chunk.BIT_SIZE - bitStep;
            int size = (Chunk.SIZE >> bitStep) + 1;
            return noise.fastNoiseSIMD.GetNoiseSet(chunkPos.x << offsetShift,
                chunkPos.y << offsetShift, chunkPos.z << offsetShift, size, size, size, 1 << bitStep);
        }

        private static float VoxelInterpLookup(int localX, int localY, int localZ, float[] interpLookup, int interpSize, int interpBitStep)
        {
            float scale = 1f / (1 << interpBitStep);
            float xs = (localX + 0.5f) * scale;
            float ys = (localY + 0.5f) * scale;
            float zs = (localZ + 0.5f) * scale;

            int x0 = Mathf.FloorToInt(xs);
            int y0 = Mathf.FloorToInt(ys);
            int z0 = Mathf.FloorToInt(zs);

            xs -= x0;
            ys -= y0;
            zs -= z0;

            int interpSizeSq = interpSize * interpSize;
            int lookupIndex = z0 + y0 * interpSize + x0 * interpSizeSq;

            float x00 = Mathf.Lerp(interpLookup[lookupIndex], interpLookup[lookupIndex + interpSizeSq], xs);
            float x10 = Mathf.Lerp(interpLookup[lookupIndex + interpSize], interpLookup[lookupIndex + interpSizeSq + interpSize], xs);
            float x01 = Mathf.Lerp(interpLookup[++lookupIndex], interpLookup[lookupIndex + interpSizeSq], xs);
            float x11 = Mathf.Lerp(interpLookup[lookupIndex + interpSize], interpLookup[lookupIndex + interpSizeSq + interpSize], xs);

            return Mathf.Lerp(Mathf.Lerp(x00, x10, ys), Mathf.Lerp(x01, x11, ys), zs);
        }

        public override void GenerateChunk(Chunk chunk)
        {
            Voxel[] voxelData = chunk.voxelData;
            byte[] biomeData = chunk.biomeData;

            int yOffset = chunk.chunkPos.y << Chunk.BIT_SIZE;

            // Simple fallback: if no biomes, flood the chunk with Empty voxels
            if (biomeSources == null || biomeSources.Length == 0)
            {
                chunk.fillType = Chunk.FillType.Null;
                TerrainGeneratorBase.ChunkFillUpdate(chunk, Voxel.Empty);

                for (int i = 0; i < Chunk.SIZE * Chunk.SIZE * Chunk.SIZE; i++)
                {
                    voxelData[i] = Voxel.Empty;
                    biomeData[i] = 0;
                }

                return;
            }

            // Get selector and terrain noise(s)
            float[] selector = biomeSelector ? GetNoiseSet(biomeSelector, chunk.chunkPos, biomeSelectorInterpBitStep) : null;
            int selectorInterpSize = selector != null ? ((Chunk.SIZE >> biomeSelectorInterpBitStep) + 1) : 0;
            float[] noiseA = biomeSources[0].terrainNoise ? GetNoiseSet(biomeSources[0].terrainNoise, chunk.chunkPos) : null;
            float[] noiseB = (biomeSources.Length > 1 && biomeSources[1].terrainNoise) ? GetNoiseSet(biomeSources[1].terrainNoise, chunk.chunkPos) : null;

            if (noiseA == null && noiseB == null)
            {
                chunk.fillType = Chunk.FillType.Null;
                TerrainGeneratorBase.ChunkFillUpdate(chunk, Voxel.Empty);

                for (int i = 0; i < Chunk.SIZE * Chunk.SIZE * Chunk.SIZE; i++)
                {
                    voxelData[i] = Voxel.Empty;
                    biomeData[i] = 0;
                }

                return;
            }

            // prepare an interp lookup for surface terrain (3D samples)
            float[] interpLookup = new float[interpSize * interpSize * interpSize];

            int index = 0;

            float selectorBias = biomeSelectorBias;

            for (int x = 0; x < interpSize; x++)
            {
                for (int y = 0; y < interpSize; y++)
                {
                    float yf = (y << interpBitStep) + yOffset;

                    for (int z = 0; z < interpSize; z++)
                    {
                        float a = noiseA != null ? noiseA[index] * biomeSources[0].biome.terrainScale : (noiseB != null ? noiseB[index] * biomeSources[1].biome.terrainScale : 0f);
                        float b = noiseB != null ? noiseB[index] * biomeSources[1].biome.terrainScale : a;

                        float mix = 0f;
                        if (selector != null)
                        {
                            float selectorValue = VoxelInterpLookup(x, y, z, selector, selectorInterpSize, biomeSelectorInterpBitStep);
                            // Bias the selector slightly toward the second biome without creating hard grid boundaries.
                            mix = Mathf.Clamp01(((selectorValue + 1f) * 0.5f) + selectorBias);
                        }

                        float value = Mathf.Lerp(a, b, mix);
                        // Depth falloff keeps the surface from turning into a flat slab while still allowing overhangs.
                        value -= yf;

                        // Add a subtle self-warp from the selector to make the 3D shape less planar.
                        if (selector != null)
                            value += VoxelInterpLookup(x, y, z, selector, selectorInterpSize, biomeSelectorInterpBitStep) * 2f;

                        interpLookup[index++] = value;
                    }
                }
            }

            // Get cave set and apply same interpolation size
            float[] caveSet = caveNoise ? CaveModule.GenerateCaveNoiseSet(caveNoise, chunk.chunkPos, interpSize, interpBitStep, caveRatio) : null;

            // Fill the chunk voxels using the final blended surface and cave carve
            index = 0;

            for (int x = 0; x < Chunk.SIZE; x++)
            {
                for (int y = 0; y < Chunk.SIZE; y++)
                {
                    for (int z = 0; z < Chunk.SIZE; z++)
                    {
                        float surfaceDensity = VoxelInterpLookup(x, y, z, interpLookup);
                        float caveDensity = caveSet != null ? VoxelInterpLookup(x, y, z, caveSet) : float.MaxValue;

                        // carve caves out: choose the minimum between surface and cave fields
                        float finalDensity = Mathf.Min(surfaceDensity, caveDensity);

                        // Sample the biome selector using the same interpolation lookup to avoid
                        // chunk-aligned stepping and ensure smooth, larger biome regions.
                        float selectorValue = 0f;
                        if (selector != null)
							selectorValue = VoxelInterpLookup(x, y, z, selector, selectorInterpSize, biomeSelectorInterpBitStep);

                        byte biomeIndex = ChooseBiomeIndex(selectorValue);

                        chunk.surfaceDensityData[index] = surfaceDensity;
                        ChunkFillUpdate(chunk, voxelData[index] = new Voxel(finalDensity));
                        biomeData[index] = biomeIndex;
                        index++;
                    }
                }
            }
        }

        private byte ChooseBiomeIndex(float selectorValue)
        {
            if (biomeSources == null || biomeSources.Length == 0)
                return 0;

            if (biomeSources.Length == 1)
                return 0;

            float t = Mathf.Clamp01(((selectorValue + 1f) * 0.5f) + biomeSelectorBias);
            return (byte)(t >= 0.5f ? 1 : 0);
        }

        // Simple color mapping: sample first biome palette as fallback
        public override Color32 DensityColor(Voxel voxel)
        {
            if (biomeSources != null && biomeSources.Length > 0 && biomeSources[0].biome != null)
            {
                if (voxel.density < 2f)
                    return biomeSources[0].biome.topColor;
                if (voxel.density < 6f)
                    return biomeSources[0].biome.midColor;
                return biomeSources[0].biome.deepColor;
            }

            return new Color32(150, 150, 150, 255);
        }

        public override BlockData.BlockType DensityBlock(Voxel voxel)
        {
            return DensityBlock(voxel, 0);
        }

        public override BlockData.BlockType DensityBlock(Voxel voxel, byte biomeIndex)
        {
            return DensityBlock(voxel, biomeIndex, voxel.density);
        }

        public override BlockData.BlockType DensityBlock(Voxel voxel, byte biomeIndex, float surfaceDensity)
        {
            BiomeDefinition biome = GetBiomeDefinition(biomeIndex);

            if (biome != null)
            {
                if (surfaceDensity < 2f)
                    return biome.topBlock;
                if (surfaceDensity < 6f)
                    return biome.midBlock;
                return biome.deepBlock;
            }

            if (surfaceDensity < 2f)
                return BlockData.BlockType.TerrGrass;
            if (surfaceDensity < 6f)
                return BlockData.BlockType.TerrDirt;
            return BlockData.BlockType.RockStone;
        }

        private BiomeDefinition GetBiomeDefinition(byte biomeIndex)
        {
            if (biomeSources == null || biomeSources.Length == 0)
                return null;

            int index = Mathf.Clamp(biomeIndex, (byte)0, (byte)(biomeSources.Length - 1));
            return biomeSources[index].biome;
        }
    }
}
