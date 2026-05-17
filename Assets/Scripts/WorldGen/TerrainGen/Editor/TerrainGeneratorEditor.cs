using UnityEngine;
using UnityEditor;

namespace VoxelEngine
{
	[CustomEditor(typeof(TerrainGenerator), true)]
	public class TerrainGeneratorEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			DrawPropertiesExcluding(serializedObject, new[] { "m_Script", "fastNoiseUnity" });

			serializedObject.ApplyModifiedProperties();
		}
	}
}