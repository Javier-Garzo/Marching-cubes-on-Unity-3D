using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    [Tooltip("Active gizmos that represent the area of the chunk")]
    public bool debug = false;
    [Tooltip("Active gizmos that represent all vertex of terrain, very expensive")]
    public bool advancedDebug = false;
    private byte[] data;

    /// <summary>
    /// Create a Chunk using a byte[] that contain all the data of the chunk.
    /// </summary>
    /// <param name="b"> data of the chunk</param>
    public Chunk ChunkInit(byte[] b)
    {
        data = b;
        Mesh myMesh = MeshBuilder.Instance.BuildChunk(b);
        GetComponent<MeshFilter>().mesh = myMesh;

        //Assign random color
        Material myMaterial = new Material(Shader.Find("Specular"));//Custom/DoubleFaceShader  |   Specular
        myMaterial.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        GetComponent<MeshRenderer>().material = myMaterial;

        return this;
    }

    //Used for visual debug
    void OnDrawGizmos()
    {
        if (debug)
        {
            //Gizmos.color = new Color(1f,0.28f,0f);
            Gizmos.color = Color.Lerp(Color.red, Color.magenta, ((transform.position.x + transform.position.z) % 100) / 100);


            Gizmos.DrawWireCube(transform.position,new Vector3(Constants.CHUNK_SIDE, Constants.MAX_HEIGHT * Constants.VOXEL_SIDE, Constants.CHUNK_SIDE));
        }
        if (advancedDebug)
        {

            Gizmos.color = Color.Lerp(Color.red, Color.magenta, ((transform.position.x + transform.position.z) % 100) / 100);

            Gizmos.matrix = transform.localToWorldMatrix;

            float isoLevel = MeshBuilder.Instance.isoLevel;
            for (int y = 120; y < Constants.CHUNK_VERTEX_HEIGHT; y++)//height
            {
                for (int z = 0; z < Constants.CHUNK_VERTEX_SIZE; z++)//column
                {
                    for (int x = 0; x < Constants.CHUNK_VERTEX_SIZE; x++)//line 
                    {

                        if (data[(x + z * Constants.CHUNK_VERTEX_SIZE + y * Constants.CHUNK_VERTEX_AREA) * Constants.CHUNK_POINT_BYTE] > isoLevel)
                        {
                            Gizmos.DrawSphere(new Vector3((x - Constants.CHUNK_SIZE / 2) * Constants.VOXEL_SIDE,
                                (y - Constants.MAX_HEIGHT / 2) * Constants.VOXEL_SIDE,
                                (z - Constants.CHUNK_SIZE / 2) * Constants.VOXEL_SIDE)
                                , 0.2f);
                        }
                    }
                }

            }
        }
    }
}


