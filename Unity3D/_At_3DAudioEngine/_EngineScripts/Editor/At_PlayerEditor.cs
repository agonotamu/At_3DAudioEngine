﻿/*
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
    // Called when the GameObject with the At_Player component is selected (Inspector is displayed) or when the component is added
    public void OnEnable()
    {
        previousIsEditor = Application.isEditor;
        previousIsPlaying = Application.isPlaying;

        // get a reference to the At_Player isntance (core engine of the player)
        player = (At_Player)target;
        
        if (!player.isDynamicInstance)
        {
            At_ExternAssets ea = GameObject.FindObjectOfType<At_ExternAssets>();
            externAssetsPath_audio = ea.externAssetsPath_audio;
            externAssetsPath_audio_standalone = ea.externAssetsPath_audio_standalone;

            player.externAssetsPath_audio = externAssetsPath_audio;
            player.externAssetsPath_audio_standalone = externAssetsPath_audio_standalone;


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
            }

            // set all the
            player.fileName = playerState.fileName;
            player.gain = playerState.gain;
            player.is3D = playerState.is3D;
            player.isDirective = playerState.isDirective; //modif mathias 06-17-2021
            player.isLooping = playerState.isLooping;
            player.isPlayingOnAwake = playerState.isPlayingOnAwake;
            player.attenuation = playerState.attenuation;
            player.omniBalance = playerState.omniBalance;
            player.timeReversal = playerState.timeReversal;
            player.minDistance = playerState.minDistance;
            player.numChannelsInAudioFile = playerState.numChannelInAudiofile;


            At_OutputState outputState = At_AudioEngineUtils.getOutputState(SceneManager.GetActiveScene().name);
            if (playerState.fileName != "")
            {
                if (playerState.channelRouting == null || playerState.channelRouting.Length == 0)//|| player.outputChannelCount != outputState.outputChannelCount)
                {
                    //currentOutputChannelCount = outputState.outputChannelCount;
                    int numChannel = player.getNumChannelInAudioFile();
                    playerState.channelRouting = new int[numChannel];
                    for (int i = 0; i < numChannel; i++)
                    {
                        playerState.channelRouting[i] = i;
                    }

                }
            }


            //|| currentOutputChannelCount != outputState.outputChannelCount
            player.outputChannelCount = outputState.outputChannelCount;
            player.channelRouting = playerState.channelRouting;
            //player.state = playerState;
            
        }
        

    }

   
    public void OnDisable()
    {
        if (player !=null && player.name != playerState.name)
        {
            if(player != null)
            {
                //Debug.Log(playerState.name + " as changed to : " + player.name);
                At_AudioEngineUtils.changePlayerName(SceneManager.GetActiveScene().name, playerState.name, player.name);
            }
        }
        if (shouldSave)
        {
            //At_AudioEngineUtils.SavePlayerStateWithName(playerState.name); modif mathias 30-06-202
            if (!player.isDynamicInstance)
            {
                At_AudioEngineUtils.SaveAllState(SceneManager.GetActiveScene().name);
                shouldSave = false;
            }
        }
        At_AudioEngineUtils.CleanAllStates(SceneManager.GetActiveScene().name);

    }

    void OnDestroy()
    {
        
        if (Application.isEditor == previousIsEditor && previousIsPlaying == Application.isPlaying)
        {
            if (player == null)
            {
                Debug.Log("remove Player !");
                At_AudioEngineUtils.removePlayerWithGuid(SceneManager.GetActiveScene().name, player.guid);
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
            /*
            meterOn = EditorGUIUtility.Load("/At_3DAudioEngine/LevelMeter.png") as Texture;
            meterOff = EditorGUIUtility.Load("/At_3DAudioEngine/LevelMeterOff.png") as Texture;
            */
            meterOn = (Texture)AssetDatabase.LoadAssetAtPath("Assets/At_3DAudioEngine/Other/Resources/At_3DAudioEngine/LevelMeter.png", typeof(Texture));
            meterOff = (Texture)AssetDatabase.LoadAssetAtPath("Assets/At_3DAudioEngine/Other/Resources/At_3DAudioEngine/LevelMeterOff.png", typeof(Texture));

        }
    }
   

    //============================================================================================================================
    //                                                       DRAWING
    //============================================================================================================================
    public override void OnInspectorGUI()
    {
        

        if (!player.isDynamicInstance)
        {
            if (player.name != playerState.name)
            {
                //Debug.Log(playerState.name + " as changed to : " + player.name);
                At_AudioEngineUtils.changePlayerName(SceneManager.GetActiveScene().name, playerState.name, player.name);


            }

            //bool shouldSave = false;

            // laod ressources if needed
            AffirmResources();

            //string audioFilePath = "";
            using (new GUILayout.HorizontalScope())
            {
                // Display and test if the Button "Open" has been clicked
                if (GUILayout.Button("Open"))
                {
                    /*
                    string[] filter = { "Audio File", "wav,aiff, mp3, aac, mp4" };
                    // if it is, open the panel for choosing an audio file
                    audioFilePath = EditorUtility.OpenFilePanelWithFilters("Open Audio File", Application.dataPath + "/StreamingAssets/", filter);

                    string s = audioFilePath.Replace(Application.dataPath + "/StreamingAssets/", "");
                    */
                    var extensions = new[] {
                        new ExtensionFilter("Sound Files", "mp3", "wav", "aiff", "aac", "mp4"),
                    };
                    string[] paths;
                    At_ExternAssetsState externAssetsState = At_AudioEngineUtils.getExternalAssetsState();
                    //paths = StandaloneFileBrowser.OpenFilePanel("Open File", externAssetsPath_audio, extensions, false);
                    paths = StandaloneFileBrowser.OpenFilePanel("Open File", externAssetsState.externAssetsPath_audio, extensions, false);
                    //string s = paths[0].Replace(externAssetsPath_audio, "");
                    string s = paths[0].Replace(externAssetsState.externAssetsPath_audio, "");

                    if (s != playerState.fileName)
                    {
                        // Get the filename without complete path
                        playerState.fileName = s;
                        shouldSave = true;
                    }
                    // update the Player State in the list with the file name
                    //At_AudioEngineUtils.getState().addFileNameToPlayerState(gameObjectName, playerState.fileName);
                    // update the Player Engine the file name

                    player.fileName = playerState.fileName;
                    player.initMeters();
                    playerState.numChannelInAudiofile = player.numChannelsInAudioFile;
                    //currentOutputChannelCount = outputState.outputChannelCount;
                    int numChannel = player.getNumChannelInAudioFile();
                    playerState.channelRouting = new int[numChannel];
                    for (int i = 0; i < numChannel; i++)
                    {
                        playerState.channelRouting[i] = i;
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
            //modif mathias 06-17-2021
            /*
            if (playerState.is3D == true)
            {
                using (new GUILayout.HorizontalScope())
                {
                    bool b = GUILayout.Toggle(playerState.isDirective, "Directive");
                    if (b != playerState.isDirective)
                    {
                        shouldSave = true;
                        playerState.isDirective = b;

                    }
                }
            }
            */
            if (playerState.is3D == true)
            {
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
                        //GUILayout.Label("meter");

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

                    if (timeReversal != playerState.omniBalance)
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

            if (playerState.fileName != "")
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
                //GUILayout.Label("");
            }


            if (playerState.is3D == false)
            {
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label("File channel Routing ");
                    if (playerState.channelRouting != null && playerState.channelRouting.Length == player.numChannelsInAudioFile)
                    {
                        string[] channelRouting = new string[player.outputChannelCount];
                        for (int i = 0; i < channelRouting.Length; i++)
                        {
                            channelRouting[i] = i.ToString();
                        }
                        int[] selectedChannelRouting = new int[player.numChannelsInAudioFile];
                        for (int i = 0; i < selectedChannelRouting.Length; i++)
                        {
                            selectedChannelRouting[i] = i;
                        }

                        for (int c = 0; c < player.numChannelsInAudioFile; c++)
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
       





            player.gain = playerState.gain;
            player.is3D = playerState.is3D;
            player.isDirective = playerState.isDirective; //modif mathias 06-17-2021
            player.isPlayingOnAwake = playerState.isPlayingOnAwake;
            player.fileName = playerState.fileName;
            player.isLooping = playerState.isLooping;
            player.attenuation = playerState.attenuation;
            player.omniBalance = playerState.omniBalance;
            player.timeReversal = playerState.timeReversal;
            player.minDistance = playerState.minDistance;
            player.channelRouting = playerState.channelRouting;
            // save the Player State to be always updated !!

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