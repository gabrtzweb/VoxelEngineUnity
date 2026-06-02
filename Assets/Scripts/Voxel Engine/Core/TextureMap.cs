using System.Collections.Generic;
using UnityEngine;

namespace VoxelEngine
{
	public static class TextureMap
	{
		public static readonly Dictionary<string, string[]> TextureNames = new Dictionary<string, string[]>
		{
			{ "grass_top", new[] { "terr_grass", "terr_grass1", "terr_grass2", "terr_grass3", "terr_grass4", "terr_grass5", "terr_grass6", "terr_grass7" } },
			{ "grass_side", new[] { "terr_grass_side", "terr_grass_side1" } },
			{ "grass_bottom", new[] { "terr_dirt", "terr_dirt1" } },
			{ "dirt", new[] { "terr_dirt", "terr_dirt1" } },
			{ "sand", new[] { "terr_sand", "terr_sand1" } },
			{ "slate", new[] { "rock_slate", "rock_slate1" } },
			{ "stone", new[] { "rock_stone", "rock_stone1" } },
			{ "water", new[] { "liqd_water_flow" } },
		};

		public static readonly Dictionary<string, int[]> Variations = new Dictionary<string, int[]>
		{
			{ "grass_top", new[] { 0, 1, 2, 3, 4, 5, 6, 7 } },
			{ "grass_side", new[] { 8, 9 } },
			{ "grass_bottom", new[] { 10, 11 } },
			{ "dirt", new[] { 12, 13 } },
			{ "sand", new[] { 14, 15 } },
			{ "slate", new[] { 16, 17 } },
			{ "stone", new[] { 18, 19 } },
			{ "water", new[] { 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55 } },
		};

		public static readonly Dictionary<string, int> FrameCounts = new Dictionary<string, int>
		{
			{ "grass_top", 1 },
			{ "grass_side", 1 },
			{ "grass_bottom", 1 },
			{ "dirt", 1 },
			{ "sand", 1 },
			{ "slate", 1 },
			{ "stone", 1 },
			{ "water", 36 },
		};

		public static Color32 GetTint(Voxel.BlockType blockType)
		{
			switch (blockType)
			{
				case Voxel.BlockType.Grass:
					return BlockData.GrassColor;
				case Voxel.BlockType.Dirt:
					return BlockData.DirtColor;
				case Voxel.BlockType.Sand:
					return BlockData.SandColor;
				case Voxel.BlockType.Water:
					return BlockData.WaterColor;
				case Voxel.BlockType.Stone:
					return BlockData.StoneColor;
				case Voxel.BlockType.Slate:
					return BlockData.SlateColor;
				default:
					return BlockData.StoneColor;
			}
		}

		public static string GetTextureKey(Voxel.BlockType blockType)
		{
			return GetTextureKey(blockType, MeshBuilder.Direction.Up);
		}

		public static string GetTextureKey(Voxel.BlockType blockType, MeshBuilder.Direction faceDirection)
		{
			return BlockData.GetTextureKey(blockType, faceDirection);
		}

		public static string[] GetTextureNames(Voxel.BlockType blockType)
		{
			return GetTextureNames(blockType, MeshBuilder.Direction.Up);
		}

		public static string[] GetTextureNames(Voxel.BlockType blockType, MeshBuilder.Direction faceDirection)
		{
			string textureKey = GetTextureKey(blockType, faceDirection);
			return TextureNames.TryGetValue(textureKey, out string[] names) ? names : new string[0];
		}

		public static Voxel.BlockType GetBlockType(Color32 tint)
		{
			if (tint.Equals(BlockData.GrassColor))
				return Voxel.BlockType.Grass;
			if (tint.Equals(BlockData.DirtColor))
				return Voxel.BlockType.Dirt;
			if (tint.Equals(BlockData.SandColor))
				return Voxel.BlockType.Sand;
			if (tint.Equals(BlockData.WaterColor))
				return Voxel.BlockType.Water;
			if (tint.Equals(BlockData.SlateColor))
				return Voxel.BlockType.Slate;
			return Voxel.BlockType.Stone;
		}

		public static int GetTextureLayer(Color32 tint, int worldX, int worldY, int worldZ, MeshBuilder.Direction faceDirection)
		{
			return GetTextureLayer(GetBlockType(tint), worldX, worldY, worldZ, faceDirection, out _);
		}

		public static int GetTextureLayer(Voxel.BlockType blockType, int worldX, int worldY, int worldZ, MeshBuilder.Direction faceDirection)
		{
			return GetTextureLayer(blockType, worldX, worldY, worldZ, faceDirection, out _);
		}

		public static int GetTextureLayer(Voxel.BlockType blockType, int worldX, int worldY, int worldZ, MeshBuilder.Direction faceDirection, out int frameCount)
		{
			string textureKey = GetTextureKey(blockType, faceDirection);
			frameCount = GetTextureFrameCount(textureKey);
			if (string.IsNullOrEmpty(textureKey))
				return 0;

			if (!Variations.TryGetValue(textureKey, out int[] variationIndices) || variationIndices.Length == 0)
				return 0;

			if (frameCount > 1)
				return variationIndices[0];

			if (variationIndices.Length == 1)
				return variationIndices[0];

			int hash = Hash(worldX, worldY, worldZ, textureKey);
			int variationIndex = PositiveModulo(hash, variationIndices.Length);
			return variationIndices[variationIndex];
		}

		public static int GetTextureCount(Voxel.BlockType blockType)
		{
			string textureKey = GetTextureKey(blockType);
			return Variations.TryGetValue(textureKey, out int[] variationIndices) ? variationIndices.Length : 0;
		}

		public static int GetTextureFrameCount(Voxel.BlockType blockType)
		{
			return GetTextureFrameCount(GetTextureKey(blockType));
		}

		public static int GetTextureFrameCount(Voxel.BlockType blockType, MeshBuilder.Direction faceDirection)
		{
			return GetTextureFrameCount(GetTextureKey(blockType, faceDirection));
		}

		public static int GetTextureFrameCount(string textureKey)
		{
			if (string.IsNullOrEmpty(textureKey))
				return 1;

			return FrameCounts.TryGetValue(textureKey, out int frameCount) ? Mathf.Max(1, frameCount) : 1;
		}

		public static int GetTextureLayer(string textureKey, int worldX, int worldY, int worldZ)
		{
			return GetTextureLayer(textureKey, worldX, worldY, worldZ, out _);
		}

		public static int GetTextureLayer(string textureKey, int worldX, int worldY, int worldZ, out int frameCount)
		{
			frameCount = GetTextureFrameCount(textureKey);

			if (string.IsNullOrEmpty(textureKey))
				return 0;

			if (!Variations.TryGetValue(textureKey, out int[] variationIndices) || variationIndices.Length == 0)
				return 0;

			if (frameCount > 1)
				return variationIndices[0];

			if (variationIndices.Length == 1)
				return variationIndices[0];

			int hash = Hash(worldX, worldY, worldZ, textureKey);
			int variationIndex = PositiveModulo(hash, variationIndices.Length);
			return variationIndices[variationIndex];
		}

		private static int Hash(int x, int y, int z, string textureKey)
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 31 + x;
				hash = hash * 31 + y;
				hash = hash * 31 + z;
				for (int i = 0; i < textureKey.Length; i++)
					hash = hash * 31 + textureKey[i];
				return hash;
			}
		}

		private static int PositiveModulo(int value, int modulo)
		{
			int result = value % modulo;
			return result < 0 ? result + modulo : result;
		}
	}
}
