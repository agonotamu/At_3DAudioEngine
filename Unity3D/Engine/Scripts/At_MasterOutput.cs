
/**
 * 
 * @file At_MasterOutput.cs
 * @author Antoine Gonot
 * @version 1.0
 * @date 19/01/2021
 * 
 * @brief  Output the mixed buffer from At_Mixer to the physical outputs of the selected ASIO device thanks to the NAudio API
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
using NAudio.Wave.SampleProviders;
using NAudioAsioPatchBay;
using System.Runtime.InteropServices;


public class At_MasterOutput : MonoBehaviour
{

    /// constants used for array initialization 
    const int MAX_BUF_SIZE = 2048;    
    /// temporary mono buffer used for processing
    private float[] tmpMonoBuffer;

    /// main NAudio class used to manage ASIO output
    private AsioOut asioOut;
    /// Naudio Class heriting from ISampleProvider used to converted the 32 bit floatting point 
    /// format to the format requiered by the audio device
    private AsioInputPatcher inputPatcher;

   
    /// Reference to an instance of the At_Mixer class
    At_Mixer mixer;
    /// List of references to the instances of At_Player classes
    public List<At_Player> playerList;
    /// Array of references to the instances of At_VirtualMic classes
    public At_VirtualMic[] virtualMics;

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
    public string audioDeviceName;
    /// number of channel used for the output bus
    public int outputChannelCount;
    /// index of the selected speaker configuration in the popup menu of the At_MasterOutput Component GUI
    public int outputConfigDimension;
    /// master gain for the output bus
    public float gain;
    /// audio sampling rate (44.1kHz by default)
    public int samplingRate;
    /// boolean telling if the ASIO output should run when starting the application
    public bool isStartingEngineOnAwake;
    /// size of the virtual mic rig in the scene (1 unit = 1 meter)
    public float virtualMicRigSize;

    At_OutputState outputState;
    //-----------------------------------------------------------------
    // data used at runtime
    // ----------------------------------------------------------------    
    /// boolean telling if the ASIO outpout is running (i.e. the callback method is called)
    public bool running;
    /// boolean telling if the code of the callback method is executed - the output ASIO buffer 
    /// is feed with the samples from the At_Mixer instance.
    public bool isEngineStarted = false;
    /// Number of physical output of the audiodevice 
    int maxDeviceChannel;
    /// output channel routing for a given audio device : 
    /// --- here is Behringer UMC1820
    int[] UMC_outputRouting = { 2, 3, 4, 5, 6, 7, 8, 9,
        12, 13, 14, 15, 16, 17, 18, 19 };
    /// rms value of the signal from each channel, used to display bargraph
    public float[] meters;


    // Start is called before the first frame update
    void Awake()
    {
        At_AudioEngineUtils.LoadAllStates();

        spatIDToDestroy = new List<int>();
        playerObjectToDestroy = new List<At_Player>();

        outputState = At_AudioEngineUtils.getOutputState();

        // get the reference of the At_Mixer instance
        mixer = gameObject.GetComponent<At_Mixer>();

        // get the reference of all the  At_Player instances and add it to the player list
        playerList = new List<At_Player>();
        At_Player[] playerArray = GameObject.FindObjectsOfType<At_Player>();
        foreach (At_Player p in playerArray){
            playerList.Add(p);
        }

        // initialize the temp buffer
        tmpMonoBuffer = new float[MAX_BUF_SIZE];
       
        // Initialize the spatializer and the ASIO output if "is starting engine on awake"
        if (isStartingEngineOnAwake) {
            StartEngine();
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
                running = true;
                asioOut = new AsioOut((string)device);
                // Get the number of inputs in the device
                int inputChannels = asioOut.DriverInputChannelCount;
                // Get the number of outputs in the device
                maxDeviceChannel = asioOut.DriverOutputChannelCount;
                // Initialize a Patcher with the correct sample rate and number of intput and outputs 
                inputPatcher = new AsioInputPatcher(samplingRate, inputChannels, maxDeviceChannel);
                // Initialize Record and Playback for the device
                asioOut.InitRecordAndPlayback(new SampleToWaveProvider(inputPatcher), inputChannels, 44100);
                // Add a callback method to proccess the sample in the in/out buffer
                asioOut.AudioAvailable += OnAsioOutAudioAvailable;
                // Start processing (i.e. calling the callback method)
                asioOut.Play();
            }
        }


    }

    private void UpdateVirtualMicPosition()
    {
        if (meters != null)
        {
            virtualMics = GameObject.FindObjectsOfType<At_VirtualMic>();
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
    * unique Saptializer Id. Pass the list of player to the At_Mixer instance.
    */
    private void InitSpatializerEngine()
    {
        AT_SPAT_setSampleRate(samplingRate);
        for (int playerIndex = 0; playerIndex < playerList.Count; playerIndex++)
        {
            int id = -1;
            AT_SPAT_CreateWfsSpatializer(ref id, playerList[playerIndex].is3D, playerList[playerIndex].isDirective); //modif mathias 06-17-2021
            playerList[playerIndex].masterOutput = this;
            playerList[playerIndex].spatID = id;
            playerList[playerIndex].outputChannelCount = outputChannelCount;
            if (playerList[playerIndex].is3D)
            {
                playerList[playerIndex].UpdateSpatialParameters();
            }
            //Debug.Log("spat created with id : " + id);

        }
        UpdateVirtualMicPosition();

        mixer.setPlayerList(playerList);
    }

    /**
    * @brief Add a new At_Player instance to the list, create a new spatializer for it with a 
    * unique ID and pass the list of player to the At_Mixer instance. 
    * 
    * @param[in] p : At_Player instance to add
    */
    public void addPlayerToList(At_Player p)
    {
        int id = playerList.Count - 1;
        playerList.Add(p);
        AT_SPAT_CreateWfsSpatializer(ref id, playerList[playerList.Count - 1].is3D, playerList[playerList.Count - 1].isDirective); //modif mathias 06-17-2021
        playerList[playerList.Count - 1].masterOutput = this;
        playerList[playerList.Count - 1].spatID = id;
        playerList[playerList.Count - 1].outputChannelCount = outputChannelCount;
        //Debug.Log("spat created with id : " + id);
        mixer.setPlayerList(playerList);
        if (playerList[playerList.Count - 1].is3D)
        {
            playerList[playerList.Count - 1].UpdateSpatialParameters();
        }

        UpdateVirtualMicPosition();

    }



    public void destroyPlayerSafely(At_Player player)
    {
        spatIDToDestroy.Add(player.spatID);
    }

    public void destroyPlayerNow(At_Player player)
    {
       
        //AT_SPAT_DestroyWfsSpatializer(player.spatID);
        player.DestroyNow();
        /*
        for (int i = 0; i < spatIDToDestroy.Count; i++)
        {
            if (spatIDToDestroy[i] == player.spatID)
            {
                spatIDToDestroy.RemoveAt(i);
            }
        } 
        */

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
        asioOut.Stop();
        asioOut.Dispose();
        asioOut = null;
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
        if (asioOut != null)
        {
            asioOut.Stop();
            asioOut.Dispose();
            asioOut = null;
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


    }


    /**
    * @brief /!\ Callback method called by NAudio to provide the output buffer to the ASIO driver.
    * 
    * @details (1) It asks all the At_PLayer instances to extract a multichannel buffer from the raw data read in the audiofile.
    * (2) It asks all the At_Player instances to conform this "input" bufer to the format of the output bus (processing spatialization, or down/upmix, etc.)
    * (3) It asks the At_Mixer instance to sum for one channel the processed buffers of each At_Player instance and to copy the result in its temporaty mono buffer.
    * (4) It processes metering to display the bargraph in the editor
    * (5) It uses the sample provider to convert the audio samples to the format required by the audio device (Int32LSB, Int16LSB, Int24LSB, Float32LSB).  
    * 
    */

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

    void OnAsioOutAudioAvailable(object sender, AsioAudioAvailableEventArgs e)
    {

        inputPatcher.ClearOutbuffer();

        bool playerReady = false;
        if (playerList != null && playerList.Count != 0)
        {
            bool result;
            for (int playerIndex = 0; playerIndex < playerList.Count; playerIndex++)
            {
                
                if (!playerIsDestroyedOnNextFrame(playerList[playerIndex]) && playerIndex < playerList.Count && playerList[playerIndex] != null)
                {
                    //Debug.Log("size =" + playerList.Count);
                    //Debug.Log("index =" +playerIndex);
                    // tell each player to extract a buffer from their audio file if it is in "play" mode
                    result = playerList[playerIndex].extractInputBuffer(e.SamplesPerBuffer);
                    // conform the input buffer to the output bus format (including spatialization if 3D player)
                    result = playerList[playerIndex].conformInputBufferToOutputBusFormat(e.SamplesPerBuffer);

                    if (result == true) playerReady = true;


                }
                
            }
        }

        // if the buffers has been filled by the At_player instances 
        if (playerReady && isEngineStarted)
        {

            
            for (int masterChannel = 0; masterChannel < outputChannelCount; masterChannel++)
            {
                // ask the At_Mixer instance to sum the samples of a buffer for a single channel. 
                // The result is copied in the tmpMonoBuffer array
                mixer.fillMasterChannelInput(ref tmpMonoBuffer, e.SamplesPerBuffer, masterChannel, spatIDToDestroy);

                if (meters != null && meters.Length != 0)
                {
                    meters[masterChannel] = 0;
                    for (int sampleCount = 0; sampleCount < e.SamplesPerBuffer; sampleCount++)
                    {
                        // Apply gain set in the custom inspector editor of the player
                        float volume = Mathf.Pow(10.0f, gain / 20.0f);
                        tmpMonoBuffer[sampleCount] *= volume;
                        // set the value of the rms value for displaying meters
                        meters[masterChannel] += Mathf.Pow(tmpMonoBuffer[sampleCount], 2f);
                    }

                    meters[masterChannel] = Mathf.Sqrt(meters[masterChannel] / e.SamplesPerBuffer);

                }
                // call the Sample Provider methtod to convert the sample format to the format requiered 
                // and output the converted samples to the output buffer of the audio device. 
                
                inputPatcher.ProcessBuffer(tmpMonoBuffer, e.OutputBuffers, e.SamplesPerBuffer, e.AsioSampleType, UMC_outputRouting[masterChannel], maxDeviceChannel);
                System.Array.Clear(tmpMonoBuffer, 0, tmpMonoBuffer.Length);
            }

            e.WrittenToOutputBuffers = true;

        }

        for (int i = 0;i< spatIDToDestroy.Count;i++)
        {
            
            At_Player p = FindPlayerWithSpatID(spatIDToDestroy[i]);
            playerObjectToDestroy.Add(p);
            AT_SPAT_DestroyWfsSpatializer(spatIDToDestroy[i]);
            //playerList.RemoveAt(indexPlayerToDestroy[i]);
            RemovePlayerFromListWithSpatID(spatIDToDestroy[i]);
            mixer.setPlayerList(playerList);
            //indexPlayerToDestroy.RemoveAt(i);
            p.DestroyOnNextFrame();
            spatIDToDestroy.RemoveAt(i);
        }
        //spatToDestroy.Clear();
        //spatIDtoDestroy.Add(spatID);

        //
    }


    /**
   * @brief Method used to display properties of the At_MasterOutput in the Scene View.    
   */

    private void OnDrawGizmos()
    {
        At_VirtualMic[] vms = GameObject.FindObjectsOfType<At_VirtualMic>();
        At_VirtualSpeaker[] vss = GameObject.FindObjectsOfType<At_VirtualSpeaker>();
        
        if (outputConfigDimension == 2)
        {
            outputState = At_AudioEngineUtils.getOutputState();

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
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(currentMic.gameObject.transform.position, currentSpk.gameObject.transform.position);
                    Gizmos.DrawLine(currentMic.gameObject.transform.position, nextMic.gameObject.transform.position);
                }
            }

        }
        
        if (At_AudioEngineUtils.setSpeakerState(vms, vss))
            At_AudioEngineUtils.saveSpeakerState();

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


    /**
     * Extern declaration of the functions provided by the 3D Audio Engine API (AudioPlugin_AtSpatializer.dll)
     */
    #region DllImport
    [DllImport("AudioPlugin_AtSpatializer")]
    private static extern void AT_SPAT_CreateWfsSpatializer(ref int id, bool is3D, bool isDirective);
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
    #endregion

}

