using UnityEngine;

namespace VoxelEngine
{
	public class TerrainGeneratorSIMD_World : TerrainGeneratorSIMD
	{
		public float grassTerrainScale = 20f;
		public float desertTerrainScale = 20f;

		public float caveRatio = .88f;
		public float caveStartDepth = 4f;
		public float caveEndDepth = 64f;
		public float caveFadeDepth = 16f;

		public float biomeTransitionWidth = 0.1f;
		public float seaLevel = 0f;

		public override void Awake()
		{
			SetInterpBitStep(2);
			SetNoiseArraySize(4);
			EnsureNoiseComponents();
			ConfigureNoiseComponents();

			minHeight = Mathf.Min(-grassTerrainScale, -desertTerrainScale)
				- caveEndDepth - caveFadeDepth;
			maxHeight = Mathf.Max(
				grassTerrainScale,
				desertTerrainScale,
				seaLevel);
		}

		public override void GenerateChunk(Chunk chunk)
		{
			Voxel[] voxelData = chunk.voxelData;
			int yOffset = chunk.chunkPos.y << Chunk.BIT_SIZE;

			float[] worldNoise = GetSurfaceNoise(0, chunk.chunkPos);
			float[] grassNoise = GetSurfaceNoise(1, chunk.chunkPos);
			float[] desertNoise = GetSurfaceNoise(2, chunk.chunkPos);
			float[] caveNoise = GetInterpNoise(3, chunk.chunkPos);

			float[] surfaceHeights = new float[interpSize * interpSize];
			float[] biomeMixes = new float[interpSize * interpSize];
			int surfaceIndex = 0;

			for (int x = 0; x < interpSize; x++)
			{
				for (int z = 0; z < interpSize; z++)
				{
					float biomeMix = GetBiomeMix(worldNoise[surfaceIndex]);
					biomeMixes[surfaceIndex] = biomeMix;

					float grassHeight = grassNoise[surfaceIndex] * grassTerrainScale;

					float desertHeight = desertNoise[surfaceIndex] * desertTerrainScale;

					surfaceHeights[surfaceIndex++] = Mathf.Lerp(grassHeight, desertHeight, biomeMix);
				}
			}

			int voxelIndex = 0;
			for (int x = 0; x < Chunk.SIZE; x++)
			{
				for (int y = 0; y < Chunk.SIZE; y++)
				{
					float worldY = yOffset + y;

					for (int z = 0; z < Chunk.SIZE; z++)
					{
						float surfaceHeight = BilinearSurfaceLookup(x, z, surfaceHeights);
						float biomeMix = BilinearSurfaceLookup(x, z, biomeMixes);
						float density = surfaceHeight - worldY;
						Voxel.BlockType blockType = GetBlockType(density, biomeMix);

						if (density > caveStartDepth)
						{
							float caveValue = VoxelInterpLookup(x, y, z, caveNoise);
							float caveDensity = (caveRatio - caveValue) * 32f;
							float caveMix = GetCaveMix(density);
							bool carved = caveMix > 0f && caveDensity < density;
							density = Mathf.Lerp(density, Mathf.Min(density, caveDensity), caveMix);
							if (carved && density > 0f)
								blockType = Voxel.BlockType.Stone;
						}

						// Fill only open air up to sea level (does not flood enclosed caves below terrain).
						if (density <= 0f && worldY <= seaLevel && worldY >= surfaceHeight - 0.001f)
						{
							density = 1f;
							blockType = Voxel.BlockType.Water;
						}

						ChunkFillUpdate(chunk, voxelData[voxelIndex++] = new Voxel(density, blockType));
					}
				}
			}
		}

		public override Color32 DensityColor(Voxel voxel)
		{
			return BlockData.GetColor(voxel.blockType);
		}

		public string GetSurfaceBiomeName(Vector3 worldPosition)
		{
			Vector3i sampleChunkPos = new Vector3i(
				Mathf.FloorToInt(worldPosition.x) >> Chunk.BIT_SIZE,
				0,
				Mathf.FloorToInt(worldPosition.z) >> Chunk.BIT_SIZE);

			float[] worldNoise = GetSurfaceNoise(0, sampleChunkPos);
			float[] grassNoise = GetSurfaceNoise(1, sampleChunkPos);
			float[] desertNoise = GetSurfaceNoise(2, sampleChunkPos);

			int localX = Mathf.Clamp(Mathf.FloorToInt(worldPosition.x) & Chunk.BIT_MASK, 0, Chunk.SIZE - 1);
			int localZ = Mathf.Clamp(Mathf.FloorToInt(worldPosition.z) & Chunk.BIT_MASK, 0, Chunk.SIZE - 1);

			float surfaceHeight = BilinearSurfaceLookup(localX, localZ, BuildSurfaceHeights(worldNoise, grassNoise, desertNoise));
			if (surfaceHeight <= seaLevel)
				return "Ocean";

			float biomeMix = BilinearSurfaceLookup(localX, localZ, BuildBiomeMixes(worldNoise));
			return biomeMix < 0.5f ? "Grasslands" : "Desert";
		}

		private float[] GetSurfaceNoise(int noiseArrayIndex, Vector3i chunkPos)
		{
			int offsetShift = Chunk.BIT_SIZE - interpBitStep;

			return fastNoiseSIMDUnity[noiseArrayIndex].fastNoiseSIMD.GetNoiseSet(
				chunkPos.x << offsetShift,
				0,
				chunkPos.z << offsetShift,
				interpSize,
				1,
				interpSize,
				1 << interpBitStep);
		}

		private float[] BuildSurfaceHeights(float[] worldNoise, float[] grassNoise, float[] desertNoise)
		{
			float[] surfaceHeights = new float[interpSize * interpSize];
			int surfaceIndex = 0;

			for (int x = 0; x < interpSize; x++)
			{
				for (int z = 0; z < interpSize; z++)
				{
					float biomeMix = GetBiomeMix(worldNoise[surfaceIndex]);
					float grassHeight = grassNoise[surfaceIndex] * grassTerrainScale;
					float desertHeight = desertNoise[surfaceIndex] * desertTerrainScale;
					surfaceHeights[surfaceIndex++] = Mathf.Lerp(grassHeight, desertHeight, biomeMix);
				}
			}

			return surfaceHeights;
		}

		private float[] BuildBiomeMixes(float[] worldNoise)
		{
			float[] biomeMixes = new float[interpSize * interpSize];
			for (int i = 0; i < worldNoise.Length; i++)
				biomeMixes[i] = GetBiomeMix(worldNoise[i]);

			return biomeMixes;
		}

		private float BilinearSurfaceLookup(float localX, float localZ, float[] surfaceHeights)
		{
			float xs = (localX + 0.5f) * interpScale;
			float zs = (localZ + 0.5f) * interpScale;

			int x0 = FastFloor(xs);
			int z0 = FastFloor(zs);

			xs -= x0;
			zs -= z0;

			int index = z0 + x0 * interpSize;

			return Lerp(
				Lerp(surfaceHeights[index], surfaceHeights[index + 1], zs),
				Lerp(surfaceHeights[index + interpSize], surfaceHeights[index + interpSize + 1], zs),
				xs);
		}

		private float GetBiomeMix(float worldNoiseValue)
		{
			float biomeValue = NoiseTo01(worldNoiseValue);
			float start = 0.5f - biomeTransitionWidth;
			float end = 0.5f + biomeTransitionWidth;

			if (biomeValue <= start)
				return 0f;

			if (biomeValue >= end)
				return 1f;

			float t = Mathf.InverseLerp(start, end, biomeValue);
			return t * t * (3f - 2f * t);
		}

		private Voxel.BlockType GetBlockType(float density, float biomeMix)
		{
			if (density <= 0f)
				return Voxel.BlockType.Empty;

			if (biomeMix < 0.5f)
			{
				if (density < 1.5f)
					return Voxel.BlockType.Grass;

				if (density < 4.5f)
					return Voxel.BlockType.Dirt;

				return Voxel.BlockType.Stone;
			}

			if (density < 1.5f)
				return Voxel.BlockType.Sand;

			if (density < 5.0f)
				return Voxel.BlockType.Sand;

			if (density < 10.0f)
				return Voxel.BlockType.Dirt;

			return Voxel.BlockType.Stone;
		}

		private static float NoiseTo01(float value)
		{
			return Mathf.Clamp01((value + 1f) * 0.5f);
		}

		private float GetCaveMix(float undergroundDepth)
		{
			float enter = Mathf.InverseLerp(caveStartDepth, caveStartDepth + caveFadeDepth, undergroundDepth);
			float exit = 1f - Mathf.InverseLerp(caveEndDepth, caveEndDepth + caveFadeDepth, undergroundDepth);

			return Mathf.Clamp01(enter * exit);
		}

		private static float Lerp(float a, float b, float t)
		{
			return a + t * (b - a);
		}

		private static int FastFloor(float value)
		{
			return value >= 0f ? (int)value : (int)value - 1;
		}

		private void EnsureNoiseComponents()
		{
			for (int i = 0; i < fastNoiseSIMDUnity.Length; i++)
			{
				if (fastNoiseSIMDUnity[i] == null)
					fastNoiseSIMDUnity[i] = gameObject.AddComponent<FastNoiseSIMDUnity>();

				if (string.IsNullOrEmpty(fastNoiseSIMDUnity[i].noiseName) || fastNoiseSIMDUnity[i].noiseName == "Default Noise")
					fastNoiseSIMDUnity[i].noiseName = GetNoiseSlotName(i);
			}
		}

		private void ConfigureNoiseComponents()
		{
			for (int i = 0; i < fastNoiseSIMDUnity.Length; i++)
			{
				FastNoiseSIMDUnity noise = fastNoiseSIMDUnity[i];

				if (noise == null)
					continue;

				if (i < 3)
					noise.axisScales = new Vector3(1f, 0f, 1f);
				else
					noise.axisScales = Vector3.one;

				noise.SaveSettings();
			}
		}

		private string GetNoiseSlotName(int index)
		{
			switch (index)
			{
				case 0: return "World";
				case 1: return "Grasslands";
				case 2: return "Desert";
				case 3: return "Caves";
				default: return "Noise " + index;
			}
		}
	}
}