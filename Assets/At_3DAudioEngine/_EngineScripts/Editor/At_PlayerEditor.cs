/*
 * Author : Antoine Gonot
 * Company : Art-Tone
 * First Creation : 19/01/2021
 * 
 * DESCRIPTION : class used to defined and draw the extended GUI of the PlayerState Inspector
 * - Add a Player State in the list when then At_Player is added 
 * - Update the parameters of an existing Player States in the list when it is changed (path, gain, etc.)
 * - Display the peak level (rms value for buffer size in dB full scale) for each channel of the audiofile
 * -----------------------------------------------------------------------------------------------------------------------
 * DEBUG NOTE: PB REMOVE !!
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using SFB;
using UnityEngine.SceneManagement;

[ExecuteInEditMode]
[CanEditMultipleObjects]
[CustomEditor(typeof(At_Player))]
public class At_PlayerEditor : Editor
{
    // reference to the instance of the At_Player which has been added 
    At_Player player;
    // name of the GameObject the At_Player script is attached to (used as a unique identifier !!!) 
    string gameObjectName;

    // DRAWING
    // ----- texture for metering ------
    private Texture meterOn;
    private Texture meterOff;

    // Class use to save/load the state of the player
    At_PlayerState playerState;

    // A GUIStyle used for drawing lines between main part in th inspector
    GUIStyle horizontalLine;

    string[] attenuationType = { "None", "Lin", "Log" };
    
    bool shouldSave = false;

    int currentOutputChannelCount;

    string externAssetsPath_audio, externAssetsPath_audio_standalone;

    private bool mRunningInEditor;

    bool previousIsEditor;
    bool previousIsPlaying;

    //----------------------------------------------------
    // To be serialize for prefab
    /// name of the audio file to play (Supposed to be in "Assets\Streaming Asset")

    SerializedProperty serialized_fileName;
    /// gain applied to the audiofile
    SerializedProperty serialized_gain;
    /// boolean telling if the player is 2D (no spatialization applied) or 3D (spatialization applied)
    SerializedProperty serialized_is3D;
    /// boolean telling if the player 3D is omnidirectional(mono) or directional (multicanal)
    SerializedProperty serialized_isDirective; //modif mathias 06-17-2021
    /// boolean telling if the player start to play On Awake
    SerializedProperty serialized_isPlayingOnAwake;
    /// boolean telling if the player is looping the read audio file
    SerializedProperty serialized_isLooping;
    /// directivity balance of the virtual microphone used for this source : balance [0,1] between omnidirectionnal and cardiod
    SerializedProperty serialized_omniBalance;
    /// balance between "normal delay" and "reverse delay" for focalised source - see Time Reversal technic used for WFS
    SerializedProperty serialized_timeReversal;
    ///type of distance attenuation in the spatialize : 0 = none, 1 = linera, 2 = square
    SerializedProperty serialized_attenuation;
    /// minimum distance above which the sound produced by the source is attenuated
    SerializedProperty serialized_minDistance;
    
    SerializedProperty serialized_channelRouting;

    SerializedProperty serialized_isUnityAudioSource;

    SerializedProperty serialized_isLookAtListener;

    SerializedProperty serialized_lowPassFc;
    SerializedProperty serialized_lowPassGain;
    SerializedProperty serialized_highPassFc;
    SerializedProperty serialized_highPassGain;

    //----------------------------------------------------

    At_OutputState outputState;


    bool isSceneLoading = false;


    // Called when the GameObject with the At_Player component is selected (Inspector is displayed) or when the component is added
    public void OnEnable()
    {

        SceneManager.sceneLoaded += OnSceneLoaded;

        // get the serialized property (in case the At_Player has been added in a prefab)
        serialized_fileName = serializedObject.FindProperty("fileName");
        serialized_gain = serializedObject.FindProperty("gain");
        serialized_is3D = serializedObject.FindProperty("is3D");
        serialized_isDirective = serializedObject.FindProperty("isDirective");
        serialized_isLooping = serializedObject.FindProperty("isLooping");
        serialized_isPlayingOnAwake = serializedObject.FindProperty("isPlayingOnAwake");
        serialized_attenuation = serializedObject.FindProperty("attenuation");
        serialized_omniBalance = serializedObject.FindProperty("omniBalance");
        serialized_timeReversal = serializedObject.FindProperty("timeReversal");
        serialized_minDistance = serializedObject.FindProperty("minDistance");
        serialized_channelRouting = serializedObject.FindProperty("channelRouting");
        serialized_isUnityAudioSource = serializedObject.FindProperty("isUnityAudioSource");
        serialized_isLookAtListener = serializedObject.FindProperty("isLookAtListener");

        serialized_lowPassFc = serializedObject.FindProperty("lowPassFc");
        serialized_lowPassGain = serializedObject.FindProperty("lowPassGain");
        serialized_highPassFc = serializedObject.FindProperty("highPassFc");
        serialized_highPassGain = serializedObject.FindProperty("highPassGain");


        previousIsEditor = Application.isEditor;
        previousIsPlaying = Application.isPlaying;

        // get a reference to the At_Player isntance (core engine of the player)
        player = (At_Player)target;

        if (!player.isDynamicInstance)
        {

            horizontalLine = new GUIStyle();
            horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
            horizontalLine.margin = new RectOffset(0, 0, 4, 4);
            horizontalLine.fixedHeight = 1;

            // get the name of the GameObject 
            gameObjectName = player.gameObject.name;

            // Get the state of the player
            // - If the GameObject name is found just return the instance in the At_PlayerState List
            // - If the GameObject name is not found, it try loud read a json file for this name.
            //      - if the file is found, it's read, an instance is created and added to the list and is returned
            //      - if the file is not found, create a new "empty" At_PlayerState"
            playerState = At_AudioEngineUtils.getPlayerStateWithGuidAndName(SceneManager.GetActiveScene().name, player.guid, gameObjectName);

            if (playerState == null)
            {               
                playerState = At_AudioEngineUtils.createNewPlayerStateWithGuidAndName(SceneManager.GetActiveScene().name, player.guid, gameObjectName);
                playerState.fileName = serialized_fileName.stringValue;
                playerState.gain = serialized_gain.floatValue;
                playerState.is3D = serialized_is3D.boolValue;
                playerState.isDirective = serialized_isDirective.boolValue;
                playerState.isLooping = serialized_isLooping.boolValue;
                playerState.isPlayingOnAwake = serialized_isPlayingOnAwake.boolValue;
                playerState.attenuation = serialized_attenuation.floatValue;
                playerState.omniBalance = serialized_omniBalance.floatValue;
                playerState.timeReversal = serialized_timeReversal.floatValue;
                playerState.minDistance = serialized_minDistance.floatValue;
                playerState.isUnityAudioSource = serialized_isUnityAudioSource.boolValue;
                playerState.isLookAtListener = serialized_isLookAtListener.boolValue;

                playerState.lowPassFc = serialized_lowPassFc.floatValue;
                playerState.lowPassGain = serialized_lowPassGain.floatValue;
                playerState.highPassFc = serialized_highPassFc.floatValue;
                playerState.lowPassGain = serialized_lowPassGain.floatValue;

                playerState.channelRouting = new int[serialized_channelRouting.arraySize];
                

                for (int i = 0; i < serialized_channelRouting.arraySize; i++)
                {
                    SerializedProperty property = serialized_channelRouting.GetArrayElementAtIndex(i);
                    playerState.channelRouting[i] = property.intValue; // seems to be impossible
                    
                }
                
            }

            // set all the parameter of the At_Player Component from the loaded playerState (or the newly created one)
            player.fileName = playerState.fileName;
            player.gain = playerState.gain;
            player.is3D = playerState.is3D;
            player.isDirective = playerState.isDirective;
            player.isLooping = playerState.isLooping;
            player.isPlayingOnAwake = playerState.isPlayingOnAwake;
            player.attenuation = playerState.attenuation;
            player.omniBalance = playerState.omniBalance;
            player.timeReversal = playerState.timeReversal;
            player.minDistance = playerState.minDistance;
            player.isUnityAudioSource = playerState.isUnityAudioSource;
            player.isLookAtListener = playerState.isLookAtListener;

            player.lowPassFc = playerState.lowPassFc;
            player.lowPassGain = playerState.lowPassGain;
            player.highPassFc = playerState.highPassFc;
            player.lowPassGain = playerState.lowPassGain;

            // init the bargraphs for displaying meters
            if (!Application.isPlaying)
            {
                player.initMeters();
            }           

            // init channel routing data for if the player is 2D
            outputState = At_AudioEngineUtils.getOutputState(SceneManager.GetActiveScene().name);
            if (playerState.fileName != "")
            {
                if (playerState.channelRouting == null || playerState.channelRouting.Length == 0)
                {

                    currentOutputChannelCount = outputState.outputChannelCount;
                    int numChannel = player.getNumChannelInAudioFile();
                    playerState.channelRouting = new int[currentOutputChannelCount];
                    for (int i = 0; i < currentOutputChannelCount; i++)
                    {
                        playerState.channelRouting[i] = i%numChannel;
                    }

                }
            }

            player.outputChannelCount = outputState.outputChannelCount;
            player.channelRouting = playerState.channelRouting;
            
        }

        if (outputState.outputChannelCount != playerState.channelRouting.Length)
        {
            currentOutputChannelCount = outputState.outputChannelCount;
            int numChannel = player.getNumChannelInAudioFile();
            playerState.channelRouting = new int[currentOutputChannelCount];
            if (numChannel > 0)
            {
                for (int i = 0; i < currentOutputChannelCount; i++)
                {
                    playerState.channelRouting[i] = i % numChannel;
                }

            }
        }
        At_AudioEngineUtils.SaveAllState(SceneManager.GetActiveScene().name);
    }

   
    public void OnDisable()
    {
        if (player !=null && player.name != playerState.name)
        {
            if(player != null)
            {
                At_AudioEngineUtils.changePlayerName(SceneManager.GetActiveScene().name, playerState.name, player.name);
            }
        }
        // SAVE ALL THE PROPERTIES OF THE PLAYER WHEN THE GUI IS DISABLED (i.e. the GameObject is not in focus anymore : quit, play, another object selected...)
        if (shouldSave)
        {            
            if (!player.isDynamicInstance)
            {
                At_AudioEngineUtils.SaveAllState(SceneManager.GetActiveScene().name);
                shouldSave = false;
            }
        }
        At_AudioEngineUtils.CleanAllStates(SceneManager.GetActiveScene().name);

    }
    

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isSceneLoading = true;
    }

    void OnDestroy()
    {
        
        if (Application.isEditor == previousIsEditor && previousIsPlaying == Application.isPlaying)
        {
            if (player == null)
            {
                if (!isSceneLoading)
                {
                    At_AudioEngineUtils.removePlayerWithGuid(SceneManager.GetActiveScene().name, player.guid);
                }
                else
                {
                    isSceneLoading = false;
                }
            }
            previousIsEditor = Application.isEditor;
            previousIsPlaying = Application.isPlaying;
        }

    }

    
    // load ressources for the GUI (textures, etc.)
    private void AffirmResources()
    {
        if (meterOn == null)
        {
            meterOn = (Texture)AssetDatabase.LoadAssetAtPath("Assets/At_3DAudioEngine/Other/Resources/At_3DAudioEngine/LevelMeter.png", typeof(Texture));
            meterOff = (Texture)AssetDatabase.LoadAssetAtPath("Assets/At_3DAudioEngine/Other/Resources/At_3DAudioEngine/LevelMeterOff.png", typeof(Texture));

        }
    }
   

    //============================================================================================================================
    //                                                    GUI  DRAWING
    //============================================================================================================================
    public override void OnInspectorGUI()
    {
       
        using (new GUILayout.HorizontalScope())
        {
            bool b = GUILayout.Toggle(playerState.isUnityAudioSource, "Unity 3D Audio Source");
            if (b != playerState.isUnityAudioSource)
            {
                shouldSave = true;
                playerState.isUnityAudioSource = b;
                playerState.is3D = true;
                playerState.gain = 0;
                playerState.numChannelInAudiofile = 1;
            }
           
        }
        
        if (!player.isDynamicInstance)
        {
            if (!playerState.isUnityAudioSource)
            {
                if (player.name != playerState.name)
                {
                    
                    At_AudioEngineUtils.changePlayerName(SceneManager.GetActiveScene().name, playerState.name, player.name);


                }

                AffirmResources();
          
                using (new GUILayout.HorizontalScope())
                {
                    // Display and test if the Button "Open" has been clicked
                    if (GUILayout.Button("Open"))
                    {
                        
                        var extensions = new[] {
                        new ExtensionFilter("Sound Files", "mp3", "wav", "aiff", "aac", "mp4"),
                    };
                        string[] paths;                        
                        
                        paths = StandaloneFileBrowser.OpenFilePanel("Open File", At_AudioEngineUtils.GetFilePathForStates(""), extensions, false);
                        
                        string s = "";
                        if (paths.Length != 0)
                        {
                            string rootPath = At_AudioEngineUtils.GetFilePathForStates("");
                            s = paths[0].Replace("\\", "/");
                            s = s.Replace(rootPath, "");
                            
                        }


                        if (s != playerState.fileName)
                        {
                            // Get the filename without complete path
                            playerState.fileName = s;
                            shouldSave = true;
                        }
                        
                        // update the Player Engine the file name
                        player.fileName = playerState.fileName;
                        player.initMeters();
                        playerState.numChannelInAudiofile = player.numChannelsInAudioFile;
                       
                        int numChannel = player.getNumChannelInAudioFile();
                        
                        currentOutputChannelCount = outputState.outputChannelCount;
                        playerState.channelRouting = new int[currentOutputChannelCount];
                        if (numChannel > 0)
                        {
                            for (int i = 0; i < currentOutputChannelCount; i++)
                            {
                                playerState.channelRouting[i] = i % numChannel;
                            }

                        }
                        GUIUtility.ExitGUI();
                    }
                }
                using (new GUILayout.HorizontalScope())
                {
                    if (playerState.fileName != "")
                    {
                        GUILayout.TextArea(player.fileName);
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    bool b = GUILayout.Toggle(playerState.is3D, "3D");
                    if (b != playerState.is3D)
                    {
                        shouldSave = true;
                        playerState.is3D = b;

                    }

                    b = GUILayout.Toggle(playerState.isPlayingOnAwake, "Play On Awake");
                    if (b != playerState.isPlayingOnAwake)
                    {
                        shouldSave = true;
                        playerState.isPlayingOnAwake = b;

                    }
                    b = GUILayout.Toggle(playerState.isLooping, "Loop");
                    if (b != playerState.isLooping)
                    {
                        shouldSave = true;
                        playerState.isLooping = b;

                    }
                }
                
            }

            if (playerState.is3D == true)
            {
                HorizontalLine(Color.grey);
                using (new GUILayout.HorizontalScope())
                {
                    bool b = GUILayout.Toggle(playerState.isLookAtListener, "Always look at listener");
                    if (b != playerState.isLookAtListener)
                    {
                        shouldSave = true;
                        playerState.isLookAtListener = b;

                    }
                }
                HorizontalLine(Color.grey);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Distance attenuation");
                    int selected = EditorGUILayout.Popup(playerState.selectedAttenuation, attenuationType);

                    if (selected != playerState.selectedAttenuation)
                    {
                        shouldSave = true;
                        if (selected == 0) playerState.attenuation = 0f;
                        if (selected == 1) playerState.attenuation = 1f;
                        if (selected == 2) playerState.attenuation = 2f;

                        playerState.selectedAttenuation = selected;
                    }
                }
                
                HorizontalLine(Color.grey);
                /*
                using (new GUILayout.VerticalScope())
                {
                    using (new GUILayout.HorizontalScope())
                    {

                        GUILayout.Label("Omni");

                        float bal = GUILayout.HorizontalSlider(playerState.omniBalance, 1f, 0);

                        if (bal != playerState.omniBalance)
                        {
                            playerState.omniBalance = bal;
                            shouldSave = true;
                        }
                        GUILayout.Label("Cardioid");

                    }


                    GUILayout.TextField((playerState.omniBalance).ToString("0.00"));

                    HorizontalLine(Color.grey);
                }
                */
                using (new GUILayout.VerticalScope())
                {
                    using (new GUILayout.HorizontalScope())
                    {

                        GUILayout.Label("Min Distance");

                        float dist = GUILayout.HorizontalSlider(playerState.minDistance, 0f, 20);

                        if (dist != playerState.minDistance)
                        {
                            playerState.minDistance = dist;
                            shouldSave = true;
                            
                            SceneView.RepaintAll();
                        }
                        

                    }


                    GUILayout.TextField((playerState.minDistance).ToString("0.00"));

                    HorizontalLine(Color.grey);
                }
            }

            
            /*
            using (new GUILayout.VerticalScope())
            {

                using (new GUILayout.HorizontalScope())
                {

                    GUILayout.Label("Time Reversal");

                    float timeReversal = GUILayout.HorizontalSlider(playerState.timeReversal, 1f, 0);

                    if (timeReversal != playerState.timeReversal)
                    {
                        playerState.timeReversal = timeReversal;
                        shouldSave = true;
                    }
                    GUILayout.Label("None");

                }


                GUILayout.TextField((playerState.timeReversal).ToString("0.00"));

                HorizontalLine(Color.grey);
            }
            */

            if (playerState.fileName != "" && playerState.isUnityAudioSource != true)
            {
                using (new GUILayout.HorizontalScope())
                {
                    // Display metering of each channel of the 
                    float[] XYstart = new float[2];
                    if (player.meters != null)
                    {
                        XYstart = DisplayMetering(player.meters, player.isPlaying);


                        float sliederHeight = 86;
                        float g = GUI.VerticalSlider(new Rect(XYstart[0] - 20, XYstart[1], 40, sliederHeight), playerState.gain, 10f, -80f);


                        if (g != playerState.gain)
                        {
                            playerState.gain = g;
                            shouldSave = true;
                        }

                        GUI.Label(new Rect(XYstart[0] - 30, XYstart[1]+sliederHeight/2f + 10, 80, sliederHeight), ((int)playerState.gain).ToString() + " dB");
                    }
                }
                GUILayout.Label("");
                HorizontalLine(Color.grey);
                
            }

            HorizontalLine(Color.grey);


            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Low Pass Filter");

                float dist = GUILayout.HorizontalSlider(playerState.lowPassFc, 20f, 20000f);

                if (dist != playerState.lowPassFc)
                {
                    playerState.lowPassFc = dist;
                    shouldSave = true;

                    //SceneView.RepaintAll();
                }
            }

            GUILayout.TextField((playerState.lowPassFc).ToString("0.00"));

            HorizontalLine(Color.grey);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("High Pass Filter");
                float dist = GUILayout.HorizontalSlider(playerState.highPassFc, 20f, 20000f);

                if (dist != playerState.highPassFc)
                {
                    playerState.highPassFc = dist;
                    shouldSave = true;

                    //SceneView.RepaintAll();
                }
            }

            GUILayout.TextField((playerState.highPassFc).ToString("0.00"));

            if (playerState.is3D == false)
            {
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label("File channel Routing ");
                    
                    int numChannel = player.getNumChannelInAudioFile();
                    currentOutputChannelCount = outputState.outputChannelCount;
                    if (playerState.channelRouting != null && playerState.channelRouting.Length == currentOutputChannelCount)
                    {
                       
                        string[] channelRouting = new string[numChannel+1];
                        for (int i = 0; i < numChannel; i++)
                        {
                            channelRouting[i] = i.ToString();
                        }
                        channelRouting[numChannel] = "none";
                        int[] selectedChannelRouting = new int[currentOutputChannelCount];
                        for (int i = 0; i < selectedChannelRouting.Length; i++)
                        {
                            selectedChannelRouting[i] = i;
                        }

                        using (new GUILayout.HorizontalScope())
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label("-Output-");
                            }
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label("-Input-");
                            }

                        }
                        for (int c = 0; c < currentOutputChannelCount; c++)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label("channel " + c);
                                int select = EditorGUILayout.Popup(playerState.channelRouting[c], channelRouting);

                                if (select != playerState.channelRouting[c])
                                {
                                    playerState.channelRouting[c] = select;
                                    shouldSave = true;
                                }

                            }

                        }
                    }
                }
            }


            // update the parameters in the At_Player component
            player.gain = playerState.gain;
            player.is3D = playerState.is3D;
            player.isDirective = playerState.isDirective; 
            player.isPlayingOnAwake = playerState.isPlayingOnAwake;
            player.fileName = playerState.fileName;
            player.isLooping = playerState.isLooping;
            player.attenuation = playerState.attenuation;
            player.omniBalance = playerState.omniBalance;
            player.timeReversal = playerState.timeReversal;
            player.minDistance = playerState.minDistance;
            player.channelRouting = playerState.channelRouting;
            player.isUnityAudioSource = playerState.isUnityAudioSource;
            player.isLookAtListener = playerState.isLookAtListener;

            player.lowPassFc = playerState.lowPassFc;
            player.lowPassGain = playerState.lowPassGain;
            player.highPassFc = playerState.highPassFc;
            player.lowPassGain = playerState.lowPassGain;

            // serialize the parameters for use in Prefab
            serialized_gain.floatValue = playerState.gain;
            serialized_is3D.boolValue = playerState.is3D;
            serialized_isDirective.boolValue = playerState.isDirective; 
            serialized_isPlayingOnAwake.boolValue = playerState.isPlayingOnAwake;
            serialized_fileName.stringValue = playerState.fileName;
            serialized_isLooping.boolValue = playerState.isLooping;
            serialized_attenuation.floatValue = playerState.attenuation;
            serialized_omniBalance.floatValue = playerState.omniBalance;
            serialized_timeReversal.floatValue = playerState.timeReversal;
            serialized_minDistance.floatValue = playerState.minDistance;
            serialized_isUnityAudioSource.boolValue = playerState.isUnityAudioSource;
            serialized_isLookAtListener.boolValue = playerState.isLookAtListener;

            serialized_lowPassFc.floatValue = playerState.lowPassFc;
            serialized_lowPassGain.floatValue = playerState.lowPassGain;
            serialized_highPassFc.floatValue = playerState.highPassFc;
            serialized_lowPassGain.floatValue = playerState.lowPassGain;

            serialized_channelRouting.ClearArray();
            for (int i = 0; i < playerState.channelRouting.Length; i++)
            {
                serialized_channelRouting.InsertArrayElementAtIndex(i);
                SerializedProperty property = serialized_channelRouting.GetArrayElementAtIndex(i);
                property.intValue = playerState.channelRouting[i];
            }

            // save the Player State to be always updated !!
            serializedObject.ApplyModifiedProperties();
        }

    }

    // ------------------------------------- DRAWING UTILITY --------------------------------------------
    float[] DisplayMetering(float[] metering, bool isPlaying)
    {
        float[] XY_start = new float[2];

        const int MeterCountMaximum = 48;
        int meterHeight = 86;
        int meterWidth = (int)((128 / (float)meterOff.height) * meterOff.width);
            
        int minimumWidth = meterWidth * MeterCountMaximum;

        Rect fullRect = GUILayoutUtility.GetRect(minimumWidth/4.0f, meterHeight,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        float baseX = fullRect.x + (fullRect.width - (meterWidth * metering.Length)) / 2;

        XY_start[0] = baseX;
        XY_start[1] = fullRect.y;
        for (int i = 0; i < player.numChannelsInAudioFile; i++)
        {
            Rect meterRect = new Rect(baseX + meterWidth * i, fullRect.y, meterWidth, fullRect.height);

            GUI.DrawTexture(meterRect, meterOff);

            float db = 20.0f * Mathf.Log10(metering[i] * Mathf.Sqrt(2.0f));
            db = Mathf.Clamp(db, -80.0f, 10.0f);
            float visible = 0;
            int[] segmentPixels = new int[] { 0, 18, 38, 60, 89, 130, 187, 244, 300 };
            float[] segmentDB = new float[] { -80.0f, -60.0f, -50.0f, -40.0f, -30.0f, -20.0f, -10.0f, 0, 10.0f };
            int segment = 1;
            while (segmentDB[segment] < db)
            {
                segment++;
            }
            visible = segmentPixels[segment - 1] + ((db - segmentDB[segment - 1]) / (segmentDB[segment] - segmentDB[segment - 1])) * (segmentPixels[segment] - segmentPixels[segment - 1]);

            visible *= fullRect.height / (float)meterOff.height;

            Rect levelPosRect = new Rect(meterRect.x, fullRect.height - visible + meterRect.y, meterWidth, visible);
            Rect levelUVRect = new Rect(0, 0, 1.0f, visible / fullRect.height);
            if (isPlaying)
                GUI.DrawTextureWithTexCoords(levelPosRect, meterOn, levelUVRect);
        }

        return XY_start;


    }
    // utility method
    void HorizontalLine(Color color)
    {
        var c = GUI.color;
        GUI.color = color;
        GUILayout.Box(GUIContent.none, horizontalLine);
        GUI.color = c;
    }


}