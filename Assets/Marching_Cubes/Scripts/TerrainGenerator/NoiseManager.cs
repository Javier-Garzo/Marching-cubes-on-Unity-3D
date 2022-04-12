using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseManager : Singleton<NoiseManager>
{

	[Header("World configuration")]
	public WorldConfig worldConfig;//Current world configuration of the noiseManager
	[System.Serializable]
	public class WorldConfig
	{
		[Tooltip("Seed of the world (0 seed generate randoms different worlds -testing-)")]
		[Range(int.MinValue + 100, int.MaxValue - 100)]
		public int worldSeed = 0;
		[Tooltip("Biomes sizes")]
		public float biomeScale = 150;

		[Header("Biome merge configuration")]
		[Tooltip("biomes.appearValue difference for merge")]
		[Range(0.01f, 0.5f)]
		public float diffToMerge = 0.025f;
		[Tooltip("Surface desired level, height where biomes merge")]
		[Range(1, Constants.MAX_HEIGHT)]
		public int surfaceLevel = Constants.MAX_HEIGHT / 8;
		[Tooltip("Octaves used in the biome noise")]
		[Range(1, 5)]
		public int octaves = 5;
		[Tooltip("Amplitude decrease of biomes per octave,very low recommended")]
		[Range(0.001f, 1f)]
		public float persistance = 0.1f;
		[Tooltip("Frequency increase of biomes per octave")]
		[Range(1, 20)]
		public float lacunarity = 9f;
	}


	[Header("Biomes Array")]// Empty for get all Biomes of inside the GameObject
	public BiomeProperties[] biomes;

	[System.Serializable]
	public struct BiomeProperties
	{
		public Biome biome;//Biome child class
		public float appearValue;//1 to 0 value when the biome appears. Next biomePropertie.appear value is where this biome end
	}

	public void Start()
	{
		if (worldConfig.worldSeed == 0 && !WorldManager.IsCreated())//Generate random seed when use 0 and is scene testing (no WorldManager exists)
		{
			Debug.Log("worldSeed 0 detected, generating random seed world");
			string selectedWorld = WorldManager.GetSelectedWorldName();
			WorldManager.DeleteWorld(selectedWorld);//Remove previous data
			WorldManager.CreateWorld(selectedWorld, worldConfig);//Create a new world folder for correct working
			worldConfig.worldSeed = Random.Range(int.MinValue, int.MaxValue);
		}
		else if((Constants.AUTO_CLEAR_WHEN_NOISE_CHANGE) && !WorldManager.IsCreated())//If AUTO_CLEAR_WHEN_NOISE_CHANGE true and world manager not exist, we clear old world data (we assume we are using a debug scene)
		{
			string selectedWorld = WorldManager.GetSelectedWorldName();
			WorldConfig loadedWorldConfig = WorldManager.GetSelectedWorldConfig();
			//If worldConfig loaded is different to the current one, remove old data and save the new config
			if(loadedWorldConfig.worldSeed != worldConfig.worldSeed || loadedWorldConfig.biomeScale != worldConfig.biomeScale || loadedWorldConfig.diffToMerge != worldConfig.diffToMerge || loadedWorldConfig.surfaceLevel != worldConfig.surfaceLevel ||
				loadedWorldConfig.octaves != worldConfig.octaves || loadedWorldConfig.persistance != worldConfig.persistance || loadedWorldConfig.lacunarity != worldConfig.lacunarity)
			{
				WorldManager.DeleteWorld(selectedWorld);//Remove old world
				WorldManager.CreateWorld(selectedWorld, worldConfig);//Create new world with the new worldConfig
			}

		}
		else if(WorldManager.IsCreated())//Load config of the world
		{
			worldConfig = WorldManager.GetSelectedWorldConfig();
		}
		if(biomes.Length == 0)
		{
			Biome[] biomeArray = GetComponents<Biome>();
			biomes = new BiomeProperties[biomeArray.Length];
			for (int i = 0; i< biomeArray.Length; i++)
			{
				biomes[i].biome = biomeArray[i];
				biomes[i].appearValue = (float)(biomeArray.Length-i) / biomeArray.Length;
			}
		}
		ChunkManager.Instance.Initialize();
	}

	public byte[] GenerateChunkData(Vector2Int vecPos)
	{
		byte[] chunkData = new byte[Constants.CHUNK_BYTES];

		float[] biomeNoise = GenerateNoiseMap(worldConfig.biomeScale * biomes.Length, worldConfig.octaves, worldConfig.persistance, worldConfig.lacunarity, vecPos);//Biomes noise (0-1) of each (x,z) position
		float[] mergeBiomeTable;//Value(0-1) of merged with other biomes in a (x,z) position
		int[] biomeTable = GetChunkBiomes(biomeNoise, out mergeBiomeTable);//biomes index in the array of BiomeProperties

		byte[][] biomesData = new byte[biomes.Length][];//Data generate from biomes.biome.GenerateChunkData()

		for (int x= 0;  x< Constants.CHUNK_VERTEX_SIZE; x++)
		{
			for(int z=0; z<Constants.CHUNK_VERTEX_SIZE; z++)
			{
				int index = x + z * Constants.CHUNK_VERTEX_SIZE;
				if (biomesData[biomeTable[index]] == null)
				{
					biomesData[biomeTable[index]] = biomes[biomeTable[index]].biome.GenerateChunkData(vecPos, mergeBiomeTable);
				}

				for (int y = 0; y < Constants.CHUNK_VERTEX_HEIGHT; y++)
				{
					int chunkByteIndex = (index + y * Constants.CHUNK_VERTEX_AREA) * Constants.CHUNK_POINT_BYTE;
					chunkData[chunkByteIndex] = biomesData[biomeTable[index]][chunkByteIndex];
					chunkData[chunkByteIndex+1] = biomesData[biomeTable[index]][chunkByteIndex+1];
				}
			}
		}

		return chunkData;

	}

	/// <summary>
	/// Get the index from the biomes array, the bool out is for get the merge biome
	/// </summary>
	private int[] GetChunkBiomes(float[] noise, out float[] mergeBiome)
	{
		float[] mergeBiomeTable= new float[Constants.CHUNK_VERTEX_AREA];//value of merge with other biome, 1 = nothing, 0 full merge
		int[] biomeTable = new int[Constants.CHUNK_VERTEX_AREA];//Value with the index of the biomes of each (x,z) position
		for (int z = 0; z< Constants.CHUNK_VERTEX_SIZE; z++)
		{
			for (int x = 0; x < Constants.CHUNK_VERTEX_SIZE; x++)
			{
				int index = x + z * Constants.CHUNK_VERTEX_SIZE;
				for (int i = biomes.Length - 1; i >= 0; i--)
				{
					if (noise[index] < biomes[i].appearValue)
					{
						if (i != 0 && worldConfig.diffToMerge + noise[index] > biomes[i].appearValue)//Biome merged with top biome
						{
							mergeBiomeTable[index] = (biomes[i].appearValue - noise[index]) / worldConfig.diffToMerge;
							//Debug.Log("TOP: "+biomes[i].appearValue + " - " + noise[index] + " / " + diffToMerge + " = " + mergeBiomeTable[index]);
						}

						else if (i != biomes.Length - 1 && worldConfig.diffToMerge - noise[index] < biomes[i+1].appearValue)//Biome merged with bottom biome
						{
							mergeBiomeTable[index] = (noise[index] - biomes[i + 1].appearValue) / worldConfig.diffToMerge;
							//Debug.Log("BOT: "+noise[index] + " - " + biomes[i + 1].appearValue + " / " + diffToMerge + " = " + mergeBiomeTable[index]);
						}

						else
							mergeBiomeTable[index] = 1;//No biome merge needed


						/*if(noise[index]+ diffToMerge > 0.5f || noise[index] - diffToMerge < 0.5f)
						{
							Debug.Log(noise[index] + " " + mergeBiomeTable[index]);
						}*/

						biomeTable[index] = i;
						break;//We get the texture, we exit from texture loop( i loop)

					}
				}
			}
		}

		mergeBiome = mergeBiomeTable;
		return biomeTable;
	}


	/// <summary>
	/// Calculate the PerlinNoise used in the relief generation, only the chunk size (no slope calculation).
	/// </summary>
	public static float[] GenerateNoiseMap (float scale, int octaves, float persistance, float lacunarity, Vector2Int offset)
	{
		float[] noiseMap = new float[Constants.CHUNK_VERTEX_AREA];//Size of vertex + all next borders (For the slope calculation)

		System.Random random = new System.Random(Instance.worldConfig.worldSeed);//Used System.random, because unity.Random is global, can cause problems if there is other random running in other script
		Vector2[] octaveOffsets = new Vector2[octaves];

		float maxPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;

		for (int i = 0; i < octaves; i++)
		{
			float offsetX = random.Next(-100000, 100000) + offset.x * Constants.CHUNK_SIZE;
			float offsetY = random.Next(-100000, 100000) + offset.y * Constants.CHUNK_SIZE;
			octaveOffsets[i] = new Vector2(offsetX, offsetY);

			maxPossibleHeight += amplitude;
			amplitude *= persistance;
		}

		float halfVertexArea = Constants.CHUNK_VERTEX_SIZE / 2f;

		for (int z = 0; z < Constants.CHUNK_VERTEX_SIZE; z++)
		{
			for (int x = 0; x < Constants.CHUNK_VERTEX_SIZE; x++)
			{
				amplitude = 1;
				frequency = 1;
				float noiseHeight = 0;

				for (int i = 0; i < octaves; i++)
				{
					float sampleX = (x - halfVertexArea + octaveOffsets[i].x) / scale * frequency ;
					float sampleY = (z - halfVertexArea + octaveOffsets[i].y) / scale * frequency ;

					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) ;
					noiseHeight += perlinValue * amplitude;

					amplitude *= persistance;
					frequency *= lacunarity;
				}
				
				noiseMap[x + z * Constants.CHUNK_VERTEX_SIZE] = noiseHeight / (maxPossibleHeight * 0.9f);//*0.9 because reach the max points it's really dificult.

			}
		}

		return noiseMap;
	}

	/// <summary>
	/// Calculate the PerlinNoise used in the relief generation, with a extra edge in each side of the chunk, for the slope calculations.
	/// </summary>
	public static float[] GenerateExtendedNoiseMap(float scale, int octaves, float persistance, float lacunarity, Vector2Int offset)
	{
		float[] noiseMap = new float[(Constants.CHUNK_VERTEX_SIZE + 2) * (Constants.CHUNK_VERTEX_SIZE + 2)];//Size of vertex + all next borders (For the slope calculation)

		System.Random random = new System.Random(NoiseManager.Instance.worldConfig.worldSeed);//Used System.random, because unity.Random is global, can cause problems if there is other random running in other script
		Vector2[] octaveOffsets = new Vector2[octaves];

		float maxPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;

		for (int i = 0; i < octaves; i++)
		{
			float offsetX = random.Next(-100000, 100000) + offset.x * Constants.CHUNK_SIZE - 1;
			float offsetY = random.Next(-100000, 100000) + offset.y * Constants.CHUNK_SIZE - 1;
			octaveOffsets[i] = new Vector2(offsetX, offsetY);

			maxPossibleHeight += amplitude;
			amplitude *= persistance;
		}

		float halfVertexArea = Constants.CHUNK_VERTEX_SIZE / 2f;

		for (int z = 0; z < Constants.CHUNK_VERTEX_SIZE + 2; z++)
		{
			for (int x = 0; x < Constants.CHUNK_VERTEX_SIZE + 2; x++)
			{
				amplitude = 1;
				frequency = 1;
				float noiseHeight = 0;

				for (int i = 0; i < octaves; i++)
				{
					float sampleX = (x - halfVertexArea + octaveOffsets[i].x) / scale * frequency;
					float sampleY = (z - halfVertexArea + octaveOffsets[i].y) / scale * frequency;

					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
					noiseHeight += perlinValue * amplitude;

					amplitude *= persistance;
					frequency *= lacunarity;
				}

				noiseMap[x + z * (Constants.CHUNK_VERTEX_SIZE + 2)] = noiseHeight / (maxPossibleHeight * 0.9f);//*0.9 because reach the max points it's really dificult.

			}
		}

		return noiseMap;
	}

	/// <summary>
	/// Similar that GenerateNoiseMap but use only one octave, for that reason use less parameters and less operations
	/// </summary>
	public static float[] GenenerateSimpleNoiseMap(float scale, Vector2Int offset)
	{
		float[] noiseMap = new float[Constants.CHUNK_VERTEX_AREA];//Size of vertex + all next borders (For the slope calculation)

		System.Random random = new System.Random(NoiseManager.Instance.worldConfig.worldSeed);//Used System.random, because unity.Random is global, can cause problems if there is other random running in other script

		float offsetX = random.Next(-100000, 100000) + offset.x * Constants.CHUNK_SIZE ;
		float offsetY = random.Next(-100000, 100000) + offset.y * Constants.CHUNK_SIZE ;

		float halfVertexArea = Constants.CHUNK_VERTEX_SIZE / 2f;

		for (int z = 0; z < Constants.CHUNK_VERTEX_SIZE; z++)
		{
			for (int x = 0; x < Constants.CHUNK_VERTEX_SIZE; x++)
			{
				float sampleX = (x - halfVertexArea + offsetX) / scale;
				float sampleY = (z - halfVertexArea + offsetY) / scale;

				noiseMap[x + z * Constants.CHUNK_VERTEX_SIZE] = Mathf.PerlinNoise(sampleX, sampleY);
			}
		}

		return noiseMap;
	}
}
