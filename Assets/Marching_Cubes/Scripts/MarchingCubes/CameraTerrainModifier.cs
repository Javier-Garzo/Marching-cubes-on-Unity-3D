using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraTerrainModifier : MonoBehaviour
{
    public Text textSize;
    public Text textMaterial;
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
        UpdateUI();
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

        //Inputs
        if (Input.GetAxis("Mouse ScrollWheel") > 0 && buildingMaterial != Constants.NUMBER_MATERIALS - 1)
        {
            buildingMaterial++;
            UpdateUI();
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0 && buildingMaterial != 0)
        {
            buildingMaterial--;
            UpdateUI();
        }
        
        if(Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            sizeHit++;
            UpdateUI();
        }
        else if((Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) && sizeHit > 1)
        {
            sizeHit--;
            UpdateUI();
        }

    }

    public void UpdateUI()
    {
        textSize.text = "(+ -) Brush size: " + sizeHit;
        textMaterial.text = "(Mouse wheel) Actual material: " + buildingMaterial;
    }
}
