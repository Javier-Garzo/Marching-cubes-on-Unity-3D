using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseTerrainViewer : MonoBehaviour
{

    [Tooltip("Number of chunks of the view area")]
    [Range(1, 20)]
    public int testSize = 1;
    [Tooltip("Offset from the chunk (0,0), move the whole map generation")]
    public Vector2Int chunkOffset;
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
                Vector2Int key = new Vector2Int(x, z);
                GameObject chunkObj = new GameObject("Chunk_" + key.x + "|" + key.y, typeof(MeshFilter), typeof(MeshRenderer));
                chunkObj.transform.parent = transform;
                chunkObj.transform.position = new Vector3(key.x * Constants.CHUNK_SIDE, 0, key.y * Constants.CHUNK_SIDE);

                Vector2Int offsetKey = new Vector2Int(x + chunkOffset.x, z+ chunkOffset.y);
                chunkDict.Add(key, chunkObj.AddComponent<Chunk>().ChunkInit(noiseManager.GenerateChunkData(offsetKey), key.x, key.y, fakeRegion, false));
            }
        }
    }

}


