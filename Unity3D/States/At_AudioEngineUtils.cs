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

    static bool readable = true;


    /***************************************************************************
     * 
     * SAVE AND LOAD VIRTUAL SPEAKERS AND VIRTUAL MICROPHONE CONFIGURATION
     * 
     **************************************************************************/

    static At_AudioEngineUtils()
    {
        Debug.Log("opening !");
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
     * LOAD ALL WHEN THE FIRST "READ" action is done
     * 
     **************************************************************************/

    static void LoadAll()
    {
        audioEngineStates = new At_3DAudioEngineState();
        
        Scene scene = SceneManager.GetActiveScene();
        string json = ReadFromFile(scene.name + "_States.txt");
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
    static string readPlayerState(string jsonAllStates, string name)
    {
        string foundLine = null;
        string[] lines = jsonAllStates.Split('\n');
        foreach (string line in lines)
        {
            if (line.IndexOf(name) != -1)
            {
                foundLine = line;
                break;
            }
        }
        return foundLine;  
    }
    public static At_PlayerState getPlayerStateWithName(string name)
    {
        if (audioEngineStates == null)
        {
            audioEngineStates = new At_3DAudioEngineState();
        }

        At_PlayerState playerStateInList = audioEngineStates.getPlayerState(name);

        if (playerStateInList == null)
        {
            At_PlayerState ps = new At_PlayerState();
            Scene scene = SceneManager.GetActiveScene();
            string json = ReadFromFile(scene.name + "_States.txt");
            string line = readPlayerState(json, name);
            JsonUtility.FromJsonOverwrite(line, ps);

            if (ps == null) ps = new At_PlayerState();
            ps.name = name;
            audioEngineStates.playerStates.Add(ps);
            json = JsonUtility.ToJson(ps);
            //WriteToFile(scene.name + "_AllStates.txt", json);
            playerStateInList = audioEngineStates.playerStates[audioEngineStates.playerStates.Count - 1];
        }
        return playerStateInList;     
    }

    //modif mathias 07-01-2021
    static string readRandomPlayerState(string jsonAllStates, string name)
    {
        string foundLine = null;
        string[] lines = jsonAllStates.Split('\n');
        foreach (string line in lines)
        {
            if (line.IndexOf(name) != -1)
            {
                foundLine = line;
                break;
            }
        }
        return foundLine;
    }
    public static At_DynamicRandomPlayerState getRandomPlayerStateWithName(string name)
    {
        if (audioEngineStates == null)
        {
            audioEngineStates = new At_3DAudioEngineState();
        }

        At_DynamicRandomPlayerState randomPlayerStateInList = audioEngineStates.getRandomPlayerState(name);

        if (randomPlayerStateInList == null)
        {
            At_DynamicRandomPlayerState rps = new At_DynamicRandomPlayerState();
            Scene scene = SceneManager.GetActiveScene();
            string json = ReadFromFile(scene.name + "_States.txt");
            string line = readRandomPlayerState(json, name);
            JsonUtility.FromJsonOverwrite(line, rps);

            if (rps == null) rps = new At_DynamicRandomPlayerState();
            rps.name = name;
            audioEngineStates.randomPlayerStates.Add(rps);
            json = JsonUtility.ToJson(rps);
            //WriteToFile(scene.name + "_AllStates.txt", json);
            randomPlayerStateInList = audioEngineStates.randomPlayerStates[audioEngineStates.randomPlayerStates.Count - 1];
        }

        return randomPlayerStateInList;
    }
    private static void WriteToFile(string fileName, string json)
    {

        string path = GetFilePath(fileName);
        FileStream fileStream = new FileStream(path, FileMode.Create);

        using (StreamWriter writer = new StreamWriter(fileStream))
        {
            writer.Write(json);
            AssetDatabase.Refresh();
        }
    }
    private static string ReadFromFile(string fileName)
    {

        string path = GetFilePath(fileName);
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

    private static string GetFilePath(string fileName)
    {
        string externAssetsPath = PlayerPrefs.GetString("externAssetsPath_state");
        //return Application.persistentDataPath + "/" + fileName;
        return (externAssetsPath + "/"+fileName);//Application.dataPath+ "/At_3DAudioEngine/States/"+ fileName;
    }

}