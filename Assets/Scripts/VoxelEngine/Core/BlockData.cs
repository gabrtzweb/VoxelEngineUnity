using System;
using UnityEngine;

namespace VoxelEngine
{
	public static class BlockData
	{
		public enum BlockType
		{
			Unknown = 0,
			TerrGrass,
			TerrDirt,
			TerrMoss,
			RockStone,
			RockSlate,
			RockCore,
			Lava,
			TerrSand,
		}

		public static string GuessBlockKey(string textureName)
		{
			if (string.IsNullOrEmpty(textureName))
				return string.Empty;

			string name = System.IO.Path.GetFileNameWithoutExtension(textureName).ToLowerInvariant();
			while (name.Length > 0 && char.IsDigit(name[name.Length - 1]))
				name = name.Substring(0, name.Length - 1);

			return name.TrimEnd('_');
		}

		public static string GetBlockKey(BlockType blockType)
		{
			switch (blockType)
			{
				case BlockType.TerrGrass:
					return "terr_grass";
				case BlockType.TerrDirt:
					return "terr_dirt";
				case BlockType.TerrMoss:
					return "terr_moss";
				case BlockType.RockStone:
					return "rock_stone";
				case BlockType.RockSlate:
					return "rock_slate";
				case BlockType.RockCore:
					return "rock_core";
				case BlockType.Lava:
					return "liqd_lava";
				case BlockType.TerrSand:
					return "terr_sand";
				default:
					return string.Empty;
			}
		}

		public static int ResolveBlockIndex(BlockType blockType)
		{
			return ResolveBlockIndex(blockType, 0, 0, 0);
		}

		public static int ResolveBlockIndex(BlockType blockType, int x, int y, int z)
		{
			return ResolveBlockIndex(GetBlockKey(blockType), x, y, z);
		}

		public static int ResolveBlockIndex(string blockKey, int x, int y, int z)
		{
			int[] indices;
			if (string.IsNullOrEmpty(blockKey) || !TextureMap.Variations.TryGetValue(blockKey, out indices) || indices == null || indices.Length == 0)
				return 0;

			int hash = unchecked((x * 73856093) ^ (y * 19349663) ^ (z * 83492791));
			int choice = Math.Abs(hash) % indices.Length;
			return indices[choice];
		}
	}
}