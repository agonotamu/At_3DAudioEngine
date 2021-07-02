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
    /*
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
    */
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
            Scene scene = SceneManager.GetActiveScene();
            get3DAudioEngineState(scene);
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

    //modif mathias 07-02-2021
    static void get3DAudioEngineState(Scene scene)
    {
        string jsonAllStates = ReadFromFile(scene.name + "_States.txt");

        // On récupère toutes les lignes 
        string[] lines = jsonAllStates.Split('\n');

        //At_3DAudioEngineState s = null;

        // On boucle sur toutes les lignes
        for (int i = 0; i < lines.Length; i++)
        {
            // si c'est la première c'est le master output, alors on l'init
            if (i == 0)
            {
                getOutputState();
            }
            else
            {
                // On trouve le numéro assocé au champs "type"
                int index = lines[i].IndexOf(":");
                int type = int.Parse(lines[i].Substring(index + 1, 1));

                if (type == 0)
                {
                    At_PlayerState ps = new At_PlayerState();
                    JsonUtility.FromJsonOverwrite(lines[i], ps);
                    audioEngineStates.playerStates.Add(ps);
                }
                else if (type == 1)
                {
                    At_DynamicRandomPlayerState rps = new At_DynamicRandomPlayerState();
                    JsonUtility.FromJsonOverwrite(lines[i], rps);
                    audioEngineStates.randomPlayerStates.Add(rps);
                }
            }
        }

    }

    public static At_PlayerState getPlayerStateWithName(string name)
    {
        if (audioEngineStates == null)
        {
            audioEngineStates = new At_3DAudioEngineState();
            Scene scene = SceneManager.GetActiveScene();
            get3DAudioEngineState(scene);
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
            Scene scene = SceneManager.GetActiveScene();
            get3DAudioEngineState(scene);
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
        }
        AssetDatabase.Refresh();
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