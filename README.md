# Marching-cubes-on-Unity-3D
Terrain voxel engine with the use of Marching Cubes implemented in Unity 2019.4.8f1.

![GifEdition](https://user-images.githubusercontent.com/58559223/91642654-6b216500-ea2d-11ea-8f30-cee65a9864c1.gif)

</br>
</br>

# Table of contents
1. [Introduction](#introduction)
2. [Unity scenes](#unityScenes)
3. [Configuration](#configuration)
4. [Biome system](#biomeSystem)
    1. [Edit biomes](#subparagraph4-1)
    2. [Create new biomes](#subparagraph4-1)
5. [Future work](#futureWork)
6. [References](#references)

## Introduction <a name="introduction"></a>
The unity project is a implementation of the algorithm [Marching Cubes](http://paulbourke.net/geometry/polygonise/) for the generation of a voxel engine for generate a random and infinite terrain. The idea is try to offer a flexible solution for developers that want integrate a free Voxel engine in his game or give a base for develop your own Marching Cube engine. Some of the actual properties of the engine:
* Marching cubes: Used in the terrain generation, support edition in real time and Job System + Burst for generate the chunks (it improve the efficiency).
* Chunk System: Chunk system for load the terrain near the player.
* Random terrain and biomes: The terrain have random generation (seed) and support different types of biomes for the generation.
* Save system: The data saved in a .reg files inside the dir: Application.persistentDataPath + "/Chunks".


<p align="center">
  <img src="https://user-images.githubusercontent.com/58559223/91642287-627b5f80-ea2a-11ea-96ae-018e0e4b2488.png">
</p>
</br>

## Unity scenes <a name="unityScenes"></a>
The unity project have a total of 3 different scenes:
* FirstPersonLevel: An example of a first person level and the recommended test level for check the project. Load/generate a terrain where you can add voxels with the left click and remove voxels the right click. Controls: AWSD, mause and + - keys.
* ChunkVisualization: Similar to FirstPersonLevel but the generated terrain use a material that give each chunk a color, for help to visualize them.
* TerrainViewer: Used to debug terrain/biomes in real time, when you modify a data of the NoiseManager get update in the terrain. Controls: AWSD, mause and + - keys.

## Configuration <a name="configuration"></a>
You can configure the project to adapt it to your necessities. You have two type configurations: constants or managers.
### Constants
The configurations of the constants used internally in the engine, all indicated in the "Constants" script. You can modify the region of "Configurable variables" (don't touch other regions). You have the explanation of each variable in the script or in the below list: 
* CHUNK_SIZE: The size of a chunk, all the chunks have the same x and z size.
* MAX_HEIGHT: The total height of the chunk, also the max terrain height.
* VOXEL_SIDE: The size of a voxel, now it's 1 so one voxel have 1x1x1 size.
* REGION_SIZE: The number of chunks per region side. That indicate the dimension of a region, actual 32 x 32 (x,z).
* REGION_LOOKTABLE_POS_BYTE: Used in the save .reg files. Number of byte needed for represent (REGION_SIZE * REGION_SIZE) +1. Example: (32 x 32) +1= 1025 = 2 bytes needed.  MAX = 4.
* NUMBER_MATERIALS: Total number of materials inside the material grid texture.
* MATERIAL_FOR_ROW: The number of materials in each row of the grid texture.
* SAVE_GENERATED_CHUNKS: Used for save generate chunks without modify (when true). Recommend: false,
* REGION_SAVE_COMPRESSED:Compress the .reg files . -File size -write time +CPU cost of compress a file. Recommend: TRUE

### Managers
Each manager (GameObjects inside Unity scene) have some parameters that you can modify for get different results, the majority of them only apply changes when new chunks are loaded if not indicate the contrary.

Mesh builder manager ("MeshBuilder"):
* isoLevel: Indicate the value above which the vertices of the voxel are inside of the terrain (Not air).
* Interpolate: Allow to generate a more organic terrain using interpolation when build the mesh.

</br>

Noise manager ("NoiseManager"): 
* World Seed: Seed of the world, for pseudo-random  terrain generation.
* Biome Scale: Size of the biomes.
* Diff To Merge: The biomes.appearValue difference for merge.
* Surface Level: Surface desired level, height where biomes merge.
* Octaves: Octaves used in the biome noise.
* Persistance: Amplitude decrease of biomes noise per octave,very low recommended.
* Lacunarity: Frequency increase of biomes per octave.
* Biomes: Biomes class array. Empty for get all Biomes of inside the GameObject (recommend).

##### The noise manager also contains biomes that with parameters to modify but this is explained in the biome section.
</br>

Chunk manager ("ChunkManager"): 
* Terrain Material: Terrain applied to all chunks of the terrain. The material of the terrain.
* Chunk View Distance: Chunks load and visible for the player,number of chunks distance. (Render distance of chunks). Change in play mode supported.
* Chunk maintain distance: Distance extra for destroy inactive chunks, this chunks consume ram, but load faster.

## Biome system<a name="biomeSystem"></a>
The biome system allow you to generate different types of terrain generated in the infinite world. It used a value between 1 and 0 where each biome has appear range, so each biome can have 1 or 2 possible neighbors biomes.

Desert-mountains biome and the "NoiseManager" with the biomes:

![Biome frontier](https://user-images.githubusercontent.com/58559223/91642278-555e7080-ea2a-11ea-9aa9-d8181c0b4b9c.png)

Ice biome and plains biome:
![ice-grass biomes](https://user-images.githubusercontent.com/58559223/91643239-b9386780-ea31-11ea-833e-7a63fc70727e.png)

</br>

### Edit biomes<a name="subparagraph4-1"></a>
For edit a biome you can use the TerrainViewer scene. You only need to remove all the biomes classes from the "NoiseManager" except the one which you want edit and then modify the values of the inspector.

You can use the "NoiseTerrainViewer" for use a bigger or smaller test area or apply offset to the terrain. All the changes applied in this scene update the test terrain, so you need start the scene, apply the changes to the biome, copy the component, stop the scene and paste the component values to the biome.

Example of edition of the "B_Mountains" biome:
![B_Mountains edition](https://user-images.githubusercontent.com/58559223/94999078-200fea00-05b7-11eb-8a27-ed4f774b4980.gif)

### Create custom biomes<a name="subparagraph4-1"></a>
For create the a biome you need create a class that heir from the class "Biome" and that must have the function "public override byte[] GenerateChunkData(Vector2Int vecPos, float[] biomeMerge)" used for generate the biome texturization. For this process you can copy a paste the code of some actual biome and apply the necessary changes to the code for get your custom biome. 

When you have the new biome class create, use the [edit biome section](#subparagraph4-1) for edit the parameters in a real time test terrain.

For apply this to other levels just add this new custom biome to the NoiseManager in the other scenes. Like in the image we add the new biome "B_NewBIome" to the NoiseManager:

<img width="40%" src="https://user-images.githubusercontent.com/58559223/91642253-2811c280-ea2a-11ea-8977-2c799d6e01c9.PNG">
<br><br>

## Future work <a name="futureWork"></a>
The priority of next update will be:
* Fix the texturization system: The texturizations is not correct and can be visible in geometrical textures (ex: brick textures).
* Support of multiple worlds in the ChunkSystem/file system (Actual only one word is used by all levels).
* Different types of terrain modifications.

Others futures updates:
* Upgrade the biome system for support 2D noise biome creator (Actual the biome is created using 1D noise from 0-1).
* Add a vegetation system to the biome system.
* Cave system suApport for NoiseManager/Biome system.
* Extend the Job System and Burst to the NoiseManager (efficiency improvements).
* Add support to filesystem to save entities (animals, monsters ...)
* Add a LOD system for the far chunks of the player.



## References <a name="references"></a>
The Marching Cube algorithm:
* Polygonising a scalar field (Paul Bourke): http://paulbourke.net/geometry/polygonise/
* Coding Adventure: Marching Cubes (Sebastian Lague): https://www.youtube.com/watch?v=M3iI2l0ltbE / https://github.com/SebLague/Marching-Cubes


Noise system:
* Procedural Landmass Generation (Sebastian Lague): https://www.youtube.com/watch?v=WP-Bm65Q-1Y&list=PLFt_AvWsXl0eBW2EiBtl_sxmDtSgZBxB3&index=2

* Making maps with noise functions (Red Blob Games): https://www.redblobgames.com/maps/terrain-from-noise/

Others:
* Region file system (Seed Of Andromeda): https://www.seedofandromeda.com/blogs/1-creating-a-region-file-system-for-a-voxel-game

* Textures used in the terrain (Hannes Delbeke): https://hannesdelbeke.blogspot.com/2012/10/handpainted-textures.html
