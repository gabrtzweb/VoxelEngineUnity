using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VoxelEngine
{
	public class TexturePackerEditor : EditorWindow
	{
		private string sourceFolder = "Assets/Content/Textures/Blocks";
		private string textureArrayAssetPath = "Assets/Content/Textures/Blocks/BlockTextureArray.asset";
		private string blockDataAssetPath = "Assets/Content/Textures/Blocks/BlockData.asset";

		[MenuItem("Tools/Voxel Engine/Texture Packer")]
		public static void Open()
		{
			GetWindow<TexturePackerEditor>("Texture Packer");
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Block Texture Baker", EditorStyles.boldLabel);
			sourceFolder = EditorGUILayout.TextField("Source Folder", sourceFolder);
			textureArrayAssetPath = EditorGUILayout.TextField("Texture Array Asset", textureArrayAssetPath);
			blockDataAssetPath = EditorGUILayout.TextField("Block Data Asset", blockDataAssetPath);

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
			List<Texture2D> textures = new List<Texture2D>();

			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

				if (texture != null)
					textures.Add(texture);
			}

			if (textures.Count == 0)
			{
				EditorUtility.DisplayDialog("Texture Packer", "No textures were found in the selected folder.", "OK");
				return;
			}

			textures = textures.OrderBy(texture => texture.name).ToList();

			Texture2D firstTexture = textures[0];
			int width = firstTexture.width;
			int height = firstTexture.height;
			bool mipChain = false;
			List<BlockData.BlockTextureEntry> blockEntries = new List<BlockData.BlockTextureEntry>();

			for (int i = 0; i < textures.Count; i++)
			{
				Texture2D texture = textures[i];
				if (texture.width != width || texture.height != height)
				{
					EditorUtility.DisplayDialog("Texture Packer",
						"All block textures must share the same dimensions. The texture '" + texture.name + "' does not match the first texture.", "OK");
					return;
				}
			}

			foreach (Texture2D texture in textures)
			{
				BlockData.BlockType blockType = BlockData.GuessBlockType(texture.name);
				if (blockType == BlockData.BlockType.Unknown)
				{
					EditorUtility.DisplayDialog("Texture Packer",
						"Could not infer a block type from texture name '" + texture.name + "'. Use one of: terr_grass, terr_dirt, terr_moss, rock_stone, rock_slate, rock_core, liqd_lava, terr_sand.", "OK");
					return;
				}

				blockEntries.Add(new BlockData.BlockTextureEntry
				{
					blockType = blockType,
					name = texture.name,
					sourceTexture = texture,
					layerIndex = 0
				});
			}

			blockEntries = blockEntries
				.OrderBy(entry => entry.blockType)
				.ThenBy(entry => entry.name)
				.ToList();

			EnsureParentFolderExists(textureArrayAssetPath);
			EnsureParentFolderExists(blockDataAssetPath);

			if (File.Exists(textureArrayAssetPath))
				AssetDatabase.DeleteAsset(textureArrayAssetPath);

			Texture2DArray textureArray = new Texture2DArray(width, height, textures.Count, firstTexture.format, mipChain);
			textureArray.filterMode = FilterMode.Point;
			textureArray.wrapMode = TextureWrapMode.Clamp;

			for (int i = 0; i < blockEntries.Count; i++)
			{
				Graphics.CopyTexture(blockEntries[i].sourceTexture, 0, 0, textureArray, i, 0);
				var entry = blockEntries[i];
				entry.layerIndex = i;
				blockEntries[i] = entry;
			}

			AssetDatabase.CreateAsset(textureArray, textureArrayAssetPath);

			BlockData blockData = AssetDatabase.LoadAssetAtPath<BlockData>(blockDataAssetPath);
			if (blockData == null)
			{
				blockData = CreateInstance<BlockData>();
				AssetDatabase.CreateAsset(blockData, blockDataAssetPath);
			}

			blockData.textureArray = textureArray;
			blockData.blockTextures = blockEntries;

			EditorUtility.SetDirty(blockData);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			EditorUtility.DisplayDialog("Texture Packer", "Baked " + textures.Count + " block textures.", "OK");
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