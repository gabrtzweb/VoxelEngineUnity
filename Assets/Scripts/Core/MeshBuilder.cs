using System;
using UnityEngine;
using System.Collections.Generic;
using System.Net;

namespace VoxelEngine
{
	public class MeshBuilder
	{
		public static readonly Vector3[] directionNormals =
		{
			Vector3.left,
			Vector3.right,
			Vector3.down,
			Vector3.up,
			Vector3.back,
			Vector3.forward,
		};

		public enum Direction
		{
			Left,
			Right,
			Down,
			Up,
			Back,
			Forward,
		};

		public enum MeshType
		{
			Basic,
			AmbientOcclusion,
		}

		private static Chunk chunk;
		private static List<Quad> quads = new List<Quad>();
		private static Dictionary<Vector3, int> lightLevels = new Dictionary<Vector3, int>();

		public static void Clean()
		{
			chunk = null;
			quads.Clear();
			lightLevels.Clear();
		}

		public static Mesh BuildMesh(Chunk chunk, MeshType meshType)
		{
			MeshBuilder.chunk = chunk;
			Mesh mesh = null;

			switch (meshType)
			{
				case MeshType.Basic:
					mesh = BasicMesh();
					break;
				case MeshType.AmbientOcclusion:
					mesh = AmbientOcclusionMesh();
					break;
				default:
					throw new ArgumentOutOfRangeException("meshType", meshType, null);
			}

			Clean();
			return mesh;
		}

		private static Mesh BasicMesh()
		{
			TerrainGeneratorBase terrainGenerator = chunk.voxelEngineManager.terrainGenerator;
			int index = -1;

			for (int x = 0; x < Chunk.SIZE; x++)
			{
				for (int y = 0; y < Chunk.SIZE; y++)
				{
					for (int z = 0; z < Chunk.SIZE; z++)
					{
						Voxel voxel = chunk.voxelData[++index];
						Voxel left = GetAdjVoxelLeft(index, x);
						Voxel down = GetAdjVoxelDown(index, y);
						Voxel back = GetAdjVoxelBack(index, z);

						if (voxel.IsSolid())
						{
							Color32 color = new Color32();
							bool colorInit = false;
							Voxel.BlockType blockType = voxel.blockType;

							// Left
							if (!left.IsSolid())
							{
								colorInit = true;
								color = GetFaceTintColor(terrainGenerator.DensityColor(voxel), blockType, Direction.Left);

								quads.Add(new Quad(
									new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
									new Vector3(x - 0.5f, y - 0.5f, z + 0.5f),
									new Vector3(x - 0.5f, y + 0.5f, z + 0.5f),
									new Vector3(x - 0.5f, y + 0.5f, z - 0.5f),
									color, Direction.Left, blockType));
							}

							// Down
							if (!down.IsSolid())
							{
								if (!colorInit)
								{
									colorInit = true;
									color = GetFaceTintColor(terrainGenerator.DensityColor(voxel), blockType, Direction.Down);
								}

								quads.Add(new Quad(
									new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
									new Vector3(x + 0.5f, y - 0.5f, z - 0.5f),
									new Vector3(x + 0.5f, y - 0.5f, z + 0.5f),
									new Vector3(x - 0.5f, y - 0.5f, z + 0.5f),
									color, Direction.Down, blockType));
							}

							// Back
							if (!back.IsSolid())
							{
								if (!colorInit)
								{
									colorInit = true;
									color = GetFaceTintColor(terrainGenerator.DensityColor(voxel), blockType, Direction.Back);
								}

								quads.Add(new Quad(
									new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
									new Vector3(x - 0.5f, y + 0.5f, z - 0.5f),
									new Vector3(x + 0.5f, y + 0.5f, z - 0.5f),
									new Vector3(x + 0.5f, y - 0.5f, z - 0.5f),
									color, Direction.Back, blockType));
							}
						}
						else // Voxel not solid
						{
							// Left
							if (left.IsSolid())
							{
								Voxel.BlockType faceBlockType = left.blockType;
								Color32 faceColor = GetFaceTintColor(terrainGenerator.DensityColor(left), faceBlockType, Direction.Right);
								quads.Add(new Quad(
									new Vector3(x - 0.5f, y + 0.5f, z - 0.5f),
									new Vector3(x - 0.5f, y + 0.5f, z + 0.5f),
									new Vector3(x - 0.5f, y - 0.5f, z + 0.5f),
									new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
									faceColor, Direction.Right, faceBlockType));
							}

							// Down
							if (down.IsSolid())
							{
								Voxel.BlockType faceBlockType = down.blockType;
								Color32 faceColor = GetFaceTintColor(terrainGenerator.DensityColor(down), faceBlockType, Direction.Up);
								quads.Add(new Quad(
									new Vector3(x - 0.5f, y - 0.5f, z + 0.5f),
									new Vector3(x + 0.5f, y - 0.5f, z + 0.5f),
									new Vector3(x + 0.5f, y - 0.5f, z - 0.5f),
									new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
									faceColor, Direction.Up, faceBlockType));
							}

							// Back
							if (back.IsSolid())
							{
								Voxel.BlockType faceBlockType = back.blockType;
								Color32 faceColor = GetFaceTintColor(terrainGenerator.DensityColor(back), faceBlockType, Direction.Forward);
								quads.Add(new Quad(
									new Vector3(x + 0.5f, y - 0.5f, z - 0.5f),
									new Vector3(x + 0.5f, y + 0.5f, z - 0.5f),
									new Vector3(x - 0.5f, y + 0.5f, z - 0.5f),
									new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
									faceColor, Direction.Forward, faceBlockType));
							}
						}
					}
				}
			}

			if (quads.Count == 0)
				return null;

			Vector3[] verts = new Vector3[quads.Count * 4];
			Vector3[] normals = new Vector3[quads.Count * 4];
			Vector2[] uvs = new Vector2[quads.Count * 4];
			Vector2[] uv2 = new Vector2[quads.Count * 4];
			Color32[] colors = new Color32[quads.Count * 4];
			int[] tris = new int[quads.Count * 6];

			int vertIndex = 0;
			int triIndex = 0;

			foreach (Quad quad in quads)
			{
				int textureLayer = GetTextureLayer(quad, out int frameCount);
				tris[triIndex++] = vertIndex;
				tris[triIndex++] = vertIndex + 1;
				tris[triIndex++] = vertIndex + 2;
				tris[triIndex++] = vertIndex;
				tris[triIndex++] = vertIndex + 2;
				tris[triIndex++] = vertIndex + 3;

				SetFaceUVs(uvs, vertIndex, quad.direction);
				SetTextureLayer(uv2, vertIndex, textureLayer, frameCount);

				colors[vertIndex] = quad.color;
				normals[vertIndex] = directionNormals[(int)quad.direction];
				verts[vertIndex++] = quad.v0;
				colors[vertIndex] = quad.color;
				normals[vertIndex] = directionNormals[(int)quad.direction];
				verts[vertIndex++] = quad.v1;
				colors[vertIndex] = quad.color;
				normals[vertIndex] = directionNormals[(int)quad.direction];
				verts[vertIndex++] = quad.v2;
				colors[vertIndex] = quad.color;
				normals[vertIndex] = directionNormals[(int)quad.direction];
				verts[vertIndex++] = quad.v3;
			}

			Mesh mesh = new Mesh
			{
				vertices = verts,
				uv = uvs,
				uv2 = uv2,
				normals = normals,
				triangles = tris,
				colors32 = colors
			};

			return mesh;
		}

		private static Mesh AmbientOcclusionMesh()
		{
			TerrainGeneratorBase terrainGenerator = chunk.voxelEngineManager.terrainGenerator;
			int index = -Chunk.VOXEL_STEP_X - Chunk.VOXEL_STEP_Y - Chunk.VOXEL_STEP_Z - 1;

			for (int x = -1; x < Chunk.SIZE - 1; x++)
			{
				for (int y = -1; y < Chunk.SIZE - 1; y++)
				{
					for (int z = -1; z < Chunk.SIZE - 1; z++)
					{
						Voxel voxel;
						Voxel left;
						Voxel down;
						Voxel back;

						if (x == -1 || y == -1 || z == -1)
						{
							voxel = GetAdjVoxel(++index, x, y, z);
							left = GetAdjVoxel(index - Chunk.VOXEL_STEP_X, x - 1, y, z);
							down = GetAdjVoxel(index - Chunk.VOXEL_STEP_Y, x, y - 1, z);
							back = GetAdjVoxel(index - Chunk.VOXEL_STEP_Z, x, y, z - 1);
						}
						else
						{
							voxel = chunk.voxelData[++index];
							left = GetAdjVoxelLeft(index, x);
							down = GetAdjVoxelDown(index, y);
							back = GetAdjVoxelBack(index, z);
						}

						if (voxel.IsSolid())
						{
							Color32 color = new Color32();
							bool colorInit = false;
							Voxel.BlockType blockType = voxel.blockType;

							// Left
							if (!left.IsSolid())
							{
								colorInit = true;
								color = GetFaceTintColor(terrainGenerator.DensityColor(voxel), blockType, Direction.Left);

								Quad q = new Quad(
									new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
									new Vector3(x - 0.5f, y - 0.5f, z + 0.5f),
									new Vector3(x - 0.5f, y + 0.5f, z + 0.5f),
									new Vector3(x - 0.5f, y + 0.5f, z - 0.5f),
									color, Direction.Left, blockType);

								q.i0 = LightLevelX(q.v0, color, y, z, -0.25f);
								q.i1 = LightLevelX(q.v1, color, y, z, -0.25f);
								q.i2 = LightLevelX(q.v2, color, y, z, -0.25f);
								q.i3 = LightLevelX(q.v3, color, y, z, -0.25f);

								quads.Add(q);
							}

							// Down
							if (!down.IsSolid())
							{
								if (!colorInit)
								{
									colorInit = true;
									color = GetFaceTintColor(terrainGenerator.DensityColor(voxel), blockType, Direction.Down);
								}

								Quad q = new Quad(
									new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
									new Vector3(x + 0.5f, y - 0.5f, z - 0.5f),
									new Vector3(x + 0.5f, y - 0.5f, z + 0.5f),
									new Vector3(x - 0.5f, y - 0.5f, z + 0.5f),
									color, Direction.Down, blockType);

								q.i0 = LightLevelY(q.v0, color, x, z, -0.25f);
								q.i1 = LightLevelY(q.v1, color, x, z, -0.25f);
								q.i2 = LightLevelY(q.v2, color, x, z, -0.25f);
								q.i3 = LightLevelY(q.v3, color, x, z, -0.25f);

								quads.Add(q);
							}

							// Back
							if (!back.IsSolid())
							{
								if (!colorInit)
									color = GetFaceTintColor(terrainGenerator.DensityColor(voxel), blockType, Direction.Back);

								Quad q = new Quad(
									new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
									new Vector3(x - 0.5f, y + 0.5f, z - 0.5f),
									new Vector3(x + 0.5f, y + 0.5f, z - 0.5f),
									new Vector3(x + 0.5f, y - 0.5f, z - 0.5f),
									color, Direction.Back, blockType);

								q.i0 = LightLevelZ(q.v0, color, x, y, -0.25f);
								q.i1 = LightLevelZ(q.v1, color, x, y, -0.25f);
								q.i2 = LightLevelZ(q.v2, color, x, y, -0.25f);
								q.i3 = LightLevelZ(q.v3, color, x, y, -0.25f);

								quads.Add(q);
							}

						}
						else // Voxel not solid
						{
							// Left
							if (left.IsSolid())
							{
								Voxel.BlockType faceBlockType = left.blockType;
								Color32 faceColor = GetFaceTintColor(terrainGenerator.DensityColor(left), faceBlockType, Direction.Right);
								Quad q = new Quad(
									new Vector3(x - 0.5f, y + 0.5f, z - 0.5f),
									new Vector3(x - 0.5f, y + 0.5f, z + 0.5f),
									new Vector3(x - 0.5f, y - 0.5f, z + 0.5f),
									new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
									faceColor, Direction.Right, faceBlockType);

								q.i0 = LightLevelX(q.v0, q.color, y, z, 0.25f);
								q.i1 = LightLevelX(q.v1, q.color, y, z, 0.25f);
								q.i2 = LightLevelX(q.v2, q.color, y, z, 0.25f);
								q.i3 = LightLevelX(q.v3, q.color, y, z, 0.25f);

								quads.Add(q);
							}

							// Down
							if (down.IsSolid())
							{
								Voxel.BlockType faceBlockType = down.blockType;
								Color32 faceColor = GetFaceTintColor(terrainGenerator.DensityColor(down), faceBlockType, Direction.Up);
								Quad q = new Quad(
									new Vector3(x - 0.5f, y - 0.5f, z + 0.5f),
									new Vector3(x + 0.5f, y - 0.5f, z + 0.5f),
									new Vector3(x + 0.5f, y - 0.5f, z - 0.5f),
									new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
									faceColor, Direction.Up, faceBlockType);

								q.i0 = LightLevelY(q.v0, q.color, x, z, 0.25f);
								q.i1 = LightLevelY(q.v1, q.color, x, z, 0.25f);
								q.i2 = LightLevelY(q.v2, q.color, x, z, 0.25f);
								q.i3 = LightLevelY(q.v3, q.color, x, z, 0.25f);

								quads.Add(q);
							}

							// Back
							if (back.IsSolid())
							{
								Voxel.BlockType faceBlockType = back.blockType;
								Color32 faceColor = GetFaceTintColor(terrainGenerator.DensityColor(back), faceBlockType, Direction.Forward);
								Quad q = new Quad(
									new Vector3(x + 0.5f, y - 0.5f, z - 0.5f),
									new Vector3(x + 0.5f, y + 0.5f, z - 0.5f),
									new Vector3(x - 0.5f, y + 0.5f, z - 0.5f),
									new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
									faceColor, Direction.Forward, faceBlockType);

								q.i0 = LightLevelZ(q.v0, q.color, x, y, 0.25f);
								q.i1 = LightLevelZ(q.v1, q.color, x, y, 0.25f);
								q.i2 = LightLevelZ(q.v2, q.color, x, y, 0.25f);
								q.i3 = LightLevelZ(q.v3, q.color, x, y, 0.25f);

								quads.Add(q);
							}
						}
					}
				}
			}

			if (quads.Count == 0)
				return null;

			Vector3[] verts = new Vector3[quads.Count * 4];
			Vector3[] normals = new Vector3[quads.Count * 4];
			Vector2[] uvs = new Vector2[quads.Count * 4];
			Vector2[] uv2 = new Vector2[quads.Count * 4];
			Color32[] colors = new Color32[quads.Count * 4];
			int[] tris = new int[quads.Count * 6];

			int vertIndex = 0;
			int triIndex = 0;

			foreach (Quad quad in quads)
			{
				int textureLayer = GetTextureLayer(quad, out int frameCount);
				int baseVertIndex = vertIndex;

				if (quad.i0 + quad.i2 < quad.i1 + quad.i3)
				{
					tris[triIndex++] = baseVertIndex;
					tris[triIndex++] = baseVertIndex + 1;
					tris[triIndex++] = baseVertIndex + 2;
					tris[triIndex++] = baseVertIndex;
					tris[triIndex++] = baseVertIndex + 2;
					tris[triIndex++] = baseVertIndex + 3;
				}
				else
				{
					tris[triIndex++] = baseVertIndex + 1;
					tris[triIndex++] = baseVertIndex + 2;
					tris[triIndex++] = baseVertIndex + 3;
					tris[triIndex++] = baseVertIndex + 1;
					tris[triIndex++] = baseVertIndex + 3;
					tris[triIndex++] = baseVertIndex;
				}

				SetFaceUVs(uvs, baseVertIndex, quad.direction);
				SetTextureLayer(uv2, baseVertIndex, textureLayer, frameCount);

				normals[vertIndex] = directionNormals[(int)quad.direction];
				colors[vertIndex] = LightColorAdjust(quad.color, quad.i0);
				verts[vertIndex++] = quad.v0;
				normals[vertIndex] = directionNormals[(int)quad.direction];
				colors[vertIndex] = LightColorAdjust(quad.color, quad.i1);
				verts[vertIndex++] = quad.v1;
				normals[vertIndex] = directionNormals[(int)quad.direction];
				colors[vertIndex] = LightColorAdjust(quad.color, quad.i2);
				verts[vertIndex++] = quad.v2;
				normals[vertIndex] = directionNormals[(int)quad.direction];
				colors[vertIndex] = LightColorAdjust(quad.color, quad.i3);
				verts[vertIndex++] = quad.v3;
			}

			Mesh mesh = new Mesh
			{
				vertices = verts,
				uv = uvs,
				uv2 = uv2,
				normals = normals,
				triangles = tris,
				colors32 = colors
			};

			return mesh;
		}

		private static Color32 LightColorAdjust(Color32 color, int lightLevel)
		{
			if (lightLevel != 0)
			{
				float lightModifier = 1.0f - lightLevel * Chunk.AMBIENT_OCCLUSION_STRENGTH;

				color.r = (byte)(color.r * lightModifier);
				color.g = (byte)(color.g * lightModifier);
				color.b = (byte)(color.b * lightModifier);
			}

			return color;
		}

		// Ambient light calulator for X normal faces
		private static int LightLevelX(Vector3 vert, Color32 color, int localY, int localZ, float xOffset)
		{
			int lightLevel = 0;
			vert.x += xOffset;

			if (!lightLevels.TryGetValue(vert, out lightLevel))
			{
				int ix = FastRound(vert.x);
				int iy = FastFloor(vert.y);
				int iz = FastFloor(vert.z);
				int sides = 0;
				int corner = 0;

				if (localY == iy)
				{
					if (localZ == iz)
					{
						if (GetAdjVoxel(ix, iy + 1, iz).IsSolid())
							sides++;
						if (GetAdjVoxel(ix, iy, iz + 1).IsSolid())
							sides++;
						if (GetAdjVoxel(ix, iy + 1, iz + 1).IsSolid())
							corner++;
					}
					else
					{
						if (GetAdjVoxel(ix, iy + 1, iz + 1).IsSolid())
							sides++;
						if (GetAdjVoxel(ix, iy, iz).IsSolid())
							sides++;
						if (GetAdjVoxel(ix, iy + 1, iz).IsSolid())
							corner++;
					}
				}
				else
				{
					if (localZ == iz)
					{
						if (GetAdjVoxel(ix, iy, iz).IsSolid())
							sides++;
						if (GetAdjVoxel(ix, iy + 1, iz + 1).IsSolid())
							sides++;
						if (GetAdjVoxel(ix, iy, iz + 1).IsSolid())
							corner++;
					}
					else
					{
						if (GetAdjVoxel(ix, iy, iz + 1).IsSolid())
							sides++;
						if (GetAdjVoxel(ix, iy + 1, iz).IsSolid())
							sides++;
						if (GetAdjVoxel(ix, iy, iz).IsSolid())
							corner++;
					}
				}

				if (sides == 2)
					lightLevel = 3;
				else
					lightLevel = sides + corner;

				lightLevels.Add(vert, lightLevel);
			}
			return lightLevel;
		}

		// Ambient light calulator for Y normal faces
		private static int LightLevelY(Vector3 vert, Color32 color, int localX, int localZ, float yOffset)
		{
			int lightLevel = 0;
			vert.y += yOffset;

			if (!lightLevels.TryGetValue(vert, out lightLevel))
			{
				int ix = FastFloor(vert.x);
				int iy = FastRound(vert.y);
				int iz = FastFloor(vert.z);
				int sides = 0;
				int corner = 0;

				if (localX == ix)
				{
					if (localZ == iz)
					{
						if (GetAdjVoxel(ix + 1, iy, iz).IsSolid())
							sides++;
						if (GetAdjVoxel(ix, iy, iz + 1).IsSolid())
							sides++;
						if (GetAdjVoxel(ix + 1, iy, iz + 1).IsSolid())
							corner++;
					}
					else
					{
						if (GetAdjVoxel(ix + 1, iy, iz + 1).IsSolid())
							sides++;
						if (GetAdjVoxel(ix, iy, iz).IsSolid())
							sides++;
						if (GetAdjVoxel(ix + 1, iy, iz).IsSolid())
							corner++;
					}
				}
				else
				{
					if (localZ == iz)
					{
						if (GetAdjVoxel(ix, iy, iz).IsSolid())
							sides++;
						if (GetAdjVoxel(ix + 1, iy, iz + 1).IsSolid())
							sides++;
						if (GetAdjVoxel(ix, iy, iz + 1).IsSolid())
							corner++;
					}
					else
					{
						if (GetAdjVoxel(ix, iy, iz + 1).IsSolid())
							sides++;
						if (GetAdjVoxel(ix + 1, iy, iz).IsSolid())
							sides++;
						if (GetAdjVoxel(ix, iy, iz).IsSolid())
							corner++;
					}
				}

				if (sides == 2)
					lightLevel = 3;
				else
					lightLevel = sides + corner;

				lightLevels.Add(vert, lightLevel);
			}
			return lightLevel;
		}

		// Ambient light calulator for Z normal faces
		private static int LightLevelZ(Vector3 vert, Color32 color, int localX, int localY, float zOffset)
		{
			int lightLevel = 0;
			vert.z += zOffset;

			if (!lightLevels.TryGetValue(vert, out lightLevel))
			{
				int ix = FastFloor(vert.x);
				int iy = FastFloor(vert.y);
				int iz = FastRound(vert.z);
				int sides = 0;
				int corner = 0;

				if (localX == ix)
				{
					if (localY == iy)
					{
						if (GetAdjVoxel(ix + 1, iy, iz).IsSolid())
							sides++;
						if (GetAdjVoxel(ix, iy + 1, iz).IsSolid())
							sides++;
						if (GetAdjVoxel(ix + 1, iy + 1, iz).IsSolid())
							corner++;
					}
					else
					{
						if (GetAdjVoxel(ix + 1, iy + 1, iz).IsSolid())
							sides++;
						if (GetAdjVoxel(ix, iy, iz).IsSolid())
							sides++;
						if (GetAdjVoxel(ix + 1, iy, iz).IsSolid())
							corner++;
					}
				}
				else
				{
					if (localY == iy)
					{
						if (GetAdjVoxel(ix, iy, iz).IsSolid())
							sides++;
						if (GetAdjVoxel(ix + 1, iy + 1, iz).IsSolid())
							sides++;
						if (GetAdjVoxel(ix, iy + 1, iz).IsSolid())
							corner++;
					}
					else
					{
						if (GetAdjVoxel(ix, iy + 1, iz).IsSolid())
							sides++;
						if (GetAdjVoxel(ix + 1, iy, iz).IsSolid())
							sides++;
						if (GetAdjVoxel(ix, iy, iz).IsSolid())
							corner++;
					}
				}

				if (sides == 2)
					lightLevel = 3;
				else
					lightLevel = sides + corner;

				lightLevels.Add(vert, lightLevel);
			}
			return lightLevel;
		}

		private static int FastFloor(float f) { return f >= 0.0f ? (int)f : (int)f - 1; }
		private static int FastRound(float f) { return (f >= 0.0f) ? (int)(f + 0.5f) : (int)(f - 0.5f); }

		private static Voxel GetAdjVoxel(int voxelIndex, int localX, int localY, int localZ)
		{
			int adjIndex = -2;

			if (localX < 0)
			{
				adjIndex += 2;
				voxelIndex += Chunk.VOXEL_STEP_CHUNK_X;
			}
			if (localY < 0)
			{
				adjIndex += 3;
				voxelIndex += Chunk.VOXEL_STEP_CHUNK_Y;
			}
			if (localZ < 0)
			{
				adjIndex += 4;
				voxelIndex += Chunk.VOXEL_STEP_CHUNK_Z;
			}

			if (adjIndex == -2)
				return chunk.voxelData[voxelIndex];

			Chunk adjChunk = chunk.adjChunks[Math.Min(adjIndex, 6)];
			return adjChunk != null ? adjChunk.voxelData[voxelIndex] : Voxel.Empty;
		}

		private static Voxel GetAdjVoxel(int localX, int localY, int localZ)
		{
			int adjIndex = -2;

			if (localX < 0)
			{
				adjIndex += 2;
				localX += Chunk.SIZE;
			}
			if (localY < 0)
			{
				adjIndex += 3;
				localY += Chunk.SIZE;
			}
			if (localZ < 0)
			{
				adjIndex += 4;
				localZ += Chunk.SIZE;
			}

			if (adjIndex == -2)
				return chunk.GetVoxelUnsafe(localX, localY, localZ);

			Chunk adjChunk = chunk.adjChunks[Math.Min(adjIndex, 6)];
			return adjChunk != null ? adjChunk.GetVoxelUnsafe(localX, localY, localZ) : Voxel.Empty;
		}

		private static Voxel GetAdjVoxelLeft(int voxelIndex, int localX)
		{
			voxelIndex -= Chunk.VOXEL_STEP_X;

			if (localX > 0)
				return chunk.voxelData[voxelIndex];

			voxelIndex += Chunk.VOXEL_STEP_CHUNK_X;
			Chunk adjChunk = chunk.adjChunks[(int)Chunk.AdjDirection.Left];
			return adjChunk != null ? adjChunk.voxelData[voxelIndex] : Voxel.Empty;
		}

		private static Voxel GetAdjVoxelDown(int voxelIndex, int localY)
		{
			voxelIndex -= Chunk.VOXEL_STEP_Y;

			if (localY > 0)
				return chunk.voxelData[voxelIndex];

			voxelIndex += Chunk.VOXEL_STEP_CHUNK_Y;
			Chunk adjChunk = chunk.adjChunks[(int)Chunk.AdjDirection.Down];
			return adjChunk != null ? adjChunk.voxelData[voxelIndex] : Voxel.Empty;
		}

		private static Voxel GetAdjVoxelBack(int voxelIndex, int localZ)
		{
			voxelIndex -= Chunk.VOXEL_STEP_Z;

			if (localZ > 0)
				return chunk.voxelData[voxelIndex];

			voxelIndex += Chunk.VOXEL_STEP_CHUNK_Z;
			Chunk adjChunk = chunk.adjChunks[(int)Chunk.AdjDirection.Back];
			return adjChunk != null ? adjChunk.voxelData[voxelIndex] : Voxel.Empty;
		}

		private static int GetTextureLayer(Quad quad, out int frameCount)
		{
			Vector3 center = (quad.v0 + quad.v1 + quad.v2 + quad.v3) * 0.25f;
			Vector3 blockPosition = center - directionNormals[(int)quad.direction] * 0.5f;

			int worldX = (chunk.chunkPos.x << Chunk.BIT_SIZE) + Mathf.RoundToInt(blockPosition.x);
			int worldY = (chunk.chunkPos.y << Chunk.BIT_SIZE) + Mathf.RoundToInt(blockPosition.y);
			int worldZ = (chunk.chunkPos.z << Chunk.BIT_SIZE) + Mathf.RoundToInt(blockPosition.z);

			return TextureMap.GetTextureLayer(quad.blockType, worldX, worldY, worldZ, quad.direction, out frameCount);
		}

		private static void SetFaceUVs(Vector2[] uvs, int vertIndex, Direction direction)
		{
			switch (direction)
			{
				case Direction.Left:
					uvs[vertIndex] = new Vector2(1f, 0f);
					uvs[vertIndex + 1] = new Vector2(0f, 0f);
					uvs[vertIndex + 2] = new Vector2(0f, 1f);
					uvs[vertIndex + 3] = new Vector2(1f, 1f);
					break;
				case Direction.Right:
					uvs[vertIndex] = new Vector2(0f, 1f);
					uvs[vertIndex + 1] = new Vector2(1f, 1f);
					uvs[vertIndex + 2] = new Vector2(1f, 0f);
					uvs[vertIndex + 3] = new Vector2(0f, 0f);
					break;
				case Direction.Back:
				case Direction.Forward:
					uvs[vertIndex] = new Vector2(0f, 0f);
					uvs[vertIndex + 1] = new Vector2(0f, 1f);
					uvs[vertIndex + 2] = new Vector2(1f, 1f);
					uvs[vertIndex + 3] = new Vector2(1f, 0f);
					break;
				case Direction.Down:
					uvs[vertIndex] = new Vector2(0f, 0f);
					uvs[vertIndex + 1] = new Vector2(1f, 0f);
					uvs[vertIndex + 2] = new Vector2(1f, 1f);
					uvs[vertIndex + 3] = new Vector2(0f, 1f);
					break;
				case Direction.Up:
					uvs[vertIndex] = new Vector2(0f, 1f);
					uvs[vertIndex + 1] = new Vector2(1f, 1f);
					uvs[vertIndex + 2] = new Vector2(1f, 0f);
					uvs[vertIndex + 3] = new Vector2(0f, 0f);
					break;
			}
		}

		private static void SetTextureLayer(Vector2[] uv2, int vertIndex, int textureLayer, int frameCount)
		{
			Vector2 layer = new Vector2(textureLayer, frameCount);
			uv2[vertIndex] = layer;
			uv2[vertIndex + 1] = layer;
			uv2[vertIndex + 2] = layer;
			uv2[vertIndex + 3] = layer;
		}

		private static Color32 GetFaceTintColor(Color32 baseColor, Voxel.BlockType blockType, Direction faceDirection)
		{
			if (blockType == Voxel.BlockType.Grass && faceDirection != Direction.Up)
				return new Color32(255, 255, 255, 255);

			return baseColor;
		}

		public struct Quad
		{
			public Vector3 v0, v1, v2, v3;
			public Color32 color;
			public Direction direction;
			public Voxel.BlockType blockType;
			public int i0, i1, i2, i3;

			public Quad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Color32 color, Direction direction, Voxel.BlockType blockType)
			{
				this.v0 = v0;
				this.v1 = v1;
				this.v2 = v2;
				this.v3 = v3;
				this.color = color;
				this.direction = direction;
				this.blockType = blockType;
				i0 = i1 = i2 = i3 = 0;
			}
		}
	}
}
