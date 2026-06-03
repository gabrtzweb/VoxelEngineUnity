using UnityEngine;

namespace VoxelEngine
{
	public static class BlockData
	{
		// Centralized block colors and texture name groups.
		public static readonly Color32 GrassColor = new Color32(76, 120, 44, 255); // #4C782C
		public static readonly Color32 DirtColor = new Color32(97, 75, 66, 255); // #614B42
		public static readonly Color32 SandColor = new Color32(229, 204, 113, 255); // #E5CC71
		public static readonly Color32 WaterColor = new Color32(0, 74, 111, 255); // #004A6F
		public static readonly Color32 StoneColor = new Color32(150, 150, 150, 255); // #969696
		public static readonly Color32 SlateColor = new Color32(63, 64, 68, 255); // #3f4044

		public static readonly string GrassTopTextureKey = "grass_top";
		public static readonly string GrassSideTextureKey = "grass_side";
		public static readonly string GrassBottomTextureKey = "grass_bottom";
		public static readonly string DirtTextureKey = "dirt";
		public static readonly string SandTextureKey = "sand";
		public static readonly string WaterTextureKey = "water";
		public static readonly string SlateTextureKey = "slate";
		public static readonly string StoneTextureKey = "stone";

		public static readonly string[] GrassTopTextureNames = { "terr_grass", "terr_grass1", "terr_grass2", "terr_grass3", "terr_grass4", "terr_grass5", "terr_grass6", "terr_grass7" };
		public static readonly string[] GrassSideTextureNames = { "terr_grass_side", "terr_grass_side1" };
		public static readonly string[] GrassBottomTextureNames = { "terr_dirt", "terr_dirt1" };
		public static readonly string[] DirtTextureNames = { "terr_dirt", "terr_dirt1" };
		public static readonly string[] SandTextureNames = { "terr_sand", "terr_sand1" };
		public static readonly string[] WaterTextureNames = { "liqd_water_flow" };
		public static readonly string[] SlateTextureNames = { "rock_slate", "rock_slate1" };
		public static readonly string[] StoneTextureNames = { "rock_stone", "rock_stone1" };

		public static Color32 GetColor(Voxel.BlockType blockType)
		{
			switch (blockType)
			{
				case Voxel.BlockType.Grass:
					return GrassColor;
				case Voxel.BlockType.Dirt:
					return DirtColor;
				case Voxel.BlockType.Sand:
					return SandColor;
				case Voxel.BlockType.Water:
					return WaterColor;
				case Voxel.BlockType.Stone:
					return StoneColor;
				case Voxel.BlockType.Slate:
					return SlateColor;
				default:
					return StoneColor;
			}
		}

		public static string GetTextureKey(Voxel.BlockType blockType)
		{
			return GetTextureKey(blockType, MeshBuilder.Direction.Up);
		}

		public static string GetTextureKey(Voxel.BlockType blockType, MeshBuilder.Direction faceDirection)
		{
			switch (blockType)
			{
				case Voxel.BlockType.Grass:
					if (faceDirection == MeshBuilder.Direction.Down)
						return DirtTextureKey;

					if (faceDirection == MeshBuilder.Direction.Up)
						return GrassTopTextureKey;

					return GrassSideTextureKey;
				case Voxel.BlockType.Dirt:
					return DirtTextureKey;
				case Voxel.BlockType.Sand:
					return SandTextureKey;
				case Voxel.BlockType.Water:
					return WaterTextureKey;
				case Voxel.BlockType.Stone:
					return StoneTextureKey;
				case Voxel.BlockType.Slate:
					return SlateTextureKey;
				default:
					return StoneTextureKey;
			}
		}

		public static string[] GetTextureNames(Voxel.BlockType blockType)
		{
			return GetTextureNames(blockType, MeshBuilder.Direction.Up);
		}

		public static string[] GetTextureNames(Voxel.BlockType blockType, MeshBuilder.Direction faceDirection)
		{
			switch (blockType)
			{
				case Voxel.BlockType.Grass:
					if (faceDirection == MeshBuilder.Direction.Down)
						return GrassBottomTextureNames;

					if (faceDirection == MeshBuilder.Direction.Up)
						return GrassTopTextureNames;

					return GrassSideTextureNames;
				case Voxel.BlockType.Dirt:
					return DirtTextureNames;
				case Voxel.BlockType.Sand:
					return SandTextureNames;
				case Voxel.BlockType.Water:
					return WaterTextureNames;
				case Voxel.BlockType.Stone:
					return StoneTextureNames;
				case Voxel.BlockType.Slate:
					return SlateTextureNames;
				default:
					return StoneTextureNames;
			}
		}

		public static string GuessBlockKey(string textureName)
		{
			if (string.IsNullOrEmpty(textureName))
				return string.Empty;

			string normalized = textureName.ToLowerInvariant();

			if (normalized.Contains("grass_side"))
				return GrassSideTextureKey;

			if (normalized.Contains("grass"))
				return GrassTopTextureKey;

			if (normalized.Contains("dirt"))
				return DirtTextureKey;

			if (normalized.Contains("sand"))
				return SandTextureKey;

			if (normalized.Contains("water") || normalized.Contains("liqd"))
				return WaterTextureKey;

			if (normalized.Contains("slate"))
				return SlateTextureKey;

			if (normalized.Contains("stone") || normalized.Contains("rock"))
				return StoneTextureKey;

			return string.Empty;
		}
	}
}
