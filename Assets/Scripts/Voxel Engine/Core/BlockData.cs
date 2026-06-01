using UnityEngine;

namespace VoxelEngine
{
	public static class BlockData
	{
		// Centralized block colors. Texture/material references can be added here later.
		public static readonly Color32 GrassColor = new Color32(112, 150, 48, 255); // #709630
		public static readonly Color32 DirtColor = new Color32(97, 75, 66, 255); // #614B42
		public static readonly Color32 SandColor = new Color32(229, 204, 113, 255); // #E5CC71
		public static readonly Color32 WaterColor = new Color32(0, 74, 111, 255); // #004A6F
		public static readonly Color32 StoneColor = new Color32(150, 150, 150, 255); // #969696

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
				default:
					return StoneColor;
			}
		}
	}
}
