/*
 * Author : Antoine Gonot
 * Company : Art-Tone
 * First Creation : 19/01/2021
 * 
 * DESCRIPTION : class defining the Player State  
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//[System.Serializable]
public class At_DynamicRandomPlayerState
{
    // integer given the type of the player 
    public int type = 1;

    public string guid;

    // Name of the GameObject the At_PLayer is attached to
    public string name = "";
    // name of the audio file to play (Supposed to be in "Assets\Streaming Asset")
    public string[] fileNames = { "" };
    public float gain = 0;
    // boolean telling if the player is 2D (no spatialization applyied) or 3D (spatialization applied)
    public bool is3D = false;
    public bool isDirective = false; //modif mathias 06-17-2021
    public float omniBalance = 0f;
    public float attenuation = 0f;
    public int selectedAttenuation = 0;
    
    public float minDistance = 1;

    public int[] channelRouting;

    public int maxChannelInAudiofile;

    public int numChannelInAudiofile;

    public float spawnMinAngle = 0;
    public float spawnMaxAngle = 0;

    public float spawnMinRateMs = 500;
    public float spawnMaxRateMs = 1000;

    public int maxInstances = 10;

    public float spawnDistance = 1;



}