/**
 * 
 * @file At_Haptic.cs
 * @author Antoine Gonot
 * @version 1.0
 * @date 14/11/2023
 * 
 * @brief Haptic feedback for and At_PLayer Object
 * 
 * @details
 * Use 3D Audio Engine API to generate a multichannel audio signal from an At_Player object to provide haptic feedback
 * 
 */



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioAsioPatchBay;
using System;
using System.Runtime.InteropServices;



public class At_HapticPlayer : MonoBehaviour
{


    // Debug
    static int playingCount = 0;
    /******************************************************************
   * DATA GIVEN THE STATE OF THE PLAYER
   * ****************************************************************/
    //-----------------------------------------------------------------
    // "persistant" data, saved in the json file - copy/saved of the At_HapticPlayerState class
    // ----------------------------------------------------------------
    public float[] listenerOutputSendGains = null;
    public string[] listenerOutputSendGuids = null;
    public string guid = "";
    public string atPlayer_guid="";

    ///type of distance attenuation in the spatialize : 0 = none, 1 = linera, 2 = square
    public float attenuation;
    /// minimum distance above which the sound produced by the source is attenuated
    public float minDistance;

    //-----------------------------------------------------------------
    // data used at runtime
    // ----------------------------------------------------------------


    /// instance of At_PLayer class used to feed the At_HapticPlayer class to generate haptic signal 
    public At_Player atPlayerInput;

    /// boolean telling if the player is actually playing
    public bool isPlaying = false;
    /// boolean telling if the player is asked to play (not necesseraly true
    public bool isAskedToPlay = false;

    bool mustBeDestroyedNow = false;
    bool mustBeDestroyedSafely = false;
    bool mustBeDestroyedOnNextFrame = false;
    public At_MasterOutput masterOutput;

    //public int outputChannelCount = 0;

    
    int hapticPlayerId;
    List<int> haptic_MixerIdList = new List<int>();
    Dictionary<int, string> haptic_MixerIdToListenerGuid_Dict = new Dictionary<int, string>();


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


    //-----------------------------------------------------     
    // Awake, Disable, Get/Start/Stop playing 
    //-----------------------------------------------------
    //public bool GetIsPlaying() { return isPlaying; }
    public void StartPlaying()
    {
        isAskedToPlay = true;
        playingCount++;
    }
    public void StopPlaying()
    {
        isAskedToPlay = false;
        isPlaying = false;
    }

    public void SafeDestroy()
    {
        mustBeDestroyedSafely = true;
    }
    public void OnDisable()
    {

        StopPlaying();
    }


    public void addSourceToHapticListener(int hapticMixerId, string guid)
    {
        
        int id = 0;
        
        HAPTIC_ENGINE_ADD_SOURCE_TO_MIXER(hapticMixerId, ref id);

        hapticPlayerId = id;
        haptic_MixerIdList.Add(hapticMixerId);
        haptic_MixerIdToListenerGuid_Dict.Add(hapticMixerId, guid);
        
        // 

    }

    public void conformInputBufferToOutputBusFormat(int bufferSize)
    {
        foreach (int hapticMixerId in haptic_MixerIdList)
        {
            float sendVolume = 1;
            string listenerOutputGuid = haptic_MixerIdToListenerGuid_Dict[hapticMixerId];
            for (int i = 0; i < listenerOutputSendGuids.Length; i++)
            {
                if (listenerOutputSendGuids[i] == listenerOutputGuid)
                {
                    sendVolume = Mathf.Pow(10.0f, listenerOutputSendGains[i] / 20.0f);
                    break;
                }
            }


            // BUG - TODO  : the source can know the number of channels of the mixer it is plugged in !!!!
            HAPTIC_ENGINE_PROCESS(hapticMixerId, hapticPlayerId, atPlayerInput.inputFileBuffer, bufferSize, atPlayerInput.inputFileBufferReadOffset, atPlayerInput.numChannelsInAudioFile, sendVolume);
        }

    }


    // Start is called before the first frame update
    void Start()
    {
        atPlayerInput = At_AudioEngineUtils.getAtPlayerWithGuid(atPlayer_guid);
    }

    public void Awake()
    {
        masterOutput = GameObject.FindObjectOfType<At_MasterOutput>();

    }

    public void initMeters()
    {
        
    }


    // Update is called once per frame
    void Update()
    {

        foreach (int hapticMixerId in haptic_MixerIdList)
        {
            float[] position = new float[3];
            position[0] = transform.position.x;
            position[1] = transform.position.y;
            position[2] = transform.position.z;
            HAPTIC_ENGINE_SET_SOURCE_POSITION(hapticMixerId, hapticPlayerId, position);
            HAPTIC_ENGINE_SET_SOURCE_ATTENUATION(hapticMixerId, hapticPlayerId, attenuation);
            HAPTIC_ENGINE_SET_SOURCE_MIN_DISTANCE(hapticMixerId, hapticPlayerId, minDistance);
        }

        if (mustBeDestroyedNow)
        {
            //masterOutput.destroyPlayerNow(this);
            // > do the same
            mustBeDestroyedNow = false;
        }
        if (mustBeDestroyedSafely)
        {
            //masterOutput.destroyPlayerSafely(this
            // > do the same
            mustBeDestroyedSafely = false;
        }
        if (mustBeDestroyedOnNextFrame)
        {
            DestroyNow();
            //masterOutput.destroyPlayerNow(this);
            // > do the same
            mustBeDestroyedOnNextFrame = false;
        }
    }

    public void DestroyOnNextFrame()
    {
        mustBeDestroyedOnNextFrame = true;
    }

    public void DestroyNow()
    {
        
        Destroy(gameObject);

    }



#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // DRAW A CIRLE GIVEN SHOWING THE "min_distance" parameter of the At_Player
        const float numStepDrawCircle = 20;
        float angle = 2 * Mathf.PI / numStepDrawCircle;

        for (int i = 0; i < numStepDrawCircle; i++)
        {
            Vector3 center = transform.position + new Vector3(minDistance * Mathf.Cos(i * angle), 0, minDistance * Mathf.Sin(i * angle));
            Vector3 nextCenter = transform.position + new Vector3(minDistance * Mathf.Cos((i + 1) * angle), 0, minDistance * Mathf.Sin((i + 1) * angle)); ;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(center, nextCenter);
        }
        // Stuck inRepaintAll() - remove it
        SceneView.RepaintAll();
    }    
#endif

    [DllImport("AudioPlugin_AtHaptic")]
    private static extern void HAPTIC_ENGINE_ADD_SOURCE_TO_MIXER(int mixerId, ref int sourceId);
    [DllImport("AudioPlugin_AtHaptic")]
    private static extern void HAPTIC_ENGINE_PROCESS(int mixerId, int sourceId, float[] inBuffer, int bufferLength, int offset, int inChannelCount, float sendVolume);
    [DllImport("AudioPlugin_AtHaptic")]
    private static extern void HAPTIC_ENGINE_SET_SOURCE_POSITION(int mixerId, int sourceId, float[] position);
    [DllImport("AudioPlugin_AtHaptic")]
    private static extern void HAPTIC_ENGINE_SET_SOURCE_ATTENUATION(int mixerId, int sourceId, float attenuation);
    [DllImport("AudioPlugin_AtHaptic")]
    private static extern void HAPTIC_ENGINE_SET_SOURCE_MIN_DISTANCE(int mixerId, int sourceId, float minDistance);



}
