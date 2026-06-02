using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace VoxelEngine
{
	public static class TexturePackerEditor
	{
		private const string BlocksFolder = "Assets/Content/Textures/Blocks";
		private const string TextureArrayPath = "Assets/Content/Textures/TextureArray.asset";
		private const string TextureMapPath = "Assets/Scripts/Voxel Engine/Core/TextureMap.cs";
		private static readonly TextureDefinition[] TextureDefinitions =
		{
			new TextureDefinition(BlockData.GrassTopTextureKey, Voxel.BlockType.Grass, MeshBuilder.Direction.Up, BlockData.GrassTopTextureNames, 1),
			new TextureDefinition(BlockData.GrassSideTextureKey, Voxel.BlockType.Grass, MeshBuilder.Direction.Forward, BlockData.GrassSideTextureNames, 1),
			new TextureDefinition(BlockData.GrassBottomTextureKey, Voxel.BlockType.Grass, MeshBuilder.Direction.Down, BlockData.GrassBottomTextureNames, 1),
			new TextureDefinition(BlockData.DirtTextureKey, Voxel.BlockType.Dirt, MeshBuilder.Direction.Up, BlockData.DirtTextureNames, 1),
			new TextureDefinition(BlockData.SandTextureKey, Voxel.BlockType.Sand, MeshBuilder.Direction.Up, BlockData.SandTextureNames, 1),
			new TextureDefinition(BlockData.SlateTextureKey, Voxel.BlockType.Slate, MeshBuilder.Direction.Up, BlockData.SlateTextureNames, 1),
			new TextureDefinition(BlockData.StoneTextureKey, Voxel.BlockType.Stone, MeshBuilder.Direction.Up, BlockData.StoneTextureNames, 1),
			new TextureDefinition(BlockData.WaterTextureKey, Voxel.BlockType.Water, MeshBuilder.Direction.Up, BlockData.WaterTextureNames, 36),
		};

		[MenuItem("VoxelEngine/Texture Packer")]
		public static void PackTextures()
		{
			Dictionary<string, Texture2D> texturesByName = LoadTexturesByName();
			List<PackedSlice> slices = new List<PackedSlice>();

			foreach (TextureDefinition definition in TextureDefinitions)
			{
				for (int i = 0; i < definition.textureNames.Length; i++)
				{
					string textureName = definition.textureNames[i];
					if (!texturesByName.TryGetValue(textureName, out Texture2D texture))
					{
						Debug.LogWarning("Texture Packer: missing texture asset '" + textureName + "' for key '" + definition.textureKey + "'.");
						continue;
					}

					AddTextureSlices(definition, textureName, texture, slices);
				}
			}

			if (slices.Count == 0)
			{
				Debug.LogWarning("Texture Packer: no textures found in " + BlocksFolder);
				return;
			}

			Texture2D firstTexture = slices[0].texture;
			int width = slices[0].width;
			int height = slices[0].height;
			TextureFormat format = firstTexture.format;
			bool mipChain = firstTexture.mipmapCount > 1;
			TextureImporter firstImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(firstTexture)) as TextureImporter;
			bool linear = firstImporter != null && !firstImporter.sRGBTexture;

			List<PackedSlice> validSlices = new List<PackedSlice>();
			for (int i = 0; i < slices.Count; i++)
			{
				PackedSlice slice = slices[i];
				if (slice.width != width || slice.height != height)
				{
					Debug.LogWarning("Texture Packer: skipping " + slice.assetPath + " because it does not match the base texture size " + width + "x" + height + ".");
					continue;
				}

				if (slice.texture.format != format)
				{
					Debug.LogWarning("Texture Packer: skipping " + slice.assetPath + " because it does not match the base texture format " + format + ".");
					continue;
				}

				validSlices.Add(slice);
			}

			if (validSlices.Count == 0)
			{
				Debug.LogError("Texture Packer: no compatible textures were found to pack.");
				return;
			}

			Texture2DArray textureArray = new Texture2DArray(width, height, validSlices.Count, format, mipChain, linear)
			{
				name = "TextureArray",
				wrapMode = TextureWrapMode.Repeat,
				filterMode = FilterMode.Bilinear,
				anisoLevel = 1
			};

			for (int layer = 0; layer < validSlices.Count; layer++)
			{
				CopySlice(validSlices[layer], textureArray, layer, mipChain);
			}

			AssetDatabase.DeleteAsset(TextureArrayPath);
			AssetDatabase.CreateAsset(textureArray, TextureArrayPath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			WriteTextureMap(validSlices);

			Debug.Log("Texture Packer: packed " + validSlices.Count + " slices into " + TextureArrayPath + " and updated TextureMap.cs.");
		}

		private static Dictionary<string, Texture2D> LoadTexturesByName()
		{
			Dictionary<string, Texture2D> texturesByName = new Dictionary<string, Texture2D>();
			string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { BlocksFolder });

			foreach (string guid in textureGuids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

				if (!texture)
					continue;

				texturesByName[Path.GetFileNameWithoutExtension(assetPath)] = texture;
			}

			return texturesByName;
		}

		private static void AddTextureSlices(TextureDefinition definition, string textureName, Texture2D texture, List<PackedSlice> slices)
		{
			int sourceWidth = texture.width;
			int sourceHeight = texture.height;
			int frameCount = 1;
			int frameHeight = sourceHeight;

			if (sourceHeight > sourceWidth)
			{
				if (sourceWidth == 0 || sourceHeight % sourceWidth != 0)
				{
					Debug.LogWarning("Texture Packer: skipping " + textureName + " because its height is not divisible by its width.");
					return;
				}

				frameCount = sourceHeight / sourceWidth;
				frameHeight = sourceWidth;
			}

			if (definition.frameCount > 1 && definition.frameCount != frameCount)
			{
				Debug.LogWarning("Texture Packer: texture '" + textureName + "' was expected to have " + definition.frameCount + " frames but has " + frameCount + ".");
			}

			if (frameCount > 1)
			{
				for (int frame = 0; frame < frameCount; frame++)
				{
					slices.Add(new PackedSlice
					{
						assetPath = AssetDatabase.GetAssetPath(texture),
						texture = texture,
						textureKey = definition.textureKey,
						textureName = textureName,
						blockType = definition.blockType,
						faceDirection = definition.faceDirection,
						sourceX = 0,
						sourceY = frame * frameHeight,
						width = sourceWidth,
						height = frameHeight,
						frameCount = frameCount
					});
				}
				return;
			}

			slices.Add(new PackedSlice
			{
				assetPath = AssetDatabase.GetAssetPath(texture),
				texture = texture,
				textureKey = definition.textureKey,
				textureName = textureName,
				blockType = definition.blockType,
				faceDirection = definition.faceDirection,
				sourceX = 0,
				sourceY = 0,
				width = sourceWidth,
				height = sourceHeight,
				frameCount = frameCount
			});
		}

		private static void CopySlice(PackedSlice slice, Texture2DArray textureArray, int layer, bool mipChain)
		{
			int mipCount = mipChain ? slice.texture.mipmapCount : 1;

			for (int mip = 0; mip < mipCount; mip++)
			{
				int sourceX = Mathf.Max(0, slice.sourceX >> mip);
				int sourceY = Mathf.Max(0, slice.sourceY >> mip);
				int sourceWidth = Mathf.Max(1, slice.width >> mip);
				int sourceHeight = Mathf.Max(1, slice.height >> mip);

				Graphics.CopyTexture(slice.texture, 0, mip, sourceX, sourceY, sourceWidth, sourceHeight, textureArray, layer, mip, 0, 0);
			}
		}

		private static void WriteTextureMap(List<PackedSlice> slices)
		{
			Dictionary<string, List<int>> variationIndices = new Dictionary<string, List<int>>();
			for (int i = 0; i < slices.Count; i++)
			{
				PackedSlice slice = slices[i];
				if (!variationIndices.TryGetValue(slice.textureKey, out List<int> indices))
				{
					indices = new List<int>();
					variationIndices.Add(slice.textureKey, indices);
				}

				indices.Add(i);
			}

			StringBuilder builder = new StringBuilder();
			builder.AppendLine("using System.Collections.Generic;");
			builder.AppendLine("using UnityEngine;");
			builder.AppendLine();
			builder.AppendLine("namespace VoxelEngine");
			builder.AppendLine("{");
			builder.AppendLine("\tpublic static class TextureMap");
			builder.AppendLine("\t{");
			builder.AppendLine();
			builder.AppendLine("\t\tpublic static readonly Dictionary<string, string[]> TextureNames = new Dictionary<string, string[]>");
			builder.AppendLine("\t\t{");
			foreach (TextureDefinition definition in TextureDefinitions)
			{
				builder.AppendLine("\t\t\t{ \"" + definition.textureKey + "\", new[] { " + BuildTextureNameValues(definition.textureNames) + " } },");
			}
			builder.AppendLine("\t\t};");
			builder.AppendLine();
			builder.AppendLine("\t\tpublic static readonly Dictionary<string, int[]> Variations = new Dictionary<string, int[]>");
			builder.AppendLine("\t\t{");
			int currentLayer = 0;
			foreach (TextureDefinition definition in TextureDefinitions)
			{
				int count = variationIndices.TryGetValue(definition.textureKey, out List<int> indices) ? indices.Count : 0;
				string layerValues = BuildLayerValues(currentLayer, count);
				builder.AppendLine("\t\t\t{ \"" + definition.textureKey + "\", new int[] { " + layerValues + " } },");
				currentLayer += count;
			}
			builder.AppendLine("\t\t};");
			builder.AppendLine();
			builder.AppendLine("\t\tpublic static readonly Dictionary<string, int> FrameCounts = new Dictionary<string, int>");
			builder.AppendLine("\t\t{");
			foreach (TextureDefinition definition in TextureDefinitions)
			{
				builder.AppendLine("\t\t\t{ \"" + definition.textureKey + "\", " + definition.frameCount + " },");
			}
			builder.AppendLine("\t\t};");
			builder.AppendLine();
			builder.AppendLine("\t\tpublic static Color32 GetTint(Voxel.BlockType blockType)");
			builder.AppendLine("\t\t{");
			builder.AppendLine("\t\t\tswitch (blockType)");
			builder.AppendLine("\t\t\t{");
			builder.AppendLine("\t\t\t\tcase Voxel.BlockType.Grass:");
			builder.AppendLine("\t\t\t\t\treturn BlockData.GrassColor;");
			builder.AppendLine("\t\t\t\tcase Voxel.BlockType.Dirt:");
			builder.AppendLine("\t\t\t\t\treturn BlockData.DirtColor;");
			builder.AppendLine("\t\t\t\tcase Voxel.BlockType.Sand:");
			builder.AppendLine("\t\t\t\t\treturn BlockData.SandColor;");
			builder.AppendLine("\t\t\t\tcase Voxel.BlockType.Water:");
			builder.AppendLine("\t\t\t\t\treturn BlockData.WaterColor;");
			builder.AppendLine("\t\t\t\tcase Voxel.BlockType.Stone:");
			builder.AppendLine("\t\t\t\t\treturn BlockData.StoneColor;");
			builder.AppendLine("\t\t\t\tcase Voxel.BlockType.Slate:");
			builder.AppendLine("\t\t\t\t\treturn BlockData.SlateColor;");
			builder.AppendLine("\t\t\t\tdefault:");
			builder.AppendLine("\t\t\t\t\treturn BlockData.StoneColor;");
			builder.AppendLine("\t\t\t}");
			builder.AppendLine("\t\t}");
			builder.AppendLine();
			builder.AppendLine("\t\tpublic static string GetTextureKey(Voxel.BlockType blockType)");
			builder.AppendLine("\t\t{");
			builder.AppendLine("\t\t\treturn GetTextureKey(blockType, MeshBuilder.Direction.Up);");
			builder.AppendLine("\t\t}");
			builder.AppendLine();
			builder.AppendLine("\t\tpublic static string GetTextureKey(Voxel.BlockType blockType, MeshBuilder.Direction faceDirection)");
			builder.AppendLine("\t\t{");
			builder.AppendLine("\t\t\treturn BlockData.GetTextureKey(blockType, faceDirection);");
			builder.AppendLine("\t\t}");
			builder.AppendLine();
			builder.AppendLine("\t\tpublic static string[] GetTextureNames(Voxel.BlockType blockType)");
			builder.AppendLine("\t\t{");
			builder.AppendLine("\t\t\treturn GetTextureNames(blockType, MeshBuilder.Direction.Up);");
			builder.AppendLine("\t\t}");
			builder.AppendLine();
			builder.AppendLine("\t\tpublic static string[] GetTextureNames(Voxel.BlockType blockType, MeshBuilder.Direction faceDirection)");
			builder.AppendLine("\t\t{");
			builder.AppendLine("\t\t\tstring textureKey = GetTextureKey(blockType, faceDirection);");
			builder.AppendLine("\t\t\treturn TextureNames.TryGetValue(textureKey, out string[] names) ? names : new string[0];");
			builder.AppendLine("\t\t}");
			builder.AppendLine();
			builder.AppendLine("\t\tpublic static Voxel.BlockType GetBlockType(Color32 tint)");
			builder.AppendLine("\t\t{");
			builder.AppendLine("\t\t\tif (tint.Equals(BlockData.GrassColor))");
			builder.AppendLine("\t\t\t\treturn Voxel.BlockType.Grass;");
			builder.AppendLine("\t\t\tif (tint.Equals(BlockData.DirtColor))");
			builder.AppendLine("\t\t\t\treturn Voxel.BlockType.Dirt;");
			builder.AppendLine("\t\t\tif (tint.Equals(BlockData.SandColor))");
			builder.AppendLine("\t\t\t\treturn Voxel.BlockType.Sand;");
			builder.AppendLine("\t\t\tif (tint.Equals(BlockData.WaterColor))");
			builder.AppendLine("\t\t\t\treturn Voxel.BlockType.Water;");
			builder.AppendLine("\t\t\tif (tint.Equals(BlockData.SlateColor))");
			builder.AppendLine("\t\t\t\treturn Voxel.BlockType.Slate;");
			builder.AppendLine("\t\t\treturn Voxel.BlockType.Stone;");
			builder.AppendLine("\t\t}");
			builder.AppendLine();
			builder.AppendLine("\t\tpublic static int GetTextureLayer(Color32 tint, int worldX, int worldY, int worldZ, MeshBuilder.Direction faceDirection)");
			builder.AppendLine("\t\t{");
			builder.AppendLine("\t\t\treturn GetTextureLayer(GetBlockType(tint), worldX, worldY, worldZ, faceDirection, out _);");
			builder.AppendLine("\t\t}");
			builder.AppendLine();
			builder.AppendLine("\t\tpublic static int GetTextureLayer(Voxel.BlockType blockType, int worldX, int worldY, int worldZ, MeshBuilder.Direction faceDirection)");
			builder.AppendLine("\t\t{");
			builder.AppendLine("\t\t\treturn GetTextureLayer(blockType, worldX, worldY, worldZ, faceDirection, out _);");
			builder.AppendLine("\t\t}");
			builder.AppendLine();
			builder.AppendLine("\t\tpublic static int GetTextureLayer(Voxel.BlockType blockType, int worldX, int worldY, int worldZ, MeshBuilder.Direction faceDirection, out int frameCount)");
			builder.AppendLine("\t\t{");
			builder.AppendLine("\t\t\tstring textureKey = GetTextureKey(blockType, faceDirection);");
			builder.AppendLine("\t\t\tframeCount = GetTextureFrameCount(textureKey);");
			builder.AppendLine("\t\t\tif (string.IsNullOrEmpty(textureKey))");
			builder.AppendLine("\t\t\t\treturn 0;");
			builder.AppendLine();
			builder.AppendLine("\t\t\tif (!Variations.TryGetValue(textureKey, out int[] layers) || layers.Length == 0)");
			builder.AppendLine("\t\t\t\treturn 0;");
			builder.AppendLine();
			builder.AppendLine("\t\t\tif (frameCount > 1)");
			builder.AppendLine("\t\t\t\treturn layers[0];");
			builder.AppendLine();
			builder.AppendLine("\t\t\tif (layers.Length == 1)");
			builder.AppendLine("\t\t\t\treturn layers[0];");
			builder.AppendLine();
			builder.AppendLine("\t\t\tint hash = Hash(worldX, worldY, worldZ, (int)blockType);");
			builder.AppendLine("\t\t\tint layerIndex = PositiveModulo(hash, layers.Length);");
			builder.AppendLine("\t\t\treturn layers[layerIndex];");
			builder.AppendLine("\t\t}");
			builder.AppendLine();
			builder.AppendLine("\t\tpublic static int GetTextureFrameCount(Voxel.BlockType blockType)");
			builder.AppendLine("\t\t{");
			builder.AppendLine("\t\t\treturn GetTextureFrameCount(GetTextureKey(blockType));");
			builder.AppendLine("\t\t}");
			builder.AppendLine();
			builder.AppendLine("\t\tpublic static int GetTextureFrameCount(Voxel.BlockType blockType, MeshBuilder.Direction faceDirection)");
			builder.AppendLine("\t\t{");
			builder.AppendLine("\t\t\treturn GetTextureFrameCount(GetTextureKey(blockType, faceDirection));");
			builder.AppendLine("\t\t}");
			builder.AppendLine();
			builder.AppendLine("\t\tpublic static int GetTextureFrameCount(string textureKey)");
			builder.AppendLine("\t\t{");
			builder.AppendLine("\t\t\tif (string.IsNullOrEmpty(textureKey))");
			builder.AppendLine("\t\t\t\treturn 1;");
			builder.AppendLine();
			builder.AppendLine("\t\t\treturn FrameCounts.TryGetValue(textureKey, out int frameCount) ? Mathf.Max(1, frameCount) : 1;");
			builder.AppendLine("\t\t}");
			builder.AppendLine();
			builder.AppendLine("\t\tpublic static int GetTextureCount(Voxel.BlockType blockType)");
			builder.AppendLine("\t\t{");
			builder.AppendLine("\t\t\tstring textureKey = GetTextureKey(blockType);");
			builder.AppendLine("\t\t\treturn Variations.TryGetValue(textureKey, out int[] layers) ? layers.Length : 0;");
			builder.AppendLine("\t\t}");
			builder.AppendLine();
			builder.AppendLine("\t\tprivate static int Hash(int x, int y, int z, int blockType)");
			builder.AppendLine("\t\t{");
			builder.AppendLine("\t\t\tunchecked");
			builder.AppendLine("\t\t\t{");
			builder.AppendLine("\t\t\t\tint hash = 17;");
			builder.AppendLine("\t\t\t\thash = hash * 31 + x;");
			builder.AppendLine("\t\t\t\thash = hash * 31 + y;");
			builder.AppendLine("\t\t\t\thash = hash * 31 + z;");
			builder.AppendLine("\t\t\t\thash = hash * 31 + blockType;");
			builder.AppendLine("\t\t\t\treturn hash;");
			builder.AppendLine("\t\t\t}");
			builder.AppendLine("\t\t}");
			builder.AppendLine();
			builder.AppendLine("\t\tprivate static int PositiveModulo(int value, int modulo)");
			builder.AppendLine("\t\t{");
			builder.AppendLine("\t\t\tint result = value % modulo;");
			builder.AppendLine("\t\t\treturn result < 0 ? result + modulo : result;");
			builder.AppendLine("\t\t}");
			builder.AppendLine("\t}");
			builder.AppendLine("}");

			Directory.CreateDirectory(Path.GetDirectoryName(TextureMapPath));
			File.WriteAllText(TextureMapPath, builder.ToString());
			AssetDatabase.ImportAsset(TextureMapPath);
		}

		private static string BuildLayerValues(int startLayer, int count)
		{
			if (count <= 0)
				return string.Empty;

			StringBuilder values = new StringBuilder();
			for (int i = 0; i < count; i++)
			{
				if (i > 0)
					values.Append(", ");

				values.Append(startLayer + i);
			}

			return values.ToString();
		}

		private static string BuildTextureNameValues(string[] textureNames)
		{
			StringBuilder values = new StringBuilder();
			for (int i = 0; i < textureNames.Length; i++)
			{
				if (i > 0)
					values.Append(", ");

				values.Append('\"').Append(textureNames[i]).Append('\"');
			}

			return values.ToString();
		}

		private static Voxel.BlockType GetBlockTypeFromName(string textureName)
		{
			string normalizedName = textureName.ToLowerInvariant();

			if (normalizedName.Contains("grass_side"))
				return Voxel.BlockType.Grass;

			if (normalizedName.Contains("grass"))
				return Voxel.BlockType.Grass;

			if (normalizedName.Contains("dirt"))
				return Voxel.BlockType.Dirt;

			if (normalizedName.Contains("sand"))
				return Voxel.BlockType.Sand;

			if (normalizedName.Contains("water") || normalizedName.Contains("liqd"))
				return Voxel.BlockType.Water;

			if (normalizedName.Contains("slate"))
				return Voxel.BlockType.Slate;

			if (normalizedName.Contains("stone") || normalizedName.Contains("rock"))
				return Voxel.BlockType.Stone;

			return Voxel.BlockType.Stone;
		}

		private struct TextureDefinition
		{
			public Voxel.BlockType blockType;
			public MeshBuilder.Direction faceDirection;
			public string textureKey;
			public string[] textureNames;
			public int frameCount;

			public TextureDefinition(string textureKey, Voxel.BlockType blockType, MeshBuilder.Direction faceDirection, string[] textureNames, int frameCount)
			{
				this.textureKey = textureKey;
				this.blockType = blockType;
				this.faceDirection = faceDirection;
				this.textureNames = textureNames;
				this.frameCount = frameCount;
			}
		}

		private struct PackedSlice
		{
			public string assetPath;
			public Texture2D texture;
			public string textureKey;
			public string textureName;
			public Voxel.BlockType blockType;
			public MeshBuilder.Direction faceDirection;
			public int sourceX;
			public int sourceY;
			public int width;
			public int height;
			public int frameCount;
		}
	}
}