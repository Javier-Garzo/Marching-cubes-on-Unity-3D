using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class WorldManager : Singleton<WorldManager>
{
    [SerializeField]private string world = "default"; //World selected by the manager
    public const string WORLDS_DIRECTORY = "/worlds"; //Directory worlds (save folder, that contains the worlds folders)
    

    private void Awake()
    {
        base.Awake();
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
    /// Create and select a new world (save/load folder), a worldConfig can be passed as second optional parameter for being used by the Noisemanager (or empty for default one).
    /// </summary>
    public static bool CreateWorld(string worldName, NoiseManager.WorldConfig newWorldConfig = null)
    {
        if (!Directory.Exists(Application.persistentDataPath + WORLDS_DIRECTORY + '/' + worldName))
        {
            Directory.CreateDirectory(Application.persistentDataPath + WORLDS_DIRECTORY + '/' + worldName);
            Instance.world = worldName;
            if(newWorldConfig != null)//Use the WorldConfig passed as parameter
            {
                string worldConfig = JsonUtility.ToJson(newWorldConfig);
                File.WriteAllText(Application.persistentDataPath + WORLDS_DIRECTORY + '/' + worldName+ "/worldConfig.json", worldConfig);
            }
            else//Use the default world config
            {
                newWorldConfig = new NoiseManager.WorldConfig();
                newWorldConfig.worldSeed = Random.Range(int.MinValue, int.MaxValue);
                string worldConfig = JsonUtility.ToJson(newWorldConfig);
                File.WriteAllText(Application.persistentDataPath + WORLDS_DIRECTORY + '/' + worldName + "/worldConfig.json", worldConfig);
            }
            return true;
        }
        else
        {
            Debug.LogError("folder already exists");
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
            Debug.LogError("folder not exists");
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
            Debug.LogError("world (folder) not exists");
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
    /// Return WorldConfig of the selected world.
    /// </summary>
    public static NoiseManager.WorldConfig GetSelectedWorldConfig()
    {
        string selectedWorld = GetSelectedWorldName();
        if (File.Exists(Application.persistentDataPath + WORLDS_DIRECTORY + '/' + selectedWorld + "/worldConfig.json"))
        {
            string json = File.ReadAllText(Application.persistentDataPath + WORLDS_DIRECTORY + '/' + selectedWorld + "/worldConfig.json");
            return JsonUtility.FromJson<NoiseManager.WorldConfig>(json);
        }
        else
        {
            Debug.LogError("No worldConfig.json exist, generating a new one, using the default parameters.");
            NoiseManager.WorldConfig newWorldConfig = new NoiseManager.WorldConfig();
            newWorldConfig.worldSeed = Random.Range(int.MinValue, int.MaxValue);
            string worldConfig = JsonUtility.ToJson(newWorldConfig);
            File.WriteAllText(Application.persistentDataPath + WORLDS_DIRECTORY + '/' + selectedWorld + "/worldConfig.json", worldConfig);
            return newWorldConfig;
        }

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
