using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshBuilder : Singleton<MeshBuilder>
{
    public float surfaceLevel = 128f;
    public bool interpolate = false;

    /// <summary>
    /// It generate a mesh from a group of vertex
    /// </summary>
    public Mesh buildMesh(List<Vector3> vertex)
    {
        Mesh mesh = new Mesh();
        int[] triangles = new int[vertex.Count];
        for(int i= 0; i< triangles.Length; i++)
            triangles[i] = i;

        mesh.vertices = vertex.ToArray();
        mesh.triangles = triangles;
        return mesh;

    }

    /// <summary>
    /// Calculate the vertex of a voxel using a cube
    /// </summary>
    /// <param name="cube">Vector4[] array corresponding to the data of each vertex of the voxel</param>
    /// <returns>array of vertex, corresponding to the mesh triangles of voxel</returns>
    public List<Vector3> CalculateVertex(Vector4 [] cube)
    {
        int cubeindex = 0;
        if (cube[0].w < surfaceLevel) cubeindex |= 1;
        if (cube[1].w < surfaceLevel) cubeindex |= 2;
        if (cube[2].w < surfaceLevel) cubeindex |= 4;
        if (cube[3].w < surfaceLevel) cubeindex |= 8;
        if (cube[4].w < surfaceLevel) cubeindex |= 16;
        if (cube[5].w < surfaceLevel) cubeindex |= 32;
        if (cube[6].w < surfaceLevel) cubeindex |= 64;
        if (cube[7].w < surfaceLevel) cubeindex |= 128;

        List<Vector3> vertexArray = new List<Vector3>();

        for(int i = 0; Constants.triTable[cubeindex,i] != -1; i++)
        {
            int v1 = Constants.cornerIndexAFromEdge[Constants.triTable[cubeindex, i]];
            int v2 = Constants.cornerIndexBFromEdge[Constants.triTable[cubeindex, i]];

            if (interpolate)
                vertexArray.Add(interporlateVertex(cube[v1], cube[v2], cube[v1].w, cube[v2].w));
            else
                vertexArray.Add(midlePointVertex(cube[v1], cube[v2]));
        }

        return vertexArray;

    }

    /// <summary>
    /// Method that calculate cubes, vertex and mesh in that order of a chunk.
    /// </summary>
    /// <param name="b"> data of the chunk</param>
    public Mesh BuildChunk(byte[] b)
    {
        List<Vector3> vertexArray = new List<Vector3>();
        for(int y = 0; y< Constants.MAX_HEIGHT; y++)//height
        {
            for(int z = 0; z < Constants.CHUNK_SIZE; z++)//column
            {
                for(int x = 0; x < Constants.CHUNK_SIZE; x++)//line 
                {
                    Vector4[] cube = new Vector4[8];
                    cube[0] = CalculateVertexChunk(x, y, z, b);
                    cube[1] = CalculateVertexChunk(x+1, y, z, b);
                    cube[2] = CalculateVertexChunk(x+1, y, z+1, b);
                    cube[3] = CalculateVertexChunk(x, y, z+1, b);
                    cube[4] = CalculateVertexChunk(x, y+1, z, b);
                    cube[5] = CalculateVertexChunk(x+1, y+1, z, b);
                    cube[6] = CalculateVertexChunk(x+1, y+1, z+1, b);
                    cube[7] = CalculateVertexChunk(x, y+1, z+1, b);
                    vertexArray.AddRange(CalculateVertex(cube));
                }
            }

        }
        return buildMesh(vertexArray);

    }

    /// <summary>
    /// Calculate the data of a vertex of a voxel
    /// </summary>
    private Vector4 CalculateVertexChunk(int x, int y, int z, byte[] b)
    {
        return new Vector4(
            (x - Constants.CHUNK_SIZE / 2) * Constants.VOXEL_SIZE,
            (y - Constants.MAX_HEIGHT / 2) * Constants.VOXEL_SIZE,
            (z - Constants.CHUNK_SIZE / 2) * Constants.VOXEL_SIZE,
            b[(x + z * Constants.CHUNK_SIZE + y * Constants.CHUNK_VOXEL_AREA) * Constants.CHUNK_POINT_BYTE]);
    }

    #region helpMethods
    public Vector3 interporlateVertex(Vector3 p1, Vector3 p2,float val1,float val2)
    {
        return Vector3.Lerp(p1, p2, (surfaceLevel - val1) / (val2 - val1));
    }
    public Vector3 midlePointVertex(Vector3 p1, Vector3 p2)
    {
        return (p1 + p2) / 2;
    }
    #endregion
}
