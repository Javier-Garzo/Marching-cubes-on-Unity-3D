using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    [Tooltip("Active gizmos that represent the area of the chunk")]
    public bool denbug = false;
    [Tooltip("Active gizmos that represent all vertex of terrain, very expensive")]
    public bool advancedDebug = false;
    private byte[] data;

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

    void OnDrawGizmos()
    {
        if (denbug)
        {
            //Gizmos.color = new Color(1f,0.28f,0f);
            Gizmos.color = Color.Lerp(Color.red, Color.magenta, ((transform.position.x + transform.position.z) % 100) / 100);


            Gizmos.DrawWireCube(transform.position,new Vector3(Constants.CHUNK_SIDE, Constants.MAX_HEIGHT * Constants.VOXEL_SIZE, Constants.CHUNK_SIDE));
        }
        if (advancedDebug)
        {

            Gizmos.color = Color.Lerp(Color.red, Color.magenta, ((transform.position.x + transform.position.z) % 100) / 100);

            Gizmos.matrix = transform.localToWorldMatrix;

            float surfaceLevel = MeshBuilder.Instance.surfaceLevel;
            for (int y = 0; y < Constants.MAX_HEIGHT+1; y++)//height
            {
                for (int z = 0; z < Constants.CHUNK_SIZE+1; z++)//column
                {
                    for (int x = 0; x < Constants.CHUNK_SIZE+1; x++)//line 
                    {
                        if(data[(x + z * Constants.CHUNK_SIZE + y * Constants.CHUNK_VOXEL_AREA) * Constants.CHUNK_POINT_BYTE] < surfaceLevel)
                        {
                            Gizmos.DrawSphere(new Vector3((x - Constants.CHUNK_SIZE / 2) * Constants.VOXEL_SIZE,
                                (y - Constants.MAX_HEIGHT / 2) * Constants.VOXEL_SIZE,
                                (z - Constants.CHUNK_SIZE / 2) * Constants.VOXEL_SIZE)
                                , 0.2f);
                        }
                    }
                }

            }
        }
    }
}


