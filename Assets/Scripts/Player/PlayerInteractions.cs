using UnityEngine;
using VoxelEngine;

public class PlayerInteractions : MonoBehaviour
{
	[SerializeField] private InteractionConfig config;
	[SerializeField] private Voxel.BlockType equippedBlock = Voxel.BlockType.Stone;

	private float interactionRange;
	private float hitEpsilon;
	private LayerMask interactionMask;
	private bool allowPickWater;
	private float holdRepeatDelay;
	private float holdRepeatInterval;

	private InputHandler inputHandler;
	private VoxelEngineManager voxelEngineManager;
	private float nextPrimaryRepeatTime;
	private float nextSecondaryRepeatTime;

	private static readonly RaycastHit[] raycastHitBuffer = new RaycastHit[32];

	private void Awake()
	{
		if (config == null)
		{
			Debug.LogError($"{nameof(PlayerInteractions)} requires an {nameof(InteractionConfig)} asset assigned.", this);
			enabled = false;
			return;
		}

		CacheConfigValues();
		inputHandler = GetComponent<InputHandler>();
		voxelEngineManager = FindAnyObjectByType<VoxelEngineManager>();
	}

	private void CacheConfigValues()
	{
		interactionRange = config.interactionRange;
		hitEpsilon = config.hitEpsilon;
		interactionMask = config.interactionMask;
		allowPickWater = config.allowPickWater;
		holdRepeatDelay = config.holdRepeatDelay;
		holdRepeatInterval = config.holdRepeatInterval;
	}

	private void Update()
	{
		if (inputHandler == null || voxelEngineManager == null)
		{
			return;
		}

		if (inputHandler.PickActionPressed)
		{
			PickBlock();
		}

		HandleRepeatAction(inputHandler.PrimaryActionPressed, inputHandler.IsPrimaryActionHeld, BreakBlock, ref nextPrimaryRepeatTime);
		HandleRepeatAction(inputHandler.SecondaryActionPressed, inputHandler.IsSecondaryActionHeld, PlaceBlock, ref nextSecondaryRepeatTime);
	}

	private void HandleRepeatAction(bool pressedThisFrame, bool held, System.Action action, ref float nextRepeatTime)
	{
		if (pressedThisFrame)
		{
			action();
			nextRepeatTime = Time.time + holdRepeatDelay;
			return;
		}

		if (!held)
		{
			nextRepeatTime = 0f;
			return;
		}

		if (Time.time < nextRepeatTime)
		{
			return;
		}

		action();
		nextRepeatTime = Time.time + holdRepeatInterval;
	}

	private void BreakBlock()
	{
		if (!TryGetTargetedVoxel(out Vector3i targetWorldVoxel, out Vector3i adjacentWorldVoxel, out Voxel targetVoxel))
		{
			return;
		}

		if (!targetVoxel.IsSolid())
		{
			return;
		}

		SetVoxelWorld(targetWorldVoxel, new Voxel(-1f, Voxel.BlockType.Empty));
	}

	private void PlaceBlock()
	{
		if (equippedBlock == Voxel.BlockType.Empty)
		{
			return;
		}

		if (!TryGetTargetedVoxel(out _, out Vector3i adjacentWorldVoxel, out _))
		{
			return;
		}

		if (!TryGetVoxelWorld(adjacentWorldVoxel, out Voxel currentVoxel))
		{
			return;
		}

		if (currentVoxel.IsSolid())
		{
			return;
		}

		if (WouldCollideWithPlayer(adjacentWorldVoxel))
		{
			return;
		}

		SetVoxelWorld(adjacentWorldVoxel, new Voxel(1f, equippedBlock));
	}

	private void PickBlock()
	{
		if (!TryGetTargetedVoxel(out _, out _, out Voxel targetVoxel))
		{
			return;
		}

		if (!targetVoxel.IsSolid())
		{
			return;
		}

		if (!allowPickWater && targetVoxel.blockType == Voxel.BlockType.Water)
		{
			return;
		}

		equippedBlock = targetVoxel.blockType;
	}

	private bool TryGetTargetedVoxel(out Vector3i hitWorldVoxel, out Vector3i adjacentWorldVoxel, out Voxel hitVoxel)
	{
		hitWorldVoxel = Vector3i.zero;
		adjacentWorldVoxel = Vector3i.zero;
		hitVoxel = Voxel.Empty;

		Camera cameraRef = Camera.main;
		if (cameraRef == null)
		{
			return false;
		}

		Ray ray = new Ray(cameraRef.transform.position, cameraRef.transform.forward);
		int hitCount = Physics.RaycastNonAlloc(ray, raycastHitBuffer, interactionRange, interactionMask, QueryTriggerInteraction.Ignore);
		if (hitCount <= 0)
		{
			return false;
		}

		RaycastHit hit = default;
		bool foundHit = false;
		float closestDistance = float.MaxValue;

		for (int i = 0; i < hitCount; i++)
		{
			ref RaycastHit candidate = ref raycastHitBuffer[i];
			if (candidate.collider == null)
			{
				continue;
			}

			if (candidate.collider.transform == transform || candidate.collider.transform.IsChildOf(transform))
			{
				continue;
			}

			if (candidate.distance >= closestDistance)
			{
				continue;
			}

			closestDistance = candidate.distance;
			hit = candidate;
			foundHit = true;
		}

		if (!foundHit)
		{
			return false;
		}

		Vector3i breakVoxel = WorldToVoxel(hit.point - hit.normal * hitEpsilon);
		Vector3i placeVoxel = breakVoxel + NormalToVoxelOffset(hit.normal);

		if (!TryGetVoxelWorld(breakVoxel, out hitVoxel))
		{
			return false;
		}

		hitWorldVoxel = breakVoxel;
		adjacentWorldVoxel = placeVoxel;
		return true;
	}

	private bool TryGetVoxelWorld(Vector3i worldVoxel, out Voxel voxel)
	{
		voxel = Voxel.Empty;

		if (!TryGetChunkAndLocal(worldVoxel, out Chunk chunk, out int localX, out int localY, out int localZ))
		{
			return false;
		}

		voxel = chunk.GetVoxelUnsafe(localX, localY, localZ);
		return true;
	}

	private bool SetVoxelWorld(Vector3i worldVoxel, Voxel voxel)
	{
		if (!TryGetChunkAndLocal(worldVoxel, out Chunk chunk, out int localX, out int localY, out int localZ))
		{
			return false;
		}

		int voxelIndex = Chunk.VoxelDataIndex(localX, localY, localZ);
		if (chunk.voxelData[voxelIndex].density == voxel.density && chunk.voxelData[voxelIndex].blockType == voxel.blockType)
		{
			return true;
		}

		chunk.voxelData[voxelIndex] = voxel;
		RefreshChunkImmediate(chunk);

		return true;
	}

	private bool TryGetChunkAndLocal(Vector3i worldVoxel, out Chunk chunk, out int localX, out int localY, out int localZ)
	{
		Vector3i chunkPos = new Vector3i(
			worldVoxel.x >> Chunk.BIT_SIZE,
			worldVoxel.y >> Chunk.BIT_SIZE,
			worldVoxel.z >> Chunk.BIT_SIZE);

		chunk = voxelEngineManager.GetChunk(chunkPos);
		if (chunk == null)
		{
			localX = 0;
			localY = 0;
			localZ = 0;
			return false;
		}

		localX = worldVoxel.x & Chunk.BIT_MASK;
		localY = worldVoxel.y & Chunk.BIT_MASK;
		localZ = worldVoxel.z & Chunk.BIT_MASK;
		return true;
	}

	private void RefreshChunkImmediate(Chunk chunk)
	{
		if (chunk == null)
		{
			return;
		}

		voxelEngineManager.RemoveQueuedChunkMeshing(chunk.chunkPos);
		chunk.dirtyMesh = true;
		chunk.fillType = Chunk.FillType.Mixed;
		chunk.FillAdjChunks();
		chunk.BuildMesh();
	}

	private bool WouldCollideWithPlayer(Vector3i worldVoxel)
	{
		if (inputHandler?.CharController == null)
		{
			return false;
		}

		Bounds playerBounds = inputHandler.CharController.bounds;
		Bounds voxelBounds = new Bounds(worldVoxel.ToVector3(), Vector3.one * 0.9f);
		return playerBounds.Intersects(voxelBounds);
	}

	private static Vector3i WorldToVoxel(Vector3 point)
	{
		return new Vector3i(
			Mathf.FloorToInt(point.x + 0.5f),
			Mathf.FloorToInt(point.y + 0.5f),
			Mathf.FloorToInt(point.z + 0.5f));
	}

	private static Vector3i NormalToVoxelOffset(Vector3 normal)
	{
		return new Vector3i(
			Mathf.RoundToInt(normal.x),
			Mathf.RoundToInt(normal.y),
			Mathf.RoundToInt(normal.z));
	}
}
