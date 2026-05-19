using UnityEditor;
using UnityEngine;

namespace VoxelEngine.Editor
{
    public class BiomeMapWindow : EditorWindow
    {
        private FastNoiseSIMDUnity noiseSource;
        private BiomeDefinition biomeA;
        private BiomeDefinition biomeB;
        private int resolution = 256;
        private Texture2D previewTex;
        private bool showBiomeColors = true;

        [MenuItem("VoxelEngine/Biome Map Viewer")]
        public static void ShowWindow()
        {
            GetWindow<BiomeMapWindow>("Biome Map");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Biome Selector Preview", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "This window can preview either the raw selector noise or a simple two-biome color blend. It does not read the world manager; it only visualizes the noise source you assign here.",
                MessageType.Info);

            noiseSource = (FastNoiseSIMDUnity)EditorGUILayout.ObjectField("SIMD Noise", noiseSource, typeof(FastNoiseSIMDUnity), true);
            showBiomeColors = EditorGUILayout.ToggleLeft("Color by Biome A/B", showBiomeColors);
            biomeA = (BiomeDefinition)EditorGUILayout.ObjectField("Biome A", biomeA, typeof(BiomeDefinition), false);
            biomeB = (BiomeDefinition)EditorGUILayout.ObjectField("Biome B", biomeB, typeof(BiomeDefinition), false);
            resolution = EditorGUILayout.IntSlider("Resolution", resolution, 32, 1024);

            if (GUILayout.Button("Generate Preview"))
                GeneratePreview();

            if (previewTex != null)
            {
                GUILayout.Label(previewTex, GUILayout.Width(Mathf.Min(position.width - 10, resolution)), GUILayout.Height(resolution));
            }
        }

        private void GeneratePreview()
        {
            if (noiseSource == null)
                return;

            previewTex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);

            Vector3[] vectors = new Vector3[resolution * resolution];
            int idx = 0;
            for (int y = 0; y < resolution; y++)
                for (int x = 0; x < resolution; x++)
                    vectors[idx++] = new Vector3(x, y, 0);

            float[] noiseSet = new float[vectors.Length];
            noiseSource.fastNoiseSIMD.FillNoiseSetVector(noiseSet, new FastNoiseSIMD.VectorSet(vectors));

            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < noiseSet.Length; i++)
            {
                min = Mathf.Min(min, noiseSet[i]);
                max = Mathf.Max(max, noiseSet[i]);
            }

            float scale = 1f / Mathf.Max(0.0001f, max - min);
            Color32[] pixels = new Color32[noiseSet.Length];
            for (int i = 0; i < noiseSet.Length; i++)
            {
                float t = Mathf.Clamp01((noiseSet[i] - min) * scale);

                if (showBiomeColors && biomeA != null && biomeB != null)
                    pixels[i] = Color32.Lerp(biomeA.topColor, biomeB.topColor, t);
                else
                {
                    byte v = (byte)Mathf.Clamp(t * 255f, 0f, 255f);
                    pixels[i] = new Color32(v, v, v, 255);
                }
            }

            previewTex.SetPixels32(pixels);
            previewTex.Apply();
        }
    }
}
