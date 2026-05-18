using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace VoxelEngine
{
	public class TexturePackerEditor : EditorWindow
	{
		private string sourceFolder = "Assets/Content/Textures/Blocks";
		private string textureArrayAssetPath = "Assets/Resources/Voxel/TextureArray.asset";
		private string textureMapScriptPath = "Assets/Scripts/VoxelEngine/Core/TextureMap.cs";

		[MenuItem("VoxelEngine/Texture Packer")]
		public static void Open()
		{
			GetWindow<TexturePackerEditor>("Texture Packer");
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Block Texture Baker", EditorStyles.boldLabel);
			sourceFolder = EditorGUILayout.TextField("Source Folder", sourceFolder);
			textureArrayAssetPath = EditorGUILayout.TextField("Texture Array Asset", textureArrayAssetPath);
			textureMapScriptPath = EditorGUILayout.TextField("Texture Map Script", textureMapScriptPath);

			EditorGUILayout.Space();

			if (GUILayout.Button("Bake Texture Array"))
				Bake();
		}

		private void Bake()
		{
			if (!AssetDatabase.IsValidFolder(sourceFolder))
			{
				EditorUtility.DisplayDialog("Texture Packer", "Source folder not found: " + sourceFolder, "OK");
				return;
			}

			string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { sourceFolder });
			List<string> texturePaths = new List<string>();

			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (!string.IsNullOrEmpty(path))
					texturePaths.Add(path);
			}

			if (texturePaths.Count == 0)
			{
				EditorUtility.DisplayDialog("Texture Packer", "No textures were found in the selected folder.", "OK");
				return;
			}

			texturePaths = texturePaths.OrderBy(path => path).ToList();

			for (int i = 0; i < texturePaths.Count; i++)
				EnsureStandardTextureImporter(texturePaths[i]);

			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

			List<Texture2D> textures = new List<Texture2D>();
			for (int i = 0; i < texturePaths.Count; i++)
			{
				Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePaths[i]);
				if (texture != null)
					textures.Add(texture);
			}

			if (textures.Count == 0)
			{
				EditorUtility.DisplayDialog("Texture Packer", "No readable textures were found after import refresh.", "OK");
				return;
			}

			textures = textures.OrderBy(texture => texture.name).ToList();

			int resolution = textures[0].width;
			Dictionary<string, List<TextureInput>> groups = new Dictionary<string, List<TextureInput>>();
			int totalSlices = 0;

			for (int i = 0; i < textures.Count; i++)
			{
				Texture2D texture = textures[i];
				if (texture.width != resolution || texture.height % resolution != 0)
				{
					EditorUtility.DisplayDialog("Texture Packer",
						"Texture '" + texture.name + "' must have width=" + resolution + " and height as multiple of width.", "OK");
					return;
				}

				TextureNameInfo info = ExtractTextureInfo(texture.name);
				int frames = texture.height / resolution;

				List<TextureInput> list;
				if (!groups.TryGetValue(info.baseName, out list))
				{
					list = new List<TextureInput>();
					groups[info.baseName] = list;
				}

				list.Add(new TextureInput
				{
					name = texture.name,
					texture = texture,
					frames = frames,
					variantIndex = info.variantIndex
				});

				totalSlices += frames;
			}

			EnsureParentFolderExists(textureArrayAssetPath);
			EnsureParentFolderExists(textureMapScriptPath);

			if (File.Exists(textureArrayAssetPath))
				AssetDatabase.DeleteAsset(textureArrayAssetPath);

			Texture2DArray textureArray = new Texture2DArray(resolution, resolution, totalSlices, TextureFormat.RGBA32, false);
			textureArray.filterMode = FilterMode.Point;
			textureArray.wrapMode = TextureWrapMode.Repeat;

			List<MapData> mapData = new List<MapData>();
			int currentSlice = 0;

			foreach (KeyValuePair<string, List<TextureInput>> kv in groups.OrderBy(g => g.Key))
			{
				List<TextureInput> ordered = kv.Value
					.OrderBy(item => item.variantIndex)
					.ThenBy(item => item.name)
					.ToList();

				int[] variations = new int[ordered.Count];
				int frameCount = 1;

				for (int v = 0; v < ordered.Count; v++)
				{
					TextureInput input = ordered[v];
					variations[v] = currentSlice;
					frameCount = Mathf.Max(frameCount, input.frames);

					for (int f = 0; f < input.frames; f++)
					{
						int startY = input.texture.height - ((f + 1) * resolution);
						Color[] pixels = input.texture.GetPixels(0, startY, resolution, resolution);
						textureArray.SetPixels(pixels, currentSlice, 0);
						currentSlice++;
					}
				}

				mapData.Add(new MapData
				{
					name = kv.Key,
					variationIndices = variations,
					frames = frameCount
				});
			}

			textureArray.Apply(false, false);
			AssetDatabase.CreateAsset(textureArray, textureArrayAssetPath);
			GenerateTextureMapScript(textureMapScriptPath, mapData);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			EditorUtility.DisplayDialog("Texture Packer", "Baked " + textures.Count + " textures into " + totalSlices + " array slices.", "OK");
		}

		private static TextureNameInfo ExtractTextureInfo(string textureName)
		{
			string key = BlockData.GuessBlockKey(textureName);
			string fileName = Path.GetFileNameWithoutExtension(textureName).ToLowerInvariant();

			int digitStart = fileName.Length;
			while (digitStart > 0 && char.IsDigit(fileName[digitStart - 1]))
				digitStart--;

			int variant = 0;
			if (digitStart < fileName.Length)
				int.TryParse(fileName.Substring(digitStart), out variant);

			return new TextureNameInfo
			{
				baseName = string.IsNullOrEmpty(key) ? fileName : key,
				variantIndex = variant
			};
		}

		private static void GenerateTextureMapScript(string scriptPath, List<MapData> mapData)
		{
			StringBuilder content = new StringBuilder();
			content.AppendLine("using System.Collections.Generic;");
			content.AppendLine();
			content.AppendLine("namespace VoxelEngine");
			content.AppendLine("{");
			content.AppendLine("\tpublic static class TextureMap");
			content.AppendLine("\t{");
			content.AppendLine("\t\tpublic static readonly Dictionary<string, int[]> Variations = new Dictionary<string, int[]>");
			content.AppendLine("\t\t{");

			for (int i = 0; i < mapData.Count; i++)
			{
				MapData entry = mapData[i];
				string indices = string.Join(", ", entry.variationIndices.Select(value => value.ToString()).ToArray());
				content.AppendLine("\t\t\t{ \"" + entry.name + "\", new int[] { " + indices + " } },");
			}

			content.AppendLine("\t\t};");
			content.AppendLine();
			content.AppendLine("\t\tpublic static readonly Dictionary<string, int> FrameCounts = new Dictionary<string, int>");
			content.AppendLine("\t\t{");

			for (int i = 0; i < mapData.Count; i++)
			{
				MapData entry = mapData[i];
				content.AppendLine("\t\t\t{ \"" + entry.name + "\", " + entry.frames + " },");
			}

			content.AppendLine("\t\t};");
			content.AppendLine("\t}");
			content.AppendLine("}");

			File.WriteAllText(scriptPath, content.ToString());
			AssetDatabase.ImportAsset(scriptPath, ImportAssetOptions.ForceUpdate);
		}

		private static void EnsureStandardTextureImporter(string assetPath)
		{
			TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
			if (importer == null)
				return;

			bool changed = false;

			if (importer.textureCompression != TextureImporterCompression.Uncompressed)
			{
				importer.textureCompression = TextureImporterCompression.Uncompressed;
				changed = true;
			}

			if (!importer.isReadable)
			{
				importer.isReadable = true;
				changed = true;
			}

			if (importer.mipmapEnabled)
			{
				importer.mipmapEnabled = false;
				changed = true;
			}

			if (importer.filterMode != FilterMode.Point)
			{
				importer.filterMode = FilterMode.Point;
				changed = true;
			}

			if (importer.wrapMode != TextureWrapMode.Clamp)
			{
				importer.wrapMode = TextureWrapMode.Clamp;
				changed = true;
			}

			if (changed)
				importer.SaveAndReimport();
		}

		private struct TextureInput
		{
			public string name;
			public Texture2D texture;
			public int frames;
			public int variantIndex;
		}

		private struct TextureNameInfo
		{
			public string baseName;
			public int variantIndex;
		}

		private struct MapData
		{
			public string name;
			public int[] variationIndices;
			public int frames;
		}

		private static void EnsureParentFolderExists(string assetPath)
		{
			string directory = Path.GetDirectoryName(assetPath);
			if (string.IsNullOrEmpty(directory) || AssetDatabase.IsValidFolder(directory))
				return;

			string[] parts = directory.Replace('\\', '/').Split('/');
			string currentPath = parts[0];

			for (int i = 1; i < parts.Length; i++)
			{
				string nextPath = currentPath + "/" + parts[i];
				if (!AssetDatabase.IsValidFolder(nextPath))
					AssetDatabase.CreateFolder(currentPath, parts[i]);

				currentPath = nextPath;
			}
		}
	}
}