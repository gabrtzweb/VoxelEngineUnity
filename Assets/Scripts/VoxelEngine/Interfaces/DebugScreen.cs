using UnityEngine;

namespace VoxelEngine
{
	// Manages the debug information display for the voxel engine
	// Provides toggleable debug features and player information visualization
	[RequireComponent(typeof(VoxelEngineManager))]
	public class DebugScreen : MonoBehaviour
	{
		[Header("Debug Features")]
		public bool showPooledChunks = true;
		public bool showPooledGameObjects = true;
		public bool showLoadedChunks = true;
		public bool showChunkQueue = true;
		public bool showMeshQueue = true;
		public bool showMeshesLastFrame = true;
		public bool showUpdateTime = true;
		public bool showThreadCount = true;
		public bool showFPS = true;
		public bool showPlayerCoordinates = true;
		public bool showFacingDirection = true;

		private VoxelEngineManager voxelEngineManager;
		private Transform targetTransform;

		private int labelSpacing = 18;

		void Start()
		{
			voxelEngineManager = GetComponent<VoxelEngineManager>();
			if (voxelEngineManager != null)
			{
				targetTransform = voxelEngineManager.targetTransform;
			}
		}

		void OnGUI()
		{
			Rect rect = new Rect(4, 0, 300, 20);
			labelSpacing = 18;

			// Draw pooled chunks info
			if (showPooledChunks)
			{
				GUI.Label(rect, "Pooled Chunks: " + voxelEngineManager.GetPooledChunksCount());
				rect.y += labelSpacing;
			}

			// Draw pooled chunk gameobjects info
			if (showPooledGameObjects)
			{
				GUI.Label(rect, "Pooled Chunk GameObjects: " + voxelEngineManager.GetPooledGameObjectsCount());
				rect.y += labelSpacing;
			}

			// Draw loaded chunks info
			if (showLoadedChunks)
			{
				GUI.Label(rect, "Chunks Loaded: " + voxelEngineManager.GetLoadedChunksCount());
				rect.y += labelSpacing;
			}

			// Draw chunk queue info
			if (showChunkQueue)
			{
				GUI.Label(rect, "Chunk Queue: " + voxelEngineManager.GetChunkQueueCount());
				rect.y += labelSpacing;
			}

			// Draw mesh queue info
			if (showMeshQueue)
			{
				GUI.Label(rect, "Chunk Mesh Queue: " + voxelEngineManager.GetMeshQueueCount());
				rect.y += labelSpacing;
			}

			// Draw meshes last frame
			if (showMeshesLastFrame)
			{
				GUI.Label(rect, "Meshes Last Frame: " + voxelEngineManager.GetMeshesLastFrame());
				rect.y += labelSpacing;
			}

			// Draw update time
			if (showUpdateTime)
			{
				GUI.Label(rect, "Update Time Last Frame: " + voxelEngineManager.GetUpdateTimerLastFrame() + "ms");
				rect.y += labelSpacing;
			}

			// Draw thread count
			if (showThreadCount)
			{
				GUI.Label(rect, "Thread Count: " + voxelEngineManager.GetThreadCount());
				rect.y += labelSpacing;
			}

			// Draw FPS
			if (showFPS)
			{
				GUI.Label(rect, "FPS: " + string.Format("{0:0.0}", voxelEngineManager.GetAverageFPS()));
				rect.y += labelSpacing;
			}

			// Draw player coordinates
			if (showPlayerCoordinates && targetTransform != null)
			{
				Vector3 pos = targetTransform.position;
				GUI.Label(rect, string.Format("Position: X:{0:0.0} Y:{1:0.0} Z:{2:0.0}", pos.x, pos.y, pos.z));
				rect.y += labelSpacing;
			}

			// Draw facing direction
			if (showFacingDirection && targetTransform != null)
			{
				Vector3 forward = targetTransform.forward;
				string direction = GetDirectionName(forward);
				GUI.Label(rect, "Facing: " + direction + string.Format(" ({0:0.0}, {1:0.0}, {2:0.0})", forward.x, forward.y, forward.z));
				rect.y += labelSpacing;
			}
		}

		/// <summary>
		/// Converts a forward vector into a cardinal direction name
		/// </summary>
		private string GetDirectionName(Vector3 forward)
		{
			// Get the horizontal direction (ignore Y component)
			Vector3 horizontalForward = new Vector3(forward.x, 0, forward.z).normalized;

			// Determine primary direction
			if (Vector3.Dot(horizontalForward, Vector3.forward) > 0.7f)
				return "North";
			else if (Vector3.Dot(horizontalForward, Vector3.back) > 0.7f)
				return "South";
			else if (Vector3.Dot(horizontalForward, Vector3.right) > 0.7f)
				return "East";
			else if (Vector3.Dot(horizontalForward, Vector3.left) > 0.7f)
				return "West";
			else if (Vector3.Dot(horizontalForward, Vector3.forward + Vector3.right) > 0.5f)
				return "North-East";
			else if (Vector3.Dot(horizontalForward, Vector3.forward + Vector3.left) > 0.5f)
				return "North-West";
			else if (Vector3.Dot(horizontalForward, Vector3.back + Vector3.right) > 0.5f)
				return "South-East";
			else if (Vector3.Dot(horizontalForward, Vector3.back + Vector3.left) > 0.5f)
				return "South-West";

			return "Unknown";
		}

		/// <summary>
		/// Toggle a specific debug feature by name
		/// </summary>
		public void ToggleFeature(string featureName)
		{
			switch (featureName.ToLower())
			{
				case "pooledchunks":
					showPooledChunks = !showPooledChunks;
					break;
				case "pooledobjects":
					showPooledGameObjects = !showPooledGameObjects;
					break;
				case "loadedchunks":
					showLoadedChunks = !showLoadedChunks;
					break;
				case "chunkqueue":
					showChunkQueue = !showChunkQueue;
					break;
				case "meshqueue":
					showMeshQueue = !showMeshQueue;
					break;
				case "mesheslastframe":
					showMeshesLastFrame = !showMeshesLastFrame;
					break;
				case "updatetime":
					showUpdateTime = !showUpdateTime;
					break;
				case "threadcount":
					showThreadCount = !showThreadCount;
					break;
				case "fps":
					showFPS = !showFPS;
					break;
				case "playercoordinates":
					showPlayerCoordinates = !showPlayerCoordinates;
					break;
				case "facingdirection":
					showFacingDirection = !showFacingDirection;
					break;
			}
		}

		/// <summary>
		/// Enable all debug features
		/// </summary>
		public void EnableAll()
		{
			showPooledChunks = true;
			showPooledGameObjects = true;
			showLoadedChunks = true;
			showChunkQueue = true;
			showMeshQueue = true;
			showMeshesLastFrame = true;
			showUpdateTime = true;
			showThreadCount = true;
			showFPS = true;
			showPlayerCoordinates = true;
			showFacingDirection = true;
		}

		/// <summary>
		/// Disable all debug features
		/// </summary>
		public void DisableAll()
		{
			showPooledChunks = false;
			showPooledGameObjects = false;
			showLoadedChunks = false;
			showChunkQueue = false;
			showMeshQueue = false;
			showMeshesLastFrame = false;
			showUpdateTime = false;
			showThreadCount = false;
			showFPS = false;
			showPlayerCoordinates = false;
			showFacingDirection = false;
		}
	}
}
