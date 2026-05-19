using UnityEditor;
using UnityEngine;

namespace VoxelEngine
{
    [CustomEditor(typeof(BiomeWorldGenerator), true)]
    public class BiomeWorldGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "m_Script");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
