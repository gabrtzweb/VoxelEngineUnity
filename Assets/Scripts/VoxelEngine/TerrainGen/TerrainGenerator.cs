using UnityEngine;

namespace VoxelEngine
{
	public class TerrainGenerator : TerrainGeneratorBase
	{
		public enum TerrainProfile
		{
			GrassLand,
			AlienPlanet,
			CrackedSurface
		}

		public FastNoiseUnity[] fastNoiseUnity = new FastNoiseUnity[2];
		public TerrainProfile terrainProfile = TerrainProfile.GrassLand;

		[Header("Grass Hills")]
		public FastNoiseUnity grassMainNoise;
		public FastNoiseUnity grassWarpNoise;
		public float grassTerrainScale = 20f;
		public Color32 grassColor = new Color32(112, 150, 48, 255);
		public Color32 grassDirtColor = new Color32(97, 75, 66, 255);
		public Color32 grassStoneColor = new Color32(150, 150, 150, 255);

		[Header("Alien Planet")]
		public FastNoiseUnity alienMainNoise;
		public float alienTerrainScale = 40f;
		public Color32 alienSurfaceColor = new Color32(130, 130, 130, 255);
		public Color32 alienCoreColor = new Color32(80, 0, 80, 255);

		[Header("Cracked Surface")]
		public FastNoiseUnity crackedMainNoise;
		public float crackedTerrainScale = 30f;
		public Color32 crackedSurfaceColor = new Color32(130, 130, 130, 255);
		public Color32 crackedCoreColor = new Color32(80, 0, 80, 255);
		public Color32 crackedLavaColor = new Color32(243, 147, 0, 255);

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
			System.Array.Resize(ref fastNoiseUnity, size);
		}

		protected FastNoise GetFastNoise(int noiseArrayIndex)
		{
			return fastNoiseUnity[noiseArrayIndex].fastNoise;
		}

		public override void GenerateChunk(Chunk chunk)
		{
			switch (terrainProfile)
			{
				case TerrainProfile.GrassLand:
					GenerateGrassLand(chunk);
					break;
				case TerrainProfile.AlienPlanet:
					GenerateAlienPlanet(chunk);
					break;
				case TerrainProfile.CrackedSurface:
					GenerateCrackedSurface(chunk);
					break;
			}
		}

		public override Color32 DensityColor(Voxel voxel)
		{
			switch (terrainProfile)
			{
				case TerrainProfile.GrassLand:
					if (voxel.density < 5f)
						return Color32.Lerp(grassColor, grassDirtColor, voxel.density * 0.2f);

					if (voxel.density < 15f)
					{
						float lerp = (voxel.density - 5f) * 0.1f;
						return Color32.Lerp(grassDirtColor, grassStoneColor, lerp * lerp);
					}

					return grassStoneColor;
				case TerrainProfile.AlienPlanet:
					if (voxel.density < 5f)
						return Color32.Lerp(alienSurfaceColor, alienCoreColor, voxel.density * 0.2f);

					return alienCoreColor;
				case TerrainProfile.CrackedSurface:
					if (voxel.density < 5f)
						return Color32.Lerp(crackedSurfaceColor, crackedCoreColor, voxel.density * 0.2f);

					if (voxel.density >= 8f)
						return crackedLavaColor;

					return crackedCoreColor;
				default:
					return grassStoneColor;
			}
		}

		public override BlockData.BlockType DensityBlock(Voxel voxel)
		{
			switch (terrainProfile)
			{
				case TerrainProfile.GrassLand:
					if (voxel.density < 5f)
						return BlockData.BlockType.TerrGrass;

					if (voxel.density < 15f)
						return BlockData.BlockType.TerrDirt;

					return BlockData.BlockType.RockStone;
				case TerrainProfile.AlienPlanet:
					if (voxel.density < 5f)
						return BlockData.BlockType.TerrMoss;

					return BlockData.BlockType.RockCore;
				case TerrainProfile.CrackedSurface:
					if (voxel.density < 5f)
						return BlockData.BlockType.RockSlate;

					if (voxel.density >= 8f)
						return BlockData.BlockType.Lava;

					return BlockData.BlockType.RockCore;
				default:
					return BlockData.BlockType.RockStone;
			}
		}

		private void RefreshProfileSettings()
		{
			switch (terrainProfile)
			{
				case TerrainProfile.GrassLand:
					SetNoiseArraySize(2);
					fastNoiseUnity[0] = grassMainNoise;
					fastNoiseUnity[1] = grassWarpNoise;
					minHeight = -grassTerrainScale - GetPerturbAmp(grassWarpNoise);
					maxHeight = grassTerrainScale + GetPerturbAmp(grassWarpNoise);
					break;
				case TerrainProfile.AlienPlanet:
					SetNoiseArraySize(1);
					fastNoiseUnity[0] = alienMainNoise;
					minHeight = -alienTerrainScale;
					maxHeight = alienTerrainScale;
					break;
				case TerrainProfile.CrackedSurface:
					SetNoiseArraySize(1);
					fastNoiseUnity[0] = crackedMainNoise;
					minHeight = -crackedTerrainScale;
					maxHeight = crackedTerrainScale;
					break;
			}
		}

		private static float GetPerturbAmp(FastNoiseUnity noise)
		{
			return noise ? noise.gradientPerturbAmp : 0f;
		}

		private void GenerateGrassLand(Chunk chunk)
		{
			float[] interpLookup = new float[interpSize*interpSize*interpSize];
			Voxel[] voxelData = chunk.voxelData;

			int xOffset = chunk.chunkPos.x << Chunk.BIT_SIZE;
			int yOffset = chunk.chunkPos.y << Chunk.BIT_SIZE;
			int zOffset = chunk.chunkPos.z << Chunk.BIT_SIZE;
			int index = 0;

			var xf = FastNoise.GetDecimalType();
			var yf = xf;
			var zf = xf;

			for (int x = 0; x < interpSize; x++)
			{
				for (int y = 0; y < interpSize; y++)
				{
					for (int z = 0; z < interpSize; z++)
					{
						xf = (x << interpBitStep) + xOffset;
						yf = (y << interpBitStep) + yOffset;
						zf = (z << interpBitStep) + zOffset;

						GetFastNoise(1).GradientPerturb(ref xf, ref yf, ref zf);

						float voxel = (float)GetFastNoise(0).GetNoise(xf, yf, zf);
						voxel *= grassTerrainScale;
						voxel -= (float)yf;

						interpLookup[index++] = voxel;
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

		private void GenerateAlienPlanet(Chunk chunk)
		{
			float[] interpLookup = new float[interpSize * interpSize * interpSize];
			Voxel[] voxelData = chunk.voxelData;

			int xOffset = chunk.chunkPos.x << Chunk.BIT_SIZE;
			int yOffset = chunk.chunkPos.y << Chunk.BIT_SIZE;
			int zOffset = chunk.chunkPos.z << Chunk.BIT_SIZE;
			int index = 0;

			for (int x = 0; x < interpSize; x++)
			{
				float xf = (x << interpBitStep) + xOffset;

				for (int y = 0; y < interpSize; y++)
				{
					float yf = (y << interpBitStep) + yOffset;

					for (int z = 0; z < interpSize; z++)
					{
						float zf = (z << interpBitStep) + zOffset;

						float voxel = -yf;
						voxel += (float)GetFastNoise(0).GetNoise(xf, yf, zf) * alienTerrainScale;

						interpLookup[index++] = voxel;
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

		private void GenerateCrackedSurface(Chunk chunk)
		{
			float[] interpLookup = new float[interpSize*interpSize*interpSize];
			Voxel[] voxelData = chunk.voxelData;

			int xOffset = chunk.chunkPos.x << Chunk.BIT_SIZE;
			int yOffset = chunk.chunkPos.y << Chunk.BIT_SIZE;
			int zOffset = chunk.chunkPos.z << Chunk.BIT_SIZE;
			int index = 0;

			for (int x = 0; x < interpSize; x++)
			{
				for (int y = 0; y < interpSize; y++)
				{
					float yf = (y << interpBitStep) + yOffset;

					for (int z = 0; z < interpSize; z++)
					{
						float zf = (z << interpBitStep) + zOffset;

						float voxel;

						if (yf <= crackedTerrainScale * -0.6f)
							voxel = 1000000f;
						else
						{
								if (yf < 0)
								{
									voxel = (float)GetFastNoise(0).GetNoise((x << interpBitStep) + xOffset, 0, zf);
									voxel *= Mathf.Abs(voxel) * 1.5f;
								}
							else
								voxel = (float)GetFastNoise(0).GetNoise((x << interpBitStep) + xOffset, yf, zf);

							voxel *= crackedTerrainScale;
							voxel -= Mathf.Abs(yf);
						}

						interpLookup[index++] = voxel;
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
	}
}
