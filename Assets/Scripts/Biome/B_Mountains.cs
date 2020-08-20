using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B_Mountains : Biome
{
	[Tooltip("The highest point of the surface")][Range(0, Constants.MAX_HEIGHT - 1)]
	public int maxSurfaceheight = Constants.MAX_HEIGHT - 1;

	[Header("Texture generation")]
	[Tooltip("Increase the effect of the hightMatMult")][Range(1, 20f)]
	public float heightMatOffset = 10;
	[Tooltip("Multiplier of the slope in dependence of the height")]
	public AnimationCurve hightMatMult;
	[Tooltip("Height where the grass change to snow")][Range(0, Constants.MAX_HEIGHT)]
	public int snowHeight = 35;
	[Tooltip("Slope vale where terrain start to be rock")][Range(0, 1f)]
	public float rockLevel = 0.6f;
	[Tooltip("Slope vale where terrain start to be dirt")][Range(0, 1f)]
	public float dirtLevel = 0.25f;


	public override byte[] GenerateChunkData(Vector2Int vecPos, float[] biomeMerge)
	{
		int surfaceStart = NoiseManager.Instance.surfaceLevel ;//Avoid too high value that generate bad mesh
		byte[] chunkData = new byte[Constants.CHUNK_BYTES];
		float[] noise = NoiseManager.GenerateExtendedNoiseMap(scale, octaves, persistance, lacunarity, vecPos);
		for (int z = 0; z < Constants.CHUNK_VERTEX_SIZE; z++)//start a 1 because the noise start at -1 of the chunk vertex
		{
			for (int x = 0; x < Constants.CHUNK_VERTEX_SIZE ; x++)//start a 1 because the noise start at -1 of the chunk vertex
			{
				// Get surface height of the x,z position 1276120704
				float height = Mathf.Lerp(
					NoiseManager.Instance.surfaceLevel,//Biome merge height
					(terrainHeightCurve.Evaluate(noise[(x+1) + (z+1) * (Constants.CHUNK_VERTEX_SIZE + 2)]) * (maxSurfaceheight - surfaceStart) + surfaceStart),//Desired biome height
					biomeMerge[x + z * Constants.CHUNK_VERTEX_SIZE]);//Merge value,0 = full merge, 1 = no merge

				//557164096
				int heightY = Mathf.CeilToInt(height);//Vertex Y where surface start
				int lastVertexWeigh = (int)((255 - isoLevel) * (height % 1) + isoLevel);//Weigh of the last vertex
				float slope = CalculateSlope((x+1), (z+1), noise);

				for (int y = 0; y < Constants.CHUNK_VERTEX_HEIGHT; y++)
				{
					int index = (x + z * Constants.CHUNK_VERTEX_SIZE + y * Constants.CHUNK_VERTEX_AREA) * Constants.CHUNK_POINT_BYTE;//apply x-1 and z-1 for get the correct index
					if (y < heightY - 5)
					{
						chunkData[index] = 255;
						chunkData[index + 1] = 4;//Rock
					}
					else if (y < heightY)
					{
						chunkData[index] = 255;
						if (slope > rockLevel)
							chunkData[index + 1] = 4;//Rock
						else if (slope < dirtLevel && y > snowHeight)//Avoid dirt in snow areas
							chunkData[index + 1] = 3;
						else
							chunkData[index + 1] = 1;//dirt
					}
					else if (y == heightY)
					{
						chunkData[index] = (byte)lastVertexWeigh;//
						if (slope > rockLevel)
							chunkData[index + 1] = 4;//Mountain Rock
						else if (slope > dirtLevel)
							chunkData[index + 1] = 1;//dirt
						else
						{
							if (y > snowHeight)
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
		return chunkData;
	}

	/// <summary>
	/// Function that calculate the slope of the terrain
	/// </summary>
	private float CalculateSlope(int x, int z, float[] noise)
	{
		float minValue = 1000;
		for (int xOffset = x - 1; xOffset <= x + 1; xOffset += 1)
		{
			for (int zOffset = z - 1; zOffset <= z + 1; zOffset += 1)
			{
				float value = terrainHeightCurve.Evaluate(noise[xOffset + zOffset * (Constants.CHUNK_VERTEX_SIZE + 2)]);
				if (value < minValue)
					minValue = value;
			}
		}
		float pointValue = terrainHeightCurve.Evaluate(noise[x + z * (Constants.CHUNK_VERTEX_SIZE + 2)]);
		return (1 - (minValue / pointValue)) * (hightMatMult.Evaluate(pointValue) * heightMatOffset); ;
	}
}
