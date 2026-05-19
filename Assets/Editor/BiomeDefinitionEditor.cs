using UnityEditor;
using UnityEngine;

namespace VoxelEngine
{
    [CustomEditor(typeof(BiomeDefinition))]
    public class BiomeDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "m_Script");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
