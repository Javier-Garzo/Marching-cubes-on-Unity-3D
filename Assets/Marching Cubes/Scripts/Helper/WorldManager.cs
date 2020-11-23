using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class WorldManager : Singleton<WorldManager>
{
    [SerializeField]private string world = "default"; //World selected by the manager
    public const string WORLDS_DIRECTORY = "/worlds"; //Directory worlds (save folder)
    

    private void Awake()
    {
        base.Awake();

        GetSelectedWorldDir();
        if (Instance == this)
        {
            DontDestroyOnLoad(gameObject);

            if (!Directory.Exists(Application.persistentDataPath + WORLDS_DIRECTORY))//in case worlds directory not created, create the "worlds" directory 
                Directory.CreateDirectory(Application.persistentDataPath + WORLDS_DIRECTORY);

            if (!Directory.Exists(Application.persistentDataPath + WORLDS_DIRECTORY + "/" + world))//in case world not created, create the world (generate folder)
                Directory.CreateDirectory(Application.persistentDataPath + WORLDS_DIRECTORY + "/" + world);
        }
    }

    /// <summary>
    /// Create and select a new world (save/load folder).
    /// </summary>
    public static bool CreateWorld(string worldName)
    {
        if (!Directory.Exists(Application.persistentDataPath + WORLDS_DIRECTORY + '/' + worldName))
        {
            Directory.CreateDirectory(Application.persistentDataPath + WORLDS_DIRECTORY + '/' + worldName);
            Instance.world = worldName;
            return true;
        }
        else
        {
            Debug.Log("folder already exists");
            return false;
        }
    }

    /// <summary>
    /// Delete a world (save/load folder) and remove all related files.
    /// </summary>
    public static bool DeleteWorld(string worldName)
    {
        if (Directory.Exists(Application.persistentDataPath + WORLDS_DIRECTORY + '/' + worldName))
        {
            Directory.Delete(Application.persistentDataPath + WORLDS_DIRECTORY + '/' + worldName, true);
            return true;
        }
        else
        {
            Debug.Log("folder not exists");
            return false;
        }
    }

    /// <summary>
    /// Select a world (save/load folder) which will load next time by the ChunkSystem.
    /// </summary>
    public static bool SelectWorld(string worldName)
    {
        if (Directory.Exists(Application.persistentDataPath + WORLDS_DIRECTORY + '/' + worldName))
        {
            Instance.world = worldName;
            return true;
        }
        else
        {
            Debug.Log("world (folder) not exists");
            return false;
        }
    }

    /// <summary>
    /// Return the name of the selected world.
    /// </summary>
    public static string GetSelectedWorldName()
    {
        return Instance.world;
    }

    /// <summary>
    /// Return the path of the selected world.
    /// </summary>
    public static string GetSelectedWorldDir()
    {
        return Application.persistentDataPath + WORLDS_DIRECTORY + "/" + Instance.world;
    }

    /// <summary>
    /// Return all the worlds as a string[].
    /// </summary>
    public static string[] GetAllWorlds()
    {
        return Directory.GetDirectories(Application.persistentDataPath + WORLDS_DIRECTORY).Select(Path.GetFileName).ToArray();
        
    }

    /// <summary>
    /// Return size of a world.
    /// </summary>
    public static long worldSize(string worldName)
    {
        if (Directory.Exists(Application.persistentDataPath + WORLDS_DIRECTORY + '/' + worldName))
            return new DirectoryInfo(Application.persistentDataPath + WORLDS_DIRECTORY + '/' + worldName).GetFiles("*.*", SearchOption.TopDirectoryOnly).Sum(file => file.Length);
        else
            return 0;
    }
}
