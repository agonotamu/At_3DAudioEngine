using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;
using System;
using UnityEngine;


public class At_HapticListenerOutput : MonoBehaviour
{

    public int hapticMixerId = - 1;

    public string guid = "";

    /******************************************************************
    * DATA GIVEN THE STATE OF THE PLAYER
    * ****************************************************************/
    //-----------------------------------------------------------------
    // "persistant" data, saved in the json file - copy/saved of the At_HapticListenerOutputState class
    // ----------------------------------------------------------------
    public float gain;
    public int outputChannelCount;
    
    public float[] meters;
    public int bufferLenght;
    public bool running;

    string objectName;

    void Reset()
    {
        setGuid();
    }

    void OnValidate()
    {
        
        Event e = Event.current;

        if (e != null)
        {
            if (e.type == EventType.ExecuteCommand && e.commandName == "Duplicate")
            {
                setGuid();
            }
            // if the object has been draged... Then it should be prefab.
            if (e.type == EventType.DragPerform)
            {
                setGuid();
            }

        }
    }

   
    public void setGuid()
    {
        guid = System.Guid.NewGuid().ToString();
    }

    /*************************************************************/
    // API call delegation method from the At_MasterOutput
    /*************************************************************/
    
    public void initializeOutput(int sampleRate, int bufferLength)
    {        
        meters = new float[outputChannelCount];
        HAPTIC_ENGINE_CREATE_MIXER(ref hapticMixerId, sampleRate, bufferLength, outputChannelCount);
        At_HapticPlayer[] hapticPlayers = FindObjectsOfType<At_HapticPlayer>();
        foreach (At_HapticPlayer hp in hapticPlayers)
            hp.addSourceToHapticListener(hapticMixerId, guid);
        
    }

    public float getMixingBufferSampleForChannelAndZero(int sampleIndex, int channelIndex)
    {
        
        float volume = Mathf.Pow(10.0f, gain / 20.0f);
        float sample = volume * HAPTIC_ENGINE_GET_MIX_SAMPLE(hapticMixerId, sampleIndex, channelIndex);        
        meters[channelIndex] += Mathf.Pow(sample, 2f);
        return sample;        

    }

    public void initializeMeterValues()
    {
        for (int c = 0; c < meters.Length; c++)
            meters[c] = 0;
    }
    public void normalizeMeterValues(int bufferLength)
    {
        for (int c = 0; c < meters.Length; c++)
            meters[c] = Mathf.Sqrt(meters[c] /bufferLength);
    }

    /*************************************************************/

    // Start is called before the first frame update
    void Start()
    {
        objectName = gameObject.name;
    }

    // Update is called once per frame
    void Update()
    {
       
        float[] position = new float[3];
        position[0] = transform.position.x;
        position[1] = transform.position.y;
        position[2] = transform.position.z;        
        HAPTIC_ENGINE_SET_LISTENER_POSITION(hapticMixerId, position);

        
    }

    [DllImport("AudioPlugin_AtHaptic")]
    private static extern void HAPTIC_ENGINE_CREATE_MIXER(ref int mixerId, int sampleRate, int bufferLength, int outChannelCount);
    [DllImport("AudioPlugin_AtHaptic")]
    private static extern float HAPTIC_ENGINE_GET_MIX_SAMPLE(int mixerId, int indexSample, int indexChannel);
    [DllImport("AudioPlugin_AtHaptic")]
    private static extern void HAPTIC_ENGINE_SET_LISTENER_POSITION(int mixerId, float[] position);

}
