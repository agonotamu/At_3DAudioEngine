/*
 * Author : Antoine Gonot
 * Company : Art-Tone
 * First Creation : 21/11/2023
 * 
 * DESCRIPTION : 
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class At_HapticListenerOutputState
{
    // integer given the type of the object 
    public int type = 3;

    public string guid;
    /// Name of the GameObject the At_PLayer is attached to
    public string name = "";
    /// number of channel used for the output bus
    public int outputChannelCount = 0;
    public int selectSpeakerConfig = 0;
    /// master gain for the output bus
    public float gain;

}
