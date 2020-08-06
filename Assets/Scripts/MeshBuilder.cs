
//IMPORTANT: Comment the #define for deactivate flatShading
//When it's using flat shading, the materials inside the texture grid are single color without variations. True: increase speed calculations
//#define FlatShading 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MeshBuilder : Singleton<MeshBuilder>
{

    [Tooltip("Value from which the vertices are inside the figure")][Range(0, 255)]
    public int isoLevel = 128;
    [Tooltip("Allow to get a middle point between the voxel vertices in function of the weight of the vertices")]
    public bool interpolate = false;

    //Code is really similar but some parameters change in dependence of the flatShading, for that reason we use #if
#if (FlatShading)

    /// <summary>
    /// It generate a mesh from a group of vertex. Flat shading type.
    /// </summary>
    public Mesh buildMesh(List<Vector3> vertex, int[] textures = null)
    {
        Mesh mesh = new Mesh();
        int[] triangles = new int[vertex.Count];

        mesh.vertices = vertex.ToArray();
        if (textures == null)
        {
            for (int i = 0; i < triangles.Length; i++)
                triangles[i] = i;
        }
        else//Calculate UV of vertices
        {
            Vector2[] uv = new Vector2[textures.Length];
            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] = i;
                uv[i] = new Vector2(Constants.MATERIAL_SIZE * (textures[i]% Constants.MATERIAL_FOR_ROW)+ Constants.MATERIAL_OFFSET, 
                                   (1 - Mathf.Floor(textures[i] / Constants.MATERIAL_FOR_ROW) * Constants.MATERIAL_SIZE - Constants.MATERIAL_OFFSET));
            }

            mesh.uv = uv;
        }

        mesh.triangles = triangles;
        return mesh;

    }



    /// <summary>
    ///  Calculate the vertices of the voxels, get the vertices of the triangulation table and his position in the world. Also check materials of that vertex (UV position).
    /// </summary>
    public List<Vector3> CalculateVertex(Vector4[] cube, int colorVert, ref List<int> matVert)
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

        for (int i = 0; Constants.triTable[cubeindex, i] != -1; i++)
        {
            int v1 = Constants.cornerIndexAFromEdge[Constants.triTable[cubeindex, i]];
            int v2 = Constants.cornerIndexBFromEdge[Constants.triTable[cubeindex, i]];

            if (interpolate)
                vertexArray.Add(interporlateVertex(cube[v1], cube[v2], cube[v1].w, cube[v2].w));
            else
                vertexArray.Add(midlePointVertex(cube[v1], cube[v2]));


            matVert.Add(colorVert);

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
        List<int> matVert = new List<int>();
        for (int y = 0; y < Constants.MAX_HEIGHT; y++)//height
        {
            for (int z = 1; z < Constants.CHUNK_SIZE + 1; z++)//column, start at 1, because Z axis is inverted and need -1 as offset
            {
                for (int x = 0; x < Constants.CHUNK_SIZE; x++)//line 
                {
                    Vector4[] cube = new Vector4[8];
                    int mat = Constants.NUMBER_MATERIALS;
                    cube[0] = CalculateVertexChunk(x, y, z, b, ref mat);
                    cube[1] = CalculateVertexChunk(x + 1, y, z, b, ref mat);
                    cube[2] = CalculateVertexChunk(x + 1, y, z - 1, b, ref mat);
                    cube[3] = CalculateVertexChunk(x, y, z - 1, b, ref mat);
                    cube[4] = CalculateVertexChunk(x, y + 1, z, b, ref mat);
                    cube[5] = CalculateVertexChunk(x + 1, y + 1, z, b, ref mat);
                    cube[6] = CalculateVertexChunk(x + 1, y + 1, z - 1, b, ref mat);
                    cube[7] = CalculateVertexChunk(x, y + 1, z - 1, b, ref mat);
                    vertexArray.AddRange(CalculateVertex(cube, mat, ref matVert));
                }

            }

        }

        return buildMesh(vertexArray, matVert.ToArray());

    }
#else
    /// <summary>
    /// It generate a mesh from a group of vertex. Flat shading type.
    /// </summary>
    public Mesh buildMesh(List<Vector3> vertex, List<Vector2> textures = null)
    {
        Mesh mesh = new Mesh();
        int[] triangles = new int[vertex.Count];

        mesh.vertices = vertex.ToArray();
        if (textures != null)
            mesh.uv = textures.ToArray();

        for (int i = 0; i < triangles.Length; i++)
            triangles[i] = i;

        mesh.triangles = triangles;
        return mesh;

    }

    /// <summary>
    ///  Calculate the vertices of the voxels, get the vertices of the triangulation table and his position in the world. Also check materials of that vertex (UV position).
    /// </summary>
    public List<Vector3> CalculateVertex(Vector4[] cube, int colorVert, ref List<Vector2> matVert)
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

        for (int i = 0; Constants.triTable[cubeindex, i] != -1; i++)
        {
            int v1 = Constants.cornerIndexAFromEdge[Constants.triTable[cubeindex, i]];
            int v2 = Constants.cornerIndexBFromEdge[Constants.triTable[cubeindex, i]];

            if (interpolate)
                vertexArray.Add(interporlateVertex(cube[v1], cube[v2], cube[v1].w, cube[v2].w));
            else
                vertexArray.Add(midlePointVertex(cube[v1], cube[v2]));


            const float uvOffset = 0.01f; //Small offset for avoid pick pixels of other textures
            //NEED REWORKING FOR CORRECT WORKING, now have problems with the directions of the uv
            if (i % 6 == 0)
                matVert.Add(new Vector2(Constants.MATERIAL_SIZE*(colorVert % Constants.MATERIAL_FOR_ROW)+ Constants.MATERIAL_SIZE-uvOffset,
                                  1- Constants.MATERIAL_SIZE * Mathf.Floor(colorVert / Constants.MATERIAL_FOR_ROW)-uvOffset));
            else if (i % 6 == 1)
                matVert.Add(new Vector2(Constants.MATERIAL_SIZE * (colorVert % Constants.MATERIAL_FOR_ROW) + Constants.MATERIAL_SIZE - uvOffset,
                                  1 - Constants.MATERIAL_SIZE * Mathf.Floor(colorVert / Constants.MATERIAL_FOR_ROW)- Constants.MATERIAL_SIZE + uvOffset));
            else if(i % 6 == 2)
                matVert.Add(new Vector2(Constants.MATERIAL_SIZE * (colorVert % Constants.MATERIAL_FOR_ROW) + uvOffset,
                                  1 - Constants.MATERIAL_SIZE * Mathf.Floor(colorVert / Constants.MATERIAL_FOR_ROW) -uvOffset));
            else if (i % 6 == 3)
                matVert.Add(new Vector2(Constants.MATERIAL_SIZE * (colorVert % Constants.MATERIAL_FOR_ROW) + Constants.MATERIAL_SIZE - uvOffset,
                                  1 - Constants.MATERIAL_SIZE * Mathf.Floor(colorVert / Constants.MATERIAL_FOR_ROW) - Constants.MATERIAL_SIZE + uvOffset));
            else if (i % 6 == 4)
                matVert.Add(new Vector2(Constants.MATERIAL_SIZE * (colorVert % Constants.MATERIAL_FOR_ROW) + uvOffset,
                                   1 - Constants.MATERIAL_SIZE * Mathf.Floor(colorVert / Constants.MATERIAL_FOR_ROW) - Constants.MATERIAL_SIZE + uvOffset));
            else if (i % 6 == 5)
                matVert.Add(new Vector2(Constants.MATERIAL_SIZE * (colorVert % Constants.MATERIAL_FOR_ROW + uvOffset),
                                  1 - Constants.MATERIAL_SIZE * Mathf.Floor(colorVert / Constants.MATERIAL_FOR_ROW) - uvOffset));


            /*
             * if (i % 6 == 0)
                matVert.Add(new Vector2(0.33f * , 0.66f));
            else if (i % 6 == 1)
                matVert.Add(new Vector2(0.33f, 0.33f));
            else if(i % 6 == 2)
                matVert.Add(new Vector2(0, 0.66f));
            else if (i % 6 == 3)
                matVert.Add(new Vector2(0.33f, 0.33f));
            else if (i % 6 == 4)
                matVert.Add(new Vector2(0, 0.33f));
            else if (i % 6 == 5)
                matVert.Add(new Vector2(0f, 0.66f));*/

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
        List<Vector2> matVert = new List<Vector2>();
        for (int y = 0; y < Constants.MAX_HEIGHT; y++)//height
        {
            for (int z = 1; z < Constants.CHUNK_SIZE + 1; z++)//column, start at 1, because Z axis is inverted and need -1 as offset
            {
                for (int x = 0; x < Constants.CHUNK_SIZE; x++)//line 
                {
                    Vector4[] cube = new Vector4[8];
                    int mat = Constants.NUMBER_MATERIALS;
                    cube[0] = CalculateVertexChunk(x, y, z, b, ref mat);
                    cube[1] = CalculateVertexChunk(x + 1, y, z, b, ref mat);
                    cube[2] = CalculateVertexChunk(x + 1, y, z - 1, b, ref mat);
                    cube[3] = CalculateVertexChunk(x, y, z - 1, b, ref mat);
                    cube[4] = CalculateVertexChunk(x, y + 1, z, b, ref mat);
                    cube[5] = CalculateVertexChunk(x + 1, y + 1, z, b, ref mat);
                    cube[6] = CalculateVertexChunk(x + 1, y + 1, z - 1, b, ref mat);
                    cube[7] = CalculateVertexChunk(x, y + 1, z - 1, b, ref mat);
                    vertexArray.AddRange(CalculateVertex(cube, mat, ref matVert));
                }

            }

        }

        return buildMesh(vertexArray, matVert);

    }
#endif


    /// <summary>
    /// Calculate the data of a vertex of a voxel
    /// </summary>
    private Vector4 CalculateVertexChunk(int x, int y, int z, byte[] b, ref int colorVoxel)
    {
        int index = (x + z * Constants.CHUNK_VERTEX_SIZE + y * Constants.CHUNK_VERTEX_AREA) * Constants.CHUNK_POINT_BYTE;
        int material = b[index+1];
        if (b[index] >= isoLevel && material < colorVoxel)
            colorVoxel = material;
        return new Vector4(
            (x - Constants.CHUNK_SIZE / 2) * Constants.VOXEL_SIDE,
            (y - Constants.MAX_HEIGHT / 2) * Constants.VOXEL_SIDE,
            (z - Constants.CHUNK_SIZE / 2) * Constants.VOXEL_SIDE,
            b[index]);
    }

    /// <summary>
    /// Overload of the CalculateVertex method but without material calculations.
    /// </summary>
    public List<Vector3> CalculateVertex(Vector4[] cube)
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

        for (int i = 0; Constants.triTable[cubeindex, i] != -1; i++)
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
