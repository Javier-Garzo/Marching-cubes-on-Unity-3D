using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    [Range(3, Constants.REGION_SIZE/2)][Tooltip("Chunks load and visible for the player,radius distance")]
    public int chunkViewDistance = 10;
    [Range(0.1f, 0.6f)][Tooltip("Distance extra for destroy inactive chunks, this chunks consume ram, but load faster.")]
    public float chunkMantainDistance = 0.3f;

    private Dictionary<Vector2Int, Chunk> chunkDict = new Dictionary<Vector2Int, Chunk>();
    private Dictionary<Vector2Int, Region> regionDict = new Dictionary<Vector2Int, Region>();
    private List<Vector2Int> chunkLoadList = new List<Vector2Int>();

    private Transform player;
    private Vector3 lastPlayerPos;
    private float hideDistance;
    private float removeDistance;
    private float loadRegionDistance;


    //Load on initialize the game
    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        lastPlayerPos = player.position;
        hideDistance = Constants.CHUNK_SIDE * chunkViewDistance;
        removeDistance = hideDistance + hideDistance * chunkMantainDistance;
        loadRegionDistance = Constants.CHUNK_SIDE * Constants.REGION_SIZE;
        initRegion(0,0);
    }

    /// <summary>
    /// Load surrounding regions of the player when first load
    /// </summary>
    void initRegion(int initX, int initZ)
    {
        for (int x = initX-1; x < initX+2; x++)
        {
            for (int z = initZ-1; z < initZ + 2; z++)
            {
                regionDict.Add(new Vector2Int(x,z), new Region(x,z));
            }
        }
    }

    /// <summary>
    /// Load new regions and unload the older.
    /// </summary>
    void LoadRegion(int initX, int initZ)
    {
        Dictionary<Vector2Int, Region> newRegionDict = new Dictionary<Vector2Int, Region>();

        for (int x = initX-1; x < initX + 2; x++)
        {
            for (int z = initZ-1; z < initZ + 2; z++)
            {
                if (regionDict.ContainsKey(new Vector2Int(x,z)))
                    newRegionDict.Add(new Vector2Int(x,z), regionDict[new Vector2Int(x,z)]);
                else
                    newRegionDict.Add(new Vector2Int(x,z), new Region(x, z));
            }
        }
        regionDict = newRegionDict;
    }

    //Called each frame
    void Update()
    {
        HiddeRemoveChunk();
        CheckNewChunks();
        LoadChunkFromList();
        CheckRegion();
        //Debug.Log("Regions: " + regionDict.Count + "   / Chunks: " + chunkDict.Count);
    }

    /// <summary>
    /// Check the distance to the player for inactive or remove the chunk.  
    /// </summary>
    void HiddeRemoveChunk()
    {
        List<Vector2Int> removeList = new List<Vector2Int>(); ;
        foreach (KeyValuePair<Vector2Int, Chunk> chunk in chunkDict)
        {
            float distance = Mathf.Sqrt(Mathf.Pow((player.position.x - chunk.Value.transform.position.x), 2) + Mathf.Pow((player.position.z - chunk.Value.transform.position.z), 2));
            if (distance > removeDistance)
            {
                //save chunk data
                Destroy(chunk.Value.gameObject);
                removeList.Add(chunk.Key);
            }
            else if (distance > hideDistance && chunk.Value.gameObject.activeSelf)
            {
                chunk.Value.gameObject.SetActive(false);
            }
        }

        //remove chunks
        if(removeList.Count != 0)
        {
            foreach(Vector2Int key in removeList)
            {
                //Debug.Log("chunk deleted: " + key);
                chunkDict.Remove(key);
            }
        }

    }

    /// <summary>
    /// Load in chunkLoadList or active Gameobject chunks at the chunkViewDistance radius of the player
    /// </summary>
    void CheckNewChunks()
    {
        Vector2Int actualChunk =new Vector2Int(Mathf.CeilToInt((player.position.x- Constants.CHUNK_SIDE / 2) / Constants.CHUNK_SIDE ),
                                                Mathf.CeilToInt((player.position.z - Constants.CHUNK_SIDE / 2) / Constants.CHUNK_SIDE ));
        //Debug.Log("Actual chunk: " + actualChunk);
        for(int x= actualChunk.x-chunkViewDistance; x< actualChunk.x + chunkViewDistance; x++)
        {
            for (int z = actualChunk.y - chunkViewDistance; z < actualChunk.y + chunkViewDistance; z++)
            {
                if (Mathf.Pow((actualChunk.x - x), 2) + Mathf.Pow((actualChunk.y - z), 2) > chunkViewDistance * chunkViewDistance)
                {
                    continue;
                }
                Vector2Int key = new Vector2Int(x, z);
                if (!chunkDict.ContainsKey(key))
                {
                    if(!chunkLoadList.Contains(key))
                    {
                        chunkLoadList.Add(key);
                    }
                }
                else
                {
                    if(!chunkDict[key].gameObject.activeSelf)
                        chunkDict[key].gameObject.SetActive(true);
                }
            }
        }
    }

    /// <summary>
    /// Load one chunk per frame from the chunkLoadList
    /// </summary>
    void LoadChunkFromList()
    {
        if (chunkLoadList.Count == 0)
            return;

        Vector2Int key = chunkLoadList[0];

        Vector2Int regionPos = new Vector2Int(Mathf.FloorToInt(((float)key.x) / Constants.REGION_SIZE), Mathf.FloorToInt(((float)key.y) / Constants.REGION_SIZE));
        GameObject chunkObj = new GameObject("Chunk_" + key.x + "|" + key.y, typeof(MeshFilter), typeof(MeshRenderer));
        chunkObj.transform.parent = transform;
        chunkObj.transform.position = new Vector3(key.x * Constants.CHUNK_SIDE, 0, key.y * Constants.CHUNK_SIDE);
        //Debug.Log("Try load: "+x+"|"+z +" in "+regionPos);
        chunkDict.Add(key, chunkObj.AddComponent<Chunk>().ChunkInit(regionDict[regionPos].GetChunk(key.x, key.y)));

        chunkLoadList.RemoveAt(0);
    }

    /// <summary>
    /// Check chunk manager need load a new regions area
    /// </summary>
    void CheckRegion()
    {
        if (Mathf.Abs(lastPlayerPos.x - player.position.x) > loadRegionDistance || Mathf.Abs(lastPlayerPos.z - player.position.z) > loadRegionDistance )
        {
            int actualX = Mathf.FloorToInt(player.position.x / loadRegionDistance);
            lastPlayerPos.x = actualX * loadRegionDistance;
            int actualZ = Mathf.FloorToInt(player.position.z / loadRegionDistance);
            lastPlayerPos.z = actualZ * loadRegionDistance;
            LoadRegion(actualX, actualZ);
        }
    }

}
