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

public class At_AudioEngineUtils : MonoBehaviour
{
    // reference to the UNIQUE class containing the list of Player States
    static public At_3DAudioEngineState audioEngineStates;
    

    static public Dictionary<String, At_3DAudioEngineState> audioEngineStatesDictionary = new Dictionary<String, At_3DAudioEngineState>();
    
    static public AsioOut asioOut;

    /***************************************************************************
     * 
     * LOAD ALL ON CONSTRUCTION
     * 
     **************************************************************************/

    static At_AudioEngineUtils()
    {
        LoadAll();
    }

   

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

        At_Player[] players = Resources.FindObjectsOfTypeAll(typeof(At_Player)) as At_Player[];

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

    }

    /***************************************************************************
    * 
    * SAVE AND LOAD VIRTUAL SPEAKERS AND VIRTUAL MICROPHONE CONFIGURATION
    * 
    **************************************************************************/
    public static bool setSpeakerState(string sceneName, At_VirtualMic[] virtualMics, At_VirtualSpeaker[] virtualSpeakers)
    {
        bool hasChanged = false;

        At_3DAudioEngineState audioEngineStatesForScene = audioEngineStatesDictionary[sceneName];
        audioEngineStatesForScene.virtualSpeakerState = new At_VirtualSpeakerState();
        audioEngineStatesForScene.virtualSpeakerState.virtualSpeakerId = new int[virtualMics.Length];
        audioEngineStatesForScene.virtualSpeakerState.virtualMicId = new int[virtualMics.Length];
        audioEngineStatesForScene.virtualSpeakerState.virtualMicPositions = new float[3 * virtualMics.Length];
        audioEngineStatesForScene.virtualSpeakerState.virtualSpeakerPositions = new float[3 * virtualSpeakers.Length];

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
        WriteToFile(sceneName + "_VirtualSpeakerState.txt", json);
    }

    // UNUSED !!! -----------------------------------
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
            
            if (stateFileName.Contains("_States") && Path.GetExtension(stateFileName) != ".meta") {
                String sceneName = stateFileName.Replace("_States.txt", "");

               
                audioEngineStates = new At_3DAudioEngineState();

                if (!audioEngineStatesDictionary.ContainsKey(sceneName))
                {
                    audioEngineStatesDictionary.Add(sceneName, audioEngineStates);
                }

                string json = ReadFromFile(stateFileName);

                string[] lines = json.Split('\n');
                for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    // OutputState
                    if (lineIndex == 0)
                    {
                        At_OutputState os = new At_OutputState();
                        JsonUtility.FromJsonOverwrite(lines[lineIndex], os);
                        audioEngineStatesDictionary[sceneName].outputState = os;
                    }
                    // At_Player or At_DynamicRandomPlayer
                    else
                    {   // this is an At_player
                        if (lines[lineIndex].Contains("\"type\":0"))
                        {
                            At_PlayerState ps = new At_PlayerState();
                            JsonUtility.FromJsonOverwrite(lines[lineIndex], ps);
                            audioEngineStatesDictionary[sceneName].playerStates.Add(ps);
                           
                        }
                        // this is an At_DynamicRandomPlayer
                        else if (lines[lineIndex].Contains("\"type\":1"))
                        {
                            At_DynamicRandomPlayerState drps = new At_DynamicRandomPlayerState();
                            JsonUtility.FromJsonOverwrite(lines[lineIndex], drps);
                            audioEngineStatesDictionary[sceneName].randomPlayerStates.Add(drps);

                        }

                    }
                } 
            }
            else if (stateFileName.Contains("_VirtualSpeakerState") && Path.GetExtension(stateFileName) != ".meta") {

                String sceneName = stateFileName.Replace("_VirtualSpeakerState.txt", "");
               
                if (audioEngineStates == null)
                {
                    audioEngineStates = new At_3DAudioEngineState();
                }

                if (!audioEngineStatesDictionary.ContainsKey(sceneName))
                {
                    audioEngineStatesDictionary.Add(sceneName, audioEngineStates);
                }

                string json = ReadFromFile(stateFileName);
                At_VirtualSpeakerState vss = new At_VirtualSpeakerState();
                JsonUtility.FromJsonOverwrite(json, vss);
                audioEngineStatesDictionary[sceneName].virtualSpeakerState = vss;
                
            }


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
        WriteToFile(sceneName + "_States.txt", jsonAllState);
    }

    // ------------------------------------------------------
    // get outputState
    // ------------------------------------------------------
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

    // ------------------------------------------------------
    // create and add a PlayerState in the audioEngineStatesForScene class when a new At_Player is added in the scene
    // ------------------------------------------------------
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

    // ------------------------------------------------------
    // get the state of an instance of At_Player in the scene
    // ------------------------------------------------------
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

    }

    // ------------------------------------------------------
    // create and add a DynamicRandomPlayerState in the audioEngineStatesForScene class when a new At_DynamicRandomPlayer is added in the scene
    // ------------------------------------------------------
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
    // ------------------------------------------------------
    // get the state of an instance of At_DynamicRandomPlayer in the scene
    // ------------------------------------------------------
    public static At_DynamicRandomPlayerState getRandomPlayerStateWithGuidAndName(string sceneName, string guid, string name)
    {
        At_3DAudioEngineState audioEngineStatesForScene = audioEngineStatesDictionary[sceneName];

        if (audioEngineStatesForScene == null)
        {            
            return null;
        }
        else
        {
            return audioEngineStatesForScene.getRandomPlayerState(guid);
        }        
    }

    /**************************************************************************
     * 
     *              UTILITY FOR READ/WRITE a string in a json file
     * 
    **************************************************************************/
    private static void WriteToFile(string fileName, string json)
    {

        string path = GetFilePathForStates(fileName);
        FileStream fileStream = new FileStream(path, FileMode.Create);

        using (StreamWriter writer = new StreamWriter(fileStream))
        {
            writer.Write(json);            
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

    /**************************************************************************
    * 
    *              set the path for state files
    * 
   **************************************************************************/
    public static string GetFilePathForStates(string fileName)
    {
        
        return Application.streamingAssetsPath + "/" + fileName;
    }


}