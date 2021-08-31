/**
 * 
 * @file At_Player.cs
 * @author Antoine Gonot
 * @version 1.0
 * @date 19/01/2021
 * 
 * @brief Multichannel audio player and spatializer
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
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioAsioPatchBay;
using System;
using System.Runtime.InteropServices;


public class At_Player : MonoBehaviour
{
    /// NAudio class used to read the audio samples in the audio file
    private AudioFileReader aud;

    //-----------------------------------------------------
    // AUDIO SAMPLE ARRAY USED BY THE PLAYER 
    //-----------------------------------------------------
    // Data flow :
    // rawAudioData -> inputFileBuffer -> playerOutputBuffer
    //-----------------------------------------------------
    /// constants used for array initialization 
    const int MAX_BUF_SIZE = 1024;
    const int MAX_OUTPUT_CHANNEL = 24; // very very very large !!
    const int MAX_INPUT_CHANNEL = 16;
    /// array containing the all the samples of the audio file
    private float[] rawAudioData;
    /// array containing a single frame of the audio file
    private float[] inputFileBuffer;
    /// array containing the processed data (after spatialization or up/downmix) of the array for a single frame
    private float[] playerOutputBuffer;


    /******************************************************************
    * DATA GIVEN THE STATE OF THE PLAYER
    * ****************************************************************/
    //-----------------------------------------------------------------
    // "persistant" data, saved in the json file - copy/saved of the At_PlayerState class
    // ----------------------------------------------------------------
    /// name of the audio file to play (Supposed to be in "Assets\Streaming Asset")
    public string fileName;
    /// gain applied to the audiofile
    public float gain;
    /// boolean telling if the player is 2D (no spatialization applied) or 3D (spatialization applied)
    public bool is3D;
    /// boolean telling if the player 3D is omnidirectional(mono) or directional (multicanal)
    public bool isDirective; //modif mathias 06-17-2021
    /// boolean telling if the player start to play On Awake
    public bool isPlayingOnAwake;
    /// boolean telling if the player is looping the read audio file
    public bool isLooping;
    /// directivity balance of the virtual microphone used for this source : balance [0,1] between omnidirectionnal and cardiod
    public float omniBalance;
    /// balance between "normal delay" and "reverse delay" for focalised source - see Time Reversal technic used for WFS
    public float timeReversal;
    ///type of distance attenuation in the spatialize : 0 = none, 1 = linera, 2 = square
    public float attenuation;
    /// minimum distance above which the sound produced by the source is attenuated
    public float minDistance;

    public int[] channelRouting;

    At_PlayerState playerState;

    public bool isStreaming = true;
    int numSampleReadForStream = 0;
    int audReadOffset = 0;
    
    bool reachEndOfFile = false;
    //-----------------------------------------------------------------
    // data used at runtime
    // ----------------------------------------------------------------
    /// id of this player in the spatialization engine
    public int spatID;
    /// boolean telling if the player is actually playing
    public bool isPlaying = false;
    /// number of player in the Unity scene - unused
    static public int playerCount;
    /// number of channel of the output bus
    public int outputChannelCount;
    /// boolean telling if the player has been created at runtime
    public bool isDynamicInstance = false;
    /// number of channel in the audio file
    public int numChannelsInAudioFile = 0;
    /// read offset for each channel of the audio file
    int[] audioFileReadOffset;
    /// rms value of the signal from each channel, used to display bargraph
    public float[] meters;

    public string externAssetsPath_audio, externAssetsPath_audio_standalone;

    bool mustBeDestroyedNow = false;
    bool mustBeDestroyedSafely = false;
    bool mustBeDestroyedOnNextFrame = false;
    public At_MasterOutput masterOutput;

    public string guid = "";
    
    void Reset()
    {        
        setGuid();        
    }

    void OnValidate()
    {
        Event e = Event.current;

        if (e != null)
        {
            Debug.Log(e.commandName);
            if (e.type == EventType.ExecuteCommand && e.commandName == "Duplicate")
            {
                setGuid();
            }
            
        }
    }

   
    public void setGuid()
    {
        guid = System.Guid.NewGuid().ToString();
        //Debug.Log("create player with guid : " + guid);
    }

    //-----------------------------------------------------     
    // Awake, Disable, Get/Start/Stop playing 
    //-----------------------------------------------------
    //public bool GetIsPlaying() { return isPlaying; }
    public void StartPlaying() {
        isPlaying = true;
        // reset the read offset for each channel
        if (audioFileReadOffset != null)
        {
            for (int i = 0; i < audioFileReadOffset.Length; i++) { audioFileReadOffset[i] = 0; }
        }
        if (aud != null)
        {
            audReadOffset = 0;
            aud.Position = 0;

        }
    }
    public void StopPlaying() { 
        isPlaying = false;
        // reset the read offset for each channel
        if (audioFileReadOffset != null)
        {
            for (int i = 0; i < audioFileReadOffset.Length; i++) { audioFileReadOffset[i] = 0; }
        }
        
        if (aud != null)
        {
            audReadOffset = 0;
            aud.Position = 0;
        }
    }
    public void SafeDestroy()
    {
        mustBeDestroyedSafely = true;
    }
    public void OnDisable() { playerCount--; }

    

    public void Awake() {

       

        playerCount++;
        
        initAudioFile(false);
    }

    // for use from the outside without any instantiated At_Player
    public static int getNumChannelInAudioFile(string path)
    {
        
        //Parse the file with NAudio
        if (path != null)
        {
            AudioFileReader reader = new AudioFileReader(path);
            // get the number of channel in audio file (should be =16)
            return reader.WaveFormat.Channels;
        }
        return 0;
    }

    public int getNumChannelInAudioFile()
    {
        //string path = Application.dataPath + "/StreamingAssets/" + fileName;
        string path = "";
#if UNITY_EDITOR
    path = externAssetsPath_audio + fileName;
#else 
    path = externAssetsPath_audio_standalone + fileName;
#endif

        //Parse the file with NAudio
        if (fileName != null)
        {
            aud = new AudioFileReader(path);
            // get the number of channel in audio file (should be =16)
            return aud.WaveFormat.Channels;
        }
        return 0;
    }

    public void initMeters() {
        //string path = Application.dataPath + "/StreamingAssets/" + fileName;
        //string path = externAssetsPath_audio + fileName;
        string path = "";
#if UNITY_EDITOR
        path = externAssetsPath_audio + fileName;
#else 
        path = externAssetsPath_audio_standalone + fileName;
#endif
        //Parse the file with NAudio
        if (fileName != null) {
            aud = new AudioFileReader(path);
            // get the number of channel in audio file (should be =16)
            numChannelsInAudioFile = aud.WaveFormat.Channels;
            // init the array containing the rms value for each channel
            meters = new float[numChannelsInAudioFile];
            aud.Dispose();
            aud = null;
        }
    }

    /**
    * @brief Initialization of the audio file reader using NAudio API.
    * Open the audio file with the AudioFileReader class using the given path and copy all the samples in the 
    * \p rawAudioData array.
    * Initialize the inputFileBuffer and playerOutputBuffer arrays.
    * Get the number of channels of the audio file
    */
    public void initAudioFile(bool isRandomDynamicPlayer)
    {
        /*
        if (is3D == false)
        {
            isStreaming = false;
        }
        */
        //string path = Application.dataPath + "/StreamingAssets/" + fileName;
        //externAssetsPath = PlayerPrefs.GetString("externAssetsPath_audio");
        Debug.Log("is dynamic ? " + isRandomDynamicPlayer);
        if (!isRandomDynamicPlayer)
        {
            playerState = At_AudioEngineUtils.getPlayerStateWithGuidAndName(guid, gameObject.name);
            if (playerState != null && playerState.fileName != null && playerState.fileName != "")
                fileName = playerState.fileName;
        }

        string path = "";
#if UNITY_EDITOR
        path = externAssetsPath_audio + fileName;
#else 
        path = externAssetsPath_audio_standalone + fileName;
        Debug.Log("init audio file with path : "+path);
#endif
        
        //string path = externAssetsPath_audio + fileName;
        //Debug.Log(path);
        //Parse the file with NAudio
        if (fileName != null)
        {
            aud = new AudioFileReader(path);
            // get the number of channel in audio file (should be =16)
            numChannelsInAudioFile = aud.WaveFormat.Channels;

            // init the read offset for each channel of the audio file
            audioFileReadOffset = new int[numChannelsInAudioFile];

            // init the array containing the rms value for each channel
            meters = new float[numChannelsInAudioFile];

            if (!isStreaming)
            {
                // init the buffer containing all the raw data of the audio file
                rawAudioData = new float[(int)aud.Length];

                // copy all the data of the audio file in the raw data buffer
                aud.Read(rawAudioData, 0, (int)aud.Length);

            }
            else
            {
                // init the buffer for streaming from disk
                // Raw Data are in Bytes !! And we read 32bit float -> 4 bytes per sample
                //rawAudioData = new float[MAX_BUF_SIZE*MAX_INPUT_CHANNEL * 4];
                rawAudioData = new float[MAX_BUF_SIZE * MAX_INPUT_CHANNEL];
                //rawAudioData = new float[768];

            }

            // if the player is set to play on awake, start playing
            if (isPlayingOnAwake)
            {
                StartPlaying();
            }
        }

        // init the buffer used for playing
        // - the buffer this used to read a small buffer in the raw data of the audio file (size = size fo the asio output buffer)
        inputFileBuffer = new float[MAX_BUF_SIZE * MAX_OUTPUT_CHANNEL];
        // - if player is 3D this buffer is used to copy the processed inputFileBuffer by spatialization algorithm (using C++ dll)
        // - if player is 2D this buffer is used to downmix/upmix and/or apply routing matrix to conform input channel order to output channel order.
        // WARNING : THIS HAS TO BE DONE - FOR NOW IT IS A SIMPLE COPY
        playerOutputBuffer = new float[MAX_BUF_SIZE * MAX_OUTPUT_CHANNEL];
    }


    /**
    * 
    * @brief Update the spatialization parameters at each frame using extern call of the 3D Audio Engine API :
    * 
    * @details - position of the GameObject the script is attached to
    * @details - type of attenuation with distance from the listener
    * @details - balance [0,1] between omnidirectionnal and cardiod directivity for the virtual microphone of the listener
    * @details - balance [0,1] between "normal delay" and "reverse delay" for focalised source (see Time Reversal technic used for WFS)
    * 
     */

    public void UpdateSpatialParameters()
    {
        // Get the position of the GameObject
        float[] position = new float[3];
        float[] rotation = new float[3]; //modif mathias 06-14-2021
        float[] forward = new float[3]; //modif mathias 06-14-2021

        position[0] = gameObject.transform.position.x;
        position[1] = gameObject.transform.position.y;
        position[2] = gameObject.transform.position.z;

        //modif mathias 06-14-2021
        rotation[0] = gameObject.transform.rotation.x;
        rotation[1] = gameObject.transform.rotation.y;
        rotation[2] = gameObject.transform.rotation.z;

        //modif mathias 06-14-2021
        forward[0] = gameObject.transform.forward.x;
        forward[1] = gameObject.transform.forward.y;
        forward[2] = gameObject.transform.forward.z;

        /// set the source position in the spatializer (C++ dll call)
        AT_SPAT_WFS_setSourcePosition(spatID, position, rotation, forward); //modif mathias 06-14-2021
        /// set the min distance for this source
        AT_SPAT_WFS_setMinDistance(spatID, minDistance);
        /// set the type of distance attenuation in the spatialize : 0 = none, 1 = linera, 2 = square (C++ dll call)
        AT_SPAT_WFS_setSourceAttenuation(spatID, attenuation);
        /// set the directivity balance of the virtual microphone used for this source : balance [0,1] between omnidirectionnal and cardiod (C++ dll call)
        AT_SPAT_WFS_setSourceOmniBalance(spatID, omniBalance);
        /// set the balance between "normal delay" and "reverse delay" for focalised source - see Time Reversal technic used for WFS (C++ dll call)
        AT_SPAT_WFS_setTimeReversal(spatID, timeReversal);
    }
    private void Update()
    {
        if (is3D)
        {
            UpdateSpatialParameters();
        }
        else //
        {
            isDirective = false; //modif mathias 06-17-2021
        }
        if (mustBeDestroyedNow)
        {
            masterOutput.destroyPlayerNow(this);
            mustBeDestroyedNow = false;
        }
        if (mustBeDestroyedSafely)
        {
            masterOutput.destroyPlayerSafely(this);
            mustBeDestroyedSafely = false;
        }
        if (mustBeDestroyedOnNextFrame)
        {
            DestroyNow();
            //masterOutput.destroyPlayerNow(this);
            mustBeDestroyedOnNextFrame = false;
        }
       

    }

    public void DestroyOnNextFrame()
    {
        mustBeDestroyedOnNextFrame = true;
    }

    public void DestroyNow()
    {
        //delete(rawAudioData);
        
        aud.Dispose();
        aud = null;
        
        Destroy(gameObject);
        
    }


    /**
     * 
     * @brief Audio file read method. Called by the At_MasterOutput, in charge of ASIO output. 
     * 
     * @details Fill the array \p inputFileBuffer with the samples of rawAudioData.
     * Copy the \p rawAudioData to inputFileBuffer which will be processed by the conformInputBufferToOutputBusFormat()
     * method to apply spatialization, upmix/downmix and/or routing.
     * It also process the rms value for each channel to display rms-meters in the GUI.
     * 
     * @param[in] bufferSize : Size of the output buffer (number of sample for a frame)
     */
    public bool extractInputBuffer(int bufferSize)
    {
        if (isPlaying)
        {

            if (isStreaming)
            {
                if (bufferSize <= MAX_BUF_SIZE)
                {
                    // fill the raw data buffer 
                    if (aud.Position + bufferSize * numChannelsInAudioFile * 4 <= (int)aud.Length)
                    {
                        
                       aud.Read(rawAudioData, 0, bufferSize * numChannelsInAudioFile);
                       numSampleReadForStream = bufferSize * numChannelsInAudioFile;
                    }
                    else
                    {
                        numSampleReadForStream = (int)(aud.Length - aud.Position) / 4;
                        aud.Read(rawAudioData, 0, numSampleReadForStream);
                        

                    }
                    audReadOffset += 4 * bufferSize * numChannelsInAudioFile;

                }   
            }

            if (rawAudioData != null && aud != null)
            {
                // loop on each channel of the audio file
                for (int channelIndex = 0; channelIndex < numChannelsInAudioFile; channelIndex++)
                {
                    meters[channelIndex] = 0;

                    // loop on each sample for the output buffer
                    for (int sampleIndex = 0; sampleIndex < bufferSize; sampleIndex++)
                    {
                        // read index : calculate the index in the raw data of the audio file
                        int indexInRawData = audioFileReadOffset[channelIndex] + numChannelsInAudioFile * sampleIndex + channelIndex;
                        // write index : calculate the index in the inputFileBuffer buffer
                        int indexInInputFileBuffer = numChannelsInAudioFile * sampleIndex + channelIndex;

                        // while the index in raw data is less than the size of the raw data, copy the data. 
                        // WARNING : I DO NOT UNDERSTANT WHY THIS SHOULD BE "aud.Length / 4.0f"
                        // Apply gain set in the custom inspector editor of the player
                        float volume = Mathf.Pow(10.0f, gain / 20.0f);// Mathf.Sqrt(2.0f);
                        if (isStreaming)
                        {
                            if (sampleIndex < numSampleReadForStream)
                            {
                                inputFileBuffer[indexInInputFileBuffer] = volume * rawAudioData[indexInInputFileBuffer];
                                meters[channelIndex] += Mathf.Pow(inputFileBuffer[indexInInputFileBuffer], 2f);
                            }
                            else
                            {
                                reachEndOfFile = true;
                            }
                            
                        }
                        else
                        {
                            if (indexInRawData < aud.Length / 4.0f)
                            {
                                inputFileBuffer[indexInInputFileBuffer] = volume * rawAudioData[indexInRawData];
                                // Start calculating the meter value for each channel (sum of square)
                                meters[channelIndex] += Mathf.Pow(inputFileBuffer[indexInInputFileBuffer], 2f);
                            }        
                            else
                            {
                                reachEndOfFile = true;
                            }
                        }

                        // Otherwise : this is the end of the audiofile
                        if (reachEndOfFile)
                        {
                            //Debug.Log("source " + spatID + "reach end");
                            for (int i = 0; i < audioFileReadOffset.Length; i++)
                            {
                                audioFileReadOffset[i] = 0;

                            }
                            //reset the offset for each channel to zero
                            
                            audReadOffset = 0;
                            aud.Position = 0;
                            
                            if (!isLooping)
                            {
                                isPlaying = false;
                                if (isDynamicInstance)
                                {
                                    // We do not need to destroy the GameObject because we use a static pool
                                    //mustBeDestroyedSafely = true;
                                }
                            }
                            reachEndOfFile = false;
                            break;
                        }

                    }
                    // update the read offset for each channel
                    audioFileReadOffset[channelIndex] += bufferSize * numChannelsInAudioFile;
                    
                    // compute the RMS value for this buffer (sa:
                    meters[channelIndex] = Mathf.Sqrt(meters[channelIndex] / bufferSize);
                }
                
            }
            return true;
        }
        return false;
    }

    /**
     * @brief private method used to get the output channel of the input channel in the audio file 
     */
    int getInputChannelForOutputchannelInrouting(int outputChannel)
    {
        /*
        int[] routing;
        
        if (isDynamicInstance)
        {
            routing = channelRouting;
        }
        else
        {
            playerState = At_AudioEngineUtils.getPlayerStateWithName(gameObject.name);
            routing = playerState.channelRouting;
        }
        */
        for (int i = 0; i< channelRouting.Length; i++)
        {
            if (outputChannel == channelRouting[i])
            {
                return i;
            }
        }
        return -1;
    }

    /**
     * @brief Spatialization/upmix/downmix/routing method called by the At_MasterOutput, in charge of ASIO output. 
     * 
     * @details Fill the \p playerOutputBuffer array with the samples of \p inputFileBuffer array.
     * If the source is 3D, it applies the spatialization WFS algorithm, otherwise it simply copy the data from a channel in the input
     * to the same channel in the output (for now).
     * TODO : fine routing / upmixing / downmixing for 2D sources
     * 
     * @param[in] bufferSize : size of the output buffer (number of sample for a frame)
     * 
     */
        public bool conformInputBufferToOutputBusFormat(int bufferSize)
    {        
        if (isPlaying)
        {
            if (rawAudioData != null)
            {
                // if the source is 3D :
                // ----------------------
                // Apply the spatialization WFS algorithm : the "inputFileBuffer" array (extracted from the audio file) is processed and the result is copied 
                // to the "playerOutputBuffer" array according to the number of output channel of the master bus
                if (is3D)
                {
                    //Debug.Log("process spatID = " + spatID);
                    AT_SPAT_WFS_process(spatID,inputFileBuffer, playerOutputBuffer, bufferSize, numChannelsInAudioFile, outputChannelCount);     
                    if (float.IsNaN(playerOutputBuffer[0]))
                    {
                        Debug.Log("out Nan from playerOutputBuffer !");
                    }
                    
                }
                // Otherwise (2D) :
                // ----------------
                // The first N channel of the "inputFileBuffer" array (extracted from the audio file) are copied into the N channel of the "playerOutputBuffer" 
                // array according to the number of output channel of the master bus 

                // The first N channel of the "inputFileBuffer" array (extracted from the audio file) are copied into N channel of the "playerOutputBuffer" 
                // array according to channel routing array 
                else
                {
                    //int indexInputChannel = 0;
                    
                    for (int outputChannelIndex = 0; outputChannelIndex < outputChannelCount; outputChannelIndex++)
                    {
                        for (int sampleIndex = 0; sampleIndex < bufferSize; sampleIndex++)
                        {
                            //if (indexInputChannel < numChannelsInAudioFile)
                            //{
                            int indexInputChannel = getInputChannelForOutputchannelInrouting(outputChannelIndex);
                            if (indexInputChannel != -1)
                            {
                                //playerOutputBuffer[outputChannelCount * sampleIndex + outputChannelIndex] = inputFileBuffer[numChannelsInAudioFile * sampleIndex + indexInputChannel];
                                playerOutputBuffer[outputChannelCount * sampleIndex + outputChannelIndex] = inputFileBuffer[numChannelsInAudioFile * sampleIndex + indexInputChannel];
                            }
                            else
                            {
                                playerOutputBuffer[outputChannelCount * sampleIndex + outputChannelIndex] = 0;
                            }
                                
                            //}
                            // if the input buffer has less channel than required by the master bus, the extra channels are feed with zeros...
                            //else
                            //{
                            //    playerOutputBuffer[outputChannelCount * sampleIndex + outputChannelIndex] = 0;
                            //}
                        }
                        //indexInputChannel++;
                    }
                }
                return true;
            }   
        }
        return false;
    }

    /**
     * @brief Method called by the At_Mixer, which sum all the buffers filled by the different players in the scene. 
     * 
     * @param[out] mixerInputBuffer : Monophonic buffer to fill for a given output channel
     * @param[in] bufferSize : size of the output buffer (number of sample for a frame)
     * @param[in] channelIndex : index of the channel to fill
     * 
     */    
    public void fillMixerChannelInputWithPlayerOutput(ref float[] mixerInputBuffer, int bufferSize, int channelIndex)
    {
        if (isPlaying)
        {
            if (rawAudioData != null)
            {
                // get a buffer for one channel 
                for (int sampleIndex = 0; sampleIndex < bufferSize; sampleIndex++)
                {
                    if (playerOutputBuffer == null)
                    {
                        Debug.Log("playerOutputBuffer is null !!");
                    }
                    else
                    {
                        mixerInputBuffer[sampleIndex] = playerOutputBuffer[outputChannelCount * sampleIndex + channelIndex];
                    }
                    
                }                
            }
        }
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        
        float distance;
        if (!isDynamicInstance)
        {
            playerState = At_AudioEngineUtils.getPlayerStateWithGuidAndName(guid, gameObject.name);//getPlayerStateWithName(gameObject.name);

            if (playerState != null)
            {
                distance = playerState.minDistance;
            }
            else
            {
                distance = 0;
            }
            
        }
        else
        {
            distance = minDistance;
        }

        if (!isDynamicInstance)
        {
                        
        }
        
        if (is3D)
        {


            if (isDirective)
            {

                float angle = 2 * Mathf.PI / numChannelsInAudioFile;
                float angleOffset = transform.eulerAngles.y * Mathf.PI / 180.0f + Mathf.PI / 2.0f + angle / 2f;
                float squareSize = 0.5f;

               

                for (int i = 0; i < numChannelsInAudioFile; i++)
                {
                    
                    

                    Vector3 center = gameObject.transform.position + new Vector3(distance * Mathf.Cos(angleOffset - i * angle), 0, distance * Mathf.Sin(angleOffset - i * angle));
                    Vector3 nextCenter = gameObject.transform.position + new Vector3(distance * Mathf.Cos(angleOffset - (i + 1) * angle), 0, distance * Mathf.Sin(angleOffset - (i + 1) * angle));
                    
                    //Color c;
                    if (i == 0) Gizmos.color = Color.red;
                    else if (i == 1) Gizmos.color = Color.green;
                    else Gizmos.color = Color.blue;
                    // draw a square :
                    Gizmos.DrawLine(new Vector3(center.x + squareSize / 2f, 0, center.z + squareSize / 2f),
                        new Vector3(center.x + squareSize / 2f, 0, center.z - squareSize / 2f));
                    Gizmos.DrawLine(new Vector3(center.x + squareSize / 2f, 0, center.z - squareSize / 2f),
                        new Vector3(center.x - squareSize / 2f, 0, center.z - squareSize / 2f));
                    Gizmos.DrawLine(new Vector3(center.x - squareSize / 2f, 0, center.z - squareSize / 2f),
                        new Vector3(center.x - squareSize / 2f, 0, center.z + squareSize / 2f));
                    Gizmos.DrawLine(new Vector3(center.x - squareSize / 2f, 0, center.z + squareSize / 2f),
                        new Vector3(center.x + squareSize / 2f, 0, center.z + squareSize / 2f));

                    // draw a line :
                    Gizmos.DrawLine(center, nextCenter);
                }


            }
            else
            {
                const float numStepDrawCircle = 20;
                float angle = 2 * Mathf.PI / numStepDrawCircle;

                for (int i = 0; i < numStepDrawCircle; i++)
                {
                    Vector3 center = transform.position + new Vector3(distance * Mathf.Cos(i * angle), 0, distance * Mathf.Sin(i * angle));
                    Vector3 nextCenter = transform.position + new Vector3(distance * Mathf.Cos((i + 1) * angle), 0, distance * Mathf.Sin((i + 1) * angle)); ;
                    //Debug.DrawLine(center, nextCenter, Color.green);
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(center, nextCenter);
                }

                SceneView.RepaintAll();

            }

        }
    }
#endif

    /**
     * Extern declaration of the functions provided by the 3D Audio Engine API (AudioPlugin_AtSpatializer.dll)
     */
#region DllImport        
    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern void AT_SPAT_WFS_setSourcePosition(int id, float[] position, float[] rotation, float[] forward); //modif mathias 06-14-2021
    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern void AT_SPAT_WFS_setSourceAttenuation(int id, float attenuation);
    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern void AT_SPAT_WFS_setSourceOmniBalance(int id, float omniBalance);
    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern void AT_SPAT_WFS_setTimeReversal(int id, float timeReversal);
    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern void AT_SPAT_WFS_setMinDistance(int id, float minDistance);
    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern void AT_SPAT_WFS_process(int id, float[] inBuffer, float[] outBuffer, int bufferLength, int inChannelCount, int outChannelCount);

#endregion

}
