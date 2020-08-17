using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NoiseManager))]
public class NoiseTerrainAutoUpdate : Editor
{
    // Function executed each time user interact with the inspector
    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();
        if(DrawDefaultInspector() && Application.isPlaying)//If we changue some value in the  NoiseManager and there is a NoiseTerrainViewer we update the terrain
        {
            GameObject obj  = GameObject.Find("TerrainGenerator");//TerrainGenerator = name of the GameObject that contain the NoiseTerrainViewer script
            if (obj != null)
                obj.GetComponent<NoiseTerrainViewer>().GenerateTerrain();
        }
    }
}
