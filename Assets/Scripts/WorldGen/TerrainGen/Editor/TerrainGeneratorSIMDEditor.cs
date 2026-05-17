using UnityEngine;
using UnityEditor;

namespace VoxelEngine
{
	[CustomEditor(typeof(TerrainGeneratorSIMD), true)]
	public class TerrainGeneratorSIMDEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			DrawPropertiesExcluding(serializedObject, new[] { "m_Script", "fastNoiseSIMDUnity" });

			serializedObject.ApplyModifiedProperties();
		}
	}

}