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

    // Save the list of Player States contained in the At_3DAudioEngineState object    
    public static void SaveOutputState()
    {
        Scene scene = SceneManager.GetActiveScene();
        string json = JsonUtility.ToJson(audioEngineStates.outputState);
        WriteToFile(scene.name+"_OutputState.txt", json);
    }

    public static void SavePlayerStateWithName(string name)
    { 
        foreach (At_PlayerState state in audioEngineStates.playerStates)
        {
            if (name == state.name)
            {
                Scene scene = SceneManager.GetActiveScene();
                string json = JsonUtility.ToJson(state);
                WriteToFile(scene.name + "_" + state.name + "_PlayerState.txt", json);
            }
        }
    }

    public static void SaveRandomPlayerStateWithName(string name)
    {
        foreach (At_DynamicRandomPlayerState state in audioEngineStates.randomPlayerStates)
        {
            if (name == state.name)
            {
                Scene scene = SceneManager.GetActiveScene();
                string json = JsonUtility.ToJson(state);
                WriteToFile(scene.name + "_" + state.name + "_RandomPlayerState.txt", json);

            }
        }
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
            string json = ReadFromFile(scene.name + "_OutputState.txt");
            JsonUtility.FromJsonOverwrite(json, os);
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
            string json = ReadFromFile(scene.name + "_" + name + "_PlayerState.txt");
            JsonUtility.FromJsonOverwrite(json, ps);
            if (ps == null) ps = new At_PlayerState();
            ps.name = name;
            audioEngineStates.playerStates.Add(ps);
            json = JsonUtility.ToJson(ps);
            WriteToFile(scene.name + "_" + ps.name + "_PlayerState.txt", json);
            playerStateInList = audioEngineStates.playerStates[audioEngineStates.playerStates.Count - 1];
        }


        return playerStateInList;     
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
            string json = ReadFromFile(scene.name + "_" + name + "_RandomPlayerState.txt");
            JsonUtility.FromJsonOverwrite(json, rps);
            if (rps == null) rps = new At_DynamicRandomPlayerState();
            rps.name = name;
            audioEngineStates.randomPlayerStates.Add(rps);
            json = JsonUtility.ToJson(rps);
            WriteToFile(scene.name + "_" + rps.name + "_RandomPlayerState.txt", json);
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
        //return Application.persistentDataPath + "/" + fileName;
        return Application.dataPath+ "/At_3DAudioEngine/States/"+ fileName;
    }

}