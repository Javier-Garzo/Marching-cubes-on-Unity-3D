using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(WorldManager))]
public class WorldManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();EditorGUILayout.Space();

        if (GUILayout.Button("Open worlds folder"))
        {
            if (!Directory.Exists(Application.persistentDataPath + WorldManager.WORLDS_DIRECTORY))
                Directory.CreateDirectory(Application.persistentDataPath + WorldManager.WORLDS_DIRECTORY);
            EditorUtility.RevealInFinder(Application.persistentDataPath +WorldManager.WORLDS_DIRECTORY);
        }
    }
}
