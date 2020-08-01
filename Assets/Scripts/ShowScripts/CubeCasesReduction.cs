using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeCasesReduction : MonoBehaviour
{
    public float rotSpeed = 40;
    public Material faceMaterial;
    private GameObject[] configurationObj;
    private MeshBuilder meshBuilder;
    private int[][] status = {
    //0 means that vertex is contained by the figure
    //Example 1
    new int[] { 0, 0, 1, 1, 0, 0, 0, 0 },
    new int[] { 1, 1, 0, 0, 1, 1, 1, 1 },
    //example 1
    new int[] { 1, 0, 0, 0, 0, 0, 0, 0 },
    new int[] { 0, 1, 0, 0, 0, 0, 0, 0 },
    new int[] { 0, 0, 1, 0, 0, 0, 0, 0 },
    new int[] { 0, 0, 0, 1, 0, 0, 0, 0 },
    new int[] { 0, 0, 0, 0, 1, 0, 0, 0 },
    new int[] { 0, 0, 0, 0, 0, 1, 0, 0 },
    new int[] { 0, 0, 0, 0, 0, 0, 1, 0 },
    new int[] { 0, 0, 0, 0, 0, 0, 0, 1 },
    new int[] { 1, 0, 0, 0, 0, 0, 0, 0 }
    };
    // Start is called before the first frame update
    void Awake()
    {
        meshBuilder = MeshBuilder.Instance;
        configurationObj = new GameObject[]{
            //Example 1
            generateConfiguration(new Vector3(-3, 6, 0), status[0]),
            generateConfiguration(new Vector3(3, 6, 0), status[1]),
            //example 2
            generateConfiguration(new Vector3(-12, 0, 0), status[2]),
            generateConfiguration(new Vector3(-9, 0, 0), status[3]),
            generateConfiguration(new Vector3(-6, 0, 0), status[4]),
            generateConfiguration(new Vector3(-3, 0, 0), status[5]),
            generateConfiguration(new Vector3(-12, -3, 0), status[6]),
            generateConfiguration(new Vector3(-9, -3, 0), status[7]),
            generateConfiguration(new Vector3(-6, -3, 0), status[8]),
            generateConfiguration(new Vector3(-3, -3, 0), status[9]),
            generateConfiguration(new Vector3(3, -1.5f, 0), status[10])
        };
    }
    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < configurationObj.Length; i++)
        {
            configurationObj[i].transform.Rotate(0, rotSpeed * Time.deltaTime, 0);
        }
    }

    //Function to help to create a cube
    GameObject generateConfiguration(Vector3 translation, int[] vertexStatus)
    {
        Vector4[] cube = {
            new Vector4(-1, -1, 1, vertexStatus[0]),
            new Vector4(1, -1, 1, vertexStatus[1]),
            new Vector4(1, -1, -1, vertexStatus[2]),
            new Vector4(-1, -1, -1, vertexStatus[3]),
            new Vector4(-1, 1, 1, vertexStatus[4]),
            new Vector4(1, 1, 1, vertexStatus[5]),
            new Vector4(1, 1, -1, vertexStatus[6]),
            new Vector4(-1, 1, -1, vertexStatus[7])
        };

        string bits = "";
        for (int i = vertexStatus.Length - 1; i >= 0; i--)
            bits = bits + vertexStatus[i].ToString();

        GameObject cubeObj = new GameObject("Configuration-" + bits, typeof(MeshFilter), typeof(MeshRenderer));
        cubeObj.transform.position = translation;
        Mesh myMesh = meshBuilder.buildMesh(meshBuilder.CalculateVertex(cube));

        cubeObj.GetComponent<MeshFilter>().mesh = myMesh;
        cubeObj.GetComponent<MeshRenderer>().material = faceMaterial;


        return cubeObj;

    }

    //draw cubes
    //*IMPORTANT* The sphere indicates an empty vertex.
    void OnDrawGizmos()
    {
        if (configurationObj == null)
            return;
        // Draw a semitransparent blue cube at the transforms position
        for (int i = 0; i < configurationObj.Length; i++)
        {
            //Gizmos.color = new Color(1f,0.28f,0f);
            Gizmos.color = Color.Lerp(Color.red, Color.magenta, ((float)i / configurationObj.Length));

            Gizmos.matrix = configurationObj[i].transform.localToWorldMatrix;
            //Gizmos.DrawCube(Vector3.zero, new Vector3(2, 2, 2));
            Gizmos.DrawLine(new Vector3(-1, -1, -1), new Vector3(1, -1, -1));
            Gizmos.DrawLine(new Vector3(1, -1, -1), new Vector3(1, -1, 1));
            Gizmos.DrawLine(new Vector3(1, -1, 1), new Vector3(-1, -1, 1));
            Gizmos.DrawLine(new Vector3(-1, -1, -1), new Vector3(-1, -1, 1));

            Gizmos.DrawLine(new Vector3(-1, -1, 1), new Vector3(-1, 1, 1));
            Gizmos.DrawLine(new Vector3(-1, -1, -1), new Vector3(-1, 1, -1));
            Gizmos.DrawLine(new Vector3(1, -1, -1), new Vector3(1, 1, -1));
            Gizmos.DrawLine(new Vector3(1, -1, 1), new Vector3(1, 1, 1));

            Gizmos.DrawLine(new Vector3(-1, 1, -1), new Vector3(1, 1, -1));
            Gizmos.DrawLine(new Vector3(1, 1, -1), new Vector3(1, 1, 1));
            Gizmos.DrawLine(new Vector3(1, 1, 1), new Vector3(-1, 1, 1));
            Gizmos.DrawLine(new Vector3(-1, 1, -1), new Vector3(-1, 1, 1));



            if (status[i][0] > meshBuilder.isoLevel) Gizmos.DrawSphere(new Vector3(-1, -1, 1), 0.2f);
            if (status[i][1] > meshBuilder.isoLevel) Gizmos.DrawSphere(new Vector3(1, -1, 1), 0.2f);
            if (status[i][2] > meshBuilder.isoLevel) Gizmos.DrawSphere(new Vector3(1, -1, -1), 0.2f);
            if (status[i][3] > meshBuilder.isoLevel) Gizmos.DrawSphere(new Vector3(-1, -1, -1), 0.2f);
            if (status[i][4] > meshBuilder.isoLevel) Gizmos.DrawSphere(new Vector3(-1, 1, 1), 0.2f);
            if (status[i][5] > meshBuilder.isoLevel) Gizmos.DrawSphere(new Vector3(1, 1, 1), 0.2f);
            if (status[i][6] > meshBuilder.isoLevel) Gizmos.DrawSphere(new Vector3(1, 1, -1), 0.2f);
            if (status[i][7] > meshBuilder.isoLevel) Gizmos.DrawSphere(new Vector3(-1, 1, -1), 0.2f);

        }
    }
}
