using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelEngine
{
	[CreateAssetMenu(menuName = "Voxel Engine/Block Data")]
	public class BlockData : ScriptableObject
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

		[Serializable]
		public struct BlockTextureEntry
		{
			public BlockType blockType;
			public string name;
			public Texture2D sourceTexture;
			public int layerIndex;
		}

		public Texture2DArray textureArray;
		public List<BlockTextureEntry> blockTextures = new List<BlockTextureEntry>();
		public BlockType defaultBlockType = BlockType.RockStone;

		public int Count
		{
			get { return blockTextures != null ? blockTextures.Count : 0; }
		}

		public bool TryGetBlockIndex(BlockType blockType, out int layerIndex)
		{
			if (blockTextures != null)
			{
				for (int i = 0; i < blockTextures.Count; i++)
				{
					if (blockTextures[i].blockType == blockType)
					{
						layerIndex = blockTextures[i].layerIndex;
						return true;
					}
				}
			}

			layerIndex = GetDefaultLayerIndex();
			return false;
		}

		public int GetBlockIndex(BlockType blockType)
		{
			int layerIndex;
			return TryGetBlockIndex(blockType, out layerIndex) ? layerIndex : layerIndex;
		}

		public int GetDefaultLayerIndex()
		{
			if (blockTextures != null)
			{
				for (int i = 0; i < blockTextures.Count; i++)
				{
					if (blockTextures[i].blockType == defaultBlockType)
						return blockTextures[i].layerIndex;
				}
			}

			return 0;
		}

		public static BlockType GuessBlockType(string textureName)
		{
			if (string.IsNullOrEmpty(textureName))
				return BlockType.Unknown;

			string name = System.IO.Path.GetFileNameWithoutExtension(textureName).ToLowerInvariant();

			if (name == "liqd_lava")
				return BlockType.Lava;
			if (name == "terr_moss")
				return BlockType.TerrMoss;
			if (name == "terr_grass")
				return BlockType.TerrGrass;
			if (name == "terr_dirt")
				return BlockType.TerrDirt;
			if (name == "rock_slate")
				return BlockType.RockSlate;
			if (name == "rock_core")
				return BlockType.RockCore;
			if (name == "rock_stone")
				return BlockType.RockStone;
			if (name == "terr_sand")
				return BlockType.TerrSand;

			return BlockType.Unknown;
		}
	}
}