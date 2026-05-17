using UnityEngine;

namespace VoxelEngine
{
	public class TerrainGeneratorSIMD : TerrainGeneratorBase
	{
		public enum TerrainProfile
		{
			Desert,
			FloatingIslands,
			Caves
		}

		public FastNoiseSIMDUnity[] fastNoiseSIMDUnity = new FastNoiseSIMDUnity[2];
		public TerrainProfile terrainProfile = TerrainProfile.Desert;

		[Header("Desert")]
		public FastNoiseSIMDUnity desertInterpNoise;
		public FastNoiseSIMDUnity desertCanyonNoise;
		public float desertTerrainScale = 20f;
		public float desertCanyonMaxHeight = 2f;
		public float desertCanyonGradient = 3f;
		public Color32 desertSandColor = new Color32(240, 190, 2, 255);
		public Color32 desertStoneColor = new Color32(120, 120, 80, 255);

		[Header("Floating Islands")]
		public FastNoiseSIMDUnity floatingIslandNoise;
		public FastNoiseSIMDUnity floatingTerrainNoise;
		public float floatingTerrainScale = 3f;
		public Color32 floatingGrassColor = new Color32(112, 150, 48, 255);
		public Color32 floatingDirtColor = new Color32(97, 75, 66, 255);
		public Color32 floatingStoneColor = new Color32(150, 150, 150, 255);

		[Header("Caves")]
		public FastNoiseSIMDUnity caveNoise;
		public float caveRatio = .88f;
		public Color32 caveStoneMinColor = new Color32(150, 150, 150, 255);
		public Color32 caveStoneMaxColor = new Color32(100, 100, 100, 255);

		public override void Awake()
		{
			SetInterpBitStep(1);
			RefreshProfileSettings();
		}

		private void OnValidate()
		{
			RefreshProfileSettings();
		}

		public void SetProfile(TerrainProfile profile)
		{
			terrainProfile = profile;
			RefreshProfileSettings();
		}

		protected void SetNoiseArraySize(int size)
		{
			System.Array.Resize(ref fastNoiseSIMDUnity, size);
		}

		protected float[] GetInterpNoise(int noiseArrayIndex, Vector3i chunkPos)
		{
			int offsetShift = Chunk.BIT_SIZE - interpBitStep;

			return fastNoiseSIMDUnity[noiseArrayIndex].fastNoiseSIMD.GetNoiseSet(chunkPos.x << offsetShift,
				chunkPos.y << offsetShift, chunkPos.z << offsetShift, interpSize, interpSize, interpSize, 1 << interpBitStep);
		}

		public override void GenerateChunk(Chunk chunk)
		{
			switch (terrainProfile)
			{
				case TerrainProfile.Desert:
					GenerateDesert(chunk);
					break;
				case TerrainProfile.FloatingIslands:
					GenerateFloatingIslands(chunk);
					break;
				case TerrainProfile.Caves:
					GenerateCaves(chunk);
					break;
			}
		}

		public override Color32 DensityColor(Voxel voxel)
		{
			switch (terrainProfile)
			{
				case TerrainProfile.Desert:
					if (voxel.density < 3.33f)
						return Color32.Lerp(desertSandColor, desertStoneColor, voxel.density * 0.3f);

					return desertStoneColor;
				case TerrainProfile.FloatingIslands:
					if (voxel.density < 2f)
						return Color32.Lerp(floatingGrassColor, floatingDirtColor, voxel.density * 0.5f);

					if (voxel.density < 6f)
					{
						float lerp = (voxel.density - 2f) * 0.25f;
						return Color32.Lerp(floatingDirtColor, floatingStoneColor, lerp * lerp);
					}

					return floatingStoneColor;
				case TerrainProfile.Caves:
					return Color32.Lerp(caveStoneMinColor, caveStoneMaxColor, voxel.density);
				default:
					return caveStoneMaxColor;
			}
		}

		private void RefreshProfileSettings()
		{
			switch (terrainProfile)
			{
				case TerrainProfile.Desert:
					SetNoiseArraySize(2);
					fastNoiseSIMDUnity[0] = desertInterpNoise;
					fastNoiseSIMDUnity[1] = desertCanyonNoise;
					minHeight = -desertTerrainScale;
					maxHeight = desertTerrainScale * (desertCanyonMaxHeight + 1f);
					break;
				case TerrainProfile.FloatingIslands:
					SetNoiseArraySize(2);
					fastNoiseSIMDUnity[0] = floatingIslandNoise;
					fastNoiseSIMDUnity[1] = floatingTerrainNoise;
					minHeight = float.MinValue;
					maxHeight = float.MaxValue;
					break;
				case TerrainProfile.Caves:
					SetNoiseArraySize(1);
					fastNoiseSIMDUnity[0] = caveNoise;
					minHeight = float.MinValue;
					maxHeight = float.MaxValue;
					break;
			}
		}

		private void GenerateDesert(Chunk chunk)
		{
			Voxel[] voxelData = chunk.voxelData;

			int yOffset = chunk.chunkPos.y << Chunk.BIT_SIZE;
			int index = 0;

			float[] interpLookup = GetInterpNoise(0, chunk.chunkPos);
			float[] canyonNoise = GetInterpNoise(1, chunk.chunkPos);

			for (int x = 0; x < interpSize; x++)
			{
				for (int y = 0; y < interpSize; y++)
				{
					float yf = (y << interpBitStep) + yOffset;

					for (int z = 0; z < interpSize; z++)
					{
						canyonNoise[index] -= 0.6f;

						interpLookup[index] = Mathf.Min(interpLookup[index] + desertCanyonMaxHeight,
							Mathf.Max(interpLookup[index], desertCanyonGradient * canyonNoise[index] * Mathf.Abs(canyonNoise[index])));
						interpLookup[index] *= desertTerrainScale;
						interpLookup[index] -= yf;

						index++;
					}
				}
			}

			index = 0;

			for (int x = 0; x < Chunk.SIZE; x++)
			{
				for (int y = 0; y < Chunk.SIZE; y++)
				{
					for (int z = 0; z < Chunk.SIZE; z++)
					{
						ChunkFillUpdate(chunk, voxelData[index++] = new Voxel(VoxelInterpLookup(x, y, z, interpLookup)));
					}
				}
			}
		}

		private void GenerateFloatingIslands(Chunk chunk)
		{
			Voxel[] voxelData = chunk.voxelData;

			int index = 0;

			float[] islandNoise = GetInterpNoise(0, chunk.chunkPos);
			float[] terrainNoise = GetInterpNoise(1, chunk.chunkPos);

			for (int x = 0; x < interpSize; x++)
			{
				for (int y = 0; y < interpSize; y++)
				{
					for (int z = 0; z < interpSize; z++)
					{
						islandNoise[index] -= 1f;
						terrainNoise[index] = Mathf.Abs(terrainNoise[index] * floatingTerrainScale) + (Mathf.Abs(islandNoise[index]) * islandNoise[index] + 0.2f) * 20.0f;

						index++;
					}
				}
			}

			index = 0;

			for (int x = 0; x < Chunk.SIZE; x++)
			{
				for (int y = 0; y < Chunk.SIZE; y++)
				{
					for (int z = 0; z < Chunk.SIZE; z++)
					{
						ChunkFillUpdate(chunk, voxelData[index++] = new Voxel(VoxelInterpLookup(x, y, z, terrainNoise)));
					}
				}
			}
		}

		private void GenerateCaves(Chunk chunk)
		{
			Voxel[] voxelData = chunk.voxelData;

			int index = 0;

			float[] caveNoise = GetInterpNoise(0, chunk.chunkPos);

			for (int x = 0; x < interpSize; x++)
			{
				for (int y = 0; y < interpSize; y++)
				{
					for (int z = 0; z < interpSize; z++)
					{
						caveNoise[index] = (caveRatio - caveNoise[index]) * 32f;

						index++;
					}
				}
			}

			index = 0;

			for (int x = 0; x < Chunk.SIZE; x++)
			{
				for (int y = 0; y < Chunk.SIZE; y++)
				{
					for (int z = 0; z < Chunk.SIZE; z++)
					{
						ChunkFillUpdate(chunk, voxelData[index++] = new Voxel(VoxelInterpLookup(x, y, z, caveNoise)));
					}
				}
			}
		}
	}
}