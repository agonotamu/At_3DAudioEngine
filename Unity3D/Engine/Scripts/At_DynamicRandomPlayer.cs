/**
 * 
 * @file At_DynamicRandomPlayer.cs
 * @author Antoine Gonot
 * @version 1.0
 * @date 19/01/2021
 * 
 * @brief Multichannel audio player and spatializer that can be instantiated at runtime, for one shot multiple audio instance with randomization of a playlist
 * 
 * @details
 * Use NAudio API to play multichannel audio file (above 8 channels)
 * Use 3D Audio Engine API to spatialize an audio file with the WAVE FIELD SYNTHESIS technic (calls of function from "AudioPlugin_AtSpatializer.dll")
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class At_DynamicRandomPlayer : MonoBehaviour
{

    public bool is3D = false;
    public float gain;
    public string[] fileNames;
    public float attenuation;
    public float omniBalance;   
    List<GameObject> playerInstances;
        
    // Start is called before the first frame update
    void Start()
    {
        playerInstances = new List<GameObject>();
        
    }


    public void AddOneShotInstanceAndRandomPlay()
    {
        if (fileNames != null && fileNames.Length != 0 && fileNames[0] !="")
        {
            playerInstances.Add(new GameObject(gameObject.name+"(Clone"+ playerInstances.Count+")"));            
            playerInstances[playerInstances.Count - 1].transform.SetParent(transform);
            playerInstances[playerInstances.Count - 1].transform.localPosition = Vector3.zero;
            playerInstances[playerInstances.Count - 1].AddComponent<At_Player>();
            At_Player p = playerInstances[playerInstances.Count - 1].GetComponent<At_Player>();
            p.is3D = is3D;
            p.gain = gain;
            p.omniBalance = omniBalance;            
            p.attenuation = attenuation;            
            int r = Random.Range(0, fileNames.Length);
            p.fileName = fileNames[r];
            p.isDynamicInstance = true;
            p.initAudioFile();
            GameObject.FindObjectOfType<At_MasterOutput>().addPlayerToList(p);
            p.StartPlaying();
        }

        
    }



}
