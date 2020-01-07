using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshBuilder : Singleton<MeshBuilder>
{
    public float surfaceLevel = 0.5f;
    public bool interpolate = false;
    // Start is called before the first frame update
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
    public List<Vector3> calculateVertex(Vector4 [] cube)
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

        for(int i = 0; Table.triTable[cubeindex,i] != -1; i++)
        {
            int v1 = Table.cornerIndexAFromEdge[Table.triTable[cubeindex, i]];
            int v2 = Table.cornerIndexBFromEdge[Table.triTable[cubeindex, i]];

            if (interpolate)
                vertexArray.Add(interporlateVertex(cube[v1], cube[v2], cube[v1].w, cube[v2].w));
            else
                vertexArray.Add(midlePointVertex(cube[v1], cube[v2]));
        }

        return vertexArray;

    }

    public Vector3 interporlateVertex(Vector3 p1, Vector3 p2,float val1,float val2)
    {
        return Vector3.Lerp(p1, p2, (surfaceLevel - val1) / (val2 - val1));
    }
    public Vector3 midlePointVertex(Vector3 p1, Vector3 p2)
    {
        return (p1 + p2) / 2;
    }
}
