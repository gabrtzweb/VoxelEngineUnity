using UnityEngine;

namespace VoxelEngine
{
	public class TerrainGenerator_GrassLand : TerrainGeneratorSIMD
	{
		public float terrainScale = 20f;
		public Color32 grassColor = new Color32(112, 150, 48, 255);
		public Color32 dirtColor = new Color32(97, 75, 66, 255);
		public Color32 stoneColor = new Color32(150, 150, 150, 255);

		public override void Awake()
		{
			SetNoiseArraySize(2);
			SetInterpBitStep(2);

			minHeight = -terrainScale - fastNoiseSIMDUnity[1].perturbAmp;
			maxHeight = terrainScale + fastNoiseSIMDUnity[1].perturbAmp;
		}

		public override void GenerateChunk(Chunk chunk)
		{
			Voxel[] voxelData = chunk.voxelData;

			int yOffset = chunk.chunkPos.y << Chunk.BIT_SIZE;
			int index = 0;

			float[] terrainNoise = GetInterpNoise(0, chunk.chunkPos);
			float[] perturbNoise = GetInterpNoise(1, chunk.chunkPos);

			for (int x = 0; x < interpSize; x++)
			{
				for (int y = 0; y < interpSize; y++)
				{
					float yf = (y << interpBitStep) + yOffset;

					for (int z = 0; z < interpSize; z++)
					{
						float yPerturb = perturbNoise[index] * fastNoiseSIMDUnity[1].perturbAmp;
						terrainNoise[index] = terrainNoise[index] * terrainScale - yf + yPerturb;
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

		public override Color32 DensityColor(Voxel voxel)
		{
			if (voxel.density < 5f)
				return Color32.Lerp(grassColor, dirtColor, voxel.density *0.2f);

			if (voxel.density < 15f)
			{
				float lerp = (voxel.density - 5f)*0.1f;
				return Color32.Lerp(dirtColor, stoneColor, lerp*lerp);
			}

			return stoneColor;
		}
	}
}