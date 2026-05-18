using System.Collections.Generic;

namespace VoxelEngine
{
	public static class TextureMap
	{
		public static readonly Dictionary<string, int[]> Variations = new Dictionary<string, int[]>
		{
			{ "liqd_lava", new int[] { 0 } },
			{ "liqd_molten", new int[] { 1 } },
			{ "liqd_ooze", new int[] { 2 } },
			{ "liqd_tar", new int[] { 3 } },
			{ "liqd_water", new int[] { 4 } },
			{ "rock_blackstone", new int[] { 5, 6 } },
			{ "rock_cobbled_slate", new int[] { 7, 8 } },
			{ "rock_cobblestone", new int[] { 9, 10 } },
			{ "rock_core", new int[] { 11, 12 } },
			{ "rock_mossy_cobbled_slate", new int[] { 13, 14 } },
			{ "rock_mossy_cobblestone", new int[] { 15, 16 } },
			{ "rock_slate", new int[] { 17, 18 } },
			{ "rock_stone", new int[] { 19, 20 } },
			{ "terr_clay", new int[] { 21, 22 } },
			{ "terr_dirt", new int[] { 23, 24 } },
			{ "terr_grass", new int[] { 25, 26, 27, 28, 29, 30, 31, 32 } },
			{ "terr_gravel", new int[] { 33, 34 } },
			{ "terr_moss", new int[] { 35, 36 } },
			{ "terr_mud", new int[] { 37, 38 } },
			{ "terr_mulch", new int[] { 39, 40 } },
			{ "terr_packed_dirt", new int[] { 41, 42 } },
			{ "terr_packed_mud", new int[] { 43, 44 } },
			{ "terr_packed_red_sand", new int[] { 45, 46 } },
			{ "terr_packed_sand", new int[] { 47, 48 } },
			{ "terr_red_sand", new int[] { 49, 50 } },
			{ "terr_rooted_dirt", new int[] { 51, 52 } },
			{ "terr_sand", new int[] { 53, 54 } },
			{ "terr_snow", new int[] { 55, 56 } },
		};

		public static readonly Dictionary<string, int> FrameCounts = new Dictionary<string, int>
		{
			{ "liqd_lava", 1 },
			{ "liqd_molten", 1 },
			{ "liqd_ooze", 1 },
			{ "liqd_tar", 1 },
			{ "liqd_water", 1 },
			{ "rock_blackstone", 1 },
			{ "rock_cobbled_slate", 1 },
			{ "rock_cobblestone", 1 },
			{ "rock_core", 1 },
			{ "rock_mossy_cobbled_slate", 1 },
			{ "rock_mossy_cobblestone", 1 },
			{ "rock_slate", 1 },
			{ "rock_stone", 1 },
			{ "terr_clay", 1 },
			{ "terr_dirt", 1 },
			{ "terr_grass", 1 },
			{ "terr_gravel", 1 },
			{ "terr_moss", 1 },
			{ "terr_mud", 1 },
			{ "terr_mulch", 1 },
			{ "terr_packed_dirt", 1 },
			{ "terr_packed_mud", 1 },
			{ "terr_packed_red_sand", 1 },
			{ "terr_packed_sand", 1 },
			{ "terr_red_sand", 1 },
			{ "terr_rooted_dirt", 1 },
			{ "terr_sand", 1 },
			{ "terr_snow", 1 },
		};
	}
}
