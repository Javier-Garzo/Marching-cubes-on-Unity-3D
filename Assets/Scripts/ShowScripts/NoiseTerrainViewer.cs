using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseTerrainViewer : MonoBehaviour
{
    public int testSize = 1;
    private Dictionary<Vector2Int, Chunk> chunkDict = new Dictionary<Vector2Int, Chunk>();
    private NoiseManager noiseManager;
    private Region fakeRegion;//Used because chunks need a fahter region
 

    private void Start()
    {
        noiseManager = NoiseManager.Instance;
        fakeRegion = new Region(1000,1000);
        GenerateTerrain();
    }

    /// <summary>
    /// Generate a terrain for preview the NoiseManager values.
    /// </summary>
    public void GenerateTerrain()
    {
        if(chunkDict.Count != 0)
        {
            foreach(Chunk chunk in chunkDict.Values)
            {
                Destroy(chunk.gameObject);
            }
            chunkDict.Clear();
        }
        int halfSize = Mathf.FloorToInt(testSize / 2);
        for(int z= -halfSize; z< halfSize+1; z++)
        {
            for (int x = -halfSize; x < halfSize+1; x++)
            {
                Vector2Int key = new Vector2Int(x,z);
                GameObject chunkObj = new GameObject("Chunk_" + key.x + "|" + key.y, typeof(MeshFilter), typeof(MeshRenderer));
                chunkObj.transform.parent = transform;
                chunkObj.transform.position = new Vector3(key.x * Constants.CHUNK_SIDE, 0, key.y * Constants.CHUNK_SIDE);

                chunkDict.Add(key, chunkObj.AddComponent<Chunk>().ChunkInit(noiseManager.GenerateChunkData(key), key.x, key.y, fakeRegion, false));
            }
        }
    }

}


