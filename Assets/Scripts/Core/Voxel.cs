
/*
Storing as a byte reduces memory usage by 4x compared to float.
*/

#define STORE_AS_BYTE


/*
Storing as a float uses the most memory but has the least processing overhead.
*/

//#define STORE_AS_FLOAT

using System;

namespace VoxelEngine
{
	// Basic voxel data structure
	// More data could be added here like block types

	public struct Voxel
	{
		public enum BlockType : byte
		{
			Empty,
			Grass,
			Dirt,
			Sand,
			Water,
			Stone,
			Slate,
		}

		public static readonly Voxel Solid = new Voxel(1f, BlockType.Stone);
		public static readonly Voxel Empty = new Voxel(-1f, BlockType.Empty);

		private byte _blockTypeByte;

		public BlockType blockType
		{
			get { return (BlockType)_blockTypeByte; }
			set { _blockTypeByte = (byte)value; }
		}

#if STORE_AS_BYTE
		private byte _densityByte;

		private const float DENSITY_BYTE_LIMIT = 8f;
		private const float DENSITY_BYTE_CONVERT = 127.5f/DENSITY_BYTE_LIMIT;
		private const float DENSITY_BYTE_CONVERT_INV = 1f/DENSITY_BYTE_CONVERT;

		public float density
		{
			get { return (_densityByte - 127.5f) * DENSITY_BYTE_CONVERT_INV; }
			set
			{
				float encoded = Math.Min(DENSITY_BYTE_LIMIT, Math.Max(-DENSITY_BYTE_LIMIT, value)) * DENSITY_BYTE_CONVERT + 127.5f;
				_densityByte = (byte)Math.Max(0f, Math.Min(255f, encoded + 0.5f));
			}
		}

		public Voxel(float density = -1.0f, BlockType blockType = BlockType.Stone)
		{
			float encoded = Math.Min(DENSITY_BYTE_LIMIT, Math.Max(-DENSITY_BYTE_LIMIT, density)) * DENSITY_BYTE_CONVERT + 127.5f;
			_densityByte = (byte)Math.Max(0f, Math.Min(255f, encoded + 0.5f));
			_blockTypeByte = (byte)blockType;
		}

		public bool IsSolid()
		{
			return _densityByte > 127;
		}

#elif STORE_AS_FLOAT
		public float density;

		public Voxel(float density = -1.0f, BlockType blockType = BlockType.Stone)
		{
			this.density = density;
			_blockTypeByte = (byte)blockType;
		}

		public bool IsSolid()
		{
			return density >= 0f;
		}
#else
#error No voxel density storage define set
#endif
	}
}
