using UnityEditor;
using UnityEngine;

namespace VoxelEngine.Editor
{
    public class BiomeMapWindow : EditorWindow
    {
        private VoxelEngine.BiomeWorldGenerator generator;
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

            generator = (VoxelEngine.BiomeWorldGenerator)EditorGUILayout.ObjectField("Biome Generator", generator, typeof(VoxelEngine.BiomeWorldGenerator), true);
            showBiomeColors = EditorGUILayout.ToggleLeft("Color by Biome Colors", showBiomeColors);
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
            if (generator == null)
                return;

            previewTex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);

            // Prepare per-pixel vectors (we map pixels to a 0..1 climate space)
            FastNoiseSIMD.VectorSet vecSet = new FastNoiseSIMD.VectorSet(new Vector3[resolution * resolution]);
            Vector3[] vecs = vecSet.vectors;
            int idx = 0;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float u = (x + 0.5f) / resolution;
                    float v = (y + 0.5f) / resolution;
                    // pack into vector x=u,y=v,z=0
                    vecs[idx++].x = u;
                    vecs[idx - 1].y = v;
                    vecs[idx - 1].z = 0f;
                }
            }

            float[] tempSet = null;
            float[] humSet = null;

            if (generator.temperatureNoise != null)
            {
                tempSet = new float[vecs.Length];
                generator.temperatureNoise.fastNoiseSIMD.FillNoiseSetVector(tempSet, vecSet);
            }

            if (generator.humidityNoise != null)
            {
                humSet = new float[vecs.Length];
                generator.humidityNoise.fastNoiseSIMD.FillNoiseSetVector(humSet, vecSet);
            }

            Color32[] pixels = new Color32[vecs.Length];

            for (int i = 0; i < vecs.Length; i++)
            {
                float temp = tempSet != null ? Mathf.Clamp01((tempSet[i] + 1f) * 0.5f) : 0.5f;
                float hum = humSet != null ? Mathf.Clamp01((humSet[i] + 1f) * 0.5f) : 0.5f;
                float elev = 0.5f;

                // choose nearest biome by climatePref
                int best = 0;
                float bestDist = float.MaxValue;
                for (int b = 0; b < generator.biomeSources.Length; b++)
                {
                    var bs = generator.biomeSources[b];
                    if (bs.biome == null) continue;
                    Vector3 p = bs.climatePref;
                    float dt = temp - p.x;
                    float dh = hum - p.y;
                    float de = elev - p.z;
                    float dist = dt * dt + dh * dh + de * de;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = b;
                    }
                }

                Color32 col = new Color32(150, 150, 150, 255);
                if (generator.biomeSources != null && generator.biomeSources.Length > 0 && generator.biomeSources[best].biome != null)
                    col = generator.biomeSources[best].biome.topColor;

                pixels[i] = showBiomeColors ? col : new Color32((byte)(temp * 255f), (byte)(hum * 255f), (byte)(elev * 255f), 255);
            }

            previewTex.SetPixels32(pixels);
            previewTex.Apply();
        }
    }
}
