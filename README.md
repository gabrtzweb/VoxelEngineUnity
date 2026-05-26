# VoxelEngineUnity

My fifth attempt at making a small, in-progress voxel engine prototype built in Unity. This repository contains experiments, editor tools, and noise-driven terrain generation used while exploring voxel world techniques.

## Quick Start / Initial setup

- Open the project in Unity 6.4.
- Run the sample scene: open a Scene in the `Scenes/Voxel Terrain` and press Play. Some tools are editor-only and will not run in builds.

## Fast Noise (FastNoise SIMD) usage in this project

- The project includes a FastNoise SIMD integration located at `Assets/Scripts/FastNoise SIMD`:
- Runtime wrapper: `FastNoiseSIMD.cs` and `FastNoiseSIMDUnity.cs` provide access to the native SIMD noise library.
- Editor UI: `Assets/Editor/FastNoiseSIMDUnityEditor.cs` and `Assets/Editor/TerrainGeneratorSIMDEditor.cs` expose noise parameters and let you attach `FastNoiseSIMDUnity` components to GameObjects.
- Native plugins: `Assets/Scripts/FastNoise SIMD/Plugins/x64/FastNoiseSIMD_CLib.dll` and the x86 equivalent are included for platforms that support the native DLL.
- Typical workflow: create or attach a `FastNoiseSIMDUnity` component, configure noise type, seed, frequency, fractal/cellular settings in the inspector, then reference that noise instance from the terrain generator component.

## Project structure

- `Assets/Content/` — materials, meshes, prefabs and shaders used by prototypes.
- `Assets/Scripts/` — core runtime scripts (voxel generation, meshing, FastNoise SIMD wrapper).
- `Assets/Editor/` — custom inspectors and editor utilities (`TerrainGeneratorSIMDEditor`, `FastNoiseSIMDUnityEditor`).

## Known notes

- Uses a native plugin (FastNoise SIMD DLLs). Ensure target platform matches the native binaries when building.
- Some tools are editor-only and rely on UnityEditor APIs.

## Roadmap (short)

- Stabilize chunked world generation and meshing pipeline.
- Implement world biome generation.
