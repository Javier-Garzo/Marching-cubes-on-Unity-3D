using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CubeConfigurations : MonoBehaviour
{
    public float rotSpeed = 40;
    public Text indicationText;
    public int actualMaterial = 1;
    public Material[] faceMaterials;
    private GameObject[] configurationObj;
    private MeshBuilder meshBuilder;
    private int[][] status = {
    //0 means that vertex is contained by the figure
    new int[] { 0, 0, 0, 0, 0, 0, 0, 0 },
    new int[] { 0, 0, 0, 200, 0, 0, 0, 0 },
    new int[] { 0, 0, 200, 200, 0, 0, 0, 0 },
    new int[] { 0, 0, 0, 200, 0, 0, 200, 0 },
    new int[] { 200, 200, 200, 0, 0, 0, 0, 0 },
    new int[] { 200, 200, 200, 200, 0, 0, 0, 0 },
    new int[] { 200, 200, 200, 0, 0, 0, 0, 200 },
    new int[] { 0, 200, 0, 200, 200, 0, 200, 0 },
    new int[] { 200, 200, 0, 200, 200, 0, 0, 0 },
    new int[] { 200, 200, 200, 0, 200, 0, 0, 0 },
    new int[] { 0, 0, 0, 200, 0, 200, 0, 0 },
    new int[] { 0, 0, 200, 200, 0, 200, 0, 0 },
    new int[] { 0, 0, 200, 0, 0, 200, 0, 200 },
    new int[] { 0, 200, 0, 200, 0, 200, 0, 200 },
    new int[] { 200, 200, 0, 200, 0, 200, 0, 0 }
    };
    // Start is called before the first frame update
    void Awake()
    {
        meshBuilder = MeshBuilder.Instance;
        configurationObj = new GameObject[]{
            generateConfiguration(new Vector3(-6, 3, 0), status[0]),
            generateConfiguration(new Vector3(-3, 3, 0), status[1]),
            generateConfiguration(new Vector3(0, 3, 0), status[2]),
            generateConfiguration(new Vector3(3, 3, 0), status[3]),
            generateConfiguration(new Vector3(6, 3, 0), status[4]),
            generateConfiguration(new Vector3(-6, 0, 0), status[5]),
            generateConfiguration(new Vector3(-3, 0, 0), status[6]),
            generateConfiguration(new Vector3(0, 0, 0), status[7]),
            generateConfiguration(new Vector3(3, 0, 0), status[8]),
            generateConfiguration(new Vector3(6, 0, 0), status[9]),
            generateConfiguration(new Vector3(-6, -3, 0), status[10]),
            generateConfiguration(new Vector3(-3, -3, 0), status[11]),
            generateConfiguration(new Vector3(0, -3, 0), status[12]),
            generateConfiguration(new Vector3(3, -3, 0), status[13]),
            generateConfiguration(new Vector3(6, -3, 0), status[14])
        };
    }
    // Update is called once per frame
    void Update()
    {
        for(int i= 0; i< configurationObj.Length; i++)
        {
            configurationObj[i].transform.Rotate(0, rotSpeed * Time.deltaTime, 0);
        }
        if (Input.GetAxis("Mouse ScrollWheel") > 0 && actualMaterial!= faceMaterials.Length-1)
        {
            actualMaterial++;
            changeTextures();
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0 && actualMaterial  != 0)
        {
            actualMaterial--;
            changeTextures();
        }
    }

    /// <summary>
    /// Function to help to create a cube
    /// </summary>
    public GameObject generateConfiguration(Vector3 translation, int[] vertexStatus)
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
        for (int i = vertexStatus.Length-1; i >= 0; i--)
            bits = bits + vertexStatus[i].ToString();

        GameObject cubeObj = new GameObject("Configuration-"+ bits,  typeof(MeshFilter), typeof(MeshRenderer));
        cubeObj.transform.position = translation;
        Mesh myMesh = meshBuilder.buildMesh(meshBuilder.CalculateVertex(cube));

        cubeObj.GetComponent<MeshFilter>().mesh = myMesh;
        cubeObj.GetComponent<MeshRenderer>().material = faceMaterials[actualMaterial];


        return cubeObj;

    }

    /// <summary>
    /// Change texture of the example voxels
    /// </summary>
    private void changeTextures()
    {
        for(int i= 0; i< configurationObj.Length;i++)
        {
            configurationObj[i].GetComponent<MeshRenderer>().material = faceMaterials[actualMaterial];
        }
        switch(actualMaterial)
        {
            case 0:
                indicationText.text = "    One face shader: Show the front face of the voxel with a wireframe effect.";
                break;
            case 1:
                indicationText.text = "    Two face shader: Show the front and back face (curl off) of the voxel with a wireframe effect in both sides.";
                break;
            case 2:
                indicationText.text = "    Color cull shader: show front faces with a red color and back faces with a blue color. (The blue faces don't render in normal cases)";
                break;
        }
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
            Gizmos.color = Color.Lerp(Color.red, Color.magenta, ((float)i/ configurationObj.Length));

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
