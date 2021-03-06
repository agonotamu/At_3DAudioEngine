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
using UnityEngine.SceneManagement;

public class At_Player : MonoBehaviour
{
    /// NAudio class used to read the audio samples in the audio file
    private AudioFileReader aud;

    // Debug
    static int playingCount = 0;
    //-----------------------------------------------------
    // AUDIO SAMPLE ARRAY USED BY THE PLAYER 
    //-----------------------------------------------------
    // Data flow :
    // rawAudioData -> inputFileBuffer -> playerOutputBuffer
    //-----------------------------------------------------
    /// constants used for array initialization 
    const int MAX_BUF_SIZE = 2048;
    const int MAX_OUTPUT_CHANNEL = 48; 
    const int MAX_INPUT_CHANNEL = 48;
    const int UNITY_AUDIOSOURCE_READOFFSET = 512;
    int currentIndexInputBufferWrite_AudioSource = 0;
    int currentIndexInputBufferRead_AudioSource = 0;
    int unityOutputBufferSize, asioOutputBufferSize;
    bool isInputBufferInitialized = false;

    const int maxIndexBufferRW_AudioSource = 4;
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
    // routing of the audiofile channels in the output channels for 2D player
    public int[] channelRouting;
    // copy of the saved player state
    At_PlayerState playerState;
    
    //-----------------------------------------------------------------
    // data used at runtime
    // ----------------------------------------------------------------

    /// id of this player in the spatialization engine
    public int spatID;
    /// boolean telling if the player is actually playing
    public bool isPlaying = false;
    /// boolean telling if the player is asked to play (not necesseraly true
    public bool isAskedToPlay = false;
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
    // boolean telling if the player use Unity AudioSource or the native NAudio API to read audio file
    public bool isUnityAudioSource;

    // true if the file is streamed from disk or if it is entirely laoded in RAM at startup
    public bool isStreaming = true;
    // current sample index of the stream
    int numSampleReadForStream = 0;

    int audReadOffset = 0;

    bool reachEndOfFile = false;

    
  
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
    public void StartPlaying() {
        isAskedToPlay = true;
        playingCount++;
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
        isAskedToPlay = false;
        isPlaying = false;
        // reset the read offset for each channel
        if (audioFileReadOffset != null)
        {
            for (int i = 0; i < audioFileReadOffset.Length; i++) { audioFileReadOffset[i] = 0; }
        }
        
        if (aud != null)
        {
            if (isDynamicInstance)
            {
                aud.Dispose();
                aud = null;
            }
            else
            {
                audReadOffset = 0;
                aud.Position = 0;
            }
            
        }
    }
    public void SafeDestroy()
    {
        mustBeDestroyedSafely = true;
    }
    public void OnDisable() {
        
        StopPlaying();
    }

    public void Awake() {

        masterOutput = GameObject.FindObjectOfType<At_MasterOutput>();
        if (!isDynamicInstance)
            masterOutput.addPlayerToList(this);
        if (!isUnityAudioSource)
        {
            initAudioFile(false);
        }
        else
        {
            // Make some init to get the buffer from Unity AudioSource Component
            isPlaying = true;
            isAskedToPlay = true;
            numChannelsInAudioFile = 1;
        }
            
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
        string path = At_AudioEngineUtils.GetFilePathForStates(fileName);
        //Parse the file with NAudio
        if (fileName != null && fileName !="")
        {
            if (aud == null)
                aud = new AudioFileReader(path);
            // get the number of channel in audio file (should be =16)
            return aud.WaveFormat.Channels;
        }
        return 0;
    }

    public void initMeters() {

        string path = At_AudioEngineUtils.GetFilePathForStates(fileName);
        
        //Parse the file with NAudio
        if (fileName != null && fileName !="") {
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

        if (!isRandomDynamicPlayer)
        {
            playerState = At_AudioEngineUtils.getPlayerStateWithGuidAndName(SceneManager.GetActiveScene().name, guid, gameObject.name);
            if (playerState != null && playerState.fileName != null && playerState.fileName != "")
            {
                //------ init player serialized parameter
                fileName = playerState.fileName;
                gain = playerState.gain;
                is3D = playerState.is3D;
                isDirective = playerState.isDirective; //modif mathias 06-17-2021
                isPlayingOnAwake = playerState.isPlayingOnAwake;
                fileName = playerState.fileName;
                isLooping = playerState.isLooping;
                attenuation = playerState.attenuation;
                omniBalance = playerState.omniBalance;
                timeReversal = playerState.timeReversal;
                minDistance = playerState.minDistance;
                channelRouting = playerState.channelRouting;
            }
            
        }

        string path = At_AudioEngineUtils.GetFilePathForStates(fileName);

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
                rawAudioData = new float[MAX_BUF_SIZE * MAX_INPUT_CHANNEL];
            }

            // if the player is set to play on awake, start playing
            if (isPlayingOnAwake)
            {
                StartPlaying();
            }
        }

        initAudioBuffer();
    }

    int gcd(int a, int b)
    {
        while (a != 0 && b != 0)
        {
            if (a > b)
                a %= b;
            else
                b %= a;
        }

        return a | b;
    }

    public void initAudioBuffer()
    {
        int inputFileBufferSize = 0, unityNumBuffer;

        if (isUnityAudioSource)
        {            
            AudioSettings.GetDSPBufferSize(out unityOutputBufferSize, out unityNumBuffer);
            asioOutputBufferSize = At_AudioEngineUtils.asioOut.FramesPerBuffer;
            int q = gcd(unityOutputBufferSize, asioOutputBufferSize);
            inputFileBufferSize = 10 * unityOutputBufferSize * asioOutputBufferSize / q;
            
        }
        else
        {
            inputFileBufferSize = MAX_BUF_SIZE * MAX_OUTPUT_CHANNEL;
        }

        inputFileBuffer = new float[inputFileBufferSize];
        // - if player is 3D this buffer is used to copy the processed inputFileBuffer by spatialization algorithm (using C++ dll)
        // - if player is 2D this buffer is used to downmix/upmix and/or apply routing matrix to conform input channel order to output channel order.
        // WARNING : THIS HAS TO BE DONE - FOR NOW IT IS A SIMPLE COPY
        playerOutputBuffer = new float[MAX_BUF_SIZE * MAX_OUTPUT_CHANNEL];

        isInputBufferInitialized = true;
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
        /*
        if(isPlaying && gameObject.activeSelf == false)
        {
            StopPlaying();
        }
        */

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
        //Debug.Log("number of instance playing : " + playingCount);

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

    public void cleanSpatializerDelayBuffer()
    {
        if (is3D)
        {
            AT_SPAT_WFS_cleanDelayBuffer(spatID);
        }
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
        
        if (isAskedToPlay && aud != null && !isUnityAudioSource)
        {
            isPlaying = true;
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


                        // Apply gain set in the custom inspector editor of the player
                        float volume = Mathf.Pow(10.0f, gain / 20.0f);
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
                                isAskedToPlay = false;
                                isPlaying = false;

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

    

    int mod(int x, int m)
    {
        int r = x % m;
        return r < 0 ? r + m : r;
    }

    /**
     * @brief Unity Callback function to read/write samples for it audio graph. 
     * 
     * @details This is where we get the sample from the AudioSource componennt to feed the At_PLayer component
     * 
     * @param[in] data : multiplexed unity sample audio buffer
     * 
     * @param[in] channels : number of channels in the audio graph
     * 
     */
    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (isUnityAudioSource && isInputBufferInitialized == true)
        {
            // asynchrone copy of the unity buffer (data) into At_Player buffer (inputFileBuffer)
            for (int sample = 0; sample < data.Length; sample += channels)
            {
                inputFileBuffer[sample / channels + currentIndexInputBufferWrite_AudioSource * unityOutputBufferSize] = data[sample];
            }

            // udpate write index
            currentIndexInputBufferWrite_AudioSource++;
            if (currentIndexInputBufferWrite_AudioSource * unityOutputBufferSize >= inputFileBuffer.Length) currentIndexInputBufferWrite_AudioSource = 0;
        }
        // clear the unity buffer to avoid audio output in the system device
        System.Array.Clear(data, 0, data.Length);

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
        if (isAskedToPlay)
        {
            if (rawAudioData != null || isUnityAudioSource)
            {
                // if the source is 3D :
                // ----------------------
                // Apply the spatialization WFS algorithm : the "inputFileBuffer" array (extracted from the audio file) is processed and the result is copied 
                // to the "playerOutputBuffer" array according to the number of output channel of the master bus
                if (is3D)
                {
                    //Debug.Log("process spatID = " + spatID);
                    int offset = 0;
                    if (isUnityAudioSource)
                    {
                        offset = currentIndexInputBufferRead_AudioSource *  asioOutputBufferSize;
                    }
                    AT_SPAT_WFS_process(spatID,inputFileBuffer, playerOutputBuffer, bufferSize, offset, numChannelsInAudioFile, outputChannelCount);
                    //currentIndexInputBufferRead_AudioSource = mod((currentIndexInputBufferRead_AudioSource + 1), maxIndexBufferRW_AudioSource);
                    currentIndexInputBufferRead_AudioSource++;
                    if (currentIndexInputBufferRead_AudioSource * asioOutputBufferSize >= inputFileBuffer.Length) currentIndexInputBufferRead_AudioSource = 0;
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
                            playerOutputBuffer[outputChannelCount * sampleIndex + outputChannelIndex] = 0;
                           

                            int indexInputChannel = channelRouting[outputChannelIndex];

                            // if indexInputChannel >= numChannelsInAudioFile, it means that the user have selected "none" for this channel in the "channel routing" of the 2D player
                            if (indexInputChannel != -1 && indexInputChannel < numChannelsInAudioFile)
                            {                               
                                playerOutputBuffer[outputChannelCount * sampleIndex + outputChannelIndex] += inputFileBuffer[numChannelsInAudioFile * sampleIndex + indexInputChannel];
                            }
                            else
                            {
                                playerOutputBuffer[outputChannelCount * sampleIndex + outputChannelIndex] = 0;
                            }
                             
                        }                        
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
        if (isAskedToPlay)
        {
            if (rawAudioData != null || isUnityAudioSource)
            {
                // get a buffer for one channel 
                for (int sampleIndex = 0; sampleIndex < bufferSize; sampleIndex++)
                {
                    if (playerOutputBuffer != null)                    
                    {
                        mixerInputBuffer[sampleIndex] = playerOutputBuffer[outputChannelCount * sampleIndex + channelIndex];
                    }
                    
                }                
            }
        }
    }

    /**
      * @brief Method used to display properties of the At_Player in the Scene View.    
      */

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        
        float distance;
        if (!isDynamicInstance)
        {
            playerState = At_AudioEngineUtils.getPlayerStateWithGuidAndName(SceneManager.GetActiveScene().name, guid, gameObject.name);

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

            // Directive Source are not supported anymore.....
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

                // DRAW A CIRLE GIVEN SHOWING THE "min_distance" parameter of the At_Player
                const float numStepDrawCircle = 20;
                float angle = 2 * Mathf.PI / numStepDrawCircle;

                for (int i = 0; i < numStepDrawCircle; i++)
                {
                    Vector3 center = transform.position + new Vector3(distance * Mathf.Cos(i * angle), 0, distance * Mathf.Sin(i * angle));
                    Vector3 nextCenter = transform.position + new Vector3(distance * Mathf.Cos((i + 1) * angle), 0, distance * Mathf.Sin((i + 1) * angle)); ;
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(center, nextCenter);
                }
                // Stuck inRepaintAll() - remove it
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
    private static extern void AT_SPAT_WFS_process(int id, float[] inBuffer, float[] outBuffer, int bufferLength, int offset,  int inChannelCount, int outChannelCount);
    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern void AT_SPAT_WFS_cleanDelayBuffer(int id);
#endregion

}
