/*
 * Author : Antoine Gonot
 * Company : Art-Tone
 * First Creation : 19/01/2021
 * 
 * DESCRIPTION : class defining the Player State - permanent data saved in a json file
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class At_PlayerState
{
    // integer given the type of the player 
    public int type = 0;

    public string guid;

    /// Name of the GameObject the At_PLayer is attached to
    public string name = "";
    /// name of the audio file to play (Supposed to be in "Assets\Streaming Asset")
    public string fileName = "";
    /// gain applied to the audiofile
    public float gain = 0;
    /// boolean telling if the player is 2D (no spatialization applyied) or 3D (spatialization applied)
    public bool is3D = false;
    /// boolean telling if the player 3D is ...
    public bool isDirective = false; //modif mathias 06-17-2021
    /// boolean telling if the player start to play On Awake
    public bool isPlayingOnAwake = false;
    /// boolean telling if the player is looping the read audio file
    public bool isLooping = false;
    /// directivity balance of the virtual microphone used for this source : balance [0,1] between omnidirectionnal and cardiod
    public float omniBalance = 0f; // Modif Rougerie 16/06/2022
    /// balance between "normal delay" and "reverse delay" for focalised source - see Time Reversal technic used for WFS
    public float timeReversal = 0;
    ///type of distance attenuation in the spatialize : 0 = none, 1 = linear, 2 = square
    public float attenuation = 2;
    /// Selected attenuation type in the popup menu of the AT_Player Component GUI
    public int selectedAttenuation = 0;
    /// minimum distance above which the sound produced by the source is attenuated
    public float minDistance = 1;

    public int[] channelRouting;

    public int numChannelInAudiofile;

    public bool isUnityAudioSource = false;

    public bool isLookAtListener = false;

    

}