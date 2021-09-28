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

public class At_AudioEngineUtils : MonoBehaviour
{
    // reference to the UNIQUE class containing the list of Player States
    static public At_3DAudioEngineState audioEngineStates;
    static public At_ExternAssetsState externalAssetsStates;

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
        string fileName = "externalAssetsState.txt";
        string json = JsonUtility.ToJson(externalAssetsStates);
        string path = GetFilePathForExternalAssets(fileName);
        FileStream fileStream = new FileStream(path, FileMode.Create);

        using (StreamWriter writer = new StreamWriter(fileStream))
        {
            writer.Write(json);            
        }
    }

    static void loadExternAssetsState()
    {
        /*
        string fileName = "externalAssetsState.txt";
        string path = GetFilePathForExternalAssets(fileName);
        string json="";
        if (File.Exists(path))
        {
            using (StreamReader reader = new StreamReader(path))
            {
                json = reader.ReadToEnd();
            }
        }
        else
            Debug.LogWarning("External Asset State File not found!");
        */
        Debug.Log("load external asset state !");

        TextAsset ta = Resources.Load<TextAsset>("externalAssetsState");
        string json = "";
        if (ta)
        {
            json = ta.text;
        }
        
        if (json == "")
        {
            Debug.LogWarning("External Asset State File not found!");
        }
        else
        {
            Debug.Log("externalAssetsState OK : "+ json);
            At_ExternAssetsState eas = new At_ExternAssetsState();
            JsonUtility.FromJsonOverwrite(json, eas);
            externalAssetsStates = eas;
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
        //Debug.Log("opening !");
        loadExternAssetsState();
        
        LoadAll();
    }

    public static void LoadAllStates()
    {
        loadExternAssetsState();
        LoadAll();
    }

    public static bool setSpeakerState(At_VirtualMic[] virtualMics, At_VirtualSpeaker[] virtualSpeakers)
    {
        bool hasChanged = false;

        if (audioEngineStates.virtualSpeakerState == null)
            audioEngineStates.virtualSpeakerState = new At_VirtualSpeakerState();
        if(audioEngineStates.virtualSpeakerState.virtualSpeakerId == null)
            audioEngineStates.virtualSpeakerState.virtualSpeakerId = new int[virtualMics.Length];
        if (audioEngineStates.virtualSpeakerState.virtualMicId == null)
            audioEngineStates.virtualSpeakerState.virtualMicId = new int[virtualMics.Length];
        if (audioEngineStates.virtualSpeakerState.virtualMicPositions == null)
            audioEngineStates.virtualSpeakerState.virtualMicPositions = new float [3*virtualMics.Length];
        if (audioEngineStates.virtualSpeakerState.virtualSpeakerPositions == null)
            audioEngineStates.virtualSpeakerState.virtualSpeakerPositions = new float[3* virtualSpeakers.Length];

        for (int i = 0; i< virtualMics.Length; i++)
        {

            if (audioEngineStates.virtualSpeakerState.virtualSpeakerPositions[3 * i] != virtualSpeakers[i].gameObject.transform.position.x)
            {
                audioEngineStates.virtualSpeakerState.virtualSpeakerPositions[3 * i] = virtualSpeakers[i].gameObject.transform.position.x;
                audioEngineStates.virtualSpeakerState.virtualMicPositions[3 * i] = virtualMics[i].gameObject.transform.position.x;
                hasChanged = true;
            }
            if (audioEngineStates.virtualSpeakerState.virtualSpeakerPositions[3 * i + 1] != virtualSpeakers[i].gameObject.transform.position.y)
            {
                audioEngineStates.virtualSpeakerState.virtualSpeakerPositions[3 * i + 1] = virtualSpeakers[i].gameObject.transform.position.y;
                audioEngineStates.virtualSpeakerState.virtualMicPositions[3 * i + 1] = virtualMics[i].gameObject.transform.position.y;
                hasChanged = true;
            }
            if (audioEngineStates.virtualSpeakerState.virtualSpeakerPositions[3 * i + 2] != virtualSpeakers[i].gameObject.transform.position.z)
            {
                audioEngineStates.virtualSpeakerState.virtualSpeakerPositions[3 * i + 2] = virtualSpeakers[i].gameObject.transform.position.z;
                audioEngineStates.virtualSpeakerState.virtualMicPositions[3 * i + 2] = virtualMics[i].gameObject.transform.position.z;
                hasChanged = true;
            }
            audioEngineStates.virtualSpeakerState.virtualSpeakerId[i] = virtualSpeakers[i].id;
            audioEngineStates.virtualSpeakerState.virtualMicId[i] = virtualMics[i].id;

        }

        return hasChanged;
    }
    
    public static void saveSpeakerState()
    {
        string json = JsonUtility.ToJson(audioEngineStates.virtualSpeakerState);
        Scene scene = SceneManager.GetActiveScene();
        WriteToFile(scene.name + "_VirtualSpeakerState.txt", json);
    }

    public static At_VirtualSpeakerState getVirtualSpeakerState()
    {
        At_VirtualSpeakerState vss = null;
        if (audioEngineStates == null)
        {
            audioEngineStates = new At_3DAudioEngineState();
        }

        if (audioEngineStates.virtualSpeakerState == null)
        {
            vss = new At_VirtualSpeakerState();
            Scene scene = SceneManager.GetActiveScene();
            string json = ReadFromFile(scene.name + "_VirtualSpeakerState.txt");
            //string Firstline = readVirtualSpeakerState(json);            
            JsonUtility.FromJsonOverwrite(json, vss);
            return vss;
            
        }
        else
        {
            return audioEngineStates.virtualSpeakerState;
        }
        
    }

    /***************************************************************************
     * 
     * CHANGE THE NAME OF A PLAYER IN A PLAYER STATE AND SAVE THE STATE
     * 
     **************************************************************************/
    public static void changePlayerName(string previousName, string newName)
    {
        foreach (At_PlayerState state in audioEngineStates.playerStates)
        {
            if (state.name == previousName)
            {
                state.name = newName;
            }
        }
        SaveAllState();
    }
    public static void changeRandomPlayerName(string previousName, string newName)
    {
        foreach (At_DynamicRandomPlayerState state in audioEngineStates.randomPlayerStates)
        {
            if (state.name == previousName)
            {
                state.name = newName;
            }
        }
        SaveAllState();
    }

    /***************************************************************************
     * 
     * REMOVE PLAYER WHEN COMPONENT IS REMOVED OR GAMEOBJECT IS DELETED
     * 
     **************************************************************************/
    public static void removePlayerWithGuid(string guid)
    {
        for (int i = 0; i< audioEngineStates.playerStates.Count; i++ )
        {
            if (audioEngineStates.playerStates[i].guid == guid)
            {
                audioEngineStates.playerStates.RemoveAt(i);
            }
        }
        SaveAllState();
    }
    public static void removeRandomPlayerWithGuid(string guid)
    {
        for (int i = 0; i < audioEngineStates.randomPlayerStates.Count; i++)
        {
            if (audioEngineStates.randomPlayerStates[i].guid == guid)
            {
                audioEngineStates.randomPlayerStates.RemoveAt(i);
            }
        }
        SaveAllState();
    }

    /***************************************************************************
     * 
     * LOAD ALL WHEN THE FIRST "READ" action is done
     * 
     **************************************************************************/

    static void LoadAll()
    {
        audioEngineStates = new At_3DAudioEngineState();
        
        Scene scene = SceneManager.GetActiveScene();
        string json = ReadFromFile(scene.name + "_States.txt");
        Debug.Log(json);
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
                    //Debug.Log("load At_Player with name : " + ps.name);
                }
                // this is an At_DynamicRandomPlayer
                else if (lines[lineIndex].Contains("\"type\":1"))
                {
                    At_DynamicRandomPlayerState drps = new At_DynamicRandomPlayerState();
                    JsonUtility.FromJsonOverwrite(lines[lineIndex], drps);
                    audioEngineStates.randomPlayerStates.Add(drps);
                    /*
                    foreach (string fileName in drps.fileNames)
                    {
                        //Debug.Log(fileName);
                    }
                    Debug.Log("load At_DynamicRandomPlayer with name : " + drps.name);
                    */
                }   

            }


        }
    }

   

    /***************************************************************************
     * 
     * SAVE OUTPUT AND PLAYER STATE IN A UNIQUE JSON FILE
     * 
     **************************************************************************/
    public static void SaveAllState()
    {
        string jsonAllState = JsonUtility.ToJson(audioEngineStates.outputState);
        foreach (At_PlayerState state in audioEngineStates.playerStates)
        {
            string jsonPlayerState = JsonUtility.ToJson(state);
            jsonAllState = jsonAllState + "\n" + jsonPlayerState;
        }
        foreach (At_DynamicRandomPlayerState state in audioEngineStates.randomPlayerStates)
        {
            string jsonRandomPlayerState = JsonUtility.ToJson(state);
            jsonAllState = jsonAllState + "\n" + jsonRandomPlayerState;
        }
        Scene scene = SceneManager.GetActiveScene();
        WriteToFile(scene.name + "_States.txt", jsonAllState);
    }
    //modif mathias 07-01-2021
    static string readOutputState(string jsonAllStates)
    {        
        return (jsonAllStates.Split('\n'))[0];        
    }
    public static At_OutputState getOutputState()
    {

        if (audioEngineStates == null)
        {
            audioEngineStates = new At_3DAudioEngineState();
        }
        if (audioEngineStates.outputState == null)
        {
            At_OutputState os = new At_OutputState();
            Scene scene = SceneManager.GetActiveScene();
            string json = ReadFromFile(scene.name + "_States.txt");
            string Firstline = readOutputState(json);
            JsonUtility.FromJsonOverwrite(Firstline, os);
            if (os == null)
            {
                audioEngineStates.outputState = new At_OutputState();
            }
            else
            {
                audioEngineStates.outputState = os;
            }
        }

        return audioEngineStates.outputState;
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

    public static At_PlayerState createNewPlayerStateWithGuidAndName(string guid, string name)
    {
        if (audioEngineStates == null)
        {
            audioEngineStates = new At_3DAudioEngineState();
        }

        At_PlayerState ps = new At_PlayerState();
        ps.name = name;
        ps.guid = guid;
        audioEngineStates.playerStates.Add(ps);
        return audioEngineStates.playerStates[audioEngineStates.playerStates.Count - 1];
    }

    public static At_PlayerState getPlayerStateWithGuidAndName(string guid, string name)
    {
        if (audioEngineStates == null)
        {            
            return null;
        }
        else
        {
            return audioEngineStates.getPlayerState(guid);
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

    public static At_DynamicRandomPlayerState createNewRandomPlayerStateWithGuidAndName(string guid, string name)
    {
        if (audioEngineStates == null)
        {
            audioEngineStates = new At_3DAudioEngineState();
        }

        At_DynamicRandomPlayerState rps = new At_DynamicRandomPlayerState();
        rps.name = name;
        rps.guid = guid;
        audioEngineStates.randomPlayerStates.Add(rps);
        return audioEngineStates.randomPlayerStates[audioEngineStates.randomPlayerStates.Count - 1];
    }

    public static At_DynamicRandomPlayerState getRandomPlayerStateWithGuidAndName(string guid, string name)
    {
        if (audioEngineStates == null)
        {
            //audioEngineStates = new At_3DAudioEngineState();
            return null;
        }
        else
        {
            return audioEngineStates.getRandomPlayerState(guid);
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
            Debug.LogWarning("File not found!");
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
            Debug.Log("in editor !");
        }
        
#else
        Debug.Log("in standalone !");
        externAssetsPath = externalAssetsStates.externAssetsPath_state_standalone;
        Debug.Log("state path :"+externAssetsPath);
#endif


        //Debug.Log("At startup read json at :"+externAssetsPath);
        //return Application.persistentDataPath + "/" + fileName;
        return (externAssetsPath + "/"+fileName);//Application.dataPath+ "/At_3DAudioEngine/States/"+ fileName;
    }

    private static string GetFilePathForExternalAssets(string fileName)
    {
        //Debug.Log("Application Data Path = " + Application.dataPath);
        
        return "Assets/Resources/" + fileName;
    }

}