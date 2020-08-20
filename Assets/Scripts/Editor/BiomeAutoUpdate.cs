using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Biome), true)]
public class BiomeAutoUpdate : Editor
{
    // Function executed each time user interact with the inspector
    public override void OnInspectorGUI()
    {

        //base.OnInspectorGUI();
        if (DrawDefaultInspector() && Application.isPlaying)//If we changue some value inside a biome and there is a NoiseTerrainViewer we update the terrain
        {
            Biome obj = (Biome)target;
            NoiseTerrainViewer noiseViewer = obj.gameObject.GetComponent<NoiseTerrainViewer>();
            if (noiseViewer != null)
                noiseViewer.GenerateTerrain();
        }
    }
}
