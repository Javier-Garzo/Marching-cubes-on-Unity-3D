using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(NoiseTerrainViewer))]
public class NoiseViewerAutoUpdate : Editor
{
    // Function executed each time user interact with the inspector
    public override void OnInspectorGUI()
    {

        //base.OnInspectorGUI();
        if (DrawDefaultInspector() && Application.isPlaying)//If we changue some value in the NoiseTerrainViewer and there is a NoiseTerrainViewer we update the terrain
        {
            NoiseTerrainViewer noiseViewer = (NoiseTerrainViewer)target;
            noiseViewer.GenerateTerrain();
        }
    }
}

