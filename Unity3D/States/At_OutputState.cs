/*
 * Author : Antoine Gonot
 * Company : Art-Tone
 * First Creation : 19/01/2021
 * 
 * DESCRIPTION : class defining the output State - permanent data saved in a json file
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class At_OutputState
{
    /// string given the name of the audio device
    public string audioDeviceName = "";
    /// number of channel used for the output bus
    public int outputChannelCount = 2;
    /// master gain for the output bus
    public float gain;
    /// selected speaker config
    public int selectSpeakerConfig = 0;
    /// index of the selected speaker configuration in the popup menu of the At_MasterOutput Component GUI
    public int outputConfigDimension = 0;
    /// index of the selected audio sampling rate in the popup menu of the At_MasterOutput Component GUI
    public int selectedSamplingRate = 0;
    /// audio sampling rate 
    public int samplingRate = 44100;
    /// boolean telling if the ASIO output should run when starting the application
    public bool isStartingEngineOnAwake = true;

    static public bool Compare(At_OutputState s1, At_OutputState s2)
    {        
        if (s1.audioDeviceName == s2.audioDeviceName && s1.outputChannelCount == s2.outputChannelCount)
        {
            return true;
        }
        return false;
    }
 

}
