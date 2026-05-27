using UnityEngine;

namespace VoxelEngine
{
	public class TerrainGeneratorSIMD_World : TerrainGeneratorSIMD
	{
		public float grassTerrainScale = 20f;
		public float desertTerrainScale = 20f;
		public float canyonMaxHeight = 2f;
		public float canyonGradient = 3f;

		public float caveRatio = .88f;
		public float caveStartDepth = 4f;
		public float caveEndDepth = 64f;
		public float caveFadeDepth = 16f;

		public Color32 grassColor = new Color32(112, 150, 48, 255);
		public Color32 dirtColor = new Color32(97, 75, 66, 255);
		public Color32 stoneColor = new Color32(150, 150, 150, 255);
		public Color32 sandColor = new Color32(240, 190, 2, 255);

		public override void Awake()
		{
			SetInterpBitStep(1);
			SetNoiseArraySize(6);
			EnsureNoiseComponents();

			minHeight = Mathf.Min(-grassTerrainScale - fastNoiseSIMDUnity[2].perturbAmp, -desertTerrainScale)
				- caveEndDepth - caveFadeDepth;
			maxHeight = Mathf.Max(
				grassTerrainScale + fastNoiseSIMDUnity[2].perturbAmp,
				desertTerrainScale*(canyonMaxHeight + 1f));
		}

		public override void GenerateChunk(Chunk chunk)
		{
			Voxel[] voxelData = chunk.voxelData;

			int yOffset = chunk.chunkPos.y << Chunk.BIT_SIZE;
			int index = 0;

			float[] biomeNoise = GetInterpNoise(0, chunk.chunkPos);
			float[] grassTerrainNoise = GetInterpNoise(1, chunk.chunkPos);
			float[] grassPerturbNoise = GetInterpNoise(2, chunk.chunkPos);
			float[] desertTerrainNoise = GetInterpNoise(3, chunk.chunkPos);
			float[] canyonNoise = GetInterpNoise(4, chunk.chunkPos);
			float[] caveNoise = GetInterpNoise(5, chunk.chunkPos);

			for (int x = 0; x < interpSize; x++)
			{
				for (int y = 0; y < interpSize; y++)
				{
					float yf = (y << interpBitStep) + yOffset;

					for (int z = 0; z < interpSize; z++)
					{
						float biomeMix = (biomeNoise[index] + 1f)*0.5f;

						float grassDensity = grassTerrainNoise[index]*grassTerrainScale - yf;
						grassDensity += grassPerturbNoise[index]*fastNoiseSIMDUnity[2].perturbAmp;

						float desertDensity = desertTerrainNoise[index];
						desertDensity = Mathf.Min(desertDensity + canyonMaxHeight,
							Mathf.Max(desertDensity, canyonGradient*canyonNoise[index]*Mathf.Abs(canyonNoise[index])));
						desertDensity = desertDensity*desertTerrainScale - yf;

						float surfaceDensity = Mathf.Lerp(grassDensity, desertDensity, biomeMix);
						float caveMix = GetCaveMix(surfaceDensity);
						float caveDensity = (caveRatio - caveNoise[index])*32f;
						float undergroundDensity = Mathf.Lerp(32f, caveDensity, caveMix);

						ChunkFillUpdate(chunk, voxelData[index] = new Voxel(Mathf.Min(surfaceDensity, undergroundDensity)));
						index++;
					}
				}
			}
		}

		public override Color32 DensityColor(Voxel voxel)
		{
			if (voxel.density < 3.33f)
				return Color32.Lerp(sandColor, stoneColor, voxel.density*0.3f);

			if (voxel.density < 5f)
				return Color32.Lerp(grassColor, dirtColor, voxel.density*0.2f);

			if (voxel.density < 15f)
			{
				float lerp = (voxel.density - 5f)*0.1f;
				return Color32.Lerp(dirtColor, stoneColor, lerp*lerp);
			}

			return stoneColor;
		}

		private float GetCaveMix(float surfaceDensity)
		{
			float enter = Mathf.InverseLerp(caveStartDepth, caveStartDepth + caveFadeDepth, surfaceDensity);
			float exit = 1f - Mathf.InverseLerp(caveEndDepth, caveEndDepth + caveFadeDepth, surfaceDensity);

			return Mathf.Clamp01(enter*exit);
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

		private string GetNoiseSlotName(int index)
		{
			switch (index)
			{
				case 0: return "Biome Blend";
				case 1: return "Grass Height";
				case 2: return "Grass Warp";
				case 3: return "Desert Height";
				case 4: return "Desert Canyon";
				case 5: return "Caves";
				default: return "Noise " + index;
			}
		}
	}
}