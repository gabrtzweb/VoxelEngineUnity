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
            // Preferred climate point: x=temperature (0..1), y=humidity (0..1), z=elevation (0..1)
            public Vector3 climatePref;
        }

        [Header("Biome selector")]
        public FastNoiseSIMDUnity biomeSelector;
        [Range(1, 4)]
        public int biomeSelectorInterpBitStep = 3;
        [Range(0f, 0.5f)]
        public float biomeSelectorBias = 0.08f;
        [Range(0f, 0.49f)]
        public float biomeBlendSoftness = 0.25f;
        [Header("Climate selectors")]
        public FastNoiseSIMDUnity temperatureNoise;
        public FastNoiseSIMDUnity humidityNoise;
        [Header("Climate weights")]
        [Range(0f, 2f)]
        public float tempWeight = 1f;
        [Range(0f, 2f)]
        public float humWeight = 1f;
        [Range(0f, 2f)]
        public float elevWeight = 1f;
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

                        // Sample climate selectors (low-frequency) and the biome selector if present
                        float selectorValue = 0f;
                        if (selector != null)
                            selectorValue = VoxelInterpLookup(x, y, z, selector, selectorInterpSize, biomeSelectorInterpBitStep);

                        float tempVal = 0.5f;
                        float humVal = 0.5f;
                        if (temperatureNoise != null)
                            tempVal = Mathf.Clamp01((VoxelInterpLookup(x, y, z, GetNoiseSet(temperatureNoise, chunk.chunkPos, biomeSelectorInterpBitStep), selectorInterpSize, biomeSelectorInterpBitStep) + 1f) * 0.5f);
                        if (humidityNoise != null)
                            humVal = Mathf.Clamp01((VoxelInterpLookup(x, y, z, GetNoiseSet(humidityNoise, chunk.chunkPos, biomeSelectorInterpBitStep), selectorInterpSize, biomeSelectorInterpBitStep) + 1f) * 0.5f);

                        float elevNorm = Mathf.Clamp01(Mathf.InverseLerp(minHeight, maxHeight, surfaceDensity));

                        byte biomeIndex = ChooseBiomeIndex(tempVal, humVal, elevNorm, selectorValue);

                        // Sea level at world Y = 0: if the voxel is empty (finalDensity < 0)
                        // and below or at sea level, fill with water block (simple solid water block)
                        int worldY = (chunk.chunkPos.y << Chunk.BIT_SIZE) + y;

                        if (finalDensity < 0f && worldY <= 0)
                        {
                            // mark as filled with water: solid voxel for rendering and special biome index 255
                            chunk.surfaceDensityData[index] = surfaceDensity;
                            ChunkFillUpdate(chunk, voxelData[index] = Voxel.Solid);
                            biomeData[index] = 255;
                        }
                        else
                        {
                            // Beach rule: if voxel is near sea level and a 'Beach' biome exists, prefer it
                            int beachIndex = -1;
                            for (int bi = 0; bi < biomeSources.Length; bi++)
                            {
                                if (biomeSources[bi].biome != null && biomeSources[bi].biome.biomeName.ToLower().Contains("beach"))
                                {
                                    beachIndex = bi;
                                    break;
                                }
                            }

                            if (beachIndex >= 0 && worldY <= 1 && worldY >= -2)
                                biomeIndex = (byte)beachIndex;

                            chunk.surfaceDensityData[index] = surfaceDensity;
                            ChunkFillUpdate(chunk, voxelData[index] = new Voxel(finalDensity));
                            biomeData[index] = biomeIndex;
                        }
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

            int n = biomeSources.Length;
            float t = Mathf.Clamp01(((selectorValue + 1f) * 0.5f) + biomeSelectorBias);

            // Map t into an index range [0, n-1] and pick the nearest biome index
            float pos = t * (n - 1);
            int low = Mathf.FloorToInt(pos);
            int high = Mathf.Clamp(low + 1, 0, n - 1);
            float frac = pos - low;

            float softness = Mathf.Clamp(biomeBlendSoftness, 0f, 0.49f);
            float center = 0.5f;

            if (frac < center - softness)
                return (byte)Mathf.Clamp(low, 0, n - 1);

            if (frac > center + softness)
                return (byte)Mathf.Clamp(high, 0, n - 1);

            // Within the soft band: pick the nearest side (this keeps storage simple — a single index).
            return (byte)((frac < center) ? Mathf.Clamp(low, 0, n - 1) : Mathf.Clamp(high, 0, n - 1));
        }

        // Climate-based selection: temperature (0..1), humidity (0..1), elevation (0..1)
        private byte ChooseBiomeIndex(float temperature, float humidity, float elevation, float selectorValue)
        {
            if (biomeSources == null || biomeSources.Length == 0)
                return 0;

            int n = biomeSources.Length;
            if (n == 1)
                return 0;

            float bestDist = float.MaxValue;
            int bestIdx = 0;

            for (int i = 0; i < n; i++)
            {
                Vector3 pref = biomeSources[i].climatePref;
                float dt = temperature - pref.x;
                float dh = humidity - pref.y;
                float de = elevation - pref.z;

                float dist = tempWeight * dt * dt + humWeight * dh * dh + elevWeight * de * de;

                // Optionally bias using the old selector value so designer can nudge transitions
                dist -= selectorValue * 0.001f;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            return (byte)Mathf.Clamp(bestIdx, 0, n - 1);
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
            // special water index
            if (biomeIndex == 255)
                return BlockData.BlockType.LiquidWater;

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
