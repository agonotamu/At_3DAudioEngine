
/**
 * 
 * @file At_MasterOutput.cs
 * @author Antoine Gonot
 * @version 1.0
 * @date 19/01/2021
 * 
 * @brief  Output the mixed buffer from At_3DAudioEngine API to the physical outputs of the selected ASIO device thanks to the NAudio API
 * 
 * @details
 * First, the At_MasterOuput object act as a listener in the scene. Then, it gives its position to the 3D Audio Engine et and the position of the virtual microphones
 * allowing the engine to process the spatialization of an At_Player according to its azimut (the spatialization is executed by the At_Player class)
 * It also uses the NAudio API to feed the output buffer of the ASIO driver, thanks to the callback method
 * OnAsioOutAudioAvailable(), called every frame. This method then uses the /p AsioInputPatcher class as a sample provider to converted the 32 bit floatting point 
 * format to the format requiered by the audio device (Int32LSB, Int16LSB, Int24LSB or Float32LSB)
 * 
 */


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.Wave.SampleProviders;
using NAudioAsioPatchBay;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;
using UnityEditor;
using System;

enum FilterType
{
    None = 0,
    LowPass = 1,
    HighPass = 2
}

public class At_MasterOutput : MonoBehaviour
{

    /// constants used for array initialization 
    const int MAX_BUF_SIZE = 2048;    
    /// temporary mono buffer used for processing
    private float[] tmpMonoBuffer;

    // Modif Gonot - 14/03/2023 - Adding Bass Managment
    /// temporary mono buffer used for processing subwoofer channels
    private float[,] subWooferTmpMonoBuffer;

    /// main NAudio class used to manage ASIO output
    private AsioOut asioOut;
    /// Naudio Class heriting from ISampleProvider used to converted the 32 bit floatting point 
    /// format to the format requiered by the audio device
    private AsioInputPatcher inputPatcher;

    /// List of references to the instances of At_Player classes
    public List<At_Player> playerList;
    /// Array of references to the instances of At_VirtualMic classes
    public At_VirtualMic[] virtualMics;
    public At_VirtualSpeaker[] virtualSpeakers;

    /// List of references to the instances of At_Player classes
    List<int> spatIDToDestroy;
    List<At_Player> playerObjectToDestroy;
    /******************************************************************
    * DATA GIVEN THE STATE OF THE PLAYER
    * ****************************************************************/
    //-----------------------------------------------------------------
    // "persistant" data, saved in the json file - copy/saved of the At_OutputState class
    // ----------------------------------------------------------------
    /// string given the name of the audio device
    public string audioDeviceName= "Voicemeeter Virtual ASIO";
    /// number of channel used for the output bus
    public int outputChannelCount;

    // Modif Gonot - 14/03/2023 - Adding Bass Managment
    /// number of channel used for the subwoofer output bus
    public int subwooferOutputChannelCount;
    public bool isBassManaged;
    public float crossoverFilterFrequency;
    public int[] indexInputSubwoofer;
    BiquadDirectFormI[] lowPassFilterLinkwitzRiley;
    BiquadDirectFormI[] highPassFilterLinkwitzRiley;

    // Modif Gonot - 21/11/2023 - Adding Haptic Feedback Managment
    At_HapticListenerOutput[] hapticListenerOutputs;
    At_HapticPlayer[] hapticPlayers;
    public int[] hapticListenerOutputChannelsCount;
    public string[] hapticListenerOutputGuid;
    public int[] hapticListenerChannelsIndex;
    //------------------------------

    /// index of the selected speaker configuration in the popup menu of the At_MasterOutput Component GUI
    public int outputConfigDimension;
    /// master gain for the output bus
    public float gain;

    // Modif Gonot - 14/03/2023 - Adding Bass Managment
    /// master gain for the output bus
    public float subwooferGain;
    /// audio sampling rate (44.1kHz by default)
    public int samplingRate;
    /// boolean telling if the ASIO output should run when starting the application
    public bool isStartingEngineOnAwake;
    /// size of the virtual mic rig in the scene (1 unit = 1 meter)
    public float virtualMicRigSize;

    public float maxDistanceForDelay;

    At_OutputState outputState;
    //-----------------------------------------------------------------
    // data used at runtime
    // ----------------------------------------------------------------    
    /// boolean telling if the ASIO outpout is running (i.e. the callback method is called)
    public bool running;
    /// boolean telling if the code of the callback method is executed - the output ASIO buffer 
    public bool isEngineStarted = false;
    /// Number of physical output of the audiodevice 
    int maxDeviceChannel;
    
    /// rms value of the signal from each channel, used to display bargraph
    public float[] meters;

    // Modif Gonot - 14/03/2023 - Adding Bass Managment
    /// rms value of the signal from each channel, used to display bargraph
    public float[] subwooferMeters;

    // Start is called before the first frame update
    void Awake()
    {
        

        At_Player[] players = FindObjectsOfType<At_Player>();
        
        spatIDToDestroy = new List<int>();
        playerObjectToDestroy = new List<At_Player>();
                
        outputState = At_AudioEngineUtils.getOutputState(SceneManager.GetActiveScene().name);
        // update internal data with the data parse in the json file :

        audioDeviceName = outputState.audioDeviceName;
        outputChannelCount = outputState.outputChannelCount;
        outputConfigDimension = outputState.outputConfigDimension;
        gain = outputState.gain;
        samplingRate = outputState.samplingRate;
        isStartingEngineOnAwake = outputState.isStartingEngineOnAwake;
        virtualMicRigSize = outputState.virtualMicRigSize;
        maxDistanceForDelay = outputState.maxDistanceForDelay;

        subwooferOutputChannelCount = outputState.subwooferOutputChannelCount;
        isBassManaged = outputState.isBassManaged;
        crossoverFilterFrequency = outputState.crossoverFilterFrequency;
        indexInputSubwoofer = outputState.indexInputSubwoofer;
        subwooferGain = outputState.subwooferGain;

        hapticListenerOutputChannelsCount = outputState.hapticListenerOutputChannelsCount;
        hapticListenerOutputGuid = outputState.hapticListenerOutputGuid;
        hapticListenerChannelsIndex = outputState.hapticListenerChannelsIndex;

        samplingRate = outputState.samplingRate;

        // initialize the temp buffer        
        tmpMonoBuffer = new float[MAX_BUF_SIZE];

        // Modif Gonot - 14/03/2023 - Adding Bass Managment
        subWooferTmpMonoBuffer = new float[subwooferOutputChannelCount,MAX_BUF_SIZE];

        // Initialize the spatializer and the ASIO output if "is starting engine on awake"
        if (isStartingEngineOnAwake) {
            StartEngine();
        }

        
        foreach (At_Player p in players)
        {
            addPlayerToList(p);
            p.outputChannelCount = outputChannelCount;
        }

        virtualMics = GameObject.FindObjectsOfType<At_VirtualMic>();
        virtualSpeakers = GameObject.FindObjectsOfType<At_VirtualSpeaker>();
        foreach (At_VirtualMic vm in virtualMics)
        {
            vm.m_maxDistanceForDelay = outputState.maxDistanceForDelay;
        }

        SoundWaveShaderManager[] swsms = GameObject.FindObjectsOfType<SoundWaveShaderManager>();
        foreach (SoundWaveShaderManager swsm in swsms)
        {
            swsm.Init();
        }

        // Modif Gonot - 14/03/2023
        // Init the the lowpass and high pass cross over filter 

        // Coefficients for the Second Order Low Pass Linkwitz - Riley Filter
        float wc = 2 * Mathf.PI * crossoverFilterFrequency;
        float k = wc / Mathf.Tan((wc/2.0f) / (float)samplingRate);
        float den = wc * wc + k * k + 2 * k * wc;
        float b0 = wc / den;
        float b1 = 2*wc*wc/ den;
        float b2 = b0;
        float a1 = (2 * wc * wc - 2 * k * k) / den;
        float a2 = (wc * wc + k * k - 2 * k * wc) / den;
        lowPassFilterLinkwitzRiley = new BiquadDirectFormI[subwooferOutputChannelCount];
        for (int i = 0; i< subwooferOutputChannelCount; i++)
        {
            lowPassFilterLinkwitzRiley[i] = new BiquadDirectFormI(b0, b1, b2, a1, a2);
        }
        

        // Coefficients for the Second Order High Pass Linkwitz - Riley Filter
        b0 = k * k / den;
        b1 = -2 * k * k / den;
        b2 = b0;
        highPassFilterLinkwitzRiley = new BiquadDirectFormI[outputChannelCount];
        for (int i = 0; i < outputChannelCount; i++)
        {
            highPassFilterLinkwitzRiley[i] = new BiquadDirectFormI(b0, b1, b2, a1, a2);
        }
    }

    /************************************************************************
     *              INITIALIZATION METHODS
     ************************************************************************/

    /**
    * @brief Method call OnAwake by the game engine
    */
    public void StartEngine()
    {
        
        isEngineStarted = true;

        // initialize the spatializer
        InitSpatializerEngine();

        // initialize the ASIO output
        InitAsio();

        meters = new float[outputChannelCount];

        // Modif Gonot - 14/03/2023 - Adding Bass Managment
        if (isBassManaged && subwooferOutputChannelCount != 0)
            subwooferMeters = new float[subwooferOutputChannelCount];
    }

    /**
    * @brief Initialise ASIO output with NAudio API : instantiate AsioOut class, get the number of intput/output 
    * of the audio device, set the "sample provider", init playback with output sampling rate, set the 
    * output callback method et start ASIO.
    */
    private void InitAsio()
    {
        foreach (var device in AsioOut.GetDriverNames())
        {

            if (device == audioDeviceName)
            {
                if (outputConfigDimension == 0 || outputChannelCount == 0)
                {                    
                    Debug.LogError("Select a speaker configuration in the At_MasterOutput GUI !");
                    Application.Quit();
                }
                else
                {

                    running = true;
                    if (At_AudioEngineUtils.asioOut == null)
                    {
                        At_AudioEngineUtils.asioOut = new AsioOut((string)device);
                        // Get the number of inputs in the device
                        int inputChannels = At_AudioEngineUtils.asioOut.DriverInputChannelCount;
                        // Get the number of outputs in the device
                        maxDeviceChannel = At_AudioEngineUtils.asioOut.DriverOutputChannelCount;
                        // Initialize a Patcher with the correct sample rate and number of intput and outputs 
                        inputPatcher = new AsioInputPatcher(samplingRate, inputChannels, maxDeviceChannel);
                        // Initialize Record and Playback for the device
                        At_AudioEngineUtils.asioOut.InitRecordAndPlayback(new SampleToWaveProvider(inputPatcher), inputChannels, samplingRate);
                    }
                    else
                    {
                        // Get the number of inputs in the device
                        int inputChannels = At_AudioEngineUtils.asioOut.DriverInputChannelCount;
                        // Get the number of outputs in the device
                        maxDeviceChannel = At_AudioEngineUtils.asioOut.DriverOutputChannelCount;
                        // Initialize a Patcher with the correct sample rate and number of intput and outputs 
                        inputPatcher = new AsioInputPatcher(samplingRate, inputChannels, maxDeviceChannel);
                    }

                    // Modif Gonot 28/03/2023 - [optim] Add Mixing Buffer
                                       
                    AT_SPAT_WFS_initializeOutput(samplingRate, At_AudioEngineUtils.asioOut.FramesPerBuffer, outputChannelCount, subwooferOutputChannelCount, crossoverFilterFrequency);

                    for (int playerIndex = 0; playerIndex < playerList.Count; playerIndex++)
                    {
                        playerList[playerIndex].initAudioBuffer();
                    }

                    // Modif Gonot - 21/11/2023 - Adding Haptic Feedback Managment
                    hapticListenerOutputs = FindObjectsOfType<At_HapticListenerOutput>();
                    foreach (At_HapticListenerOutput hlo in hapticListenerOutputs)
                        hlo.initializeOutput(samplingRate, At_AudioEngineUtils.asioOut.FramesPerBuffer);

                    hapticPlayers = FindObjectsOfType<At_HapticPlayer>();

                    // Add a callback method to proccess the sample in the in/out buffer
                    At_AudioEngineUtils.asioOut.AudioAvailable += OnAsioOutAudioAvailable;
                    // Start processing (i.e. calling the callback method)
                    At_AudioEngineUtils.asioOut.Play();
                }

            }
            else if(audioDeviceName == "select")
            {
                Debug.LogError("Select a device in the At_MasterOutput GUI !");
                Application.Quit();
            }
        }


    }

    private void UpdateVirtualMicPosition()
    {
        if (meters != null)
        {
            
            float[] positions = new float[outputChannelCount * 3];
            float[] rotations = new float[outputChannelCount * 3];
            float[] forwards = new float[outputChannelCount * 3];
            for (int i = 0; i < virtualMics.Length; i++)
            {

                float eulerX = virtualMics[i].gameObject.transform.eulerAngles.x;
                float eulerY = virtualMics[i].gameObject.transform.eulerAngles.y;
                float eulerZ = virtualMics[i].gameObject.transform.eulerAngles.z;

                if (eulerY == 180 && eulerZ == 180)
                {
                    eulerX = 180 - eulerX;
                    eulerY = 0;
                    eulerZ = 0;
                }
                // x, y and z value fo the vectors are multiplexed in a unique array (x0,y0,z0, x1,y1,z1, ..., xN,yN,zN)
                positions[virtualMics[i].id * 3] = virtualMics[i].gameObject.transform.position.x;
                rotations[virtualMics[i].id * 3] = eulerX;
                forwards[virtualMics[i].id * 3] = virtualMics[i].gameObject.transform.forward.x;
                positions[virtualMics[i].id * 3 + 1] = virtualMics[i].gameObject.transform.position.y;
                rotations[virtualMics[i].id * 3 + 1] = eulerY;
                forwards[virtualMics[i].id * 3 + 1] = virtualMics[i].gameObject.transform.forward.y;
                positions[virtualMics[i].id * 3 + 2] = virtualMics[i].gameObject.transform.position.z;
                rotations[virtualMics[i].id * 3 + 2] = eulerZ;
                forwards[virtualMics[i].id * 3 + 2] = virtualMics[i].gameObject.transform.forward.z;

            }
            AT_SPAT_WFS_setVirtualMicPosition(outputChannelCount, 1, positions, rotations, forwards);
        }
    }

    /**
    * @brief Initialise the spatialization engine : set the sampling rate, and create as much 
    * Spatializer than there are At_Player instances in the scene. Each player is given a 
    * unique Saptializer Id.
    */
    private void InitSpatializerEngine()
    {
        AT_SPAT_setSampleRate(samplingRate);
        /* MODIF GONOT - DEBUG PLAYER LIST
         *  The Spatializer has already been created when the At_Player has been added to the list previously
        for (int playerIndex = 0; playerIndex < playerList.Count; playerIndex++)
        {
            int id = -1;            

            AT_SPAT_CreateWfsSpatializer(ref id, playerList[playerIndex].is3D, playerList[playerIndex].isDirective, maxDistanceForDelay);
            playerList[playerIndex].masterOutput = this;
            playerList[playerIndex].spatID = id;
            playerList[playerIndex].outputChannelCount = outputChannelCount;
            if (playerList[playerIndex].is3D)
            {
                playerList[playerIndex].UpdateSpatialParameters();
            }

        }
        */
        UpdateVirtualMicPosition();
    }

    /**
    * @brief Add a new At_Player instance to the list, create a new spatializer for it with a 
    * unique ID
    * 
    * @param[in] p : At_Player instance to add
    */
    public void addPlayerToList(At_Player p)
    {
        if (playerList == null)
            playerList = new List<At_Player>();

        bool isAlreadyInList = false;
        foreach(At_Player at in playerList)
        {
            if(at.guid == p.guid)
            {
                isAlreadyInList = true;
            }
        }

        if (!isAlreadyInList || p.isDynamicInstance)
        {
            int id = playerList.Count - 1;
            playerList.Add(p);
            
            if(AT_SPAT_CreateWfsSpatializer(ref id, playerList[playerList.Count - 1].is3D, playerList[playerList.Count - 1].isDirective, maxDistanceForDelay))
            {
                playerList[playerList.Count - 1].masterOutput = this;
                playerList[playerList.Count - 1].spatID = id;
                playerList[playerList.Count - 1].outputChannelCount = outputChannelCount;

                p.initAudioBuffer();
            }
            else
            {
                Debug.LogError("Trying to create a spatializer without initialize engine \n " +
                    "Call AT_SPAT_WFS_initializeOutput() function first");
            }

            /*
            if (playerList[playerList.Count - 1].is3D)
            {
                playerList[playerList.Count - 1].UpdateSpatialParameters();
            }

            UpdateVirtualMicPosition();
            */
        }

    }

    public void destroyPlayerSafely(At_Player player)
    {
        spatIDToDestroy.Add(player.spatID);
    }

    public void destroyPlayerNow(At_Player player)
    {  
        player.DestroyNow();

    }

    /************************************************************************
     *              DESTROY METHODS
     *              
     *************************************************************************              
     *     /!\ WARNING !!! Destroying is not done in every case !!
     *     THIS SHOULD BE DONE FOR REALEASE VERSION !!!!
     ************************************************************************/

    public void StopEngine() {
        isEngineStarted = false;
        At_AudioEngineUtils.asioOut.Stop();
        At_AudioEngineUtils.asioOut.Dispose();
        At_AudioEngineUtils.asioOut = null;
        running = false;
    }
    // Destroy all spatializer when the At_OutputMixer class is remove from the Game Object
    private void OnDisable(){
        //Debug.Log("OnDisable Output");
        AT_SPAT_WFS_destroyAllSpatializer();
    }
    // Destroy all spatializer when the the user quit the application
    public void OnApplicationQuit(){
        //Debug.Log("OnDisable Output");
        AT_SPAT_WFS_destroyAllSpatializer();

        //HAPTIC_ENGINE_DESTROY_ALL_MIXER();

        if (At_AudioEngineUtils.asioOut != null)
        {
            At_AudioEngineUtils.asioOut.Stop();
            At_AudioEngineUtils.asioOut.Dispose();
            At_AudioEngineUtils.asioOut = null;
            running = false;

        }
        At_AudioEngineUtils.CleanAllStates(SceneManager.GetActiveScene().name);
    }

    private void OnSceneUnloaded(Scene current)
    {
        Debug.Log("OnSceneUnloaded: " + current);
        AT_SPAT_WFS_destroyAllSpatializer();
        if (asioOut != null)
        {
            At_AudioEngineUtils.asioOut.Stop();
            //asioOut.Dispose();
            //asioOut = null;
            running = false;

        }
    }
    /**
    * @brief Set the position, the rotation and the forward vector of the game object 
    * in the the 3D audio engine with the AT_SPAT_WFS_setVirtualMicPosition() function
    * provide by the API.
    * 
    */
    private void Update()
    {
        UpdateVirtualMicPosition();
        AT_SPAT_WFS_setSubwooferCutoff(crossoverFilterFrequency);       
    }


   

    bool playerIsDestroyedOnNextFrame(At_Player player)
    {
        for (int i = 0;i< spatIDToDestroy.Count; i++)
        {
            if (player.spatID == spatIDToDestroy[i])
            {
                return true;
            }
        }
        return false;
    }

    At_Player FindPlayerWithSpatID(int spatID)
    {
        foreach (At_Player p in playerList)
        {
            if (p.spatID == spatID)
            {
                return p;
            }
        }
        return null;

    }
    void RemovePlayerFromListWithSpatID(int spatID)
    {
        for (int i = 0;  i< playerList.Count; i++)
        {
            if (playerList[i].spatID == spatID)
            {
                playerList.RemoveAt(i);
            }
        }
    }

    // Modif Gonot - 21/11/2023 - Adding Haptic Feedback Managment
    At_HapticListenerOutput FindHapticListenerOutputWithChannelIndex(int channelIndex, out int indexChannelOfHapticListenerOutput)
    {
        string objectGuid= "";
        indexChannelOfHapticListenerOutput = -1;

        int offset = 0;
        for (int indexObject = 0; indexObject < hapticListenerOutputChannelsCount.Length; indexObject++)
        {
            int channelCount = hapticListenerOutputChannelsCount[indexObject];
            for (int i = offset; i< offset + channelCount; i++)
            {
                if (hapticListenerChannelsIndex[i] == channelIndex)
                {
                    objectGuid = hapticListenerOutputGuid[indexObject];
                    indexChannelOfHapticListenerOutput = i - offset;
                    break;
                }
            }
            offset += channelCount;
        }
        At_HapticListenerOutput hlo;

        for (int i = 0; i < hapticListenerOutputs.Length; i++)
        {
            if (hapticListenerOutputs[i].guid == objectGuid)
            {
                return hapticListenerOutputs[i];
            }
        }

        return null;
    }
    /**
   * @brief /!\ Callback method called by NAudio to provide the output buffer to the ASIO driver.
   * 
   * @details (1) It asks all the At_PLayer instances to extract a multichannel buffer from the raw data read in the audiofile.
   * (2) It asks all the At_Player instances to conform this "input" bufer to the format of the output bus (processing spatialization, or down/upmix, etc.)
   * (3) It processes metering to display the bargraph in the editor
   * (4) It uses the sample provider to convert the audio samples to the format required by the audio device (Int32LSB, Int16LSB, Int24LSB, Float32LSB).  
   * 
   */
    void OnAsioOutAudioAvailable(object sender, AsioAudioAvailableEventArgs e)
    {       

        bool playerReady = false;
        if (playerList != null && playerList.Count != 0)
        {
            bool result;
            for (int playerIndex = 0; playerIndex < playerList.Count; playerIndex++)
            {
                
                if (!playerIsDestroyedOnNextFrame(playerList[playerIndex]) && playerIndex < playerList.Count && playerList[playerIndex] != null)
                {

                    // tell each player to extract a buffer from their audio file if it is in "play" mode
                    result = playerList[playerIndex].extractInputBuffer(e.SamplesPerBuffer); // C# loop on N Channel x M samples - not avoidable 
                    // conform the input buffer to the output bus format (including spatialization if 3D player)
                    result = playerList[playerIndex].conformInputBufferToOutputBusFormat(e.SamplesPerBuffer); // no C# if 3D


                    if (result == true) playerReady = true;


                }
                
            }
        }

        if (hapticPlayers != null & hapticPlayers.Length != 0)
        {
            
            // TODO : check is Haptic Player should be destroyed has we do for At_Player
            for (int playerIndex = 0; playerIndex < hapticPlayers.Length; playerIndex++)
            {
                hapticPlayers[playerIndex].conformInputBufferToOutputBusFormat(e.SamplesPerBuffer);
            }
        }

        // if the buffers has been filled by the At_player instances 
        if (playerReady && isEngineStarted)
        {

            int numChannel;
            if (isBassManaged) numChannel = outputChannelCount + subwooferOutputChannelCount + hapticListenerChannelsIndex.Length;
            else numChannel = outputChannelCount + hapticListenerChannelsIndex.Length;

            foreach (At_HapticListenerOutput hlo in hapticListenerOutputs)
                hlo.initializeMeterValues();

            for (int channelIndex = 0; channelIndex < numChannel; channelIndex++)
            {
                if (channelIndex < outputChannelCount)
                {
                    meters[channelIndex] = 0;
                    
                }
                else if (channelIndex >= outputChannelCount + hapticListenerChannelsIndex.Length && channelIndex < outputChannelCount + hapticListenerChannelsIndex.Length + subwooferOutputChannelCount)
                {
                    subwooferMeters[channelIndex - (outputChannelCount + hapticListenerChannelsIndex.Length)] = 0;
                }
               

                for (int sampleIndex = 0; sampleIndex < e.SamplesPerBuffer; sampleIndex++)
                {
                    
                    float sample=0;                    
                    
                    if (channelIndex < outputChannelCount)
                    {

                        // Apply gain set in the custom inspector editor of the player
                        float volume = Mathf.Pow(10.0f, gain / 20.0f);
       
                        sample = volume * AT_SPAT_WFS_getMixingBufferSampleForChannelAndZero(sampleIndex, channelIndex, isBassManaged);
                        

                        meters[channelIndex] += Mathf.Pow(sample, 2f);

                    }
                    // Modif Gonot - 21/11/2023 - Adding Haptic Feedback Managment
                    else if (channelIndex >= outputChannelCount && channelIndex < outputChannelCount + hapticListenerChannelsIndex.Length)
                    {
                        int indexChannelOfHapticListenerOutput;
                        At_HapticListenerOutput hlo = FindHapticListenerOutputWithChannelIndex(channelIndex, out indexChannelOfHapticListenerOutput);
                        if (indexChannelOfHapticListenerOutput != -1 && hlo != null)
                        {
                            sample = hlo.getMixingBufferSampleForChannelAndZero(sampleIndex, indexChannelOfHapticListenerOutput);
                        }
                        else
                        {
                            Debug.LogError("can't find Haptic Listener Output for channel " + channelIndex + "\n Check Haptic Channel Routing in MasterOutput Component ! ");

                            sample = 0;
                        }


                    }
                    // Subwoofer Channels
                    else if(channelIndex >= outputChannelCount + hapticListenerChannelsIndex.Length && channelIndex < outputChannelCount + hapticListenerChannelsIndex.Length + subwooferOutputChannelCount)
                    {
                        float subwooferVolume = Mathf.Pow(10.0f, subwooferGain / 20.0f);
                        // channels for subwoofer : 
                        //sample = subwooferVolume * AT_SPAT_WFS_getLowPasMixingBufferForChannel(sampleIndex, indexInputSubwoofer[channelIndex - (outputChannelCount + hapticListenerChannelsIndex.Length)]);
                        sample = subwooferVolume * AT_SPAT_WFS_getSubWooferSample(sampleIndex);
                        subwooferMeters[channelIndex - (outputChannelCount + hapticListenerChannelsIndex.Length)] += Mathf.Pow(sample, 2f);

                    }
                    

                    if (channelIndex < maxDeviceChannel)
                    {
                        IntPtr buffer = e.OutputBuffers[channelIndex];
                        if (e.AsioSampleType == AsioSampleType.Int32LSB)
                            SetOutputSampleInt32LSB(buffer, sampleIndex, sample);
                        else if (e.AsioSampleType == AsioSampleType.Int16LSB)
                            SetOutputSampleInt16LSB(buffer, sampleIndex, sample);
                        else if (e.AsioSampleType == AsioSampleType.Int24LSB)
                            throw new InvalidOperationException("Not supported");
                        else if (e.AsioSampleType == AsioSampleType.Float32LSB)
                            SetOutputSampleFloat32LSB(buffer, sampleIndex, sample);
                        else
                            throw new ArgumentException(@"Unsupported ASIO sample type {sampleType}");
                    }

                }
                

                if (channelIndex < outputChannelCount)
                {
                    meters[channelIndex] = Mathf.Sqrt(meters[channelIndex] / e.SamplesPerBuffer);
                }
                else if (channelIndex >= outputChannelCount + hapticListenerChannelsIndex.Length && channelIndex < outputChannelCount + hapticListenerChannelsIndex.Length + subwooferOutputChannelCount)
                {
                    subwooferMeters[channelIndex - (outputChannelCount + hapticListenerChannelsIndex.Length)] = Mathf.Sqrt(subwooferMeters[channelIndex - (outputChannelCount + hapticListenerChannelsIndex.Length)] / e.SamplesPerBuffer);
                }               
            }

            foreach (At_HapticListenerOutput hlo in hapticListenerOutputs)
                hlo.normalizeMeterValues(e.SamplesPerBuffer);


            e.WrittenToOutputBuffers = true;

        }

        // check for player to destroy
        for (int i = 0;i< spatIDToDestroy.Count;i++)
        {
            
            At_Player p = FindPlayerWithSpatID(spatIDToDestroy[i]);
            playerObjectToDestroy.Add(p);
            AT_SPAT_DestroyWfsSpatializer(spatIDToDestroy[i]);

            RemovePlayerFromListWithSpatID(spatIDToDestroy[i]);

            p.DestroyOnNextFrame();
            spatIDToDestroy.RemoveAt(i);
        }

    }

    private unsafe void SetOutputSampleInt32LSB(IntPtr buffer, int n, float value)
    {
        *((int*)buffer + n) = (int)(value * int.MaxValue);
    }

    private unsafe void SetOutputSampleInt16LSB(IntPtr buffer, int n, float value)
    {
        *((short*)buffer + n) = (short)(value * short.MaxValue);
    }

    private unsafe void SetOutputSampleFloat32LSB(IntPtr buffer, int n, float value)
    {
        *((float*)buffer + n) = value;
    }

    /**
   * @brief Method used to display properties of the At_MasterOutput in the Scene View.    
   */
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        At_VirtualMic[] vms = GameObject.FindObjectsOfType<At_VirtualMic>();
        At_VirtualSpeaker[] vss = GameObject.FindObjectsOfType<At_VirtualSpeaker>();

        if (outputConfigDimension == 1)
        {
            if (vms != null && vms.Length != 0 && vms != null && vms.Length != 0)
            {
                At_VirtualMic currentMic = null;
                At_VirtualSpeaker currentSpk = null;
                for (int micCount = 0; micCount < vms.Length; micCount++)
                {
                    currentMic = micWithIndex(vms, micCount);
                    currentSpk = speakerWithIndex(vss, micCount);
                    currentMic.transform.position = currentSpk.transform.position;
                    currentMic.transform.eulerAngles = currentSpk.transform.eulerAngles;
                    currentMic.transform.Rotate(0f, 180f, 0f);

                }
            }
        }
        
        outputState = At_AudioEngineUtils.getOutputState(SceneManager.GetActiveScene().name);

        if (vms != null && vms.Length != 0 && vms != null && vms.Length != 0)
        {
            At_VirtualMic currentMic = null;
            At_VirtualMic nextMic = null;
            At_VirtualSpeaker currentSpk = null;
            for (int micCount = 0; micCount < vms.Length; micCount++)
            {
                currentMic = micWithIndex(vms, micCount);
                currentSpk = speakerWithIndex(vss, micCount);
                //currentMic.transform.position = currentMic.transform.position.normalized * virtualMicRigSize * 0.5f * currentSpk.transform.position.magnitude / currentSpk.distance ;                 
                //At_SpeakerConfig.updateVirtualMicPosition(currentMic, currentSpk, virtualMicRigSize, virtualMicWidth, speakerRigSize);
                if (currentSpk.gameObject.transform.position.magnitude != 0 && currentSpk.distance != 0)
                {
                    
                    float ratio = currentSpk.distance / currentSpk.gameObject.transform.position.magnitude;
                        
                    currentMic.transform.position = currentMic.transform.parent.transform.position + currentSpk.transform.position.normalized * outputState.virtualMicRigSize / ratio;
                    
                }
            }

            for (int micCount = 0; micCount < vms.Length; micCount++)
            {

                currentMic.gameObject.transform.LookAt(currentSpk.gameObject.transform);
                currentSpk.gameObject.transform.LookAt(currentMic.gameObject.transform);
                currentMic = micWithIndex(vms, micCount);
                nextMic = micWithIndex(vms, (micCount + 1) % vms.Length);
                currentSpk = speakerWithIndex(vss, micCount);
                Gizmos.color = new Color(0, 1, 1, 0.1f);
                Gizmos.DrawLine(currentMic.gameObject.transform.position, currentSpk.gameObject.transform.position);
                Gizmos.color = new Color(0, 1, 1, 1f);
                Gizmos.DrawLine(currentMic.gameObject.transform.position, nextMic.gameObject.transform.position);
            }
        }

        
        /*
        if (At_AudioEngineUtils.setSpeakerState(vms, vss))
            At_AudioEngineUtils.saveSpeakerState();
        */
        //At_AudioEngineUtils.saveVirtualSpeakerState(vms, vss);
    }

    At_VirtualSpeaker speakerWithIndex(At_VirtualSpeaker[] vss, int index)
    {

        if (vss != null)
        {
            foreach (At_VirtualSpeaker vs in vss)
            {
                if (vs.id == index)
                {
                    return vs;
                }
            }
        }
        return null;
    }


    At_VirtualMic micWithIndex(At_VirtualMic[] vms, int index)
    {

        if (vms != null)
        {
            foreach (At_VirtualMic vm in vms)
            {
                if (vm.id == index)
                {
                    return vm;
                }
            }
        }
        return null;
    }
#endif

    public At_VirtualSpeaker speakerWithIndex(int index)
    {

        if (virtualSpeakers != null)
        {
            foreach (At_VirtualSpeaker vs in virtualSpeakers)
            {
                if (vs.id == index)
                {
                    return vs;
                }
            }
        }
        return null;

    }
    public At_VirtualMic micWithIndex(int index)
    {

        if (virtualMics != null)
        {
            foreach (At_VirtualMic vm in virtualMics)
            {
                if (vm.id == index)
                {
                    return vm;
                }
            }
        }
        return null;
    }

    /**
     * Extern declaration of the functions provided by the 3D Audio Engine API (AudioPlugin_AtSpatializer.dll)
     */
    #region DllImport


    //[DllImport("AudioPlugin_AtSpatializer", CallingConvention = CallingConvention.Cdecl)]
    //private static extern void AT_SPAT_WFS_getDemultiplexMixingBuffer(IntPtr demultiplexMixingBuffer, int indexChannel);
    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern void AT_SPAT_WFS_setSubwooferCutoff(float subwooferCutoff);
    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern float AT_SPAT_WFS_getMixingBufferSampleForChannelAndZero(int indexSample, int indexChannel, bool isHighPassFiltered);
    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern float AT_SPAT_WFS_getLowPasMixingBufferForChannel(int indexSample, int indexChannel);
    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern float AT_SPAT_WFS_getSubWooferSample(int indexSample);

    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern void AT_SPAT_WFS_initializeOutput(int sampleRate, int bufferLength, int outChannelCount, int subwooferOutputChannelCount, float subwooferCutoff);

    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern bool AT_SPAT_CreateWfsSpatializer(ref int id, bool is3D, bool isDirective, float maxDistanceForDelay);
    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern void AT_SPAT_DestroyWfsSpatializer(int id);
    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern void AT_SPAT_WFS_setListenerPosition(float[] position, float[] rotation);
    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern void AT_SPAT_WFS_setVirtualMicPosition(int speakerCount, float virtualMicMinDistance, float[] positions, float[] rotations, float[] forwards);
    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern void AT_SPAT_WFS_destroyAllSpatializer();
    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern void AT_SPAT_setSampleRate(float sampleRate);

    // Modif Gonot - 21/11/2023 - Adding Haptic Feedback Managment
    [DllImport("AudioPlugin_AtHaptic")]
    private static extern void HAPTIC_ENGINE_DESTROY_ALL_MIXER();
    #endregion

}

