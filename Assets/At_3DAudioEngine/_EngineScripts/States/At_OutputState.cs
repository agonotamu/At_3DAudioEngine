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
    public string audioDeviceName = "select";
    /// number of channel used for the output bus
    public int outputChannelCount = 0;
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
    /// size of the virtual mic rig in the the scne (1 unit = 1 meter)
    public float virtualMicRigSize = 3;
    /// maximum distance between a player and a virtual mic to be rendered (value used to set the size of the ring buffer => max delay)
    public float maxDistanceForDelay = 10.0f;

    // Modif Gonot - 14/03/2023 - Adding Bass Managment
    public int subwooferOutputChannelCount = 1;
    public bool isBassManaged = false;
    public float crossoverFilterFrequency = 100;
    public int[] indexInputSubwoofer = new int[2];
    public float subwooferGain;

    // Modif Gonot - 21/11/2023 - Adding Haptic Feedback Managment
    public int[] hapticListenerOutputChannelsCount;
    public string[] hapticListenerOutputGuid;
    public int[] hapticListenerChannelsSelectionIndex;
    public int[] hapticListenerChannelsIndex;


    static public bool Compare(At_OutputState s1, At_OutputState s2)
    {        
        if (s1.audioDeviceName == s2.audioDeviceName && s1.outputChannelCount == s2.outputChannelCount)
        {
            return true;
        }
        return false;
    }
 

}
