﻿/*
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
    // Name of the GameObject the At_PLayer is attached to
    public string name = "";
    // name of the audio file to play (Supposed to be in "Assets\Streaming Asset")
    public string[] fileNames = { "" };
    public float gain = 0;
    // boolean telling if the player is 2D (no spatialization applyied) or 3D (spatialization applied)
    public bool is3D = false;
    public float omniBalance = 0f;
    public float attenuation = 2f;
    public int selectedAttenuation = 2;

}