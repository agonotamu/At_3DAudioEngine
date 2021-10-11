/*
 * Author : Antoine Gonot
 * Company : Art-Tone
 * First Creation : 19/01/2021
 * 
 * DESCRIPTION : Util class using for :
 * - Maintaining an updated reference to the UNIQUE list of Player States (static variable) / updated by the player engine, by its customized editor or the mixer 
 * - Methods for saving and loading (serializing) the list of Players States
 * 
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.SceneManagement;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioAsioPatchBay;
using System.Collections;

public class At_AudioEngineUtils : MonoBehaviour
{
    // reference to the UNIQUE class containing the list of Player States
    static public At_3DAudioEngineState audioEngineStates;
    static public At_ExternAssetsState externalAssetsStates;

    static public Dictionary<String, At_3DAudioEngineState> audioEngineStatesDictionary = new Dictionary<String, At_3DAudioEngineState>();
    
    static public AsioOut asioOut;

    public static At_ExternAssetsState getExternalAssetsState()
    {

        if (externalAssetsStates == null)
        {
            loadExternAssetsState();
        }

        if (externalAssetsStates == null)
        {
            externalAssetsStates = new At_ExternAssetsState();
        }
        return externalAssetsStates;
    }

    public static void saveExternAssetsState()
    {
        string fileName = "data_audioengine.json";
        string json = JsonUtility.ToJson(externalAssetsStates);
        string path = GetFilePathForExternalAssets(fileName);
        FileStream fileStream = new FileStream(path, FileMode.Create);

        using (StreamWriter writer = new StreamWriter(fileStream))
        {
            writer.Write(json);
        }
    }

    static bool loadExternAssetsState()
    {

        string fileName = "data_audioengine.json";
        TextAsset jsonTextAsset = Resources.Load<TextAsset>("externalAssetsState");
        string path = GetFilePathForExternalAssets(fileName);
        string json_new = "";//= ReadFromFile(path);
       
        
        if (File.Exists(path))
        {
            
            using (StreamReader reader = new StreamReader(path))
            {
                json_new = reader.ReadToEnd();
                
            }
        }
        else
        {
            Debug.LogError("External Asset State File not found!");
           
        }


        /*
        if (jsonTextAsset == null)
        {
            Debug.LogWarning("External Asset State File not found!");
            return false;
        }
        else
        */
        if(json_new != "")
        {
            
            //string json = jsonTextAsset.text;

            At_ExternAssetsState eas = new At_ExternAssetsState();
            //JsonUtility.FromJsonOverwrite(json, eas);
            JsonUtility.FromJsonOverwrite(json_new, eas);
            
#if UNITY_EDITOR
            if (Directory.Exists(eas.externAssetsPath_state)){                
#else
            if (Directory.Exists(eas.externAssetsPath_state_standalone)){                
#endif

                //Debug.Log("asset path exists");
                externalAssetsStates = eas;
                return true;
            }
            else
            {
                return false;
            }
            
        }
        else
        {
            return false;
        }

        
        /*
        At_External os = new At_OutputState();
        JsonUtility.FromJsonOverwrite(lines[lineIndex], os);
        audioEngineStates.outputState = os;
        */
    }

    /***************************************************************************
     * 
     * SAVE AND LOAD VIRTUAL SPEAKERS AND VIRTUAL MICROPHONE CONFIGURATION
     * 
     **************************************************************************/
    
    static At_AudioEngineUtils()
    {
       

        bool isExternAssetsLoaded = loadExternAssetsState();

        

        if(isExternAssetsLoaded) LoadAll();
    }

    
   
    /*
    public static void LoadAllStates()
    {
        loadExternAssetsState();
        LoadAll();
    }
    */

    static bool findGuidOfPlayerdInScene(string guid, At_Player[] players)
    {
        foreach(At_Player p in players)
        {
            if (p.guid == guid) return true;
        }
        return false;
    }
    static bool findGuidOfRandomInScene(string guid, At_DynamicRandomPlayer[] randomPlayers)
    {
        foreach (At_DynamicRandomPlayer drp in randomPlayers)
        {
            if (drp.guid == guid) return true;
        }
        return false;
    }
  
    public static void CleanAllStates(string sceneName)
    {
        At_3DAudioEngineState audioEngineStatesForScene = audioEngineStatesDictionary[sceneName];


        //At_Player[] players = GameObject.FindObjectsOfType<At_Player>();
        At_Player[] players = Resources.FindObjectsOfTypeAll(typeof(At_Player)) as At_Player[];

        //At_DynamicRandomPlayer[] randomPlayers = GameObject.FindObjectsOfType<At_DynamicRandomPlayer>();
        At_DynamicRandomPlayer[] randomPlayers = Resources.FindObjectsOfTypeAll(typeof(At_DynamicRandomPlayer)) as At_DynamicRandomPlayer[];

        bool isStateFileClean = false;

        while (!isStateFileClean)
        {
            for (int i = 0; i < audioEngineStatesForScene.playerStates.Count; i++)
            {
                At_PlayerState ps = audioEngineStatesForScene.playerStates[i];
                if (!findGuidOfPlayerdInScene(ps.guid, players))
                {
                    removePlayerWithGuid(sceneName, ps.guid);
                }
            }

            // test if clean
            isStateFileClean = true;
            for (int i = 0; i < audioEngineStatesForScene.playerStates.Count; i++)
            {
                At_PlayerState ps = audioEngineStatesForScene.playerStates[i];
                if (!findGuidOfPlayerdInScene(ps.guid, players))
                {
                    isStateFileClean = false;
                    break;
                }

            }
        }

        isStateFileClean = false;

        while (!isStateFileClean)
        {
            for (int i = 0; i < audioEngineStatesForScene.randomPlayerStates.Count; i++)
            {
                At_DynamicRandomPlayerState rps = audioEngineStatesForScene.randomPlayerStates[i];
                if (!findGuidOfRandomInScene(rps.guid, randomPlayers))
                {
                    removeRandomPlayerWithGuid(sceneName, rps.guid);
                }
            }

            // test if clean
            isStateFileClean = true;
            for (int i = 0; i < audioEngineStatesForScene.randomPlayerStates.Count; i++)
            {
                At_DynamicRandomPlayerState rps = audioEngineStatesForScene.randomPlayerStates[i];
                if (!findGuidOfRandomInScene(rps.guid, randomPlayers))
                {
                    isStateFileClean = false;
                    break;
                }

            }
        }

        /*
        //foreach (At_DynamicRandomPlayerState rps in audioEngineStates.randomPlayerStates)
        for (int i = 0; i < audioEngineStates.randomPlayerStates.Count; i++)
        {
            At_DynamicRandomPlayerState rps = audioEngineStates.randomPlayerStates[i];
            if (!findGuidOfRandomInScene(rps.guid, randomPlayers))
            {
                removeRandomPlayerWithGuid(rps.guid);
            }
        }
        */
        //SaveAllState();

    }

    public static bool setSpeakerState(string sceneName, At_VirtualMic[] virtualMics, At_VirtualSpeaker[] virtualSpeakers)
    {
        bool hasChanged = false;

        At_3DAudioEngineState audioEngineStatesForScene = audioEngineStatesDictionary[sceneName];

        if (audioEngineStatesForScene.virtualSpeakerState == null)
            audioEngineStatesForScene.virtualSpeakerState = new At_VirtualSpeakerState();
        if(audioEngineStatesForScene.virtualSpeakerState.virtualSpeakerId == null)
            audioEngineStatesForScene.virtualSpeakerState.virtualSpeakerId = new int[virtualMics.Length];
        if (audioEngineStatesForScene.virtualSpeakerState.virtualMicId == null)
            audioEngineStatesForScene.virtualSpeakerState.virtualMicId = new int[virtualMics.Length];
        if (audioEngineStatesForScene.virtualSpeakerState.virtualMicPositions == null)
            audioEngineStatesForScene.virtualSpeakerState.virtualMicPositions = new float [3*virtualMics.Length];
        if (audioEngineStatesForScene.virtualSpeakerState.virtualSpeakerPositions == null)
            audioEngineStatesForScene.virtualSpeakerState.virtualSpeakerPositions = new float[3* virtualSpeakers.Length];

        for (int i = 0; i< virtualMics.Length; i++)
        {

            if (audioEngineStatesForScene.virtualSpeakerState.virtualSpeakerPositions[3 * i] != virtualSpeakers[i].gameObject.transform.position.x)
            {
                audioEngineStatesForScene.virtualSpeakerState.virtualSpeakerPositions[3 * i] = virtualSpeakers[i].gameObject.transform.position.x;
                audioEngineStatesForScene.virtualSpeakerState.virtualMicPositions[3 * i] = virtualMics[i].gameObject.transform.position.x;
                hasChanged = true;
            }
            if (audioEngineStatesForScene.virtualSpeakerState.virtualSpeakerPositions[3 * i + 1] != virtualSpeakers[i].gameObject.transform.position.y)
            {
                audioEngineStatesForScene.virtualSpeakerState.virtualSpeakerPositions[3 * i + 1] = virtualSpeakers[i].gameObject.transform.position.y;
                audioEngineStatesForScene.virtualSpeakerState.virtualMicPositions[3 * i + 1] = virtualMics[i].gameObject.transform.position.y;
                hasChanged = true;
            }
            if (audioEngineStatesForScene.virtualSpeakerState.virtualSpeakerPositions[3 * i + 2] != virtualSpeakers[i].gameObject.transform.position.z)
            {
                audioEngineStatesForScene.virtualSpeakerState.virtualSpeakerPositions[3 * i + 2] = virtualSpeakers[i].gameObject.transform.position.z;
                audioEngineStatesForScene.virtualSpeakerState.virtualMicPositions[3 * i + 2] = virtualMics[i].gameObject.transform.position.z;
                hasChanged = true;
            }
            audioEngineStatesForScene.virtualSpeakerState.virtualSpeakerId[i] = virtualSpeakers[i].id;
            audioEngineStatesForScene.virtualSpeakerState.virtualMicId[i] = virtualMics[i].id;

        }

        return hasChanged;
    }
    
    public static void saveSpeakerState(string sceneName)
    {
        At_3DAudioEngineState audioEngineStatesForScene = audioEngineStatesDictionary[sceneName];
        string json = JsonUtility.ToJson(audioEngineStatesForScene.virtualSpeakerState);
        //Scene scene = SceneManager.GetActiveScene();
        WriteToFile(sceneName + "_VirtualSpeakerState.txt", json);
    }

    public static At_VirtualSpeakerState getVirtualSpeakerState(string sceneName)
    {
        At_3DAudioEngineState audioEngineStatesForScene = audioEngineStatesDictionary[sceneName];

        At_VirtualSpeakerState vss = null;
        if (audioEngineStatesForScene == null)
        {
            audioEngineStatesForScene = new At_3DAudioEngineState();
        }

        if (audioEngineStatesForScene.virtualSpeakerState == null)
        {
            
            // this should be completly updated !!!!!!!!!
            vss = new At_VirtualSpeakerState();
            /*
            Scene scene = SceneManager.GetActiveScene();
            string json = ReadFromFile(scene.name + "_VirtualSpeakerState.txt");
            //string Firstline = readVirtualSpeakerState(json);            
            JsonUtility.FromJsonOverwrite(json, vss);
            */
            return vss;
            
        }
        else
        {
            return audioEngineStatesForScene.virtualSpeakerState;
        }
        
    }

    /***************************************************************************
     * 
     * CHANGE THE NAME OF A PLAYER IN A PLAYER STATE AND SAVE THE STATE
     * 
     **************************************************************************/
    public static void changePlayerName(string sceneName, string previousName, string newName)
    {
        At_3DAudioEngineState audioEngineStatesForScene = audioEngineStatesDictionary[sceneName];

        foreach (At_PlayerState state in audioEngineStatesForScene.playerStates)
        {
            if (state.name == previousName)
            {
                state.name = newName;
            }
        }
        SaveAllState(sceneName);
    }

    public static void changeRandomPlayerName(string sceneName, string previousName, string newName)
    {
        At_3DAudioEngineState audioEngineStatesForScene = audioEngineStatesDictionary[sceneName];
        foreach (At_DynamicRandomPlayerState state in audioEngineStatesForScene.randomPlayerStates)
        {
            if (state.name == previousName)
            {
                state.name = newName;
            }
        }
        SaveAllState(sceneName);
    }

    /***************************************************************************
     * 
     * REMOVE PLAYER WHEN COMPONENT IS REMOVED OR GAMEOBJECT IS DELETED
     * 
     **************************************************************************/
    public static void removePlayerWithGuid(string sceneName, string guid)
    {
        At_3DAudioEngineState audioEngineStatesForScene = audioEngineStatesDictionary[sceneName];

        for (int i = 0; i< audioEngineStatesForScene.playerStates.Count; i++ )
        {
            if (audioEngineStatesForScene.playerStates[i].guid == guid)
            {
                audioEngineStatesForScene.playerStates.RemoveAt(i);
                break;
            }
        }
        SaveAllState(sceneName);
    }
    public static void removeRandomPlayerWithGuid(string sceneName, string guid)
    {
        At_3DAudioEngineState audioEngineStatesForScene = audioEngineStatesDictionary[sceneName];

        for (int i = 0; i < audioEngineStatesForScene.randomPlayerStates.Count; i++)
        {
            if (audioEngineStatesForScene.randomPlayerStates[i].guid == guid)
            {
                audioEngineStatesForScene.randomPlayerStates.RemoveAt(i);
                break;
            }
        }
        SaveAllState(sceneName);
    }

    /***************************************************************************
     * 
     * LOAD ALL WHEN THE FIRST "READ" action is done
     * 
     **************************************************************************/

    static void LoadAll()
    {
        // get all json file in the "/states" folder :
        string statesPath = GetFilePathForStates("");

        string[] stateFilesPath = Directory.GetFiles(statesPath);
        
        foreach (string stateFilePath in stateFilesPath)
        {
            
            string stateFileName = stateFilePath.Replace(statesPath, "");

            String sceneName = stateFileName.Replace("_States.txt", "");
            
            audioEngineStates = new At_3DAudioEngineState();

            //Scene scene = SceneManager.GetActiveScene();
            //string json = ReadFromFile(scene.name + "_States.txt");
            string json = ReadFromFile(stateFileName);
            

            string[] lines = json.Split('\n');
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                // OutputState
                if (lineIndex == 0)
                {
                    
                    At_OutputState os = new At_OutputState();
                    JsonUtility.FromJsonOverwrite(lines[lineIndex], os);
                    audioEngineStates.outputState = os;
                }
                // At_Player or At_DynamicRandomPlayer
                else
                {   // this is an At_player
                    if (lines[lineIndex].Contains("\"type\":0"))
                    {
                        
                        At_PlayerState ps = new At_PlayerState();
                        JsonUtility.FromJsonOverwrite(lines[lineIndex], ps);
                        

                        audioEngineStates.playerStates.Add(ps);
                        
                    }
                    // this is an At_DynamicRandomPlayer
                    else if (lines[lineIndex].Contains("\"type\":1"))
                    {
                        At_DynamicRandomPlayerState drps = new At_DynamicRandomPlayerState();
                        JsonUtility.FromJsonOverwrite(lines[lineIndex], drps);
                        
                        audioEngineStates.randomPlayerStates.Add(drps);
                        
                    }

                }


            }
            audioEngineStatesDictionary.Add(sceneName, audioEngineStates);
        }
        
    }

   

    /***************************************************************************
     * 
     * SAVE OUTPUT AND PLAYER STATE IN A UNIQUE JSON FILE
     * 
     **************************************************************************/
    public static void SaveAllState(string sceneName)
    {
        At_3DAudioEngineState audioEngineStatesForScene = audioEngineStatesDictionary[sceneName];

        string jsonAllState = JsonUtility.ToJson(audioEngineStatesForScene.outputState);
        foreach (At_PlayerState state in audioEngineStatesForScene.playerStates)
        {
            string jsonPlayerState = JsonUtility.ToJson(state);
            jsonAllState = jsonAllState + "\n" + jsonPlayerState;
        }
        foreach (At_DynamicRandomPlayerState state in audioEngineStatesForScene.randomPlayerStates)
        {
            string jsonRandomPlayerState = JsonUtility.ToJson(state);
            jsonAllState = jsonAllState + "\n" + jsonRandomPlayerState;
        }
        //Scene scene = SceneManager.GetActiveScene();

        //WriteToFile(scene.name + "_States.txt", jsonAllState);
        WriteToFile(sceneName + "_States.txt", jsonAllState);
    }
    //modif mathias 07-01-2021
    static string readOutputState(string jsonAllStates)
    {        
        return (jsonAllStates.Split('\n'))[0];        
    }
    public static At_OutputState getOutputState(string sceneName)
    {
        At_3DAudioEngineState audioEngineStatesForScene = null;

        if (audioEngineStatesDictionary.ContainsKey(sceneName))
        {
            audioEngineStatesForScene = audioEngineStatesDictionary[sceneName];

            if (audioEngineStatesForScene == null)
            {
                audioEngineStatesForScene = new At_3DAudioEngineState();
            }
            if (audioEngineStatesForScene.outputState == null)
            {
                At_OutputState os = new At_OutputState();
                //Scene scene = SceneManager.GetActiveScene();
                string json = ReadFromFile(sceneName + "_States.txt");
                string Firstline = readOutputState(json);
                JsonUtility.FromJsonOverwrite(Firstline, os);
                if (os == null)
                {
                    audioEngineStatesForScene.outputState = new At_OutputState();
                }
                else
                {
                    audioEngineStatesForScene.outputState = os;
                }
            }
        }
        else
        {
            audioEngineStatesForScene = new At_3DAudioEngineState();
            audioEngineStatesDictionary.Add(sceneName, audioEngineStatesForScene);

            if (audioEngineStatesForScene.outputState == null)
            {
                At_OutputState os = new At_OutputState();
                string json = ReadFromFile(sceneName + "_States.txt");
                string Firstline = readOutputState(json);
                JsonUtility.FromJsonOverwrite(Firstline, os);
                if (os == null)
                {
                    audioEngineStatesForScene.outputState = new At_OutputState();
                }
                else
                {
                    audioEngineStatesForScene.outputState = os;
                }
            }
        }

        return audioEngineStatesForScene.outputState;
    }

    //modif mathias 07-01-2021
    static string readPlayerState(string jsonAllStates, string guid)
    {
        string foundLine = null;
        string[] lines = jsonAllStates.Split('\n');
        foreach (string line in lines)
        {
            if (line.IndexOf(guid) != -1)
            {
                foundLine = line;
                break;
            }
        }
        return foundLine;  
    }

    public static At_PlayerState createNewPlayerStateWithGuidAndName(string sceneName, string guid, string name)
    {
        At_3DAudioEngineState audioEngineStatesForScene = audioEngineStatesDictionary[sceneName];

        if (audioEngineStatesForScene == null)
        {
            audioEngineStatesForScene = new At_3DAudioEngineState();
        }

        At_PlayerState ps = new At_PlayerState();
        ps.name = name;
        ps.guid = guid;
        audioEngineStatesForScene.playerStates.Add(ps);
        return audioEngineStatesForScene.playerStates[audioEngineStatesForScene.playerStates.Count - 1];
    }

    public static At_PlayerState getPlayerStateWithGuidAndName(string sceneName, string guid, string name)
    {
        At_3DAudioEngineState audioEngineStatesForScene = audioEngineStatesDictionary[sceneName];

        if (audioEngineStatesForScene == null)
        {            
            return null;
        }
        else
        {
            return audioEngineStatesForScene.getPlayerState(guid);
        }

        //At_PlayerState playerStateInList = audioEngineStates.getPlayerState(guid);
       
        /*
        if (playerStateInList == null)
        {
            At_PlayerState ps = new At_PlayerState();
            Scene scene = SceneManager.GetActiveScene();
            string json = ReadFromFile(scene.name + "_States.txt");
            string line = readPlayerState(json, guid);
            JsonUtility.FromJsonOverwrite(line, ps);

            if (ps == null) ps = new At_PlayerState();
            ps.name = name;
            ps.guid = guid;
            audioEngineStates.playerStates.Add(ps);
            json = JsonUtility.ToJson(ps);
            //WriteToFile(scene.name + "_AllStates.txt", json);
            playerStateInList = audioEngineStates.playerStates[audioEngineStates.playerStates.Count - 1];
        }
        return playerStateInList;  
        */
    }

    //modif mathias 07-01-2021
    static string readRandomPlayerState(string jsonAllStates, string guid)
    {
        string foundLine = null;
        string[] lines = jsonAllStates.Split('\n');
        foreach (string line in lines)
        {
            if (line.IndexOf(guid) != -1)
            {
                foundLine = line;
                break;
            }
        }
        return foundLine;
    }

    public static At_DynamicRandomPlayerState createNewRandomPlayerStateWithGuidAndName(string sceneName, string guid, string name)
    {
        At_3DAudioEngineState audioEngineStatesForScene = audioEngineStatesDictionary[sceneName];

        if (audioEngineStatesForScene == null)
        {
            audioEngineStatesForScene = new At_3DAudioEngineState();
        }

        At_DynamicRandomPlayerState rps = new At_DynamicRandomPlayerState();
        rps.name = name;
        rps.guid = guid;
        audioEngineStatesForScene.randomPlayerStates.Add(rps);
        return audioEngineStatesForScene.randomPlayerStates[audioEngineStatesForScene.randomPlayerStates.Count - 1];
    }

    public static At_DynamicRandomPlayerState getRandomPlayerStateWithGuidAndName(string sceneName, string guid, string name)
    {
        At_3DAudioEngineState audioEngineStatesForScene = audioEngineStatesDictionary[sceneName];

        if (audioEngineStatesForScene == null)
        {
            //audioEngineStates = new At_3DAudioEngineState();
            return null;
        }
        else
        {
            return audioEngineStatesForScene.getRandomPlayerState(guid);
        }
        /*
        At_DynamicRandomPlayerState randomPlayerStateInList = audioEngineStates.getRandomPlayerState(guid);

        if (randomPlayerStateInList == null)
        {
            At_DynamicRandomPlayerState rps = new At_DynamicRandomPlayerState();
            Scene scene = SceneManager.GetActiveScene();
            string json = ReadFromFile(scene.name + "_States.txt");
            string line = readRandomPlayerState(json, guid);
            JsonUtility.FromJsonOverwrite(line, rps);

            if (rps == null) rps = new At_DynamicRandomPlayerState();
            rps.name = name;
            rps.guid = guid;
            audioEngineStates.randomPlayerStates.Add(rps);
            json = JsonUtility.ToJson(rps);
            //WriteToFile(scene.name + "_AllStates.txt", json);
            randomPlayerStateInList = audioEngineStates.randomPlayerStates[audioEngineStates.randomPlayerStates.Count - 1];
        }

        return randomPlayerStateInList;
        */
    }

    private static void WriteToFile(string fileName, string json)
    {

        string path = GetFilePathForStates(fileName);
        FileStream fileStream = new FileStream(path, FileMode.Create);

        using (StreamWriter writer = new StreamWriter(fileStream))
        {
            writer.Write(json);
            //AssetDatabase.Refresh();
        }
    }
    private static string ReadFromFile(string fileName)
    {
        

        string path = GetFilePathForStates(fileName);
        if (File.Exists(path))
        {
            using (StreamReader reader = new StreamReader(path))
            {
                string json = reader.ReadToEnd();
                return json;
            }
        }
        else
            Debug.LogError("File not found!");
            return "";       
    }

    private static string GetFilePathForStates(string fileName)
    {
        //string externAssetsPath = PlayerPrefs.GetString("externAssetsPath_state");
        if (externalAssetsStates == null)
        {
            loadExternAssetsState();
        }

        string externAssetsPath = "";
#if UNITY_EDITOR
        if (externalAssetsStates != null)
        {
            externAssetsPath = externalAssetsStates.externAssetsPath_state;
        }
#else
        
        if (externalAssetsStates != null)
        {
            externAssetsPath = externalAssetsStates.externAssetsPath_state_standalone;
        }
        else {
            Debug.LogError("externalAssetsStates is null");
        }
       
#endif


        //Debug.Log("At startup read json at :"+externAssetsPath);
        //return Application.persistentDataPath + "/" + fileName;

        return (externAssetsPath + "/"+fileName);//Application.dataPath+ "/At_3DAudioEngine/States/"+ fileName;
    }

    private static string GetFilePathForExternalAssets(string fileName)
    {
        //Debug.Log("Application Data Path = " + Application.dataPath);

        //return "Assets/StreamingAssets/" + fileName;
        return Application.streamingAssetsPath +"/"+ fileName;
        //return "Assets/Resources/" + fileName;
    }

}