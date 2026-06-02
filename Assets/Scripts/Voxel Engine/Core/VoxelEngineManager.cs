using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine.InputSystem;

namespace VoxelEngine
{
	// This is the main class that manages all the chunks that create the voxel terrain
	// It is resonsible for loading and unloading chunks as the target transform moves around

	public class VoxelEngineManager : MonoBehaviour
	{
		public TerrainGeneratorBase terrainGenerator;
		public Transform targetTransform;
		public float loadDistance = 256f;
		public float unloadDistanceModifier = 1.2f;
		public float yDistanceModifier = 1.5f;
		public int maxThreads = 8;
		public float targetFPS = 60f;
		public Material meshMaterial;

		// More low level voxel engine settings can be found in Chunk.cs

		private static ObjectPool<Chunk> chunkPool = new ObjectPool<Chunk>(128);
		private Dictionary<Vector3i, Chunk> chunkMap = new Dictionary<Vector3i, Chunk>();
		private ChunkQueue chunkQueue = new ChunkQueue();
		private Queue<Vector3i> chunkMeshQueue = new Queue<Vector3i>();
		private HashSet<Vector3i> chunkMeshQueueSet = new HashSet<Vector3i>();
		private Stack<Chunk> chunkUnloadStack = new Stack<Chunk>();

		private int yLoadTick = -1;
		private int unloadTick = 0;
		private int threadCount = 0;
		private int meshesLastFrame = 0;
		private int updateTimerLastFrame = 0;
		private float averageFPS = 0.0f;
		private float deltaTimeFPS = 0.0f;

		public Light directionalLight;
		public Light cameraLight;

		void Awake()
		{
			if (terrainGenerator == null)
				terrainGenerator = FindAnyObjectByType<TerrainGeneratorBase>();

			if (terrainGenerator == null)
				UnityEngine.Debug.LogError("VoxelEngineManager needs a terrainGenerator assigned.");
		}

		void Start()
		{
			averageFPS = targetFPS;

			ResetAll();
		}

		public int PooledChunkCount => chunkPool.Count;
		public int PooledChunkGameObjectCount => Chunk.chunkGameObjectPool.Count;
		public int LoadedChunkCount => chunkMap.Count;
		public int ChunkQueueCount => chunkQueue.Count;
		public int ChunkMeshQueueCount => chunkMeshQueue.Count;
		public int MeshesLastFrame => meshesLastFrame;
		public int UpdateTimeLastFrameMs => updateTimerLastFrame;
		public int ThreadCount => threadCount;
		public float AverageFPS => averageFPS;

		void ResetAll(bool useCameraLight = false)
		{
			UnloadAllChunks();
			targetTransform.position = new Vector3(0,50,0);

			if (cameraLight && directionalLight)
			{
				cameraLight.enabled = useCameraLight;
				directionalLight.enabled = !useCameraLight;
			}
		}

		void Update()
		{
			deltaTimeFPS += (Time.deltaTime - deltaTimeFPS) * 0.1f;

			averageFPS = Mathf.Lerp(averageFPS, 1f/deltaTimeFPS, 0.05f);
			
			if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
				Application.Quit();
		}

		// Uses called in late update since it is called after corountine updates allowing it to start new threads if they have just finished
		// Not using fixed update so that the updates will speed up/slow down based on PC performance
		void LateUpdate()
		{
			Stopwatch updateTimer = new Stopwatch();
			updateTimer.Start();
			UpdateLoadingQueue();
			CheckUnloadChunks();
		
			LoadChunksFromQueue();

			MeshChunksFromQueue(updateTimer);

			// For debug info
			updateTimerLastFrame = (int)updateTimer.ElapsedMilliseconds;
		}

		private void UpdateLoadingQueue()
		{
			// All distance checks use the distance squared since it saves calulcating a square root for every distance
			float loadDistanceSq = loadDistance * loadDistance;
			// Load distances in chunks to know how far to extend the for loop from the player's chunk
			int loadDistanceChunk = ((Mathf.CeilToInt(loadDistance) - Chunk.SIZE2) >> Chunk.BIT_SIZE) + 1;
			int loadDistanceChunkY = Mathf.CeilToInt(loadDistanceChunk * yDistanceModifier);

			// How much to sections to stagger the chunk location checking
			const int yCheckDelay = 8;

			Vector3i chunkPos = new Vector3i();
			Vector3 chunkRealPos = new Vector3();
			Vector3 targetPosition = targetTransform.position;
			Vector3i targetChunk = new Vector3i(
				Mathf.RoundToInt(targetPosition.x) >> Chunk.BIT_SIZE,
				Mathf.RoundToInt(targetPosition.y) >> Chunk.BIT_SIZE,
				Mathf.RoundToInt(targetPosition.z) >> Chunk.BIT_SIZE);

			// yLoadTick staggers chunk location checking to reduce time spent each frame
			for (int y = yLoadTick - loadDistanceChunkY; y < loadDistanceChunkY; y += yCheckDelay)
			{
				chunkPos.y = targetChunk.y + y;
				chunkRealPos.y = ((y + targetChunk.y) << Chunk.BIT_SIZE) + Chunk.SIZE2;

				for (int x = -loadDistanceChunk; x < loadDistanceChunk; x++)
				{
					chunkPos.x = targetChunk.x + x;
					chunkRealPos.x = ((x + targetChunk.x) << Chunk.BIT_SIZE) + Chunk.SIZE2;

					for (int z = -loadDistanceChunk; z < loadDistanceChunk; z++)
					{
						chunkPos.z = targetChunk.z + z;

						// Don't try to queue the chunk location if is already loaded or already in queue
						if (chunkMap.ContainsKey(chunkPos) || chunkQueue.Contains(chunkPos))
							continue;

						chunkRealPos.z = ((z + targetChunk.z) << Chunk.BIT_SIZE) + Chunk.SIZE2;

						float distanceSq = ScaledTargetDistanceSq(targetPosition, chunkRealPos);

						if (distanceSq < loadDistanceSq)
							chunkQueue.Enqueue(distanceSq, chunkPos);
					}
				}
			}

			// Increment the yLoadTick so that different locations will be checked next frame
			if (++yLoadTick >= yCheckDelay)
				yLoadTick = 0;
		}

		private void CheckUnloadChunks()
		{
			float unloadDistanceSq = loadDistance * loadDistance * unloadDistanceModifier * unloadDistanceModifier;
			Vector3 targetPosition = targetTransform.position;

			// Unloading sections stagger must be (2^n)-1
			const int unloadTickMax = 31;
			
			// Check if chunk is in stagger section then if it is outside the unload distance
			foreach (Chunk chunk in chunkMap.Values)
			{
				if ((chunk.chunkPos.y & unloadTickMax) != unloadTick && 
					ScaledTargetDistanceSq(targetPosition, chunk.realPos) > unloadDistanceSq)
				{
					chunkUnloadStack.Push(chunk);
				}
			}

			if (++unloadTick > unloadTickMax)
				unloadTick = 0;

			// Unload chunks outside the foreach to avoid removing elements causing errors
			while (chunkUnloadStack.Count != 0)
			{
				UnloadChunk(chunkUnloadStack.Pop());
			}
		}

		private void LoadChunksFromQueue()
		{
			Vector3i chunkPos = new Vector3i();

			int adjustedMaxThreads = Mathf.Max(1, Mathf.RoundToInt(maxThreads - chunkMeshQueue.Count * 0.2f));

			while (threadCount < adjustedMaxThreads)
			{
				// Get the closest chunk location from the queue if one exists
				if (!chunkQueue.Dequeue(out chunkPos))
					break;
				
				// Threaded
				StartCoroutine(LoadChunkThreaded(chunkPos));

				// Not threaded
				//LoadChunkThreaded(chunkPos);
			}
		}

		private void MeshChunksFromQueue(Stopwatch updateTimer)
		{
			// For debug info
			meshesLastFrame = 0;

			// Allow more time meshing if above target FPS
			int milliMax = Mathf.RoundToInt(averageFPS - targetFPS);
			int itemsToProcess = chunkMeshQueue.Count;

			while (itemsToProcess-- > 0 && chunkMeshQueue.Count > 0)
			{
				Chunk chunk;
				Vector3i chunkPos = chunkMeshQueue.Dequeue();
				chunkMeshQueueSet.Remove(chunkPos);

				// Try and get the chunk from it's postion (it may have been unloaded since it was added to queue)
				if (!chunkMap.TryGetValue(chunkPos, out chunk))
					continue;

				// This should always be true, but adjacent chunks may have unloaded since being added to queue
				if (chunk.CanBuildMesh())
				{
					chunk.BuildMesh();
					meshesLastFrame++;
				}
				else
				{
					chunkMeshQueue.Enqueue(chunkPos);
					continue;
				}
				
				// Stop meshing if too long has been spent updating this frame
				// This is at the end of the loop to ensure at least 1 mesh will generate per frame
				if (updateTimer.ElapsedMilliseconds >= milliMax)
					break;
			}
			
		}

		// Get distance squared to target using the yDistanceModifier
		public float ScaledTargetDistanceSq(Vector3 targetPosition, Vector3 realPos)
		{
			return new Vector3(
				targetPosition.x - realPos.x,
				(targetPosition.y - realPos.y) * yDistanceModifier,
				targetPosition.z - realPos.z).sqrMagnitude;
		}

		public void LoadChunk(Vector3i chunkPos)
		{
			Chunk chunk = chunkPool.Get();
			chunk.Setup(chunkPos, this);

			// Skip generating if outside terrain generator bounds
			if (chunk.CheckTerrainBounds())
				chunk.GenerateVoxelData();

			// Add the chunk to the dictionary before adjacency updates so neighbors can see it
			chunkMap.Add(chunkPos, chunk);

			// Notify adjacent chunks this chunk can be used for meshing
			chunk.FillAdjChunks();
			// Mark the chunk position as complete and remove from the queue
			chunkQueue.Remove(chunkPos);
		}

		public IEnumerator LoadChunkThreaded(Vector3i chunkPos)
		{
			Chunk chunk = chunkPool.Get();
			chunk.Setup(chunkPos, this);

			// Skip generating if outside terrain generator bounds
			if (chunk.CheckTerrainBounds())
			{
				// Start a new thread to generate the voxel data
				threadCount++;
				bool done = false;
				Thread thread = new Thread(() =>
				{
					chunk.GenerateVoxelData();
					done = true;
				})
				{
					Priority = System.Threading.ThreadPriority.BelowNormal
				};

				thread.Start();

				// Corountine waits for the thread to finish before continuing on the main thread
				while (!done)
					yield return null;

				threadCount--;
			}

			// Add the chunk to the dictionary before adjacency updates so neighbors can see it
			chunkMap.Add(chunkPos, chunk);

			// Notify adjacent chunks this chunk can be used for meshing
			chunk.FillAdjChunks();

			// Mark the chunk position as complete and remove from the queue
			// This is needed for threaded loading as there is a delay between dequeuing and it being added to the chunkMap
			chunkQueue.Remove(chunkPos);
		}

		// Clear the chunkMap and all queues
		// Use this if changing terrain/meshing to load with updated values
		public void UnloadAllChunks()
		{
			// Stop all threaded chunk loading and reset thread counter
			StopAllCoroutines();
			threadCount = 0;

			foreach (Chunk chunk in chunkMap.Values)
			{
				chunkUnloadStack.Push(chunk);
			}

			while (chunkUnloadStack.Count != 0)
			{
				UnloadChunk(chunkUnloadStack.Pop());
			}

			chunkQueue.Clear();
			chunkMeshQueue.Clear();
			chunkMeshQueueSet.Clear();
		}

		public void UnloadChunk(Chunk chunk)
		{
			chunkMap.Remove(chunk.chunkPos);

			// Try to add the chunk object to the pool, if not destroy it
			if (chunkPool.Add(chunk))
				chunk.Clean();
			else
				chunk.Destroy();
		}

		// Try and get a chunk, returns null if chunk is not loaded
		public Chunk GetChunk(Vector3i chunkPos)
		{
			Chunk chunk;
			chunkMap.TryGetValue(chunkPos, out chunk);
			return chunk;
		}

		// Returns a chunk, if the chunk is not loaded this will throw an exeption
		public Chunk GetChunkUnsafe(Vector3i chunkPos)
		{
			return chunkMap[chunkPos];
		}

		// Used by chunks to queue themselves for meshing
		public void QueueChunkMeshing(Vector3i chunkPos)
		{
			if (chunkMeshQueueSet.Add(chunkPos))
				chunkMeshQueue.Enqueue(chunkPos);
		}

		public void RemoveQueuedChunkMeshing(Vector3i chunkPos)
		{
			if (!chunkMeshQueueSet.Remove(chunkPos))
				return;

			Queue<Vector3i> filteredQueue = new Queue<Vector3i>(chunkMeshQueue.Count);
			while (chunkMeshQueue.Count > 0)
			{
				Vector3i queuedChunkPos = chunkMeshQueue.Dequeue();
				if (queuedChunkPos != chunkPos)
					filteredQueue.Enqueue(queuedChunkPos);
			}

			chunkMeshQueue = filteredQueue;
		}
	}
}
