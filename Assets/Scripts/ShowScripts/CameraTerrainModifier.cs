using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTerrainModifier : MonoBehaviour
{

    [Tooltip("Range where the player can interact with the terrain")]
    public float rangeHit = 100;
    [Tooltip("Force of modifications applied to the terrain")]
    public float modiferStrengh = 10;
    [Tooltip("Size of the brush, number of vertex modified")]
    public float sizeHit = 6;
    [Tooltip("Color of the new voxels generated")][Range(0, Constants.NUMBER_MATERIALS-1)]
    public int buildingMaterial = 0;

    private RaycastHit hit;
    private ChunkManager chunkManager;

    void Awake()
    {
        chunkManager = ChunkManager.Instance;
        
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            float modification = (Input.GetMouseButton(0)) ? modiferStrengh : -modiferStrengh;
            if (Physics.Raycast(transform.position, transform.forward, out hit, rangeHit))
            {
                chunkManager.ModifyChunkData(hit.point, sizeHit, modification, buildingMaterial);
            }
        }
        
    }
}
