using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseManager : Singleton<NoiseManager>
{
	[Header("Noise / terrain generation")]
	[Tooltip("Seed of the world")] [Range(int.MinValue + 100, int.MaxValue - 100)]
	public int worldSeed = 0;
	[Tooltip("The deepest point of the surface")] [Range(1, Constants.MAX_HEIGHT)]
	public int surfaceStart = 1;
	[Tooltip("The highest point of the surface")] [Range(0, Constants.MAX_HEIGHT-1)]
	public int maxSurfaceheight = Constants.MAX_HEIGHT-1;
	[Tooltip("Animation curve for attenuate the height in some ranges")]
	public AnimationCurve terrainHeightCurve;
	[Tooltip("Scale of the noise map")] [Range(0.001f, 100f)]
	public float scale = 50f;
	[Tooltip("Number of deferents relief apply to the terrain surface")] [Range(1, 8)]
	public int octaves = 4;
	[Tooltip("Amplitude decrease of reliefs")] [Range(0.001f, 1f)]
	public float persistance = 0.5f;
	[Tooltip("Frequency increase of reliefs")] [Range(1, 20)]
	public float lacunarity = 2f;
	
	[Header("Texture generation")]
	[Tooltip("Increase the effect of the hightMatMult")][Range(1, 20f)]
	public float heightMatOffset = 10;
	[Tooltip("Multiplier of the slope in dependence of the height")]
	public AnimationCurve hightMatMult;
	[Range(0, Constants.MAX_HEIGHT)]
	public int snowHeight = 35;
	[Range(0, 1f)]
	public float rockLevel = 0.6f;
	[Range(0, 1f)]
	public float dirtLevel = 0.25f;

	private int isoLevel;

	public void Awake()
	{
		isoLevel = MeshBuilder.Instance.isoLevel;
		if (worldSeed == 0)//Generate random seed when use 0
			worldSeed = Random.Range(int.MinValue, int.MaxValue);

	}

	/// <summary>
	/// Generate the data of the chunk using the values of the manager.
	/// </summary>
	public byte[] GenerateChunkData(Vector2Int vecPos)
	{
		byte[] chunkData = new byte[Constants.CHUNK_BYTES];
		float[] noise = GenerateNoiseMap(scale, octaves, persistance, lacunarity, vecPos);
		float maxSlope = 0;
		float minSlope = 100;
		for (int z = 1; z < Constants.CHUNK_VERTEX_SIZE+1; z++)//start a 1 because the noise start at -1 of the chunk vertex
		{
			for (int x = 1; x < Constants.CHUNK_VERTEX_SIZE+1; x++)//start a 1 because the noise start at -1 of the chunk vertex
			{
				// Get surface height of the x,z position 
				float height = terrainHeightCurve.Evaluate(noise[x + z * (Constants.CHUNK_VERTEX_SIZE+2)]) * (maxSurfaceheight - surfaceStart) + surfaceStart;
				int heightY = Mathf.CeilToInt(height);//Vertex Y where surface start
				int lastVertexWeigh = (int)((255 - isoLevel) * (height % 1) + isoLevel);//Weigh of the last vertex
				float slope = CalculateSlope(x,z,noise);

				if (maxSlope < slope)
					maxSlope = slope;
				if (minSlope > slope)
					minSlope = slope;

				//Debug.Log(x + "|" + z + "= " + slope);

				for (int y = 0; y < Constants.CHUNK_VERTEX_HEIGHT; y++)
				{
					int index = ((x-1) + (z-1) * Constants.CHUNK_VERTEX_SIZE + y * Constants.CHUNK_VERTEX_AREA) * Constants.CHUNK_POINT_BYTE;//apply x-1 and z-1 for get the correct index
					if (y < heightY-5)
					{
						chunkData[index] = 255;
						chunkData[index + 1] = 4;//Rock
					}
					else if (y < heightY)
					{
						chunkData[index] = 255;
						if (slope > rockLevel)
							chunkData[index + 1] = 4;//Rock
						else if(slope < dirtLevel && y > snowHeight)//Avoid dirt in snow areas
							chunkData[index + 1] = 3;
						else
							chunkData[index + 1] = 1;//dirt
					}
					else if (y == heightY)//ESTO ES UN ==
					{
						chunkData[index] = (byte)lastVertexWeigh;//
						if (slope > rockLevel)
							chunkData[index + 1] = 4;//Mountain Rock
						else if (slope > dirtLevel)
							chunkData[index + 1] = 1;//dirt
						else
						{
							if(y > snowHeight)
								chunkData[index + 1] = 3;//snow
							else
								chunkData[index + 1] = 0;//grass
						}
							
					}
					else
					{
						chunkData[index] = 0;
						chunkData[index + 1] = Constants.NUMBER_MATERIALS;
					}
				}
			}
		}
		//Debug.Log(minSlope +" > " + maxSlope);
		return chunkData;

	}

	
	/// <summary>
	/// Function that calculate the slope of the terrain
	/// </summary>
	private float CalculateSlope(int x, int z, float[] noise)
	{
		float maxValue = 0;
		float minValue = 100;
		for(int xOffset = x-1; xOffset <= x+1; xOffset += 1)
		{
			for (int zOffset = z-1; zOffset <= z+1; zOffset += 1)
			{
				float value =terrainHeightCurve.Evaluate(noise[xOffset + zOffset * (Constants.CHUNK_VERTEX_SIZE + 2)]);
				if (value > maxValue)
					maxValue = value;
				if (value < minValue)
					minValue = value;
			}
		}
		float pointValue = terrainHeightCurve.Evaluate(noise[x + z * (Constants.CHUNK_VERTEX_SIZE + 2)]);
		return (1- (minValue / pointValue) ) * (hightMatMult.Evaluate(pointValue) * heightMatOffset); ;
	}


	/// <summary>
	/// Calculate the PerlinNoise used in the relief generation.
	/// </summary>
	public float[] GenerateNoiseMap (float scale, int octaves, float persistance, float lacunarity, Vector2Int offset)
	{
		float[] noiseMap = new float[(Constants.CHUNK_VERTEX_SIZE + 2) * (Constants.CHUNK_VERTEX_SIZE + 2)];//Size of vertex + all next borders (For the slope calculation)

		System.Random random = new System.Random(worldSeed);//Used System.random, because unity.Random is global, can cause problems if there is other random running in other script
		Vector2[] octaveOffsets = new Vector2[octaves];

		float maxPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;

		for (int i = 0; i < octaves; i++)
		{
			float offsetX = random.Next(-100000, 100000) + offset.x * Constants.CHUNK_SIZE-1;
			float offsetY = random.Next(-100000, 100000) + offset.y * Constants.CHUNK_SIZE-1;
			octaveOffsets[i] = new Vector2(offsetX, offsetY);

			maxPossibleHeight += amplitude;
			amplitude *= persistance;
		}

		float halfVertexArea = Constants.CHUNK_VERTEX_SIZE / 2f;

		for (int z = 0; z < Constants.CHUNK_VERTEX_SIZE+2; z++)
		{
			for (int x = 0; x < Constants.CHUNK_VERTEX_SIZE+2; x++)
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
				
				noiseMap[x + z *(Constants.CHUNK_VERTEX_SIZE+2)] = noiseHeight / (maxPossibleHeight * 0.9f);//*0.9 because reach the max points it's really dificult.

			}
		}

		return noiseMap;
	}
}
