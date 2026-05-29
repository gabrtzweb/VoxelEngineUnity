using UnityEngine;

namespace VoxelEngine
{
	public class DebugScreen : MonoBehaviour
	{
		public VoxelEngineManager voxelEngineManager;
		public Transform targetTransform;

		public bool showDebugOverlay = true;
		public bool showSimdLevel = true;
		public bool showPooledChunks = true;
		public bool showPooledChunkGameObjects = true;
		public bool showChunksLoaded = true;
		public bool showChunkQueue = true;
		public bool showChunkMeshQueue = true;
		public bool showMeshesLastFrame = true;
		public bool showUpdateTime = true;
		public bool showThreadCount = true;
		public bool showFps = true;
		public bool showCoordinates = true;
		public bool showDirection = true;
		public bool showBiome = true;

		private void Awake()
		{
			if (voxelEngineManager == null)
				voxelEngineManager = FindAnyObjectByType<VoxelEngineManager>();
		}

		private void OnGUI()
		{
			if (!showDebugOverlay)
				return;

			int labelSpacing = 18;
			Rect rect = new Rect(4, 4, 420, 20);

			if (voxelEngineManager == null)
			{
				GUI.Label(rect, "DebugScreen: No VoxelEngineManager found.");
				return;
			}

			if (showSimdLevel)
			{
				GUI.Label(rect, "SIMD Level: " + FastNoiseSIMD.GetSIMDLevel());
				rect.y += labelSpacing;
			}

			if (showPooledChunks)
			{
				GUI.Label(rect, "Pooled Chunks: " + voxelEngineManager.PooledChunkCount);
				rect.y += labelSpacing;
			}

			if (showPooledChunkGameObjects)
			{
				GUI.Label(rect, "Pooled Chunk GameObjects: " + voxelEngineManager.PooledChunkGameObjectCount);
				rect.y += labelSpacing;
			}

			if (showChunksLoaded)
			{
				GUI.Label(rect, "Chunks Loaded: " + voxelEngineManager.LoadedChunkCount);
				rect.y += labelSpacing;
			}

			if (showChunkQueue)
			{
				GUI.Label(rect, "Chunk Queue: " + voxelEngineManager.ChunkQueueCount);
				rect.y += labelSpacing;
			}

			if (showChunkMeshQueue)
			{
				GUI.Label(rect, "Chunk Mesh Queue: " + voxelEngineManager.ChunkMeshQueueCount);
				rect.y += labelSpacing;
			}

			if (showMeshesLastFrame)
			{
				GUI.Label(rect, "Meshes Last Frame: " + voxelEngineManager.MeshesLastFrame);
				rect.y += labelSpacing;
			}

			if (showUpdateTime)
			{
				GUI.Label(rect, "Update Time Last Frame: " + voxelEngineManager.UpdateTimeLastFrameMs + "ms");
				rect.y += labelSpacing;
			}

			if (showThreadCount)
			{
				GUI.Label(rect, "Thread Count: " + voxelEngineManager.ThreadCount);
				rect.y += labelSpacing;
			}

			if (showFps)
			{
				GUI.Label(rect, "FPS: " + voxelEngineManager.AverageFPS.ToString("0.0"));
				rect.y += labelSpacing;
			}

			Transform debugTarget = ResolveDebugTarget();
			if (debugTarget == null)
				return;

			if (showCoordinates)
			{
				Vector3 pos = debugTarget.position;
				GUI.Label(rect, "Coordinates: (" + Mathf.FloorToInt(pos.x) + ", " + Mathf.FloorToInt(pos.y) + ", " + Mathf.FloorToInt(pos.z) + ")");
				rect.y += labelSpacing;
			}

			if (showDirection)
			{
				GUI.Label(rect, "Direction: " + GetCardinalDirection(debugTarget.forward));
				rect.y += labelSpacing;
			}

			if (showBiome)
				GUI.Label(rect, "Biome: " + GetBiomeName(debugTarget.position));
		}

		private Transform ResolveDebugTarget()
		{
			if (targetTransform != null)
				return targetTransform;

			if (voxelEngineManager != null && voxelEngineManager.targetTransform != null)
				return voxelEngineManager.targetTransform;

			if (Camera.main != null)
				return Camera.main.transform;

			return null;
		}

		private static string GetCardinalDirection(Vector3 forward)
		{
			Vector2 dir = new Vector2(forward.x, forward.z);
			if (dir.sqrMagnitude < 0.0001f)
				return "Unknown";

			dir.Normalize();
			float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
			if (angle < 0f)
				angle += 360f;

			if (angle >= 337.5f || angle < 22.5f)
				return "North";
			if (angle < 67.5f)
				return "North-East";
			if (angle < 112.5f)
				return "East";
			if (angle < 157.5f)
				return "South-East";
			if (angle < 202.5f)
				return "South";
			if (angle < 247.5f)
				return "South-West";
			if (angle < 292.5f)
				return "West";
			return "North-West";
		}

		private string GetBiomeName(Vector3 worldPosition)
		{
			TerrainGeneratorSIMD_World worldGenerator = voxelEngineManager.terrainGenerator as TerrainGeneratorSIMD_World;
			if (worldGenerator == null)
				return "Unknown";

			return worldGenerator.GetSurfaceBiomeName(worldPosition);
		}
	}
}
