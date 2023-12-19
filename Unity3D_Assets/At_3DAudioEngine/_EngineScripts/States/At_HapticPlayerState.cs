/*
 * Author : Antoine Gonot
 * Company : Art-Tone
 * First Creation : 14/11/2023
 * 
 * DESCRIPTION : class defining the Haptic State - permanent data saved in a json file
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class At_HapticPlayerState 
{
    // integer given the type of the player 
    public int type = 2;

    public string guid;

    public string name = "";

    public float[] listenerOutputSendGains;
    public string[] listenerOutputSendGuids;

    public string atPlayer_guid="";

    ///type of distance attenuation in the spatialize : 0 = none, 1 = linear, 2 = square
    public float attenuation = 2;
    /// Selected attenuation type in the popup menu of the At_HapticPlayer Component GUI
    public int selectedAttenuation = 0;
    /// minimum distance above which the sound produced by the source is attenuated
    public float minDistance = 1;

    public float lowPassFc = 20000.0f;
    public float highPassFc = 20.0f;
    public float makeupGain = 0.0f;

}
