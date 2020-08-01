using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshBuilder : Singleton<MeshBuilder>
{
    public float isoLevel = 128f;
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
        //Values above isoLevel are inside the figure, value of 0 means that the cube is entirely inside of the figure.
        int cubeindex = 0;
        if (cube[0].w < isoLevel) cubeindex |= 1;
        if (cube[1].w < isoLevel) cubeindex |= 2;
        if (cube[2].w < isoLevel) cubeindex |= 4;
        if (cube[3].w < isoLevel) cubeindex |= 8;
        if (cube[4].w < isoLevel) cubeindex |= 16;
        if (cube[5].w < isoLevel) cubeindex |= 32;
        if (cube[6].w < isoLevel) cubeindex |= 64;
        if (cube[7].w < isoLevel) cubeindex |= 128;

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
        bool h = false;
        List<Vector3> vertexArray = new List<Vector3>();
        for(int y = 0; y< Constants.MAX_HEIGHT; y++)//height
        {
            for(int z = 1; z < Constants.CHUNK_SIZE+1; z++)//column, start at 1, because Z axis is inverted and need -1 as offset
            {
                for(int x = 0; x < Constants.CHUNK_SIZE; x++)//line 
                {
                    Vector4[] cube = new Vector4[8];
                    cube[0] = CalculateVertexChunk(x, y, z, b);
                    cube[1] = CalculateVertexChunk(x+1, y, z, b);
                    cube[2] = CalculateVertexChunk(x+1, y, z-1, b);
                    cube[3] = CalculateVertexChunk(x, y, z-1, b);
                    cube[4] = CalculateVertexChunk(x, y+1, z, b);
                    cube[5] = CalculateVertexChunk(x+1, y+1, z, b);
                    cube[6] = CalculateVertexChunk(x+1, y+1, z-1, b);
                    cube[7] = CalculateVertexChunk(x, y+1, z-1, b);
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
            (x - Constants.CHUNK_SIZE / 2) * Constants.VOXEL_SIDE,
            (y - Constants.MAX_HEIGHT / 2) * Constants.VOXEL_SIDE,
            (z - Constants.CHUNK_SIZE / 2) * Constants.VOXEL_SIDE,
            b[(x + z * Constants.CHUNK_VERTEX_SIZE + y * Constants.CHUNK_VERTEX_AREA) * Constants.CHUNK_POINT_BYTE]);
    }

    #region helpMethods
    /// <summary>
    /// Calculate a point between two vertex using the weight of each vertex , used in interpolation voxel building.
    /// </summary>
    public Vector3 interporlateVertex(Vector3 p1, Vector3 p2,float val1,float val2)
    {
        return Vector3.Lerp(p1, p2, (isoLevel - val1) / (val2 - val1));
    }
    /// <summary>
    /// Calculate the middle point between two vertex, for no interpolation voxel building.
    /// </summary>
    public Vector3 midlePointVertex(Vector3 p1, Vector3 p2)
    {
        return (p1 + p2) / 2;
    }
    #endregion
}
