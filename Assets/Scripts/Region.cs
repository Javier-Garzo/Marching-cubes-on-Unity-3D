using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Region
{
    private byte[] chunkData;
    private int regionX;
    private int regionZ;


    /// <summary>
    /// Create/load a region
    /// </summary>
    public Region(int x, int z)
    {

        regionX = x;
        regionZ = z;
        chunkData = new byte[Constants.REGION_BYTES];
        //load region data, temp implementation: Same terrain
        int index = 0;
        int offset = 289;
        for (int i = 0; i< Constants.REGION_CHUNKS; i++)
        {
            for (int j = 0; j< Constants.CHUNK_BYTES/2 + offset ; j+=2)
            {
                chunkData[index] = 255;//terrain
                if(j< Constants.CHUNK_BYTES *2/5)
                    chunkData[index + 1] = 2;//stone
                else if(j < Constants.CHUNK_BYTES *2.438f/5)
                    chunkData[index + 1] = 1;//dirt
                else
                    chunkData[index + 1] = 0;//grass

                index += 2;
            }
            
            for (int j = Constants.CHUNK_BYTES/2 + offset; j < Constants.CHUNK_BYTES; j += 2)
            {
                chunkData[index] = 0;//air
                chunkData[index + 1] = 255;//type material, now empty
                index += 2;
            }
        }
    }

    /// <summary>
    /// Extract data of a chunk from a region
    /// </summary>
    /// <returns>Data of the chunk</returns>
    public byte[] GetChunk(int x, int z)
    {
        x -= regionX * 32;
        z -= regionZ * 32;

        byte[] chunk = new byte[Constants.CHUNK_BYTES];
        int startSize = Constants.CHUNK_BYTES * (z + x * Constants.REGION_SIZE);
        for (int i = startSize, j = 0; i < (startSize + Constants.CHUNK_BYTES); i ++,j++)
        {
            chunk[j] = chunkData[i];
        }
        return chunk;
    }


}
